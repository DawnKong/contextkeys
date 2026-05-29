using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ContextKeys.Models;
using ContextKeys.Utils;

namespace ContextKeys.Views;

public partial class RuleEditorDialog : Window
{
    private bool _isCapturingHotkey;
    private string _capturedKey = string.Empty;
    private List<string> _capturedModifiers = new();

    private bool _capturingAction;
    private readonly List<string> _actionKeys = new();

    // Win32 message constants
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private ActionStep? _savedAction;

    public HotkeyRule? ResultRule { get; private set; }

    public RuleEditorDialog()
    {
        InitializeComponent();
        Title = "添加快捷键规则";
        UpdateRuleHint();
    }

    public RuleEditorDialog(HotkeyRule rule)
    {
        InitializeComponent();
        Title = "编辑快捷键规则";

        RuleNameBox.Text = rule.Name;
        SuppressKeyCheck.IsChecked = rule.SuppressOriginalKey;

        _capturedKey = rule.Hotkey.Key;
        _capturedModifiers = new List<string>(rule.Hotkey.Modifiers);
        var display = HotkeyParser.BuildDisplay(_capturedKey, _capturedModifiers);
        HotkeyDisplayText.Text = display;
        HotkeyDisplayBox.Visibility = Visibility.Visible;
        HotkeyPlaceholder.Visibility = Visibility.Collapsed;

        // Load single action
        if (rule.Actions is { Count: > 0 })
        {
            _savedAction = rule.Actions[0];
            ShowActionPreview(_savedAction);
        }

        UpdateRuleHint();
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

    private void HotkeyCapture_Click(object sender, MouseButtonEventArgs e)
    {
        StopActionRecording();

        _isCapturingHotkey = !_isCapturingHotkey;
        if (_isCapturingHotkey)
        {
            HotkeyCaptureBox.Background = (System.Windows.Media.Brush)FindResource("SelectedSurfaceBrush");
            HotkeyCaptureBox.BorderBrush = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
            HotkeyPlaceholder.Text = "请按下任意按键或组合键...";
            HotkeyDisplayBox.Visibility = Visibility.Collapsed;
            HotkeyPlaceholder.Visibility = Visibility.Visible;
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

    private void RecordAction_Click(object sender, RoutedEventArgs e)
    {
        if (_capturingAction)
        {
            // Already recording — finish it
            FinishActionRecording();
            return;
        }

        _isCapturingHotkey = false;
        ResetCaptureBoxUI();

        _capturingAction = true;
        _actionKeys.Clear();
        _savedAction = null;
        ActionPreview.Visibility = Visibility.Collapsed;

        SequenceStatus.Visibility = Visibility.Visible;
        SequenceStatusText.Text = "正在录制… 按 Esc 或点击「✓ 完成录制」结束。";
        RecordBtn.Content = "✓ 完成录制";
        RecordHint.Visibility = Visibility.Visible;
        SetCaptureMode(true);
    }

    private void FinishActionRecording()
    {
        _capturingAction = false;
        SequenceStatus.Visibility = Visibility.Collapsed;
        RecordBtn.Content = "🎙 录制输出";
        RecordHint.Visibility = Visibility.Collapsed;
        SetCaptureMode(false);

        if (_actionKeys.Count > 0)
        {
            var modifierKeys = GetModifiersFromKeyboard();
            if (modifierKeys.Count > 0)
            {
                var chordKeys = new List<string>();
                chordKeys.AddRange(modifierKeys);
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
        RecordBtn.Content = "🎙 录制输出";
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
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) modifiers.Add("Ctrl");
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) modifiers.Add("Alt");
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) modifiers.Add("Shift");
        if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) modifiers.Add("Win");
        return modifiers;
    }

    private static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;

    private void UpdateHotkeyPreview(string display)
    {
        HotkeyDisplayText.Text = display;
        HotkeyDisplayBox.Visibility = Visibility.Visible;
        HotkeyPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void ResetCaptureBoxUI()
    {
        HotkeyCaptureBox.Background = (System.Windows.Media.Brush)FindResource("MainSurfaceBrush");
        HotkeyCaptureBox.BorderBrush = (System.Windows.Media.Brush)FindResource("BorderStrongBrush");
        HotkeyPlaceholder.Text = "点击后按下任意按键或组合键...";
    }

    private bool IsCapturing => _isCapturingHotkey || _capturingAction;

    private void CleanupMessageFilter()
    {
        ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
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
            // Window was closed — detach this leaked handler immediately
            CleanupMessageFilter();
            return;
        }

        // Only capture when explicitly in capture mode
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
            if (!string.IsNullOrEmpty(keyName) && !IsModifierKey(e.Key) && keyName != "Escape")
            {
                _actionKeys.Add(keyName);
                SequenceStatusText.Text = "已录入: " + string.Join(" → ", _actionKeys);
            }
            return;
        }

        // Hotkey capture
        if (_isCapturingHotkey)
        {
            var modifiers = GetModifiersFromKeyboard();
            if (IsModifierKey(e.Key))
            {
                if (modifiers.Count > 0)
                    HotkeyPlaceholder.Text = string.Join(" + ", modifiers) + " + ...";
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
            return;
        }
    }

    // ── Key mapping ──

    private static string MapWpfKey(Key key) => key switch
    {
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
}
