namespace NewLife.Melsec.Protocols;

/// <summary>错误码</summary>
public enum ErrorCodes
{
    ///// <summary>常规错误</summary>
    //Normal = 0x00,

    ///// <summary>常规错误</summary>
    //Normal2 = 0x01,

    /// <summary>校验错误</summary>
    SumError = 0x02,

    /// <summary>通信计数器错误</summary>
    /// <remarks>Protocol error (the communication protocol does not conform to the format selected with D8120)</remarks>
    ProtocolError = 0x03,

    /// <summary>字符区域错误</summary>
    /// <remarks>Character area error (the character area is incorrectly defined, or the specified command is not available)</remarks>
    CharacterAreaError = 0x06,

    /// <summary>字符区域错误</summary>
    /// <remarks>Character error (the data to be written to a device consists of ASCII codes other than hexadecimal codes)</remarks>
    CharacterError = 0x07,

    /// <summary>PLC号错误</summary>
    /// <remarks>PLC number error (the PLC number is not set to “FF” or not available from this station)</remarks>
    PLCError = 0x0A,

    /// <summary>PLC号错误</summary>
    /// <remarks>PLC number error (the PLC number is not set to “FF” or not available from this station)</remarks>
    PLCError2 = 0x10,

    /// <summary>远程错误</summary>
    /// <remarks>Remote error (remote run/stop is disabled)</remarks>
    RemoteError = 0x18,
}
