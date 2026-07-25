namespace LimeMeta;

/// <summary>
/// LimeMeta 业务异常。
/// </summary>
public sealed class LimeMetaException : Exception
{
    public LimeMetaException(string message) : base(message)
    {
    }
}
