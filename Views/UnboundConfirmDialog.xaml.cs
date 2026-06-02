using System.Windows;

namespace ContextKeys.Views;

public partial class UnboundConfirmDialog : Window
{
    public bool ShouldBind { get; private set; }

    public UnboundConfirmDialog()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
    }

    private void Bind_Click(object sender, RoutedEventArgs e)
    {
        ShouldBind = true;
        DialogResult = true;
        Close();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ShouldBind = false;
        DialogResult = true;
        Close();
    }
}