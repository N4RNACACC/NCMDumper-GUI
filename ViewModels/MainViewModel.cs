using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NCMDumperGUI.Tools;
using SukiUI.Toasts;

namespace NCMDumperGUI.ViewModels;

public enum DumpStatus
{
    Pending,
    Running,
    Success,
    Failed
}

public partial class DumpItem : ViewModelBase
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }

    [ObservableProperty] public partial DumpStatus Status { get; set; } = DumpStatus.Pending;
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
}

public partial class MainViewModel : ViewModelBase
{
    // Interfaces

    #region Interfaces

    public ISukiToastManager ToastManager { get; } = new SukiToastManager();

    #endregion

    // Properties

    #region Properties

    [ObservableProperty] public partial int PickedCount { get; set; }

    [ObservableProperty] public partial int DumpedCount { get; set; }

    [ObservableProperty] public partial int FailedCount { get; set; }

    [ObservableProperty] public partial bool IsAboutVisible { get; set; } = false;

    [ObservableProperty] public partial bool IsDumping { get; set; } = false;

    public ObservableCollection<DumpItem> Files { get; } = [];

    public string ToggleButtonText => IsAboutVisible ? "Back" : "About";

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnIsAboutVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ToggleButtonText));
    }

    #endregion

    // Commands

    #region Commands

    [RelayCommand]
    private void TogglePage()
    {
        IsAboutVisible = !IsAboutVisible;
    }

    [RelayCommand]
    private async Task DumpAsync()
    {
        Debug.WriteLine("Command method DumpAsync() Triggered");
        IsDumping = true;
        if (Files.Count == 0)
        {
            Toast("Dumper", "Not pick any .ncm files !", NotificationType.Warning);
            return;
        }

        // 初始化状态
        DumpedCount = 0;
        FailedCount = 0;

        // 初始化进度条
        var progressBar = new ProgressBar
        {
            Value = 0,
            Maximum = Files.Count,
            ShowProgressText = true
        };

        // 创建Toast
        Debug.WriteLine("ProgressToast created, waiting for progressbar update");
        var progressToast = ToastManager.CreateToast()
            .WithTitle("Dumping...")
            .WithContent(progressBar)
            .WithLoadingState(true)
            .Queue();

        // 重置状态
        foreach (var item in Files)
        {
            item.Status = DumpStatus.Pending;
            item.ErrorMessage = null;
        }

        var result = await Task.Run(() =>
        {
            int ok = 0, ng = 0;
            for (var i = 0; i < Files.Count; i++)
            {
                var item = Files[i];
                var index = i;

                Dispatcher.UIThread.Post(() => item.Status = DumpStatus.Running);

                var dumper = new NCMDumper();
                Debug.WriteLine("Dumper initialized");
                try
                {
                    if (string.IsNullOrEmpty(item.FilePath) || !File.Exists(item.FilePath))
                        throw new FileNotFoundException("File not found", item.FilePath);

                    Debug.WriteLine($"Dumping file: {item.FilePath}");
                    Debug.WriteLine("Dumping file 1/2 : encrypting...");
                    dumper.NeteaseCrypt(item.FilePath);
                    var r = dumper.Dump("");
                    if (r == 0)
                    {
                        Debug.WriteLine("Dumping file 2/2 : fixing metadata...");
                        dumper.FixMetadata();
                        ok++;
                        Dispatcher.UIThread.Post(() => item.Status = DumpStatus.Success);
                    }
                    else
                    {
                        Debug.WriteLine($"fail to dump file:{item.FilePath}");
                        ng++;
                        Dispatcher.UIThread.Post(() =>
                        {
                            item.Status = DumpStatus.Failed;
                            item.ErrorMessage = $"Dump returned {r}";
                        });
                    }
                }
                catch (Exception ex)
                {
                    ng++;
                    Debug.WriteLine($"Dump failed: {ex}");
                    Dispatcher.UIThread.Post(() =>
                    {
                        item.Status = DumpStatus.Failed;
                        item.ErrorMessage = ex.Message;
                    });
                }
                finally
                {
                    dumper.Destroy();
                    Debug.WriteLine("Dumper destroyed");
                }

                // 更新进度和计数
                var okLocal = ok;
                var ngLocal = ng;
                Dispatcher.UIThread.Post(() =>
                {
                    DumpedCount = okLocal;
                    FailedCount = ngLocal;
                    Dispatcher.UIThread.Post(() =>
                    {
                        DumpedCount = okLocal;
                        FailedCount = ngLocal;
                        progressBar.Value = index + 1; // 同步进度条
                    });
                });
            }

            return (ok, ng);
        });

        IsDumping = false;
        ToastManager.Dismiss(progressToast);
        Toast("Dumper", $"Dump finished. Success: {result.ok}, Failed: {result.ng}", NotificationType.Information);
    }

    [RelayCommand]
    private void ClearFilesList()
    {
        Debug.WriteLine("Command method ClearFilesList() Triggered");
        if (Files.Count == 0) return;

        Files.Clear();
        PickedCount = 0;
        DumpedCount = 0;
        FailedCount = 0;

        Toast("TaskList", "Tasklist cleared", NotificationType.Information);
    }

    #endregion

    #region Tool Methods

    // 接收文件选择
    public void SetPickedFiles(IReadOnlyList<IStorageFile> files)
    {
        Files.Clear();
        foreach (var f in files)
        {
            var local = f.TryGetLocalPath();
            Files.Add(new DumpItem
            {
                FilePath = local ?? string.Empty,
                FileName = f.Name
            });
        }

        PickedCount = Files.Count;
        Toast("FilePicker", $".ncm files picked. Count: {PickedCount}", NotificationType.Information);
    }

    // 通用Toast方法
    private void Toast(string title, string content, NotificationType type)
    {
        ToastManager.CreateToast()
            .WithTitle(title)
            .WithContent(content)
            .OfType(type)
            .Dismiss().After(TimeSpan.FromSeconds(5))
            .Queue();
        Debug.WriteLine($"Toast show: \n |Title:{title}\n |Content:{content}\n |Type:{type.ToString()}");
    }

    // 文件drag辅助方法
    public void AddFiles(IReadOnlyList<IStorageFile> files)
    {
        foreach (var f in files)
        {
            var local = f.TryGetLocalPath();
            if (string.IsNullOrEmpty(local)) continue;
            // 去重
            if (Files.Any(x => x.FilePath == local)) continue;

            Files.Add(new DumpItem
            {
                FilePath = local,
                FileName = f.Name
            });
        }

        PickedCount = Files.Count;
        Toast("FilePicker", $".ncm files added. Count: {Files.Count}", NotificationType.Information);
    }

    // 文件预检查过滤提示
    public void NotifyNoValidFiles()
    {
        Toast("FilePicker", "Only .ncm files are allowed.", NotificationType.Warning);
    }

    public void NotifySkippedNonNcm(int skipped)
    {
        Toast("FilePicker", $"Skipped {skipped} non-.ncm file(s).", NotificationType.Warning);
    }

    #endregion
}