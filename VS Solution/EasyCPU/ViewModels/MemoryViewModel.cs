using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU.ViewModels;

public partial class MemoryViewModel : Tool
{
    [ObservableProperty] private string _dump = "";
}
