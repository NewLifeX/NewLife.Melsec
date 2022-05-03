using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Melsec.Drivers;

Console.WriteLine("服务端地址默认为：127.0.0.1:6000，保持默认请回车开始连接，否则请输入服务端地址：");
var address = Console.ReadLine();

if (address == null || address == "") address = "127.0.0.1:6000";

var driver = new MelsecDriver();
var pm = new MelsecParameter { Address = address };
var node = driver.Open(null, pm);

// 测试打开两个通道
node = driver.Open(null, pm);

Console.WriteLine($"连接成功=>{address}！");

Console.WriteLine("请输入整数值，按q退出：");

var str = Console.ReadLine();

do
{
    // 写入
    var data = BitConverter.GetBytes(Int32.Parse(str));
    var point = new Point
    {
        Name = "test",
        Address = "D100:50",
        Type = "Int32",
        Length = data.Length
    };

    var res = driver.Write(node, point, data);

    // 读取
    var dic = driver.Read(node, new[] { point });
    var data1 = dic[point.Name] as Byte[];

    Console.WriteLine($"读取结果：{BitConverter.ToInt32(data1)}");
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