using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NCMDumperGUI.ViewModels;

namespace NCMDumperGUI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }


    private async void FilePick_ClickAsync(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Click method FilePick_ClickAsync() Triggered");
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider is null)
            {
                Debug.WriteLine("topLevel.StorageProvider is null");
                return;
            }

            var ncmFileType = new FilePickerFileType("NCM 文件")
            {
                Patterns = ["*.ncm"]
            };

            var pickedFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                Title = "选择文件",
                FileTypeFilter = [ncmFileType]
            });

            if (DataContext is MainViewModel vm)
            {
                Debug.WriteLine("Picked file");
                vm.SetPickedFiles(pickedFiles);
            }
            else
            {
                Debug.WriteLine("DataContext set is wrong");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    // 处理文件drag

    #region File drag

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        Debug.WriteLine("DragOver method OnDragOver() Triggered");
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        Debug.WriteLine("Drop method OnDrop() Triggered");
        if (DataContext is not MainViewModel vm) return;

        if (e.DataTransfer.TryGetFiles() is not { } files) return;

        var ncmFiles = files
            .OfType<IStorageFile>()
            .Where(f => string.Equals(Path.GetExtension(f.Name), ".ncm", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (ncmFiles.Count == 0)
        {
            vm.NotifyNoValidFiles();
            return;
        }

        vm.AddFiles(ncmFiles);

        var total = files.OfType<IStorageFile>().Count();
        if (ncmFiles.Count < total)
            vm.NotifySkippedNonNcm(total - ncmFiles.Count);
    }

    #endregion
}