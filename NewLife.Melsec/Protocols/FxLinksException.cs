namespace NewLife.Melsec.Protocols;

/// <summary>FxLinks异常</summary>
public class FxLinksException : Exception
{
    /// <summary>异常代码</summary>
    public ErrorCodes ErrorCode { get; set; }

    /// <summary>
    /// 实例化异常
    /// </summary>
    /// <param name="errorCode"></param>
    /// <param name="message"></param>
    public FxLinksException(ErrorCodes errorCode, String message) : base(message) => ErrorCode = errorCode;
}