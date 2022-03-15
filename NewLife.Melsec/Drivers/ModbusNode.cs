using NewLife.IoT.Protocols;

namespace NewLife.IoT.Drivers;

/// <summary>
/// Modbus节点
/// </summary>
public class ModbusNode : INode
{
    /// <summary>主机地址</summary>
    public Byte Host { get; set; }

    /// <summary>读取功能码</summary>
    public FunctionCodes ReadCode { get; set; }

    /// <summary>写入功能码</summary>
    public FunctionCodes WriteCode { get; set; }

    /// <summary>通道</summary>
    public IChannel Channel { get; set; }
}