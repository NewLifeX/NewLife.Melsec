using NewLife.IoT.Drivers;

namespace NewLife.Melsec.Drivers;

/// <summary>Melsec参数</summary>
public class MelsecParameter : IDriverParameter
{
    /// <summary>地址</summary>
    public String Address { get; set; }

    /// <summary>数据格式。ABCD/BADC/CDAB/DCBA</summary>
    public String DataFormat { get; set; }
}