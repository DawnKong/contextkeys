using System.Windows;
using System.Windows.Threading;
using ContextKeys.Models;

namespace ContextKeys.Services;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _closeTimer;

    public ToastWindow(Profile profile)
    {
        InitializeComponent();

        ToastTitle.Text = $"已启用：{profile.Name}";
        ToastRules.ItemsSource = profile.Rules;

        // Position at bottom-right of screen
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - 380;
        Top = workArea.Bottom - Height - 24;

        // Auto-close after 2 seconds (DispatcherTimer runs on UI thread, auto-disposed)
        _closeTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Normal, OnTimerTick, Dispatcher);
        _closeTimer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _closeTimer.Stop();
        try { Close(); }
        catch { /* window may already be closing */ }
    }

    protected override void OnClosed(EventArgs e)
    {
        _closeTimer.Stop();
        base.OnClosed(e);
    }
}
