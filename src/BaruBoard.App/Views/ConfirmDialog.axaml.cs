using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BaruBoard.App.Views;

public enum ConfirmChoice
{
    Primary,
    Secondary,
    Cancel,
}

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public static Task<ConfirmChoice> ShowAsync(
        Window owner,
        string title,
        string message,
        string primaryLabel,
        string? secondaryLabel,
        string? cancelLabel)
    {
        var dialog = new ConfirmDialog { Title = title };
        dialog.MessageText.Text = message;
        dialog.PrimaryButton.Content = primaryLabel;

        if (secondaryLabel is null)
            dialog.SecondaryButton.IsVisible = false;
        else
            dialog.SecondaryButton.Content = secondaryLabel;

        if (cancelLabel is null)
            dialog.CancelButton.IsVisible = false;
        else
            dialog.CancelButton.Content = cancelLabel;

        return dialog.ShowDialog<ConfirmChoice>(owner);
    }

    private void OnPrimary(object? sender, RoutedEventArgs e) => Close(ConfirmChoice.Primary);

    private void OnSecondary(object? sender, RoutedEventArgs e) => Close(ConfirmChoice.Secondary);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(ConfirmChoice.Cancel);
}
