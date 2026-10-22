using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private IReadOnlyList<IStorageFile> ncmFiles;
    #endregion
    
    // Commands
    #region Commands
    [RelayCommand]
    public void Dump()
    {
        Debug.WriteLine("Command method Dump() Triggered");
        // TODO:实现解密
    }
    #endregion
    
    public void SetPickedFiles(IReadOnlyList<IStorageFile> files)
    {
        Debug.WriteLine(".ncm files receive");
        PickedCount = files.Count;
        Debug.WriteLine($"Files count: {PickedCount}");
        ncmFiles = files;
    }
}