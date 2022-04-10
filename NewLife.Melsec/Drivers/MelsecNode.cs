using NewLife.IoT.Protocols;

namespace NewLife.IoT.Drivers;

/// <summary>
/// 节点
/// </summary>
public class MelsecNode : INode
{
    /// <summary>主机地址</summary>
    public String Address { get; set; }

    /// <summary>通道</summary>
    public IChannel Channel { get; set; }
}