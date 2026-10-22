using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Toasts;

namespace NCMDumperGUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // Properties
    #region Properties
    [ObservableProperty] public partial int PickedCount { get; set; }

    [ObservableProperty] public partial int DumpedCount { get; set; }

    [ObservableProperty] public partial int FailedCount { get; set; }
    #endregion

    // Interfaces
    #region Interfaces
    private IReadOnlyList<IStorageFile> ncmFiles = [];
    public ISukiToastManager ToastManager { get; } = new SukiToastManager();
    #endregion
    
    // Commands
    #region Commands
    [RelayCommand]
    public void Dump()
    {
        Debug.WriteLine("Command method Dump() Triggered");
        if (ncmFiles.Count == 0)
        {
            Toast("Dumper","Not pick any .ncm files !",NotificationType.Warning);
        }
        else
        {
            Toast("Dumper","Dumping...",NotificationType.Information);
        }
        // TODO:实现解密
    }
    #endregion

    #region Tool Methods
    private void Toast(string title, string content, NotificationType type)
    {
        Debug.WriteLine("Tool method Toast() Triggered");
        ToastManager.CreateToast()
            .WithTitle(title)
            .WithContent(content)
            .OfType(type)
            .Dismiss().After(TimeSpan.FromSeconds(5))
            .Queue();
        Debug.WriteLine($"Toast: \n |Title:{title} \n |Content:{content} \n |Type:{type.ToString()}" );
    }

    public void SetPickedFiles(IReadOnlyList<IStorageFile> files)
    {
        Debug.WriteLine("Tool method SetPickedFiles() Triggered");
        PickedCount = files.Count;
        ncmFiles = files;
        Toast("FilePicker",$".ncm files picked. \nCount: {PickedCount}", NotificationType.Information);
        Debug.WriteLine($"Files count: {PickedCount}");
    }
    #endregion
    
}