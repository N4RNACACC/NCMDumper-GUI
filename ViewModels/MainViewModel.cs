using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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

    [ObservableProperty] public partial bool ButtonProgressMode { get; set; } = false;
    
    [ObservableProperty] public partial double ProgressValue { get; set; }
    
    [ObservableProperty] public partial double ProgressMax { get; set; } = 1;

    public ObservableCollection<DumpItem> Files { get; } = [];
    
    #endregion

    // Interfaces
    #region Interfaces
    public ISukiToastManager ToastManager { get; } = new SukiToastManager();
    #endregion
    
    // Commands
#region Commands
    [RelayCommand]
    public async Task DumpAsync()
    {
        Debug.WriteLine("Command method DumpAsync() Triggered");

        if (Files.Count == 0)
        {
            Toast("Dumper", "Not pick any .ncm files !", false, NotificationType.Warning);
            return;
        }

        ButtonProgressMode = true;
        DumpedCount = 0;
        FailedCount = 0;
        ProgressMax = Files.Count;
        ProgressValue = 0;

        // 重置状态
        foreach (var item in Files)
        {
            item.Status = DumpStatus.Pending;
            item.ErrorMessage = null;
        }

        Toast("Dumper", "Dumping...", true, NotificationType.Information);
        
        var result = await Task.Run(() =>
        {
            int ok = 0, ng = 0;
            for (int i = 0; i < Files.Count; i++)
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
                    ProgressValue = index + 1;
                    DumpedCount = okLocal;
                    FailedCount = ngLocal;
                });
            }
            return (ok, ng);
        });

        ButtonProgressMode = false;
        Toast("Dumper", $"Dump finished. Success: {result.ok}, Failed: {result.ng}",
            false, NotificationType.Information);
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
        ProgressMax = Math.Max(1, Files.Count);
        ProgressValue = 0;
        Toast("FilePicker", $".ncm files picked. Count: {PickedCount}", false, NotificationType.Information);
    }

    private void Toast(string title, string content, bool loadingMode, NotificationType type)
    {
        ToastManager.CreateToast()
            .WithTitle(title)
            .WithContent(content)
            .WithLoadingState(loadingMode)
            .OfType(type)
            .Dismiss().After(TimeSpan.FromSeconds(5))
            .Queue();
    }
    #endregion
    
}