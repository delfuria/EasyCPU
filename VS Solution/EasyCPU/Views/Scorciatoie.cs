using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Input;
using EasyCPU.ViewModels;

namespace EasyCPU.Views;

/// <summary>
/// Scorciatoie da tastiera dei comandi di esecuzione, della guida e (desktop) dei file. Le InputGesture dei MenuItem
/// sono solo testo: servono KeyBinding sulla finestra (Windows/Linux) o su MainView (browser, mobile).
/// Su macOS le gestisce il menu nativo (Gesture dei NativeMenuItem).
/// </summary>
public static class Scorciatoie
{
    /// <param name="comandiFile">Anche Nuovo/Apri/Salva/Salva come/Stampa: solo sulla finestra desktop,
    /// nel browser Ctrl+N e simili sono riservati al browser.</param>
    public static void Collega(InputElement elemento, MainViewModel vm, bool comandiFile)
    {
        if (elemento.KeyBindings.Count > 0) return;
        List<(string Tasti, ICommand Comando)> elenco =
        [
            ("F1",        vm.ShowGuidaCommand),
            ("Ctrl+B",    vm.CompileCommand),
            ("F5",        vm.RunCommand),
            ("Ctrl+F5",   vm.RunUntilCommand),
            ("F11",       vm.StepIntoCommand),
            ("F10",       vm.StepOverCommand),
            ("Shift+F11", vm.StepOutCommand),
            ("F8",        vm.StopCommand),
            ("F9",        vm.ToggleBreakpointCommand),
        ];
        if (comandiFile)
            elenco.AddRange(
            [
                ("Ctrl+N",       vm.NewCommand),
                ("Ctrl+O",       vm.OpenCommand),
                ("Ctrl+S",       vm.SaveCommand),
                ("Ctrl+Shift+S", vm.SaveAsCommand),
                ("Ctrl+P",       vm.PrintCommand),
            ]);
        foreach (var (tasti, comando) in elenco)
            elemento.KeyBindings.Add(new KeyBinding { Gesture = KeyGesture.Parse(tasti), Command = comando });
    }
}
