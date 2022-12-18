namespace NewLife.Melsec.Protocols;

/// <summary>错误码</summary>
public enum NetworkErrorCodes
{
    /// <summary>命令超时</summary>
    /// <remarks>After master station send request to save station, no answer passing comms time-out.</remarks>
    CommsTimeout = 0x01,

    /// <summary>站号错误</summary>
    /// <remarks>Station No. is not agreement between the master station and the slave station.</remarks>
    StationError = 0x02,

    /// <summary>通信计数器错误</summary>
    /// <remarks>Communication counter is not agreement between the master station and the slave station.</remarks>
    CommsCounterError = 0x03,

    /// <summary>通信格式错误</summary>
    /// <remarks>Communication format is not right from slave station.</remarks>
    CommsFormatError = 0x04,

    /// <summary>主机通信超时</summary>
    /// <remarks>After slave station send answer to master station, master station do not send request to next slave station.</remarks>
    MasterCommsTimeoutError = 0x11,

    /// <summary>主机通信格式错误</summary>
    /// <remarks>Communication format is not right from master station.</remarks>
    MasterCommsFormatError = 0x14,

    /// <summary>从机不存在</summary>
    /// <remarks>The station No. is not in this network.</remarks>
    NoSlave = 0x21,

    /// <summary>站号错误</summary>
    /// <remarks>Station No. is not agreement between the master station and the slave station.</remarks>
    StationError2 = 0x22,

    /// <summary>通信计数器错误</summary>
    /// <remarks>Communication counter is not agreement between the master station and the slave station.</remarks>
    CommsCounterError2 = 0x23,

    /// <summary>未收到通信参数</summary>
    /// <remarks>When slave station receive request from master station before communication parameter.</remarks>
    NotReceiveCommsParameter = 0x31,
}
