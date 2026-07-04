#nullable enable
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using EasyCPU.ViewModels;

namespace EasyCPU.Views;

public partial class ErrorsView : UserControl
{
    public ErrorsView()
    {
        InitializeComponent();
        var grid = this.FindControl<DataGrid>("ErrorsGrid");
        grid?.AddHandler(InputElement.PointerPressedEvent, OnGridPointerPressed,
                         Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);

        if (grid is not null)
        {
            var s = SettingsViewModel.Instance;
            ApplyFontSize(grid, s.FontPanelliSize);
            s.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SettingsViewModel.FontPanelliSize))
                    ApplyFontSize(grid, s.FontPanelliSize);
            };
        }
    }

    private static void ApplyFontSize(DataGrid grid, float size)
    {
        if (size > 0) grid.FontSize = size;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (DataContext is not ErrorsViewModel vm) return;
        if (this.FindControl<DataGrid>("ErrorsGrid") is not { } grid) return;
        if (grid.SelectedItem is not CompilerErrorAdapter item) return;
        vm.MainVm.NavigateToError(item.Source);
    }
}
