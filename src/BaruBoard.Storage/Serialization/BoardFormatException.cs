namespace BaruBoard.Storage.Serialization;

public sealed class BoardFormatException : Exception
{
    public BoardFormatException(string message)
        : base(message)
    {
    }

    public BoardFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
