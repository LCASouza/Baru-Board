using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BaruBoard.App.Controls;
using BaruBoard.App.Rendering;
using BaruBoard.App.Views;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Exporting;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;
using BaruBoard.Storage.Autosave;
using BaruBoard.Storage.Files;
using BaruBoard.Storage.RecentFiles;
using BaruBoard.Storage.Serialization;

namespace BaruBoard.App.Session;

/// <summary>
/// Owns the open board's file identity: new, open, save, autosave and recovery.
/// Every file operation snapshots the document on the UI thread before any I/O,
/// and only touches session state once the operation has actually succeeded.
/// </summary>
public sealed class BoardSession
{
    private static readonly FilePickerFileType BoardFileType = new("Quadro do Baru Board")
    {
        Patterns = ["*.baru"],
    };

    private static readonly FilePickerFileType ImageFileType = new("Imagens")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"],
    };

    private static readonly FilePickerFileType PngFileType = new("Imagem PNG")
    {
        Patterns = ["*.png"],
    };

    private static readonly TimeSpan AutosaveDebounce = TimeSpan.FromSeconds(3);

    private readonly BoardCanvas _canvas;
    private readonly TextEditController _textEdit;
    private readonly Window _owner;
    private readonly BoardFileService _fileService = new();
    private readonly BoardFileState _state = new();
    private readonly RecentFilesService _recentFiles = new(AppPaths.RecentFilesIndex);
    private readonly RecoveryStore _recovery = new(AppPaths.RecoveryDirectory);
    private readonly AutosaveService _autosave;

    private int _sessionId;
    private bool _swappingDocument;

    public BoardSession(BoardCanvas canvas, TextEditController textEdit, Window owner)
    {
        _canvas = canvas;
        _textEdit = textEdit;
        _owner = owner;
        _autosave = new AutosaveService(AutosaveDebounce, AutosaveAsync);

        _state.MarkNewDocument();
        RecentFiles = _recentFiles.Load();
        _canvas.History.Changed += OnHistoryChanged;
    }

    public event Action? StateChanged;

    public bool IsDirty => _state.IsDirty(_canvas.History);

    public string? FilePath => _state.FilePath;

    public string DisplayName =>
        _state.FilePath is { } path ? Path.GetFileNameWithoutExtension(path) : "Sem título";

    public IReadOnlyList<string> RecentFiles { get; private set; }

    public bool IsGridVisible
    {
        get => _canvas.Grid.IsVisible;
        set
        {
            if (_canvas.Grid.IsVisible == value)
                return;

            _canvas.Grid.IsVisible = value;
            _canvas.InvalidateVisual();
        }
    }

    // Grid and snap are session preferences: they never touch the document, the
    // history or the dirty state.
    public bool IsSnapEnabled
    {
        get => _canvas.Grid.SnapEnabled;
        set => _canvas.Grid.SnapEnabled = value;
    }

#if DEBUG
    public void GenerateSyntheticBoard()
    {
        if (!BeginFileOperation())
            return;

        ApplyBoard(new BoardLoadResult(SyntheticBoard.Create(), default, 0.2), null, recovered: false);
        FitToScreen();
    }
