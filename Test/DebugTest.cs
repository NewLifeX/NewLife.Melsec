using NewLife;
using NewLife.Melsec.Protocols;

var msg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
Console.WriteLine($"Binary hex: {msg.ToBytes().ToHex()}");

var asciiMsg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
asciiMsg.DataFormat = MCDataFormat.Ascii;
var asciiBytes = asciiMsg.ToBytes();
Console.WriteLine($"ASCII bytes hex: {asciiBytes.ToHex()}");
Console.WriteLine($"ASCII text: {System.Text.Encoding.ASCII.GetString(asciiBytes)}");
Console.WriteLine($"ASCII text length: {System.Text.Encoding.ASCII.GetString(asciiBytes).Length}");
