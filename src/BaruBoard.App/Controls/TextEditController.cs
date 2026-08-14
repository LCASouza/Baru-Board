using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using BaruBoard.App.Rendering;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Geometry;

namespace BaruBoard.App.Controls;

/// <summary>
/// Coordinates the single transient TextBox used for in-place text editing.
/// While active, keyboard input stays in the editor and never reaches the canvas.
/// </summary>
public sealed class TextEditController
{
    private readonly BoardCanvas _canvas;
    private readonly TextBox _editor;

    private TextElement? _element;
    private bool _isNew;
    private bool _finishing;
    private string _originalText = string.Empty;
    private SizeD _originalSize;

    public TextEditController(BoardCanvas canvas, TextBox editor)
    {
        _canvas = canvas;
        _editor = editor;
        _editor.LostFocus += (_, _) => Commit();
        _editor.KeyDown += OnEditorKeyDown;
        _canvas.ViewportChanged += UpdatePlacement;
    }

    public bool IsEditing => _element is not null;

    public void BeginEdit(TextElement element, bool isNew)
    {
        if (_element is not null)
            Commit();

        _element = element;
        _isNew = isNew;
        _originalText = element.Text;
        _originalSize = element.Bounds.Size;
        _canvas.SetEditingElement(element);

        _editor.Text = element.Text;
        UpdatePlacement();
        _editor.IsVisible = true;
        _editor.Focus();
        _editor.CaretIndex = _editor.Text?.Length ?? 0;
    }

    /// <summary>
    /// Flushes an in-progress edit into the document. File operations call this
    /// so what the user sees in the editor is part of what gets saved.
    /// </summary>
    public void CommitIfActive() => Commit();

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            Commit();
            e.Handled = true;
        }
    }

    private void Commit()
    {
        if (_finishing || _element is null)
            return;

        _finishing = true;
        var element = _element;
        var text = _editor.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
            CommitEmpty(element);
        else
            CommitText(element, text);

        EndEdit();
    }

    private void CommitEmpty(TextElement element)
    {
        var index = _canvas.Document.IndexOf(element);
        RemoveElement(element);

        // Clearing the text of an existing element deletes it, and that deletion
        // has to be undoable; a brand new element was never really created.
        if (!_isNew && index >= 0)
            _canvas.History.Record(new RemoveElementsCommand(_canvas.Document, [new RemovedElement(element, index)]));
    }

    private void CommitText(TextElement element, string text)
    {
        element.Text = text;
        element.SetMeasuredSize(TextMeasurement.Measure(text, element.FontSize));
        _canvas.Selection.Select(element);

        if (_isNew)
        {
            var index = _canvas.Document.IndexOf(element);
            if (index >= 0)
                _canvas.History.Record(new AddElementCommand(_canvas.Document, element, index));
        }
        else if (!string.Equals(text, _originalText, StringComparison.Ordinal))
        {
            _canvas.History.Record(new ChangeTextCommand(
                element, _originalText, _originalSize, element.Text, element.Bounds.Size));
        }
    }

    private void Cancel()
    {
        if (_finishing || _element is null)
            return;

        _finishing = true;

        // An existing element is only written to on commit, so cancelling just
        // needs to discard a newly created one.
        if (_isNew)
            RemoveElement(_element);

        EndEdit();
    }

    private void RemoveElement(TextElement element)
    {
        _canvas.Document.RemoveElement(element);
        _canvas.Selection.Remove(element);
    }

    private void EndEdit()
    {
        _element = null;
        _editor.IsVisible = false;
        _canvas.SetEditingElement(null);
        _canvas.InvalidateVisual();
        _canvas.Focus();
        _finishing = false;
    }

    private void UpdatePlacement()
    {
        if (_element is null)
            return;

        var viewport = _canvas.Viewport;
        var screen = viewport.WorldToScreen(_element.Bounds.Position);
        _editor.Margin = new Thickness(screen.X, screen.Y, 0, 0);
        _editor.FontSize = Math.Max(_element.FontSize * viewport.Zoom, 1);
    }
}
