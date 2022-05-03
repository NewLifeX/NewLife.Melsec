using System.ComponentModel;
using NewLife.IoT.Drivers;

namespace NewLife.Melsec.Drivers;

/// <summary>Melsec参数</summary>
public class MelsecParameter : IDriverParameter
{
    /// <summary>地址。例如 127.0.0.1:6000</summary>
    [Description("地址。例如 127.0.0.1:6000")]
    public String Address { get; set; }

    /// <summary>数据格式。ABCD/BADC/CDAB/DCBA</summary>
    [Description("数据格式。ABCD/BADC/CDAB/DCBA")]
    public String DataFormat { get; set; }
}