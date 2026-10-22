using System;
using System.Diagnostics;
using Avalonia.Controls;
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

            if (pickedFiles.Count == 0)
            {
                Debug.WriteLine("Not pick anything");
            }
            else
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SetPickedFiles(pickedFiles);
                }
            }
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}