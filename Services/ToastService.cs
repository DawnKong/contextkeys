using System.Windows;
using ContextKeys.Models;

namespace ContextKeys.Services;

public class ToastService
{
    private ToastWindow? _currentToast;

    public void ShowProfileToast(Profile profile, string displayMode)
    {
        if (displayMode == "hidden")
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            CloseCurrentToast();
            _currentToast = new ToastWindow(profile, displayMode);
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
