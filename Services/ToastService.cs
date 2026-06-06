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
            var toast = new ToastWindow(profile, displayMode);
            toast.Closed += OnToastClosed;
            _currentToast = toast;
            toast.Show();
        });
    }

    public void CloseCurrentToast()
    {
        var toast = _currentToast;
        if (toast != null)
        {
            _currentToast = null;
            toast.Closed -= OnToastClosed;
            try { toast.Close(); }
            catch { }
        }
    }

    private void OnToastClosed(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _currentToast))
            _currentToast = null;
    }
}
