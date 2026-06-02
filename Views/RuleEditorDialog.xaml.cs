using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ContextKeys.Models;
using ContextKeys.Services;
using ContextKeys.Utils;

namespace ContextKeys.Views;

public partial class RuleEditorDialog : Window
{
    private bool _isCapturingHotkey;
    private string _capturedKey = string.Empty;
    private List<string> _capturedModifiers = new();

    private bool _capturingAction;
    private readonly List<string> _actionKeys = new();
    private bool _isChordMode = false;  // true=同时录制(chord), false=依次录制(sequence)
    private List<string> _savedChordModifiers = new();  // 保存录制时的修饰键

    // Win32 message constants
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private ActionStep? _savedAction;

    public HotkeyRule? ResultRule { get; private set; }
    private readonly List<HotkeyRule>? _existingRules;
    private readonly string? _editingRuleId;
    private readonly bool _isGlobalProfile;

    public RuleEditorDialog() : this(null, false) { }

    public RuleEditorDialog(List<HotkeyRule>? existingRules, bool isGlobalProfile = false)
    {
        InitializeComponent();
        SetIcon();
        Title = "添加快捷键规则";
        _existingRules = existingRules;
        _isGlobalProfile = isGlobalProfile;
        UpdateRuleHint();
    }

    public RuleEditorDialog(HotkeyRule rule, List<HotkeyRule>? existingRules, bool isGlobalProfile = false)
    {
        InitializeComponent();
        SetIcon();
        Title = "编辑快捷键规则";
        _existingRules = existingRules;
        _editingRuleId = rule.Id;
        _isGlobalProfile = isGlobalProfile;

        RuleNameBox.Text = rule.Name;
        SuppressKeyCheck.IsChecked = rule.SuppressOriginalKey;

        _capturedKey = rule.Hotkey.Key;
        _capturedModifiers = new List<string>(rule.Hotkey.Modifiers);
        var display = HotkeyParser.BuildDisplay(_capturedKey, _capturedModifiers);
        HotkeyDisplayText.Text = display;
        HotkeyDisplayBox.Visibility = Visibility.Visible;

        // Load single action
        if (rule.Actions is { Count: > 0 })
        {
            _savedAction = rule.Actions[0];
            ShowActionPreview(_savedAction);
        }

        UpdateRuleHint();
        ShowTestArea();
    }

    // ── Rule name watermark ──

    private void RuleNameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => UpdateRuleHint();

    private void RuleNameBox_GotFocus(object sender, RoutedEventArgs e)
        => RuleNameHint.Visibility = Visibility.Collapsed;

    private void RuleNameBox_LostFocus(object sender, RoutedEventArgs e)
        => UpdateRuleHint();

