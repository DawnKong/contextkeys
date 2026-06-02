using System.Windows;
using System.Windows.Threading;
using ContextKeys.Models;

namespace ContextKeys.Services;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer? _closeTimer;

    public ToastWindow(Profile profile, string displayMode)
    {
        InitializeComponent();

        ToastTitle.Text = $"已启用：{profile.Name}";
        ToastRules.ItemsSource = profile.Rules;

        // Setup close timer based on display mode
        switch (displayMode)
        {
            case "always":
                _closeTimer = null;
                break;
            case "timed":
            default:
                _closeTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Normal, OnTimerTick, Dispatcher);
                _closeTimer.Start();
                break;
        }

        // Position at bottom-right after window is loaded
        Loaded += (_, _) =>
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - ActualWidth - 16;
            Top = workArea.Bottom - ActualHeight - 16;
        };
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _closeTimer?.Stop();
        try { Close(); }
        catch { /* window may already be closing */ }
    }

    protected override void OnClosed(EventArgs e)
    {
        _closeTimer?.Stop();
        base.OnClosed(e);
    }
}
