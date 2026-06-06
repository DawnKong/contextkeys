using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using ContextKeys.Models;
using ContextKeys.Services;
using ContextKeys.Views;
using ContextKeys.Utils;

namespace ContextKeys;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

        // Set taskbar/Alt+Tab icon from PNG
        SetWindowIcon();

        // Show empty state if no profiles
        UpdateEmptyState();

        App.ConfigService.SettingsChanged += OnSettingsChanged;
    }

    private void SetWindowIcon()
    {
        try
        {
            var icoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LKey.ico");
            if (!File.Exists(icoPath)) return;
            using var fs = new FileStream(icoPath, FileMode.Open, FileAccess.Read);
            using var ico = new System.Drawing.Icon(fs);
            var bmp = ico.ToBitmap();
            var hbmp = bmp.GetHbitmap();
            Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hbmp, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            Win32Api.DeleteObject(hbmp);
        }
        catch { /* fallback to default icon */ }
    }

    private void OnSettingsChanged(ContextKeys.Models.AppSettings _)
    {
        Dispatcher.Invoke(UpdateEmptyState);
    }

    private void UpdateEmptyState()
    {
        var hasProfiles = ProfileList.Items.Count > 0;
        EmptyState.Visibility = hasProfiles ? Visibility.Collapsed : Visibility.Visible;
    }

    private void NavigateTo(string page, System.Windows.Controls.Border navElement)
    {
        // Hide all pages
        PageProfiles.Visibility = Visibility.Collapsed;
        PageSettings.Visibility = Visibility.Collapsed;
        PageAbout.Visibility = Visibility.Collapsed;

        // Reset nav styles
        NavProfiles.Style = (Style)FindResource("NavItemStyle");
        NavSettings.Style = (Style)FindResource("NavItemStyle");
        NavAbout.Style = (Style)FindResource("NavItemStyle");

        // Show selected page
        switch (page)
        {
            case "profiles":
                PageProfiles.Visibility = Visibility.Visible;
                NavProfiles.Style = (Style)FindResource("NavItemSelectedStyle");
                break;
            case "settings":
                PageSettings.Visibility = Visibility.Visible;
                NavSettings.Style = (Style)FindResource("NavItemSelectedStyle");
                break;
            case "about":
                PageAbout.Visibility = Visibility.Visible;
                NavAbout.Style = (Style)FindResource("NavItemSelectedStyle");
                break;
        }
    }

    private void NavProfiles_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        NavigateTo("profiles", NavProfiles);
    }

    private void NavSettings_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        NavigateTo("settings", NavSettings);
    }

    private void NavAbout_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        NavigateTo("about", NavAbout);
    }

    private void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProfileEditorWindow();
        if (dialog.ShowDialog() == true && dialog.ResultProfile != null)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.AddProfile(dialog.ResultProfile);
                UpdateEmptyState();
            }
        }
    }

    private void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element && element.Tag is Profile profile)
        {
            var dialog = new ProfileEditorWindow(profile);
            if (dialog.ShowDialog() == true && dialog.ResultProfile != null)
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.UpdateProfile(profile, dialog.ResultProfile);
                }
            }
        }
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element && element.Tag is Profile profile)
        {
            var result = MessageBox.Show(
                $"确定要删除配置【{profile.Name}】吗？",
                "删除配置",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.DeleteProfile(profile);
                    UpdateEmptyState();
                }
            }
        }
    }

    private void TogglePause_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.Paused = !vm.Paused;
        }
    }

    private void ProfileToggle_Changed(object sender, RoutedEventArgs e)
    {
        // Save config immediately when toggle changes
        App.ConfigService.Save();
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.RefreshCurrentProfile();
        }
    }

    private void OpenLog_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var logPath = Utils.Logger.GetLogPath();
        if (System.IO.File.Exists(logPath))
        {
            Process.Start("notepad.exe", logPath);
        }
        else
        {
            MessageBox.Show("日志文件尚不存在，请先触发一次快捷键。", "ContextKeys");
        }
    }

    private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
    {
        var configDir = App.ConfigService.ConfigDirectory;
        if (System.IO.Directory.Exists(configDir))
        {
            Process.Start("explorer.exe", configDir);
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        // Don't hide to tray while modal dialogs are open — it would break
        // the modality chain and cause child windows to disappear.
        var hasModalWindows = Application.Current.Windows
            .Cast<Window>()
            .Any(w => w != this && w.IsVisible && w.Owner == this);

        if (WindowState == WindowState.Minimized
            && App.ConfigService.Settings.Settings.MinimizeToTray
            && !hasModalWindows)
        {
            Hide();
            ShowTrayIcon();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        App.ConfigService.SettingsChanged -= OnSettingsChanged;
        HideTrayIcon();
        _appIcon?.Dispose();
        _appIcon = null;
        base.OnClosed(e);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose && App.ConfigService.Settings.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            ShowTrayIcon();
            return;
        }

        base.OnClosing(e);
    }

    // System tray support
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private System.Drawing.Icon? _appIcon;

    private void LoadTrayIcons()
    {
        if (_appIcon != null) return;
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var iconPath = System.IO.Path.Combine(baseDir, "LKey.ico");
        var appPath = Environment.ProcessPath ?? System.IO.Path.Combine(baseDir, "ContextKeys.exe");

        if (File.Exists(iconPath))
        {
            using var fs = new FileStream(iconPath, FileMode.Open, FileAccess.Read);
            _appIcon = new System.Drawing.Icon(fs);
        }
        else
        {
            _appIcon = System.Drawing.Icon.ExtractAssociatedIcon(appPath);
        }
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null) return;
        if (_appIcon != null)
            _trayIcon.Icon = _appIcon;
    }

    private void ShowTrayIcon()
    {
        if (_trayIcon != null) return;
        LoadTrayIcons();

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = _appIcon ?? System.Drawing.Icon.ExtractAssociatedIcon(
                Environment.ProcessPath ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ContextKeys.exe")),
            Visible = true,
            Text = "ContextKeys"
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("打开 ContextKeys", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var pauseItem = new System.Windows.Forms.ToolStripMenuItem("暂停所有快捷键");
        pauseItem.Click += (_, _) =>
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.Paused = !vm.Paused;
                pauseItem.Text = vm.Paused ? "恢复快捷键" : "暂停所有快捷键";
                UpdateTrayIcon();
            }
        };
        menu.Items.Add(pauseItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) =>
        {
            _allowClose = true;
            HideTrayIcon();
            Application.Current.Shutdown();
        });

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                RestoreFromTray();
        };
    }

    private void HideTrayIcon()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}
