using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using BaruBoard.App.Controls;
using BaruBoard.App.Session;
using BaruBoard.App.ViewModels;
using BaruBoard.Core.Tools;

namespace BaruBoard.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly TextEditController _textEditController;
    private readonly BoardSession _session;
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        _textEditController = new TextEditController(Board, TextEditor);
        _session = new BoardSession(Board, _textEditController, this);
        _viewModel = new MainWindowViewModel(_session);
        DataContext = _viewModel;

        Board.TextEditRequested += (element, isNew) => _textEditController.BeginEdit(element, isNew);
        Board.FilesDropped += (paths, worldPoint) => _ = _session.ImportImagesAsync(paths, worldPoint);
        Board.ActiveToolChanged += OnCanvasToolChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.ExitRequested += Close;

        Loaded += async (_, _) =>
        {
            Board.Focus();
            await _session.InitializeAsync();
        };
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closeConfirmed)
            return;

        e.Cancel = true;
        _ = ConfirmAndCloseAsync();
    }

    private async Task ConfirmAndCloseAsync()
    {
        if (!await _session.TryCloseAsync())
            return;

        _closeConfirmed = true;
        Close();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.SelectedTool) || _viewModel.SelectedTool is not { } option)
            return;

        Board.ActivateTool(option.Kind);

        // Clicking the toolbar moves focus to the list; the board needs it back
        // or every keyboard shortcut stops working. An inline text editor keeps
        // the focus it just took, otherwise the edit would end before the first
        // keystroke.
        Dispatcher.UIThread.Post(() =>
        {
            if (!_textEditController.IsEditing)
                Board.Focus();
        });
    }

    private void OnCanvasToolChanged(ToolKind kind)
    {
        var option = _viewModel.ToolOptions.FirstOrDefault(o => o.Kind == kind);
        if (option is not null)
            _viewModel.SelectedTool = option;
    }
}
