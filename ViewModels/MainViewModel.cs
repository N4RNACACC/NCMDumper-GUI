using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Toasts;
using NCMDumperGUI.Tools;

namespace NCMDumperGUI.ViewModels;

public enum DumpStatus { Pending, Running, Success, Failed }

public partial class DumpItem : ViewModelBase
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }

    [ObservableProperty] public partial DumpStatus Status { get; set; } = DumpStatus.Pending;
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
}

public partial class MainViewModel : ViewModelBase
{
    // Properties
    #region Properties
    [ObservableProperty] public partial int PickedCount { get; set; }

    [ObservableProperty] public partial int DumpedCount { get; set; }

    [ObservableProperty] public partial int FailedCount { get; set; }
    
    [ObservableProperty] public partial bool IsAboutVisible { get; set; } = false;

    public ObservableCollection<DumpItem> Files { get; } = [];
    
    public string ToggleButtonText => IsAboutVisible ? "Back" : "About";
    
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnIsAboutVisibleChanged(bool value) => OnPropertyChanged(nameof(ToggleButtonText));
    
    #endregion

    // Interfaces
    #region Interfaces
    public ISukiToastManager ToastManager { get; } = new SukiToastManager();
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
        var progressToast = ToastManager.CreateToast()
            .WithTitle("Dumper")
            .WithContent(progressBar)
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
                try
                {
                    if (string.IsNullOrEmpty(item.FilePath) || !File.Exists(item.FilePath))
                        throw new FileNotFoundException("File not found", item.FilePath);

                    dumper.NeteaseCrypt(item.FilePath);
                    var r = dumper.Dump("");
                    if (r == 0)
                    {
                        dumper.FixMetadata();
                        ok++;
                        Dispatcher.UIThread.Post(() => item.Status = DumpStatus.Success);
                    }
                    else
                    {
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

        ToastManager.Dismiss(progressToast);
        Toast("Dumper", $"Dump finished. Success: {result.ok}, Failed: {result.ng}", NotificationType.Information);
    }
    #endregion

    #region Tool Methods
    public void SetPickedFiles(IReadOnlyList<IStorageFile> files)
    {
        Files.Clear();
        foreach (var f in files)
        {
            var local = f.TryGetLocalPath();
            Files.Add(new DumpItem
            {
                FilePath = local ?? string.Empty,
                FileName = f.Name,
            });
        }
        PickedCount = Files.Count;
        Toast("FilePicker", $".ncm files picked. Count: {PickedCount}", NotificationType.Information);
    }

    private void Toast(string title, string content, NotificationType type)
    {
        ToastManager.CreateToast()
            .WithTitle(title)
            .WithContent(content)
            .OfType(type)
            .Dismiss().After(TimeSpan.FromSeconds(5))
            .Queue();
    }
    #endregion
    
}