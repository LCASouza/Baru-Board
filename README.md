<div align="center">
  <img src="assets/branding/Icon-Baru-Board.svg" width="96" alt="Baru Board">
  <h1>Baru Board</h1>
  <p>A local-first desktop whiteboard for visual thinking.</p>
  <p><a href="README.pt-BR.md">Português (Brasil)</a></p>
</div>

## About

Baru Board is an open source desktop application for sketching, diagramming and
organising ideas on an effectively infinite canvas. Boards live in a single file
on your machine and the application works entirely offline.

It is built with C#, .NET 10 and [Avalonia](https://avaloniaui.net). The canvas
is rendered directly through Avalonia's `DrawingContext` in a custom control:
elements live in world-space coordinates and are drawn by a single renderer
rather than being backed by individual UI controls.

## Features

- Infinite canvas with pan and cursor-centred zoom
- Rectangles, ellipses, lines and arrows
- Text with in-place editing
- Freehand drawing with stroke smoothing, plus an eraser for ink
- Images imported from disk or dropped onto the board
- Selection, multi-selection and rubber-band selection
- Move, proportional image resize, alignment and distribution
- Adaptive grid with optional snapping
- Unlimited undo and redo
- Copy, paste and duplicate, including across boards
- Autosave with crash recovery
- PNG export of the whole board, the current selection or the visible area
- Recent files

## Current status

Version 1.0.0 is the first public source release. The application is functional
and covered by an automated test suite, but no pre-built binaries are published
yet — see [Running from source](#running-from-source).

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download) or newer
- Windows or Linux

## Running from source

```bash
git clone https://github.com/LCASouza/Baru-Board.git
cd Baru-Board
dotnet run --project src/BaruBoard.App
```

## Basic usage

Boards start empty. Pick a tool from the toolbar, draw on the canvas, then save
the board to a `.baru` file.

- **Create**: select a shape tool and drag on the canvas. A single click places
  an element at its default size.
- **Select**: with the selection tool, click an element, hold <kbd>Shift</kbd> or
  <kbd>Ctrl</kbd> to add or remove elements, or drag over empty space to select
  everything the rectangle touches.
- **Move and resize**: drag a selected element, or drag the handles that appear
  when a single element is selected. Images keep their aspect ratio.
- **Edit text**: double-click a text element. Confirm with
  <kbd>Ctrl</kbd>+<kbd>Enter</kbd> or by clicking elsewhere, cancel with
  <kbd>Esc</kbd>.
- **Save and open**: <kbd>Ctrl</kbd>+<kbd>S</kbd> and <kbd>Ctrl</kbd>+<kbd>O</kbd>.
- **Export**: <kbd>Ctrl</kbd>+<kbd>E</kbd>, then choose the region, the scale and
  whether the background should be transparent.

## Tools

| Tool | Shortcut | Behaviour |
| --- | --- | --- |
| Select | <kbd>V</kbd> | Select, move, resize and delete elements |
| Rectangle | <kbd>R</kbd> | Drag to draw a rectangle |
| Ellipse | <kbd>O</kbd> | Drag to draw an ellipse |
| Line | <kbd>L</kbd> | Drag to draw a straight line |
| Arrow | <kbd>A</kbd> | Drag to draw an arrow |
| Text | <kbd>T</kbd> | Click to place a text element and start typing |
| Pen | <kbd>P</kbd> | Draw freehand strokes |
| Eraser | <kbd>E</kbd> | Remove freehand strokes under the cursor |

## Navigation

| Action | Input |
| --- | --- |
| Pan | Middle mouse drag, or <kbd>Space</kbd> + left drag |
| Zoom | Mouse wheel, centred on the pointer |
| Fit board to window | <kbd>Ctrl</kbd>+<kbd>0</kbd> |
| Zoom to 100% | <kbd>Ctrl</kbd>+<kbd>1</kbd> |

## Keyboard shortcuts

| Action | Shortcut |
| --- | --- |
| New board | <kbd>Ctrl</kbd>+<kbd>N</kbd> |
| Open | <kbd>Ctrl</kbd>+<kbd>O</kbd> |
| Save | <kbd>Ctrl</kbd>+<kbd>S</kbd> |
| Save as | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd> |
| Export PNG | <kbd>Ctrl</kbd>+<kbd>E</kbd> |
| Undo | <kbd>Ctrl</kbd>+<kbd>Z</kbd> |
| Redo | <kbd>Ctrl</kbd>+<kbd>Y</kbd> or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> |
| Copy / Paste / Duplicate | <kbd>Ctrl</kbd>+<kbd>C</kbd> / <kbd>Ctrl</kbd>+<kbd>V</kbd> / <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Select all | <kbd>Ctrl</kbd>+<kbd>A</kbd> |
| Clear selection | <kbd>Esc</kbd> |
| Delete selection | <kbd>Delete</kbd> or <kbd>Backspace</kbd> |
| Suspend grid snapping | Hold <kbd>Alt</kbd> while dragging |

## File format

Boards are saved as `.baru` files. A board is a zip container holding
`board.json` and the images it references:

```text
board.baru
├── board.json
└── assets/
    └── <sha256>.png
```

`board.json` carries a `formatVersion` field, the board metadata, the saved
viewport and the list of elements. Assets are content-addressed by the SHA-256
of their bytes, which is verified when a board is opened. Files written by
earlier versions of the format are still read.

## Local-first

Baru Board has no accounts, no servers and no network access. Boards are plain
files you own, and application data such as recent files and autosaved recovery
copies stays in your user profile directory.

## Building

```bash
dotnet build
```

## Running tests

```bash
dotnet test
```

The project ships an automated test suite covering the geometry, the viewport
math, the editing commands, the file format and the export calculations.

## Roadmap

Planned directions, without committed dates:

- Element colours and richer text styling
- Pasting images from the system clipboard
- Sticky notes and checklist cards
- Packaging and public desktop releases for Windows and Linux

## Contributing

Issues and pull requests are welcome. Please make sure `dotnet build` and
`dotnet test` succeed before opening a pull request.

## License

Released under the [MIT License](LICENSE).

## Language

[Português (Brasil)](README.pt-BR.md)
