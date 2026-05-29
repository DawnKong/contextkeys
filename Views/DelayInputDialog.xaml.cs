using System.Windows;
using System.Windows.Input;

namespace ContextKeys.Views;

public partial class DelayInputDialog : Window
{
    public int DelayMs { get; private set; }

    public DelayInputDialog()
    {
        InitializeComponent();
        DelayMsBox.Focus();
        DelayMsBox.SelectAll();
    }

    private void DelayMsBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow digits
        foreach (var c in e.Text)
        {
            if (!char.IsDigit(c))
            {
                e.Handled = true;
                return;
            }
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(DelayMsBox.Text, out var ms) && ms > 0)
        {
            DelayMs = ms;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("请输入有效的正整数。", "提示");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
