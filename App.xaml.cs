using System.Windows;
using ContextKeys.Services;

namespace ContextKeys;

public partial class App : System.Windows.Application
{
    public static ConfigService ConfigService { get; } = new();
    public static WindowEnumerationService WindowEnumService { get; } = new();
    public static ForegroundWindowService ForegroundService { get; } = new();
    public static KeyboardHookService KeyboardHookService { get; } = new();
    public static InputSimulationService InputSimService { get; } = new(ConfigService.Settings.Settings.InputIntervalMs);
    public static ToastService ToastService { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers to prevent silent crashes
        DispatcherUnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"UI 线程未处理异常: {args.Exception}");
            MessageBox.Show($"发生错误: {args.Exception.Message}", "ContextKeys 错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"未处理异常(非UI): {args.ExceptionObject}");
        };

        try
        {
            ForegroundService.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"前台窗口监听启动失败: {ex}");
        }

        try
        {
            KeyboardHookService.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"键盘监听启动失败: {ex.Message}\n\n请尝试以管理员身份运行。",
                "ContextKeys", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { KeyboardHookService.Stop(); } catch { }
        try { ForegroundService.Stop(); } catch { }

        base.OnExit(e);
    }
}
