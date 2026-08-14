using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BaruBoard.App.Session;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Tools;

namespace BaruBoard.App.ViewModels;

public sealed record ToolOption(ToolKind Kind, string Label);

public sealed record RecentFileItem(string Header, ICommand Command);

public partial class MainWindowViewModel : ObservableObject
{
    private readonly BoardSession _session;

    public MainWindowViewModel(BoardSession session)
    {
        _session = session;
        Title = string.Empty;
        RecentFiles = [];
        SelectedTool = ToolOptions[0];
        IsGridVisible = session.IsGridVisible;
        IsSnapEnabled = session.IsSnapEnabled;

        _session.StateChanged += RefreshFromSession;
        RefreshFromSession();
    }

    public event Action? ExitRequested;

    public IReadOnlyList<ToolOption> ToolOptions { get; } =
    [
        new(ToolKind.Selection, "Selecionar"),
        new(ToolKind.Rectangle, "Retângulo"),
        new(ToolKind.Ellipse, "Elipse"),
        new(ToolKind.Line, "Linha"),
        new(ToolKind.Arrow, "Seta"),
        new(ToolKind.Text, "Texto"),
        new(ToolKind.Pen, "Caneta"),
        new(ToolKind.Eraser, "Borracha"),
    ];

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial ToolOption SelectedTool { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<RecentFileItem> RecentFiles { get; set; }

    [ObservableProperty]
    public partial bool IsGridVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSnapEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsDiagnosticsVisible { get; set; }

    public bool IsDebugBuild =>
#if DEBUG
        true;
#else
        false;
#endif

    partial void OnIsDiagnosticsVisibleChanged(bool value) => _session.IsDiagnosticsVisible = value;

    [RelayCommand]
    private Task ExportPngAsync() => _session.ExportPngAsync();

    [RelayCommand]
    private void FitToScreen() => _session.FitToScreen();

    [RelayCommand]
    private void ZoomToActualSize() => _session.ZoomToActualSize();

    [RelayCommand]
    private void GenerateSyntheticBoard()
    {
#if DEBUG
        _session.GenerateSyntheticBoard();
#endif
    }

    partial void OnIsGridVisibleChanged(bool value) => _session.IsGridVisible = value;

    partial void OnIsSnapEnabledChanged(bool value) => _session.IsSnapEnabled = value;

    [RelayCommand]
    private void Align(string mode)
    {
        if (Enum.TryParse<AlignmentMode>(mode, out var alignment))
            _session.Align(alignment);
    }

    [RelayCommand]
    private void Distribute(string mode)
    {
        if (Enum.TryParse<DistributionMode>(mode, out var distribution))
            _session.Distribute(distribution);
    }

    [RelayCommand]
    private Task NewBoardAsync() => _session.NewAsync();

    [RelayCommand]
    private Task OpenBoardAsync() => _session.OpenAsync();

    [RelayCommand]
    private Task SaveBoardAsync() => _session.SaveAsync();

    [RelayCommand]
    private Task SaveBoardAsAsync() => _session.SaveAsAsync();

    [RelayCommand]
    private Task InsertImageAsync() => _session.ImportImagesAsync();

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke();

    private void RefreshFromSession()
    {
        var marker = _session.IsDirty ? " •" : string.Empty;
        Title = $"{_session.DisplayName}{marker} — Baru Board";
        RecentFiles =
        [
            .. _session.RecentFiles.Select(path => new RecentFileItem(
                Path.GetFileName(path),
                new AsyncRelayCommand(() => _session.OpenPathAsync(path))))
        ];
    }
}
