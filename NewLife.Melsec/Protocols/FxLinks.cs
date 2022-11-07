using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace NewLife.Melsec.Protocols;

/// <summary>三菱PLC计算机链路协议</summary>
public class FxLinks : DisposeBase
{
    #region 属性
    /// <summary>网络超时。发起请求后等待响应的超时时间，默认3000ms</summary>
    [Description("网络超时。发起请求后等待响应的超时时间，默认3000ms")]
    public Int32 Timeout { get; set; } = 3000;

    #endregion

    #region 构造
    protected override void Dispose(Boolean disposing) => base.Dispose(disposing);
    #endregion

    #region 方法
    public void Open()
    {

    }

    public void Close()
    {

    }

    public Object Read(String address, Int32 count)
    {
        return null;
    }

    public Object Write(String address, Object value)
    {
        return null;
    }
    #endregion
}