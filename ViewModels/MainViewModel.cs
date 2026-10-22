using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NCMDumperGUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // Property
    [ObservableProperty] public partial int PickedCount { get; set; }

    [ObservableProperty] public partial int DumpedCount { get; set; }

    [ObservableProperty] public partial int FailedCount { get; set; }

    private IReadOnlyList<IStorageFile> ncmFiles;

    [RelayCommand]
    public void Dump()
    {
        Debug.WriteLine("Command method Dump() Triggered");
        // TODO:实现解密
    }
    
    public void SetPickedFiles(IReadOnlyList<IStorageFile> files)
    {
        Debug.WriteLine(".ncm files receive");
        PickedCount = files.Count;
        Debug.WriteLine($"Files count: {PickedCount}");
        ncmFiles = files;
    }
}