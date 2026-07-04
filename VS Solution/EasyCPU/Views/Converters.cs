using System;
using System.Globalization;
using Avalonia.Data.Converters;
using EasyCpu.Assembler.Parsing;

namespace EasyCPU.Views;

public class Int32PlusOneConverter : IValueConverter
{
    public static readonly Int32PlusOneConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i ? i + 1 : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class CompilerErrorTipoConverter : IValueConverter
{
    public static readonly CompilerErrorTipoConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int tipo ? (tipo == CompilerError.CODICE ? "Codice" : "Dati") : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
