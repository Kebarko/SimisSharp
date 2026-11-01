using BenchmarkDotNet.Attributes;
using System.Text;

namespace KE.MSTS.SimisSharp.Benchmarks;

/// <summary>
/// Measures reading SIMIS text: parsing a string into a tree and loading a UTF-16 file from disk.
/// </summary>
[MemoryDiagnoser]
public class ReadBenchmarks
{
    private readonly SimisProcessor processor = new();
    private string text = string.Empty;
    private string path = string.Empty;

    /// <summary>
    /// The sample to read: a small engine file, or a world file with many objects.
    /// </summary>
    [Params("Engine", "World")]
    public string Sample { get; set; } = "Engine";

    /// <summary>
    /// Generates the sample text and writes it to a temporary UTF-16 file, as MSTS saves it.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        text = Sample == "Engine" ? SampleFiles.Engine() : SampleFiles.World(10_000);
        path = Path.Combine(Path.GetTempPath(), $"simis-benchmark-{Sample}.txt");
        File.WriteAllText(path, text, Encoding.Unicode);
    }

    /// <summary>
    /// Deletes the temporary file.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        File.Delete(path);
    }

    /// <summary>
    /// Parses the sample text into a tree.
    /// </summary>
    [Benchmark]
    public SimisDocument Parse()
    {
        return processor.Parse(text);
    }

    /// <summary>
    /// Loads the sample file, decoding and parsing it into a tree.
    /// </summary>
    [Benchmark]
    public SimisDocument Load()
    {
        return processor.Load(path);
    }

    /// <summary>
    /// Reads every token with <see cref="SimisReader"/> and parses every number, without building a tree.
    /// </summary>
    [Benchmark]
    public double Read()
    {
        double sum = 0;
        SimisReader reader = new(text);
        while (reader.Read())
        {
            if (reader.TokenType == SimisTokenType.Value && reader.TryGetDouble(out double value))
                sum += value;
        }

        return sum;
    }
}
