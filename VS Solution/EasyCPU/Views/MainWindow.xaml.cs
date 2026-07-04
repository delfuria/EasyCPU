using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using EasyCPU.ViewModels;

namespace EasyCPU;

public partial class MainWindow : Window
{
    private NativeMenu? _nativeRecentiMenu;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
        if (SettingsViewModel.Instance.PienoSchermo)
            WindowState = WindowState.Maximized;
        if (OperatingSystem.IsMacOS())
        {
            this.FindControl<Menu>("MainMenu")!.IsVisible = false;
            DataContextChanged += OnDataContextChanged;
        }
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.SaveAll();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            UpdateNativeCheckmarks(vm);
            SetupNativeRecentiMenu();
            UpdateNativeRecentiMenu(vm);
            vm.RecentFileItems.CollectionChanged += (_, _) => UpdateNativeRecentiMenu(vm);
        }
    }

    private void SetupNativeRecentiMenu()
    {
        if (_nativeRecentiMenu is not null) return;
        var nativeMenu = NativeMenu.GetMenu(this);
        if (nativeMenu is null || nativeMenu.Items.Count == 0) return;
        if (nativeMenu.Items[0] is not NativeMenuItem fileItem) return;
        var fileMenu = fileItem.Menu;
        if (fileMenu is null) return;

        _nativeRecentiMenu = new NativeMenu();
        var recentiItem = new NativeMenuItem { Header = "Recenti", Menu = _nativeRecentiMenu };
        // Insert: Sep at 4, Recenti at 5 (after Nuovo/Apri/Salva/SalvaAs)
        fileMenu.Items.Insert(4, new NativeMenuItemSeparator());
        fileMenu.Items.Insert(5, recentiItem);
    }

    private void UpdateNativeRecentiMenu(MainViewModel vm)
    {
        if (_nativeRecentiMenu is null) return;
        _nativeRecentiMenu.Items.Clear();
        foreach (var item in vm.RecentFileItems)
        {
            var path = item.Header;
            _nativeRecentiMenu.Items.Add(new NativeMenuItem
            {
                Header = path,
                Command = new RelayCommand(() => vm.OpenRecentFileCommand.Execute(path))
            });
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsCodeEditorVisible)
                           or nameof(MainViewModel.IsDataEditorVisible)
                           or nameof(MainViewModel.IsRegistersVisible)
                           or nameof(MainViewModel.IsStackVisible)
                           or nameof(MainViewModel.IsMemoryVisible)
                           or nameof(MainViewModel.IsErrorsVisible)
            && sender is MainViewModel vm)
        {
            UpdateNativeCheckmarks(vm);
        }
    }

    private void UpdateNativeCheckmarks(MainViewModel vm)
    {
        var nativeMenu = NativeMenu.GetMenu(this);
        if (nativeMenu is null || nativeMenu.Items.Count < 4) return;
        // Root order: File(0), Modifica(1), Esegui(2), Finestre(3), Strumenti(4)
        if (nativeMenu.Items[3] is not NativeMenuItem finestre) return;
        var items = finestre.Menu?.Items;
        if (items is null || items.Count < 6) return;
        SetCheck(items[0], vm.IsCodeEditorVisible);
        SetCheck(items[1], vm.IsDataEditorVisible);
        SetCheck(items[2], vm.IsRegistersVisible);
        SetCheck(items[3], vm.IsStackVisible);
        SetCheck(items[4], vm.IsMemoryVisible);
        SetCheck(items[5], vm.IsErrorsVisible);
    }

    private static void SetCheck(NativeMenuItemBase item, bool value)
    {
        if (item is NativeMenuItem mi)
        {
            mi.ToggleType = MenuItemToggleType.CheckBox;
            mi.IsChecked = value;
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