    private void UpdateRuleHint()
    {
        RuleNameHint.Visibility = string.IsNullOrEmpty(RuleNameBox.Text)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Trigger key capture ──

    private void HotkeyCapture_Click(object sender, RoutedEventArgs e)
    {
        StopActionRecording();

        _isCapturingHotkey = !_isCapturingHotkey;
        if (_isCapturingHotkey)
        {
            HotkeyDisplayBox.Visibility = Visibility.Collapsed;
            HotkeyRecordingStatus.Visibility = Visibility.Visible;
            HotkeyRecordBtn.Content = "✓ 完成录制";
            HotkeyRecordHint.Visibility = Visibility.Visible;
            HotkeyWarning.Visibility = Visibility.Collapsed;
            _capturedKey = string.Empty;
            _capturedModifiers.Clear();
            SetCaptureMode(true);
        }
        else
        {
            ResetCaptureBoxUI();
            SetCaptureMode(false);
        }
    }

    // ── Output action recording ──

    private void InputField_GotFocus(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        CancelAllCaptures();
    }

    private void CancelAllCaptures()
    {
        if (_isCapturingHotkey)
        {
            _isCapturingHotkey = false;
            ResetCaptureBoxUI();
            SetCaptureMode(false);
        }
        if (_capturingAction)
        {
            StopActionRecording();
        }
    }

    private void RecordSequence_Click(object sender, RoutedEventArgs e)
    {
        if (_capturingAction)
        {
            FinishActionRecording();
            return;
        }

        _isCapturingHotkey = false;
        ResetCaptureBoxUI();

        _capturingAction = true;
        _isChordMode = false;
        _actionKeys.Clear();
        _savedChordModifiers.Clear();
        _savedAction = null;
        ActionPreview.Visibility = Visibility.Collapsed;

        SequenceStatus.Visibility = Visibility.Visible;
        SequenceStatusText.Text = "单键依次输出… 按下要输出的按键，按 Esc 完成";
        RecordSequenceBtn.Content = "✓ 完成";
        RecordChordBtn.IsEnabled = false;
        RecordHint.Visibility = Visibility.Visible;
        SetCaptureMode(true);
    }

    private void RecordChord_Click(object sender, RoutedEventArgs e)
    {
        if (_capturingAction)
        {
            FinishActionRecording();
            return;
        }

        _isCapturingHotkey = false;
        ResetCaptureBoxUI();

        _capturingAction = true;
        _isChordMode = true;
        _actionKeys.Clear();
        _savedChordModifiers.Clear();
        _savedAction = null;
        ActionPreview.Visibility = Visibility.Collapsed;

        SequenceStatus.Visibility = Visibility.Visible;
        SequenceStatusText.Text = "多键同时输出… 按住修饰键+按键，按 Esc 完成";
        RecordSequenceBtn.IsEnabled = false;
        RecordChordBtn.Content = "✓ 完成";
        RecordHint.Visibility = Visibility.Visible;
        SetCaptureMode(true);
    }

    private void FinishActionRecording()
    {
        _capturingAction = false;
        SequenceStatus.Visibility = Visibility.Collapsed;
        RecordSequenceBtn.Content = "单键依次输出";
        RecordChordBtn.Content = "多键同时输出";
        RecordSequenceBtn.IsEnabled = true;
        RecordChordBtn.IsEnabled = true;
        RecordHint.Visibility = Visibility.Collapsed;
        SetCaptureMode(false);

        if (_actionKeys.Count > 0)
        {
            if (_isChordMode)
            {
                var chordKeys = new List<string>();
                chordKeys.AddRange(_savedChordModifiers);
                chordKeys.AddRange(_actionKeys);
                _savedAction = new ActionStep
                {
                    Type = "chord",
                    Keys = chordKeys,
                    Display = string.Join(" + ", chordKeys)
                };
            }
            else
            {
                _savedAction = new ActionStep
                {
                    Type = "sequence",
                    Keys = new List<string>(_actionKeys),
                    IntervalMs = 30,
                    Display = string.Join(" → ", _actionKeys)
                };
            }
            ShowActionPreview(_savedAction);
        }

        // Show test area after action recording is complete
        if (_savedAction != null && !string.IsNullOrEmpty(_capturedKey))
            ShowTestArea();
    }

    private void ClearAction_Click(object sender, RoutedEventArgs e)
    {
        _savedAction = null;
        _actionKeys.Clear();
        ActionPreview.Visibility = Visibility.Collapsed;
    }

    private void StopActionRecording()
    {
        _capturingAction = false;
        SequenceStatus.Visibility = Visibility.Collapsed;
        RecordSequenceBtn.Content = "单键依次输出";
        RecordChordBtn.Content = "多键同时输出";
        RecordSequenceBtn.IsEnabled = true;
        RecordChordBtn.IsEnabled = true;
        RecordHint.Visibility = Visibility.Collapsed;
        SetCaptureMode(false);
    }

    private void ShowActionPreview(ActionStep action)
    {
        ActionTypeText.Text = action.Type == "chord" ? "同时输出" : "依次输出";
        ActionTypeLabel.Style = action.Type == "chord"
            ? (Style)FindResource("ActionLabelChordStyle")
            : (Style)FindResource("ActionLabelSequenceStyle");
        ActionDisplayText.Text = action.Display;
        ActionPreview.Visibility = Visibility.Visible;
    }

    // ── Helpers ──

    private static List<string> GetModifiersFromKeyboard()
    {
        var modifiers = new List<string>();
        // Use GetAsyncKeyState for specific L/R detection
        if ((GetAsyncKeyState(0xA0) & 0x8000) != 0) modifiers.Add("LShift");
        else if ((GetAsyncKeyState(0xA1) & 0x8000) != 0) modifiers.Add("RShift");
        else if ((GetAsyncKeyState(0x10) & 0x8000) != 0) modifiers.Add("Shift");

        if ((GetAsyncKeyState(0xA2) & 0x8000) != 0) modifiers.Add("LCtrl");
        else if ((GetAsyncKeyState(0xA3) & 0x8000) != 0) modifiers.Add("RCtrl");
        else if ((GetAsyncKeyState(0x11) & 0x8000) != 0) modifiers.Add("Ctrl");

        if ((GetAsyncKeyState(0xA4) & 0x8000) != 0) modifiers.Add("LAlt");
        else if ((GetAsyncKeyState(0xA5) & 0x8000) != 0) modifiers.Add("RAlt");
        else if ((GetAsyncKeyState(0x12) & 0x8000) != 0) modifiers.Add("Alt");

        if ((GetAsyncKeyState(0x5B) & 0x8000) != 0) modifiers.Add("LWin");
        else if ((GetAsyncKeyState(0x5C) & 0x8000) != 0) modifiers.Add("RWin");
        return modifiers;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;

    private void AutoScrollToEnd(ScrollViewer scrollViewer)
    {
        if (scrollViewer == null || scrollViewer.ScrollableWidth <= 0) return;

        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            scrollViewer.ScrollToRightEnd();
        }), System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    private void UpdateHotkeyPreview(string display)
    {
        HotkeyDisplayText.Text = display;
        HotkeyDisplayBox.Visibility = Visibility.Visible;
        HotkeyRecordingStatus.Visibility = Visibility.Collapsed;
        HotkeyRecordBtn.Content = "🎙 录制触发键";
        HotkeyRecordHint.Visibility = Visibility.Collapsed;
    }

    private void ResetCaptureBoxUI()
    {
        _isCapturingHotkey = false;
        HotkeyRecordingStatus.Visibility = Visibility.Collapsed;
        HotkeyRecordBtn.Content = "🎙 录制触发键";
        HotkeyRecordHint.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrEmpty(_capturedKey))
        {
            var display = HotkeyParser.BuildDisplay(_capturedKey, _capturedModifiers);
            HotkeyDisplayText.Text = display;
            HotkeyDisplayBox.Visibility = Visibility.Visible;
        }
    }

