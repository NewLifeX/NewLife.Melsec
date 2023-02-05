using System.IO.Ports;
using NewLife;
using NewLife.Data;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Melsec.Drivers;

XTrace.UseConsole();

{
    // FxLinks485 读取 WR
    // 05 30 35 46 46 57 52 30 44 30 32 31 30 30 31 33 32
    // 02 30 35 46 46 30 30 30 31 03 42 35
    // 05FFWR0D02100132
    // 05FF0001

    var buf = "05 30 35 46 46 57 52 30 44 30 32 31 30 30 31 33 32".ToHex();
    XTrace.WriteLine("buf = {0}", buf.ToHex(" "));

    var buf2 = buf.ReadBytes(1, -1);
    XTrace.WriteLine("req = {0}", buf2.ToStr());
    XTrace.WriteLine("res = {0}", "02 30 35 46 46 30 30 30 31 03 42 35".ToHex().ReadBytes(1, -1).ToStr());
}

{
    // FxLinks485 写入 WW
    // 05 30 35 46 46 57 57 30 44 30 32 31 30 30 31 30 30 30 31 46 38
    // 06 30 35 46 46
    // 05FFWW0D0210010001F8
    // 05FF

    var buf = "05 30 35 46 46 57 57 30 44 30 32 31 30 30 31 30 30 30 31 46 38".ToHex();
    XTrace.WriteLine("buf = {0}", buf.ToHex(" "));

    var buf2 = buf.ReadBytes(1, -1);
    XTrace.WriteLine("req = {0}", buf2.ToStr());
    XTrace.WriteLine("res = {0}", "06 30 35 46 46".ToHex().ReadBytes(1, -1).ToStr());
}

try
{
    var pts = "Y0,Y1,D202,D203,D200,D210,D212,D213,X3,X4,M100,M101";
    var points = pts.Split(",").Select(e => new PointModel { Name = e, Address = e }).ToArray();

    var p = new FxLinksParameter { PortName = "COM5", Baudrate = 9600, Host = 5 };
    var driver = new FxLinksDriver
    {
        Log = XTrace.Log,
    };

    var node = driver.Open(null, p);

    var rs = driver.Read(node, points);
    XTrace.WriteLine("rs={0}", rs.Count);
    foreach (var item in rs)
    {
        var value = item.Value;
        if (value is Byte[] buf)
        {
            if (buf.Length == 1)
                value = buf[0];
            else
                value = buf.ToUInt16(0, false);
        }

        XTrace.WriteLine("{0}={1}", item.Key, value);
    }

    //var sp = new SerialPort("COM5", 9600)
    //{
    //    DataBits = 7,
    //    StopBits = StopBits.One,
    //    Parity = Parity.Even,

    //    ReadTimeout = 3000,
    //    WriteTimeout = 3000
    //};
    //sp.Open();

    //var str = "02 30 31 31 39 34 30 32 03 36 34";
    //var buf = str.ToHex();
    ////var buf = str.GetBytes();

    //XTrace.WriteLine("send {0}", buf.ToHex("-"));

    //sp.Write(buf, 0, buf.Length);

    //Thread.Sleep(50);

    //var count = sp.BytesToRead;
    //XTrace.WriteLine("count {0}", count);

    //var rs = new Byte[256];
    //count = sp.Read(rs, 0, rs.Length);

    //var pk = new Packet(rs, 0, count);

    //XTrace.WriteLine("recv {0}", pk.ToHex(256, "-"));
}
catch (Exception ex)
{
    XTrace.WriteException(ex);
}

Console.WriteLine("OK!");
Console.ReadKey();