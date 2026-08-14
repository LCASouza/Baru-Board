using BaruBoard.Core.Boards;

namespace BaruBoard.Storage.Serialization;

/// <summary>
/// Everything needed to write a board, fully detached from the live document.
/// Assets are immutable by construction, so carrying the instances is safe.
/// </summary>
public sealed record BoardSnapshot(BoardFileDto Board, IReadOnlyList<BoardAsset> Assets);
