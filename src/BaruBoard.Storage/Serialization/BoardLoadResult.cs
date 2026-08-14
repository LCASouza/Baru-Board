using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Storage.Serialization;

public sealed record BoardLoadResult(BoardDocument Document, PointD ViewportPosition, double Zoom);
