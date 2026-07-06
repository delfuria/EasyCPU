using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU.ViewModels;

public partial class ConsoleViewModel : Tool
{
    [ObservableProperty] private string _output = "";

    // Sollevato dalla View quando l'utente preme un tasto col focus sul pannello Console.
    public event Action<char>? CarattereDigitato;

    public void NotificaCarattere(char c) => CarattereDigitato?.Invoke(c);
}
