using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EasyCPU.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        AvaloniaXamlLoader.Load(this);

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        this.FindControl<TextBlock>("NameText")!.Text = "EasyCPU vNext";
        this.FindControl<TextBlock>("VersionText")!.Text = $"Versione {version}";
        this.FindControl<TextBlock>("CopyrightText")!.Text = "© 2026 Stefano Del Furia - Paolo Meozzi";
    }
}
