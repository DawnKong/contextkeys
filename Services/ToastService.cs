using System.Windows;
using ContextKeys.Models;

namespace ContextKeys.Services;

public class ToastService
{
    private ToastWindow? _currentToast;

    public void ShowProfileToast(Profile profile)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CloseCurrentToast();
            _currentToast = new ToastWindow(profile);
            _currentToast.Show();
        });
    }

    public void CloseCurrentToast()
    {
        if (_currentToast != null)
        {
            _currentToast.Close();
            _currentToast = null;
        }
    }
}
