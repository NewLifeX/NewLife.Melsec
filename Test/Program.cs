using NewLife;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Melsec.Drivers;
using NewLife.Net;
using NewLife.Serialization;

XTrace.UseConsole();

// var ps = SerialTransport.GetPortNames();
// Console.WriteLine("请选择串口：");

// for (var i = 0; i < ps.Length; i++)
// {
//     Console.WriteLine($"{i}、{ps[i]}");
// }

// var idx = Int32.Parse(Console.ReadLine());

// var cfg = SerialPortConfig.Current;

// var pt = new SerialTransport();
// pt.PortName = ps[idx];
// pt.BaudRate = cfg.BaudRate;
// pt.Parity = cfg.Parity;
// pt.DataBits = cfg.DataBits;
// pt.StopBits = cfg.StopBits;
// pt.Disconnected += Pt_Disconnected;
// pt.Received += Pt_Received;

// void Pt_Disconnected(Object? sender, EventArgs e)
// {
//     Console.WriteLine("断开连接");
//     pt.Disconnected -= Pt_Disconnected;
//     pt.Received -= Pt_Received;
//     pt.Close();
// }



// void Pt_Received(Object? sender, ReceivedEventArgs e)
// {
//     var data = e.Packet.ReadBytes();
//     Console.WriteLine(data.ToHex());
// }
// pt.Open();

// Console.WriteLine(ps[idx]);
// Console.WriteLine(pt.Serial.IsOpen);
// Console.ReadLine();

Console.WriteLine("请选择协议：");
Console.WriteLine("1、MCQna3E");
Console.WriteLine("2、FxLinks485");

var mode = Console.ReadLine();
var pm = new MelsecParameter();

if (mode == "1")
{
    Console.WriteLine("服务端地址默认为：127.0.0.1:6000，保持默认请回车开始连接，否则请输入服务端地址：");
    var address = Console.ReadLine();

    if (address == null || address == "") address = "127.0.0.1:6000";

    pm.Protocol = Protocol.MCQna3E;
    pm.Address = address;
}
else if (mode == "2")
{
    var ps = SerialTransport.GetPortNames();
    Console.WriteLine("请选择串口：");

    for (var i = 0; i < ps.Length; i++)
    {
        Console.WriteLine($"{i}、{ps[i]}");
    }

    var idx = Int32.Parse(Console.ReadLine());

    pm.Protocol = Protocol.FxLinks485;
    pm.Baudrate = 9600;
    pm.PortName = ps[idx];
}

//var driver = new MelsecDriver();
var driver = new FxLinksDriver();

var node = driver.Open(null, pm);

// 测试打开两个通道
node = driver.Open(null, pm);

if (mode == "1") Console.WriteLine($"连接成功=>{pm.Address}！");
else Console.WriteLine($"连接成功=>{pm.PortName}！");


Console.WriteLine("请输入整数值，按q退出：");


var str = Console.ReadLine();
var point = new Point
{
    Name = "test",
    Address = "D232",
    Type = "ushort",
    Length = 2
};

do
{
    // 写入
    var data = BitConverter.GetBytes(Int32.Parse(str));


    // var res = (OperateResult)driver.Write(node, point, Int16.Parse(str));

    // Console.WriteLine($"写入结果：{res.ToJson()}");

    // point.Address = "Y" + str;
    // 读取
    var dic = driver.Read(node, new[] { point });
    Console.WriteLine($"读取结果1：{dic.ToJson()}");

    var data1 = dic[point.Name] as Byte[];

    //Console.WriteLine($"读取结果：{BitConverter.ToInt32(data1)}");
    Console.WriteLine($"读取结果：{data1.ToHex()}");
    Console.WriteLine($"");
    Console.WriteLine("请输入整数值，按q退出：");

} while ((str = Console.ReadLine()) != "q");

// 断开连接
driver.Close(node);
driver.Close(node);


public class Point : IPoint
{
    public String Name { get; set; }
    public String Address { get; set; }
    public String Type { get; set; }
    public Int32 Length { get; set; }
}