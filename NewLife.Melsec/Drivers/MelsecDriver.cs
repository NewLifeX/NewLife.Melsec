using System.ComponentModel;
using HslCommunication.Core;
using HslCommunication.Profinet.Melsec;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Melsec.Drivers
{
    /// <summary>
    /// 三菱PLC驱动
    /// </summary>
    [Driver("MelsecPLC")]
    [DisplayName("三菱PLC")]
    public class MelsecDriver : IDriver
    {
        /// <summary>
        /// 数据顺序
        /// </summary>
        private readonly DataFormat dataFormat = DataFormat.DCBA;

        private MelsecMcNet _plcNet;

        /// <summary>
        /// 打开通道数量
        /// </summary>
        private Int32 _nodes;


        /// <summary>
        /// 从点位中解析地址
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public virtual String GetAddress(IPoint point)
        {
            if (point == null) throw new ArgumentException("点位信息不能为空！");

            // 去掉冒号后面的位域
            var addr = point.Address;
            var p = addr.IndexOf(':');
            if (p > 0) addr = addr[..p];

            return addr;
        }

        /// <summary>
        /// 打开通道。一个ModbusTcp设备可能分为多个通道读取，需要共用Tcp连接，以不同节点区分
        /// </summary>
        /// <param name="channel">通道</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public virtual INode Open(IChannel channel, IDictionary<String, Object> parameters)
        {
            var address = parameters["Address"] as String;
            if (address.IsNullOrEmpty()) throw new ArgumentException("参数中未指定地址address");

            var i = address.IndexOf(':');
            if (i < 0) throw new ArgumentException($"参数中地址address格式错误:{address}");

            var node = new MelsecNode
            {
                Address = address,
                Channel = channel,
            };

            if (_plcNet == null)
            {
                lock (this)
                {
                    if (_plcNet == null)
                    {
                        _plcNet = new MelsecMcNet
                        {
                            ConnectTimeOut = 3000,

                            IpAddress = address[..i],
                            Port = address[(i + 1)..].ToInt(),
                        };
                        _plcNet.ByteTransform.DataFormat = dataFormat;

                        var connect = _plcNet.ConnectServer();

                        if (!connect.IsSuccess) throw new Exception($"连接失败：{connect.Message}");
                    }

                    Interlocked.Increment(ref _nodes);
                }
            }

            return node;
        }

        /// <summary>
        /// 关闭设备驱动
        /// </summary>
        /// <param name="node"></param>
        public void Close(INode node)
        {
            if (Interlocked.Decrement(ref _nodes) <= 0)
            {
                _plcNet?.ConnectClose();
                _plcNet.TryDispose();
                _plcNet = null;
            }
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
        /// <param name="points">点位集合，Address属性地址示例：D100、C100、W100、H100</param>
        /// <returns></returns>
        public virtual IDictionary<String, Object> Read(INode node, IPoint[] points)
        {
            var dic = new Dictionary<String, Object>();

            if (points == null || points.Length == 0) return dic;

            foreach (var point in points)
            {
                var name = point.Name;
                var addr = GetAddress(point);
                var length = point.Length;
                var data = _plcNet.Read(addr, (UInt16)(length / 2));
                if (!data.IsSuccess) throw new Exception($"读取数据失败：{data.ToJson()}");
                dic[name] = data.Content;
            }

            return dic;
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
        /// <param name="point">点位，Address属性地址示例：D100、C100、W100、H100</param>
        /// <param name="value">数据</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public virtual Object Write(INode node, IPoint point, Object value)
        {
            var addr = GetAddress(point);
            var res = value switch
            {
                Int32 v1 => _plcNet.Write(addr, v1),
                String v2 => _plcNet.Write(addr, v2),
                Boolean v3 => _plcNet.Write(addr, v3),
                Byte[] v4 => _plcNet.Write(addr, v4),
                _ => throw new ArgumentException("暂不支持写入该类型数据！"),
            };
            return res;
        }

        /// <summary>
        /// 控制设备，特殊功能使用
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parameters"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Control(INode node, IDictionary<String, Object> parameters) => throw new NotImplementedException();

        #region 日志
        /// <summary>链路追踪</summary>
        public ITracer Tracer { get; set; }

        /// <summary>日志</summary>
        public ILog Log { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}
