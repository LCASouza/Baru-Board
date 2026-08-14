using Avalonia.Controls;
using Avalonia.Interactivity;
using BaruBoard.Core.Exporting;

namespace BaruBoard.App.Views;

public sealed record ExportChoice(ExportRegionKind Region, double Scale, bool TransparentBackground);

public partial class ExportDialog : Window
{
    private Func<ExportRegionKind, double, ExportPlan?> _planner = (_, _) => null;

    public ExportDialog()
    {
        InitializeComponent();
    }

    public static Task<ExportChoice?> ShowAsync(
        Window owner,
        bool hasSelection,
        Func<ExportRegionKind, double, ExportPlan?> planner)
    {
        var dialog = new ExportDialog { _planner = planner };
        dialog.SelectionOption.IsEnabled = hasSelection;

        foreach (var option in new[]
                 {
                     dialog.ContentOption, dialog.SelectionOption, dialog.VisibleOption,
                     dialog.Scale1Option, dialog.Scale2Option, dialog.Scale3Option,
                 })
        {
            option.IsCheckedChanged += (_, _) => dialog.UpdateSummary();
        }

        dialog.UpdateSummary();
        return dialog.ShowDialog<ExportChoice?>(owner);
    }

    private ExportRegionKind SelectedRegion =>
        SelectionOption.IsChecked == true ? ExportRegionKind.Selection :
        VisibleOption.IsChecked == true ? ExportRegionKind.VisibleArea :
        ExportRegionKind.Content;

    private double SelectedScale =>
        Scale3Option.IsChecked == true ? 3.0 :
        Scale2Option.IsChecked == true ? 2.0 :
        1.0;

    // The effective size is shown before confirming, including when the limits
    // force a lower scale than the one asked for.
    private void UpdateSummary()
    {
        var scale = SelectedScale;
        if (_planner(SelectedRegion, scale) is not { } plan)
        {
            SummaryText.Text = "Nada a exportar nesta região.";
            ConfirmButton.IsEnabled = false;
            return;
        }

        ConfirmButton.IsEnabled = true;
        SummaryText.Text = plan.WasScaleReduced
            ? $"A imagem será gerada com {plan.PixelWidth} × {plan.PixelHeight} px. " +
              $"A escala foi reduzida de {scale:0.#}× para {plan.EffectiveScale:0.##}× para respeitar o limite de tamanho."
            : $"A imagem será gerada com {plan.PixelWidth} × {plan.PixelHeight} px.";
    }

    private void OnConfirm(object? sender, RoutedEventArgs e) =>
        Close(new ExportChoice(SelectedRegion, SelectedScale, TransparentOption.IsChecked == true));

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
}
