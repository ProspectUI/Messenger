using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Messenger.Client.Converters;

public class FirstLetterConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is string s && s.Length > 0 ? s[0].ToString().ToUpper() : "?";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public class AvatarColorConverter : IValueConverter
{
    private static readonly Color[] Palette =
    [
        Color.FromRgb(0xe1, 0x70, 0x55), Color.FromRgb(0x6c, 0x5c, 0xe7),
        Color.FromRgb(0x00, 0xb8, 0x94), Color.FromRgb(0x09, 0x84, 0xe3),
        Color.FromRgb(0xfd, 0xcb, 0x6e), Color.FromRgb(0xa2, 0x9b, 0xfe),
        Color.FromRgb(0x55, 0xec, 0xc4), Color.FromRgb(0xe8, 0x44, 0x93),
    ];
    public object Convert(object v, Type t, object p, CultureInfo c)
    {
        var idx = v is string s ? Math.Abs(s.GetHashCode()) % Palette.Length : 0;
        return new SolidColorBrush(Palette[idx]);
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public class BoolToHorizontalAlignmentConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public class BoolToMessageBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Own = new(Color.FromRgb(0x2b, 0x52, 0x78));
    private static readonly SolidColorBrush Other = new(Color.FromRgb(0x18, 0x25, 0x33));
    public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? Own : Other;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is not true;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => v is not true;
}

public class StringEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        string.IsNullOrEmpty(v as string) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        string.IsNullOrEmpty(v as string) ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
