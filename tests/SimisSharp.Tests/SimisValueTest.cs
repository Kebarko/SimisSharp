using System.Globalization;

namespace KE.MSTS.SimisSharp.Tests;

public class SimisValueTest
{
    [Theory]
    [InlineData("10t", 10, "t")]
    [InlineData("1.2kN", 1.2, "kN")]
    [InlineData("-3.5e2m", -350, "m")]
    [InlineData("2E+3lb", 2000, "lb")]
    [InlineData("1e3", 1000, "")]
    [InlineData("3.50f", 3.5, "f")]
    [InlineData("+5", 5, "")]
    [InlineData("10,", 10, "")]
    [InlineData("1,000lb", 1000, "lb")]
    [InlineData("10 t", 10, "t")]
    [InlineData("220kN#est", 220, "kN")]
    [InlineData("20 #note", 20, "")]
    [InlineData("60km/h", 60, "km/h")]
    [InlineData("5e", 5, "e")]
    [InlineData("0.5in", 0.5, "in")]
    public void TryParseNumberReadsNumberAndUnit(string text, double expectedValue, string expectedUnit)
    {
        // Act
        bool parsed = SimisValue.TryParseNumber(text, out double value, out string unit);

        // Assert
        Assert.True(parsed);
        Assert.Equal(expectedValue, value);
        Assert.Equal(expectedUnit, unit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("t")]
    [InlineData("abc")]
    [InlineData("-")]
    [InlineData(".")]
    public void TryParseNumberRejectsText(string? text)
    {
        Assert.False(SimisValue.TryParseNumber(text, out _, out string unit));
        Assert.Equal(string.Empty, unit);
    }

    [Theory]
    [InlineData("1.5", true, 1.5)]
    [InlineData("-2e3", true, -2000)]
    [InlineData("10,", true, 10)]
    [InlineData("1.5t", false, 0)]
    [InlineData("", false, 0)]
    [InlineData(null, false, 0)]
    public void TryParseDoubleRejectsUnits(string? text, bool expected, double expectedValue)
    {
        Assert.Equal(expected, SimisValue.TryParseDouble(text, out double value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("42", true, 42)]
    [InlineData("-7", true, -7)]
    [InlineData("1e3", true, 1000)]
    [InlineData("3,", true, 3)]
    [InlineData("1.5", false, 0)]
    [InlineData("4294967295", false, 0)]
    [InlineData("10t", false, 0)]
    public void TryParseInt32Test(string text, bool expected, int expectedValue)
    {
        Assert.Equal(expected, SimisValue.TryParseInt32(text, out int value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("4294967295", true, uint.MaxValue)]
    [InlineData("0", true, 0u)]
    [InlineData("-1", false, 0u)]
    [InlineData("4294967296", false, 0u)]
    public void TryParseUInt32Test(string text, bool expected, uint expectedValue)
    {
        Assert.Equal(expected, SimisValue.TryParseUInt32(text, out uint value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("ffffffff", true, uint.MaxValue)]
    [InlineData("FF00", true, 0xFF00u)]
    [InlineData("0", true, 0u)]
    [InlineData("0x10", false, 0u)]
    [InlineData("g", false, 0u)]
    [InlineData("100000000", false, 0u)]
    public void TryParseHexTest(string text, bool expected, uint expectedValue)
    {
        Assert.Equal(expected, SimisValue.TryParseHex(text, out uint value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("true", true, true)]
    [InlineData("FALSE", true, false)]
    [InlineData("1", true, true)]
    [InlineData("0", true, false)]
    [InlineData("2", true, true)]
    [InlineData("yes", false, false)]
    [InlineData(null, false, false)]
    public void TryParseBooleanTest(string? text, bool expected, bool expectedValue)
    {
        Assert.Equal(expected, SimisValue.TryParseBoolean(text, out bool value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData(1.5, null, "1.5")]
    [InlineData(12.5, "t", "12.5t")]
    [InlineData(1.2, "kN", "1.2kN")]
    [InlineData(-0.25, "m", "-0.25m")]
    [InlineData(100, "km/h", "100km/h")]
    [InlineData(0.1, "", "0.1")]
    public void FormatDoubleTest(double value, string? unit, string expected)
    {
        Assert.Equal(expected, SimisValue.Format(value, unit));
    }

    [Fact]
    public void FormatSingleUsesShortestText()
    {
        Assert.Equal("0.1", SimisValue.Format(0.1f));
        Assert.Equal("2.5kW", SimisValue.Format(2.5f, "kW"));
    }

    [Fact]
    public void FormatIntegerTest()
    {
        Assert.Equal("42", SimisValue.Format(42));
        Assert.Equal("800kW", SimisValue.Format(800, "kW"));
        Assert.Equal("4294967295", SimisValue.Format(uint.MaxValue));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FormatRejectsNonFiniteNumbers(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimisValue.Format(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => SimisValue.Format((float)value));
    }

    [Theory]
    [InlineData("5")]
    [InlineData("e5")]
    [InlineData(".5")]
    [InlineData(" t")]
    [InlineData("kN#x")]
    public void FormatRejectsUnitsThatDoNotReadBack(string unit)
    {
        Assert.Throws<ArgumentException>(() => SimisValue.Format(10.0, unit));
    }

    [Theory]
    [InlineData(255u, 8, "000000ff")]
    [InlineData(255u, 2, "ff")]
    [InlineData(0xFF00FF00u, 8, "ff00ff00")]
    [InlineData(0u, 0, "0")]
    public void FormatHexTest(uint value, int digits, string expected)
    {
        Assert.Equal(expected, SimisValue.FormatHex(value, digits));
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-0.001)]
    [InlineData(123456.789)]
    [InlineData(1e-7)]
    [InlineData(1e20)]
    public void FormattedNumbersReadBack(double value)
    {
        Assert.True(SimisValue.TryParseNumber(SimisValue.Format(value, "m"), out double parsed, out string unit));
        Assert.Equal(value, parsed);
        Assert.Equal("m", unit);
    }

    [Fact]
    public void ParsingAndFormattingIgnoreCurrentCulture()
    {
        // Arrange
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("sk-SK");

        try
        {
            // Act
            bool parsed = SimisValue.TryParseNumber("1.5t", out double value, out _);
            string formatted = SimisValue.Format(1.5, "t");

            // Assert
            Assert.True(parsed);
            Assert.Equal(1.5, value);
            Assert.Equal("1.5t", formatted);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void NodeAccessorsReadAndWriteValues()
    {
        // Arrange
        SimisProcessor processor = new();
        SimisDocument document = processor.Parse("Wagon ( Mass ( 10t ) MaxPower ( \"800 kW\" ) Flag ( 1 ) Colour ( ff00ff00 ) Count ( 3 ) )");
        SimisNode wagon = document.Root.Nodes[0];
        SimisNode mass = wagon.GetNode("Mass")!.Nodes[0];
        SimisNode power = wagon.GetNode("MaxPower")!.Nodes[0];
        SimisNode colour = wagon.GetNode("Colour")!.Nodes[0];
        SimisNode count = wagon.GetNode("Count")!.Nodes[0];

        // Act & Assert
        Assert.True(mass.TryGetNumber(out double massValue, out string massUnit));
        Assert.Equal((10.0, "t"), (massValue, massUnit));
        Assert.False(mass.TryGetDouble(out _));
        Assert.True(power.TryGetNumber(out double powerValue, out string powerUnit));
        Assert.Equal((800.0, "kW"), (powerValue, powerUnit));
        Assert.True(wagon.GetNode("Flag")!.Nodes[0].TryGetBoolean(out bool flag));
        Assert.True(flag);
        Assert.True(colour.TryGetHex(out uint colourValue));
        Assert.Equal(0xFF00FF00u, colourValue);
        Assert.True(count.TryGetInt32(out int countValue));
        Assert.Equal(3, countValue);
        Assert.True(count.TryGetUInt32(out uint countUnsigned));
        Assert.Equal(3u, countUnsigned);

        mass.SetValue(12.5, "t");
        power.SetValue(1000, "kW");
        colour.SetHexValue(0xFFu);
        count.SetValue(4.5f);

        Assert.Equal(
            string.Join(Environment.NewLine,
                "Wagon (",
                "\tMass ( 12.5t )",
                "\tMaxPower ( \"1000kW\" )",
                "\tFlag ( 1 )",
                "\tColour ( 000000ff )",
                "\tCount ( 4.5 )",
                ")",
                ""),
            processor.Save(document));
    }
}