    private bool IsCapturing => _isCapturingHotkey || _capturingAction;

    private void CleanupMessageFilter()
    {
        ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
    }

    private Profile? _testProfile;

    private void ShowTestArea()
    {
        if (string.IsNullOrEmpty(_capturedKey)) return;

        TestKeycapText.Text = HotkeyParser.BuildDisplay(_capturedKey, _capturedModifiers);
        TestArea.Visibility = Visibility.Visible;

        // Create a temporary profile that binds to ContextKeys itself
        RemoveTestProfile();
        _testProfile = new Profile
        {
            Id = "__test__",
            Name = "【测试模式】",
            Enabled = true,
            Match = new WindowMatch
            {
                ProcessName = "ContextKeys",
                MatchMode = "process_only"
            },
            Rules = new List<HotkeyRule>
            {
                new()
                {
                    Name = "test",
                    Hotkey = new HotkeyDefinition
                    {
                        Key = _capturedKey,
                        Modifiers = new List<string>(_capturedModifiers),
                        Display = HotkeyParser.BuildDisplay(_capturedKey, _capturedModifiers)
                    },
                    SuppressOriginalKey = true,
                    Actions = _savedAction != null
                        ? new List<ActionStep> { _savedAction }
                        : new List<ActionStep>()
                }
            }
        };

        App.ConfigService.Settings.Profiles.Add(_testProfile);

        // Also add to the UI's Profiles collection so normal matching works
        var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (mainWindow?.DataContext is ViewModels.MainViewModel vm)
        {
            vm.Profiles.Add(_testProfile);
            vm.RefreshCurrentProfile();
        }
    }

