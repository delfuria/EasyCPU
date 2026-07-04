#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;
        var name = data.GetType().FullName!
            .Replace(".ViewModels.", ".Views.")
            .Replace("ViewModel", "View");
        var type = typeof(App).Assembly.GetType(name);
        return type != null
            ? (Control)Activator.CreateInstance(type)!
            : new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data) => data is Tool or Document;
}
