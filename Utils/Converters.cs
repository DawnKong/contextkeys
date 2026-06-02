using System.Windows;
using System.Windows.Data;

namespace ContextKeys.Utils;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        return boolValue ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is Visibility v && v != Visibility.Visible;
    }
}

[ValueConversion(typeof(string), typeof(bool))]
public class StringEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var strValue = value as string;
        var strParameter = parameter as string;
        return string.Equals(strValue, strParameter, StringComparison.Ordinal);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return parameter as string ?? "";
        }
        return System.Windows.Data.Binding.DoNothing;
    }
}
