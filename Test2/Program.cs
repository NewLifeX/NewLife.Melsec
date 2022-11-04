using System.IO.Ports;
using System.Xml;
using NewLife;
using NewLife.Data;
using NewLife.Log;

XTrace.UseConsole();

try
{
    var sp = new SerialPort("COM7", 9600)
    {
        DataBits = 7,
        StopBits = StopBits.One,
        Parity = Parity.Even,

        ReadTimeout = 3000,
        WriteTimeout = 3000
    };
    sp.Open();

    var str = "02 30 31 31 39 34 30 32 03 36 34";
    var buf = str.ToHex();
    //var buf = str.GetBytes();

    XTrace.WriteLine("send {0}", buf.ToHex("-"));

    sp.Write(buf, 0, buf.Length);

    Thread.Sleep(50);

    var count = sp.BytesToRead;
    XTrace.WriteLine("count {0}", count);

    var rs = new Byte[256];
    count = sp.Read(rs, 0, rs.Length);

    var pk = new Packet(rs, 0, count);

    XTrace.WriteLine("recv {0}", pk.ToHex(256, "-"));
}
catch (Exception ex)
{
    XTrace.WriteException(ex);
}

Console.WriteLine("OK!");
Console.ReadKey();