#endif

    public bool IsDiagnosticsVisible
    {
        get => _canvas.Diagnostics.IsEnabled;
        set
        {
            if (_canvas.Diagnostics.IsEnabled == value)
                return;

            _canvas.Diagnostics.IsEnabled = value;
            _canvas.InvalidateVisual();
        }
    }

    public void FitToScreen()
    {
        if (_canvas.Document.GetContentBounds() is not { } content)
            return;

        var viewport = _canvas.Viewport;
        var framing = ViewportFraming.FitToContent(
            content,
            viewport.ViewportSize,
            ViewportFraming.DefaultPaddingDips,
            viewport.Options.MinZoom,
            viewport.Options.MaxZoom);

        if (framing is not { } result)
            return;

        viewport.Zoom = result.Zoom;
        viewport.Position = result.Position;
        _canvas.InvalidateVisual();
    }

    // Keeps the world point at the centre of the viewport in place, so changing
    // the scale never jumps to another region of the board.
    public void ZoomToActualSize()
    {
        var viewport = _canvas.Viewport;
        var size = viewport.ViewportSize;
        viewport.ZoomAt(new PointD(size.Width / 2, size.Height / 2), 1.0);
        _canvas.InvalidateVisual();
    }

    public async Task<bool> ExportPngAsync()
    {
        if (!BeginFileOperation())
            return false;

        if (_canvas.Document.Elements.Count == 0)
        {
            await ShowMessageAsync("Exportar", "O quadro está vazio.");
            return false;
        }

        // The selection is captured before the dialog so the export cannot be
        // configured against one set of elements and rendered with another.
        var selection = _canvas.Selection.Elements.ToList();
        var selectionBounds = _canvas.Selection.Bounds;

        ExportPlan? Planner(ExportRegionKind kind, double scale) => CreateExportPlan(kind, scale, selectionBounds);

        var choice = await ExportDialog.ShowAsync(_owner, selection.Count > 0, Planner);
        if (choice is null || Planner(choice.Region, choice.Scale) is not { } plan)
            return false;

        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar PNG",
            SuggestedFileName = DisplayName,
            DefaultExtension = "png",
            FileTypeChoices = [PngFileType],
        });

        if (file?.TryGetLocalPath() is not { } path)
            return false;

        try
        {
            BoardExporter.ExportPng(
                path,
                _canvas.Document,
                plan,
                choice.TransparentBackground,
                choice.Region == ExportRegionKind.Selection ? selection : null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync($"Não foi possível exportar {Path.GetFileName(path)}.", exception);
            return false;
        }

        return true;
    }

    private ExportPlan? CreateExportPlan(ExportRegionKind kind, double scale, RectD? selectionBounds)
    {
        var region = kind switch
        {
            ExportRegionKind.Content => _canvas.Document.GetContentBounds(),
            ExportRegionKind.Selection => selectionBounds,
            _ => _canvas.Viewport.VisibleWorldBounds,
        };

        if (region is not { } bounds)
            return null;

        // The visible area is already what the user framed; only content and
        // selection get breathing room around them.
        var margin = kind == ExportRegionKind.VisibleArea ? 0 : ExportSettings.MarginPixels;
        return ExportGeometry.CreatePlan(bounds, scale, margin);
    }

    public bool Align(AlignmentMode mode) => ApplyArrangement(
        EditingOperations.Align(_canvas.Selection, _canvas.History, mode));

    public bool Distribute(DistributionMode mode) => ApplyArrangement(
        EditingOperations.Distribute(_canvas.Selection, _canvas.History, mode));

    private bool ApplyArrangement(bool changed)
    {
        if (changed)
            _canvas.InvalidateVisual();

        return changed;
    }

    public async Task InitializeAsync()
    {
        var entries = _recovery.List();
        if (entries.Count == 0)
        {
            CenterViewportOnOrigin();
            _canvas.InvalidateVisual();
            RaiseStateChanged();
            return;
        }

        var entry = entries[0];
        var origin = entry.OriginalPath is { } path ? Path.GetFileName(path) : "um quadro sem arquivo";
        var choice = await ConfirmDialog.ShowAsync(
            _owner,
            "Recuperação",
            $"O aplicativo foi encerrado com alterações não salvas em {origin} " +
            $"({entry.SavedAt:g}). Deseja recuperá-las?",
            "Recuperar",
            "Descartar",
            null);

        if (choice == ConfirmChoice.Primary)
        {
            try
            {
                var result = await _recovery.LoadAsync(entry);
                ApplyBoard(result, entry.OriginalPath, recovered: true);
                return;
            }
            catch (Exception exception) when (IsFileFailure(exception))
            {
                await ShowErrorAsync("Não foi possível recuperar o quadro.", exception);
            }
        }

        _recovery.Clear();
        RaiseStateChanged();
    }

    public async Task<bool> NewAsync()
    {
        if (!BeginFileOperation())
            return false;

        if (!await ConfirmDiscardChangesAsync())
            return false;

        DiscardRecoveryOfCurrentDocument();
        ApplyBoard(new BoardLoadResult(new BoardDocument(), default, 1.0), null, recovered: false, centerOrigin: true);
        return true;
    }

    public async Task<bool> OpenAsync()
    {
        if (!BeginFileOperation())
            return false;

        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir quadro",
            AllowMultiple = false,
            FileTypeFilter = [BoardFileType],
        });

        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } path)
            return false;

        return await OpenPathAsync(path);
    }

    public async Task<bool> OpenPathAsync(string path)
    {
        if (!BeginFileOperation())
            return false;

        if (!await ConfirmDiscardChangesAsync())
            return false;

        BoardLoadResult result;
        try
        {
            result = await _fileService.OpenAsync(path);
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            await ShowErrorAsync($"Não foi possível abrir {Path.GetFileName(path)}.", exception);
            return false;
        }

        DiscardRecoveryOfCurrentDocument();
        ApplyBoard(result, path, recovered: false);
        _recentFiles.Add(path);
        RecentFiles = _recentFiles.Load();
        RaiseStateChanged();
        return true;
    }

    public async Task<bool> SaveAsync()
    {
        if (!BeginFileOperation())
            return false;

        return _state.FilePath is { } path ? await SaveToAsync(path) : await SaveAsAsync();
    }

    public async Task<bool> SaveAsAsync()
    {
        if (!BeginFileOperation())
            return false;

        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salvar quadro",
            SuggestedFileName = DisplayName,
            DefaultExtension = "baru",
            FileTypeChoices = [BoardFileType],
        });

        if (file?.TryGetLocalPath() is not { } path)
            return false;

        return await SaveToAsync(path);
    }

    public async Task<bool> ImportImagesAsync()
    {
        if (!BeginFileOperation())
            return false;

        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Inserir imagem",
            AllowMultiple = true,
            FileTypeFilter = [ImageFileType],
        });

        var paths = files.Select(file => file.TryGetLocalPath()).OfType<string>().ToList();
        return paths.Count > 0 && await ImportImagesAsync(paths, VisibleWorldCenter());
    }

    /// <summary>
    /// Inserts images centred on <paramref name="worldCenter"/>; the whole batch
    /// becomes a single history entry.
    /// </summary>
    public async Task<bool> ImportImagesAsync(IReadOnlyList<string> paths, PointD worldCenter)
    {
        if (_canvas.IsInteracting)
            return false;

        _textEdit.CommitIfActive();

        var imported = new List<ImportedImage>();
        var failed = new List<string>();
        foreach (var path in paths)
        {
            if (await ImageImporter.TryLoadAsync(path) is { } image)
                imported.Add(image);
            else
                failed.Add(Path.GetFileName(path));
        }

        if (imported.Count > 0)
            InsertImages(imported, worldCenter);

        if (failed.Count > 0)
        {
            await ConfirmDialog.ShowAsync(
                _owner,
                "Imagem não suportada",
                $"Não foi possível importar: {string.Join(", ", failed)}.",
                "OK",
                null,
                null);
        }

        return imported.Count > 0;
    }

    /// <summary>
    /// Returns whether the window may close, asking about unsaved work first.
    /// </summary>
    public async Task<bool> TryCloseAsync()
    {
        if (_canvas.IsInteracting)
            return false;

        _textEdit.CommitIfActive();
        if (!await ConfirmDiscardChangesAsync())
            return false;

        _autosave.Cancel();
        DiscardRecoveryOfCurrentDocument();
        return true;
    }

    private async Task<bool> SaveToAsync(string path)
    {
        var documentId = _canvas.Document.Id;
        var stateId = _canvas.History.CurrentStateId;
        _canvas.Document.Name = Path.GetFileNameWithoutExtension(path);
        var snapshot = BoardSerializer.CreateSnapshot(_canvas.Document, _canvas.Viewport);

        try
        {
            await _fileService.SaveAsync(path, snapshot);
        }
        catch (Exception exception) when (IsFileFailure(exception))
        {
            await ShowErrorAsync($"Não foi possível salvar {Path.GetFileName(path)}.", exception);
            return false;
        }

        _state.MarkSaved(path, stateId);
        _autosave.Cancel();
        _recovery.Remove(documentId);
        _recentFiles.Add(path);
        RecentFiles = _recentFiles.Load();
        RaiseStateChanged();
        return true;
    }

    private void InsertImages(IReadOnlyList<ImportedImage> images, PointD worldCenter)
    {
        var document = _canvas.Document;
        var additions = new List<AddedElement>(images.Count);

        for (var i = 0; i < images.Count; i++)
        {
            var image = images[i];
            var asset = document.AddAsset(image.Asset);
            var offset = EditingDefaults.PasteOffset * i;
            var bounds = new RectD(
                new PointD(
                    worldCenter.X - image.InitialSize.Width / 2 + offset,
                    worldCenter.Y - image.InitialSize.Height / 2 + offset),
                image.InitialSize);

            additions.Add(new AddedElement(
                new ImageElement(bounds, asset.Id),
                document.Elements.Count + i));
        }

        _canvas.History.Execute(new AddElementsCommand(document, additions));
        _canvas.Selection.SelectMany(additions.Select(addition => addition.Element));
        _canvas.InvalidateVisual();
    }

    private PointD VisibleWorldCenter()
    {
        var bounds = _canvas.Viewport.VisibleWorldBounds;
        return new PointD(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    private bool BeginFileOperation()
    {
        // A pointer gesture owns the board until release; saving mid-drag would
        // capture a half-applied operation.
        if (_canvas.IsInteracting)
            return false;

        // Whatever is being typed belongs to the document before it is written.
        _textEdit.CommitIfActive();
        return true;
    }

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!IsDirty)
            return true;

        var choice = await ConfirmDialog.ShowAsync(
            _owner,
            "Alterações não salvas",
            $"O quadro {DisplayName} possui alterações não salvas. Deseja salvá-las?",
            "Salvar",
            "Descartar",
            "Cancelar");

        return choice switch
        {
            ConfirmChoice.Primary => await SaveAsync(),
            ConfirmChoice.Secondary => true,
            _ => false,
        };
    }

    private void ApplyBoard(
        BoardLoadResult result, string? path, bool recovered, bool centerOrigin = false)
    {
        _autosave.Cancel();
        _sessionId++;
        _swappingDocument = true;
        try
        {
            TextMeasurement.Remeasure(result.Document);
            _canvas.Document = result.Document;
            _canvas.Viewport.Zoom = result.Zoom;
            if (centerOrigin)
                CenterViewportOnOrigin();
            else
                _canvas.Viewport.Position = result.ViewportPosition;
        }
        finally
        {
            _swappingDocument = false;
        }

        if (recovered)
            _state.MarkRecovered(path);
        else if (path is null)
            _state.MarkNewDocument();
        else
            _state.MarkOpened(path);

        _canvas.InvalidateVisual();
        RaiseStateChanged();
    }

    // An empty board opens with the world origin in the middle of the window.
    private void CenterViewportOnOrigin()
    {
        var viewport = _canvas.Viewport;
        var size = viewport.ViewportSize;
        viewport.Position = new PointD(
            -size.Width / 2 / viewport.Zoom,
            -size.Height / 2 / viewport.Zoom);
    }

    private void DiscardRecoveryOfCurrentDocument() => _recovery.Remove(_canvas.Document.Id);

    private void OnHistoryChanged()
    {
        if (_swappingDocument)
            return;

        RaiseStateChanged();

        if (IsDirty)
            _autosave.Notify();
        else
            _autosave.Cancel();
    }

    private async Task AutosaveAsync(CancellationToken cancellationToken)
    {
        var session = _sessionId;

        // The snapshot has to be taken where the document lives; the write that
        // follows must never touch the live entities.
        var snapshot = await Dispatcher.UIThread.InvokeAsync(() =>
            _sessionId == session ? BoardSerializer.CreateSnapshot(_canvas.Document, _canvas.Viewport) : null);

        if (snapshot is null || _sessionId != session || cancellationToken.IsCancellationRequested)
            return;

        await _recovery.SaveAsync(_state.FilePath, snapshot, cancellationToken);
    }

    private Task ShowMessageAsync(string title, string message) =>
        ConfirmDialog.ShowAsync(_owner, title, message, "OK", null, null);

    private Task ShowErrorAsync(string message, Exception exception) => ConfirmDialog.ShowAsync(
        _owner,
        "Erro",
        $"{message}\n\n{exception.Message}",
        "OK",
        null,
        null);

    private static bool IsFileFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or BoardFormatException;

    private void RaiseStateChanged() => StateChanged?.Invoke();
}
