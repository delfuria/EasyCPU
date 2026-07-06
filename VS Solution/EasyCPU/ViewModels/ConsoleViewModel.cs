using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU.ViewModels;

public partial class ConsoleViewModel : Tool
{
    [ObservableProperty] private string _output = "";
    [ObservableProperty] private bool _isInAttesaInput;

    public string CaretText => IsInAttesaInput ? "▌" : "";

    partial void OnIsInAttesaInputChanged(bool value) => OnPropertyChanged(nameof(CaretText));

    // Sollevato dalla View quando l'utente preme un tasto col focus sul pannello Console.
    public event Action<char>? CarattereDigitato;

    public void NotificaCarattere(char c) => CarattereDigitato?.Invoke(c);
}
