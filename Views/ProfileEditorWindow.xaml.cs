using System.Windows;
using ContextKeys.Models;
using ContextKeys.Services;

namespace ContextKeys.Views;

public partial class ProfileEditorWindow : Window
{
    private readonly Profile _original;
    private readonly List<HotkeyRule> _rules = new();

    public Profile? ResultProfile { get; private set; }

    // Internal constructor for new profile
    public ProfileEditorWindow()
    {
        InitializeComponent();
        _original = new Profile { Name = "新配置" };
        ProfileNameBox.Text = string.Empty;
        UpdateProfileHint();
        Title = "新增配置";
    }

    // Constructor for editing existing profile
    public ProfileEditorWindow(Profile profile)
    {
        InitializeComponent();
        _original = profile ?? throw new ArgumentNullException(nameof(profile));

        ProfileNameBox.Text = profile.Name ?? string.Empty;
        UpdateProfileHint();
        Title = "编辑配置";

        // Set match info (defensive null checks)
        var match = profile.Match ?? new WindowMatch();
        WinProcessName.Text = string.IsNullOrEmpty(match.ProcessName) ? "-" : match.ProcessName;
        TitleContainsBox.Text = match.TitleContains ?? string.Empty;

        // Copy rules (defensive)
        if (profile.Rules != null)
        {
            foreach (var rule in profile.Rules)
            {
                if (rule != null)
                    _rules.Add(rule);
            }
        }
        RefreshRulesList();
    }

    private void ProfileNameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateProfileHint();
    }

    private void ProfileNameBox_GotFocus(object sender, RoutedEventArgs e)
    {
        ProfileNameHint.Visibility = Visibility.Collapsed;
    }

    private void ProfileNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        UpdateProfileHint();
    }

    private void UpdateProfileHint()
    {
        ProfileNameHint.Visibility = string.IsNullOrEmpty(ProfileNameBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void BindWindow_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WindowPickerDialog();
        if (dialog.ShowDialog() == true && dialog.SelectedWindow != null)
        {
            var info = dialog.SelectedWindow;
            WinProcessName.Text = info.ProcessName;
            TitleContainsBox.Text = info.Title;

            // Auto-fill profile name if it's still empty
            if (string.IsNullOrWhiteSpace(ProfileNameBox.Text))
            {
                ProfileNameBox.Text = $"{info.ProcessName} - {info.Title}";
                UpdateProfileHint();
            }
        }
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RuleEditorDialog();
        if (dialog.ShowDialog() == true && dialog.ResultRule != null)
        {
            _rules.Add(dialog.ResultRule);
            RefreshRulesList();
        }
    }

    private void EditRule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is HotkeyRule rule)
        {
            var dialog = new RuleEditorDialog(rule);
            if (dialog.ShowDialog() == true && dialog.ResultRule != null)
            {
                var index = _rules.IndexOf(rule);
                if (index >= 0)
                    _rules[index] = dialog.ResultRule;
                RefreshRulesList();
            }
        }
    }

    private void DeleteRule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is HotkeyRule rule)
        {
            var result = MessageBox.Show(
                $"确定要删除快捷键规则【{rule.Name}】吗？",
                "删除规则", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _rules.Remove(rule);
                RefreshRulesList();
            }
        }
    }

    private void RefreshRulesList()
    {
        RulesList.ItemsSource = null;
        RulesList.ItemsSource = _rules;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = ProfileNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("请输入配置名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            ProfileNameBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(WinProcessName.Text) || WinProcessName.Text == "-")
        {
            MessageBox.Show("请先绑定窗口。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Check for duplicate profile name (only for new profiles or name changes)
        var allProfiles = App.ConfigService.Settings.Profiles;
        if (allProfiles.Any(p => p.Id != _original.Id && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"配置名称【{name}】已存在，请使用其他名称。", "名称重复", MessageBoxButton.OK, MessageBoxImage.Warning);
            ProfileNameBox.Focus();
            ProfileNameBox.SelectAll();
            return;
        }

        // Always use the default match mode
        string titleContains = TitleContainsBox.Text?.Trim() ?? string.Empty;

        var result = new Profile
        {
            Id = _original.Id,
            Name = name,
            Enabled = _original.Enabled,
            Match = new WindowMatch
            {
                ProcessName = WinProcessName.Text,
                TitleContains = titleContains,
                MatchMode = "process_and_title_contains"
            },
            Rules = new List<HotkeyRule>(_rules)
        };

        ResultProfile = result;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
