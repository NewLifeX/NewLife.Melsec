using System;
using NewLife.Melsec;
using Xunit;

namespace XUnitTest;

public class HexHelperTests
{
    [Fact]
    public void ToHexChars()
    {
        Byte b = 0x05;
        var str = b.ToHexChars();
        Assert.Equal("05", str);

        b = 0xab;
        str = b.ToHexChars();
        Assert.Equal("AB", str);
    }
}