    private void RemoveTestProfile()
    {
        if (_testProfile == null) return;
        App.ConfigService.Settings.Profiles.Remove(_testProfile);

        var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (mainWindow?.DataContext is ViewModels.MainViewModel vm)
        {
            vm.Profiles.Remove(_testProfile);
            vm.RefreshCurrentProfile();
        }

        _testProfile = null;
    }

    private void TestKeycap_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var preview = _savedAction != null
            ? FormatActionForPreview(_savedAction)
            : "（未录制输出动作）";
        TestOutputBox.Text = preview;
    }

    private static string FormatActionForPreview(ActionStep action)
    {
        if (action.Type == "sequence" && action.Keys != null)
        {
            var parts = new List<string>();
            foreach (var key in action.Keys)
                parts.Add(KeyDisplayToText(key));
            return string.Join("", parts);
        }
        if (action.Type == "chord" && action.Keys != null)
        {
            return "[" + string.Join("+", action.Keys) + "]";
        }
        if (action.Type == "delay")
        {
            return $"({action.Milliseconds}ms)";
        }
        return action.Display;
    }

    private static string KeyDisplayToText(string key)
    {
        return key switch
        {
            "Space" => " ",
            "Enter" => "↵",
            "Tab" => "→",
            "Backspace" => "⌫",
            "Escape" => "Esc",
            "Up" => "↑",
            "Down" => "↓",
            "Left" => "←",
            "Right" => "→",
            _ => key.Length == 1 ? key : $"[{key}]"
        };
    }

    private void SetCaptureMode(bool capturing)
    {
        SaveBtn.IsEnabled = !capturing;
        if (!capturing) SequenceStatus.Visibility = Visibility.Collapsed;

        if (capturing)
        {
            // Intercept Windows messages BEFORE the IME to capture letter keys
            ComponentDispatcher.ThreadFilterMessage += OnThreadFilterMessage;
        }
        else
        {
            ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
        }
    }

    /// <summary>
    /// Intercept WM_KEYDOWN at the Win32 message level, before the IME can consume
    /// letter keys for Chinese composition. This is the only reliable way to capture
    /// all keystrokes regardless of the active input method.
    /// </summary>
    private void OnThreadFilterMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message != WM_KEYDOWN && msg.message != WM_SYSKEYDOWN)
            return;

        var source = PresentationSource.FromVisual(this);
        if (source == null)
        {
            CleanupMessageFilter();
            return;
        }

        if (!_isCapturingHotkey && !_capturingAction)
            return;

        var key = KeyInterop.KeyFromVirtualKey((int)msg.wParam);
        var args = new System.Windows.Input.KeyEventArgs(
            Keyboard.PrimaryDevice,
            source,
            0,
            key);

        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        HandleCaptureKey(args);

        if (args.Handled)
            handled = true;
    }

    // ── Save / Cancel ──

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_capturedKey))
        {
            MessageBox.Show("请设置触发键。", "提示");
            return;
        }

        var display = HotkeyParser.BuildDisplay(_capturedKey, _capturedModifiers);

        // Check for duplicate trigger key within the same profile
        if (_existingRules != null)
        {
            var conflict = _existingRules.FirstOrDefault(r =>
                r.Id != _editingRuleId &&
                HotkeyParser.AreEqual(r.Hotkey.Key, r.Hotkey.Modifiers, _capturedKey, _capturedModifiers));
            if (conflict != null)
            {
                MessageBox.Show(
                    $"触发键【{display}】已被规则「{conflict.Name}」使用，请更换其他按键。",
                    "触发键冲突", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // Check against ALL other profiles' rules for global conflicts
        var allProfiles = App.ConfigService.Settings.Profiles;
        foreach (var profile in allProfiles)
        {
            if (profile.Rules == null) continue;

            bool isTargetGlobal = string.Equals(profile.Match?.ProcessName, "*", StringComparison.Ordinal);
            bool isCurrentGlobal = _isGlobalProfile;

            // Skip checking same rules
            foreach (var rule in profile.Rules)
            {
                if (rule == null) continue;
                var isSameRule = _existingRules?.Any(r => r.Id == rule.Id) ?? false;
                if (!isSameRule && HotkeyParser.AreEqual(rule.Hotkey.Key, rule.Hotkey.Modifiers, _capturedKey, _capturedModifiers))
                {
                    // Global vs Any: always warn
                    if (isTargetGlobal || isCurrentGlobal)
                    {
                        var targetDesc = isTargetGlobal ? "全局" : "";
                        MessageBox.Show(
                            $"触发键【{display}】已与{targetDesc}配置「{profile.Name}」中的规则「{rule.Name}」冲突。\n\n全局配置的快捷键会在所有窗口生效，请更换其他按键。",
                            "全局快捷键冲突", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }
        }
        var name = RuleNameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            name = _savedAction != null
                ? $"{display} = {_savedAction.Display}"
                : display;
        }

        var actions = new List<ActionStep>();
        if (_savedAction != null) actions.Add(_savedAction);

        ResultRule = new HotkeyRule
        {
            Name = name,
            Hotkey = new HotkeyDefinition
            {
                Key = _capturedKey,
                Modifiers = new List<string>(_capturedModifiers),
                Display = display
            },
            SuppressOriginalKey = SuppressKeyCheck.IsChecked == true,
            Actions = actions
        };

        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        CleanupMessageFilter();
        RemoveTestProfile();
        base.OnClosed(e);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── Window-level key handler (primary) ──

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // ThreadFilterMessage handles all capture at Win32 level now.
        // PreviewKeyDown is only here as a last-resort safety net, but we
        // must NOT call HandleCaptureKey here to avoid double-processing
        // (which can cause the capture state to be consumed twice).
        if (!_isCapturingHotkey && !_capturingAction)
        {
            if (e.Key == Key.Tab) return;
        }
    }

    private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) { }

    private void HandleCaptureKey(System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        // Action recording
        if (_capturingAction)
        {
            if (e.Key == Key.Escape)
            {
                FinishActionRecording();
                return;
            }

            var keyName = MapWpfKey(e.Key);
            if (string.IsNullOrEmpty(keyName) || keyName == "Escape")
                return;

            if (_isChordMode)
            {
                // Chord mode: ignore modifier keys, wait for the main key
                if (IsModifierKey(e.Key))
                    return;

                _actionKeys.Clear();
                _actionKeys.Add(keyName);
                _savedChordModifiers = new List<string>(GetModifiersFromKeyboard());
                SequenceStatusText.Text = "同时输出: " + (_savedChordModifiers.Count > 0 
                    ? string.Join(" + ", _savedChordModifiers) + " + " + keyName 
                    : keyName);
                AutoScrollToEnd(SequenceStatusScroll);
            }
            else
            {
                // Sequence mode: record multiple keys, user presses Esc to finish
                if (!IsModifierKey(e.Key))
                {
                    _actionKeys.Add(keyName);
                    SequenceStatusText.Text = "已录入: " + string.Join(" → ", _actionKeys);
                    AutoScrollToEnd(SequenceStatusScroll);
                }
            }
            return;
        }

        // Hotkey capture
        if (_isCapturingHotkey)
        {
            var modifiers = GetModifiersFromKeyboard();
            if (IsModifierKey(e.Key))
            {
                return;
            }

            var keyName = MapWpfKey(e.Key);
            if (string.IsNullOrEmpty(keyName))
                return;

            _capturedKey = keyName;
            _capturedModifiers = modifiers;
            var display = HotkeyParser.BuildDisplay(keyName, modifiers);
            UpdateHotkeyPreview(display);

            var reserved = new[] { "Ctrl+Alt+Del", "Win+L", "Alt+Tab", "Ctrl+Shift+Esc", "Win+D", "Win+Tab" };
            HotkeyWarning.Visibility = reserved.Contains(display) ? Visibility.Visible : Visibility.Collapsed;

            _isCapturingHotkey = false;
            ResetCaptureBoxUI();
            SetCaptureMode(false);

            // Show test area if action already recorded
            if (_savedAction != null)
                ShowTestArea();
            return;
        }
    }

    // ── Key mapping ──

    private static string MapWpfKey(Key key) => key switch
    {
        Key.LeftCtrl or Key.RightCtrl => "Ctrl",
        Key.LeftShift or Key.RightShift => "Shift",
        Key.LeftAlt or Key.RightAlt => "Alt",
        Key.LWin or Key.RWin => "Win",
        Key.F1 => "F1", Key.F2 => "F2", Key.F3 => "F3", Key.F4 => "F4",
        Key.F5 => "F5", Key.F6 => "F6", Key.F7 => "F7", Key.F8 => "F8",
        Key.F9 => "F9", Key.F10 => "F10", Key.F11 => "F11", Key.F12 => "F12",
        Key.F13 => "F13", Key.F14 => "F14", Key.F15 => "F15", Key.F16 => "F16",
        Key.F17 => "F17", Key.F18 => "F18", Key.F19 => "F19", Key.F20 => "F20",
        Key.F21 => "F21", Key.F22 => "F22", Key.F23 => "F23", Key.F24 => "F24",
        Key.Up => "Up", Key.Down => "Down", Key.Left => "Left", Key.Right => "Right",
        Key.Home => "Home", Key.End => "End", Key.PageUp => "PageUp", Key.PageDown => "PageDown",
        Key.Insert => "Insert", Key.Delete => "Delete",
        Key.Back => "Backspace", Key.Tab => "Tab",
        Key.Capital => "CapsLock", Key.Escape => "Escape",
        Key.Return => "Enter", Key.Space => "Space",
        Key.PrintScreen => "PrintScreen", Key.Scroll => "ScrollLock", Key.Pause => "Pause",
        Key.NumPad0 => "Num0", Key.NumPad1 => "Num1", Key.NumPad2 => "Num2",
        Key.NumPad3 => "Num3", Key.NumPad4 => "Num4", Key.NumPad5 => "Num5",
        Key.NumPad6 => "Num6", Key.NumPad7 => "Num7", Key.NumPad8 => "Num8",
        Key.NumPad9 => "Num9",
        Key.Add => "NumAdd", Key.Subtract => "NumSubtract",
        Key.Multiply => "NumMultiply", Key.Divide => "NumDivide",
        Key.Decimal => "NumDecimal", Key.NumLock => "NumLock",
        Key.VolumeUp => "VolumeUp", Key.VolumeDown => "VolumeDown", Key.VolumeMute => "VolumeMute",
        Key.MediaPlayPause => "MediaPlayPause", Key.MediaNextTrack => "MediaNextTrack",
        Key.MediaPreviousTrack => "MediaPreviousTrack", Key.MediaStop => "MediaStop",
        Key.OemMinus => "Minus", Key.OemPlus => "Equal",
        Key.OemOpenBrackets => "LeftBracket", Key.OemCloseBrackets => "RightBracket",
        Key.OemBackslash => "Backslash",
        Key.OemSemicolon => "Semicolon", Key.OemQuotes => "Quote",
        Key.OemComma => "Comma", Key.OemPeriod => "Period",
        Key.OemQuestion => "Slash", Key.OemTilde => "Backtick",
        _ when key >= Key.A && key <= Key.Z => key.ToString(),
        _ when key >= Key.D0 && key <= Key.D9 => key.ToString().Last().ToString(),
        _ => string.Empty
    };

    private void SetIcon()
    {
        try
        {
            var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LKey.ico");
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
        catch { }
    }
}
