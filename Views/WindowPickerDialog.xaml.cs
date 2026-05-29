using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ContextKeys.Models;
using ContextKeys.Services;
using ContextKeys.Utils;

namespace ContextKeys.Views;

public partial class WindowPickerDialog : Window
{
    private List<Models.WindowInfo> _windows = new();
    private Models.WindowInfo? _selectedWindow;
    private Border? _selectedBorder;

    public Models.WindowInfo? SelectedWindow => _selectedWindow;

    public WindowPickerDialog()
    {
        InitializeComponent();
        LoadWindows();
    }

    private void LoadWindows()
    {
        try
        {
            _windows = App.WindowEnumService.EnumVisibleWindows();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"枚举窗口失败: {ex}");
            _windows = new List<WindowInfo>();
        }
        WindowList.ItemsSource = null;
        WindowList.ItemsSource = _windows;
    }

    private void WindowItem_Click(object sender, MouseButtonEventArgs e)
    {
        // Find the Border that has the Tag (walk up in case click lands on inner TextBlock)
        var element = sender as DependencyObject;
        while (element != null && element is not Border)
        {
            element = VisualTreeHelper.GetParent(element);
        }

        if (element is Border border && border.Tag is Models.WindowInfo info)
        {
            // Reset previous selection style
            if (_selectedBorder != null)
                _selectedBorder.Style = (Style)FindResource("ListItemStyle");

            _selectedWindow = info;
            _selectedBorder = border;
            BindBtn.IsEnabled = true;

            // Apply selected style
            _selectedBorder.Style = (Style)FindResource("ListItemSelectedStyle");
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _selectedBorder = null;
        LoadWindows();
        _selectedWindow = null;
        BindBtn.IsEnabled = false;
    }

    private void Bind_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWindow != null)
        {
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
