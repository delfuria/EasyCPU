using System;
using CommunityToolkit.Mvvm.Input;

namespace EasyCPU.ViewModels;

public sealed class RecentFileItem
{
    public string Header { get; }
    public IRelayCommand Command { get; }

    public RecentFileItem(string path, Action<string> open)
    {
        Header = path;
        Command = new RelayCommand(() => open(path));
    }
}
