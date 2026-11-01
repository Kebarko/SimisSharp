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

    [GlobalSetup]
    public void Setup()
    {
        text = Sample == "Engine" ? SampleFiles.Engine() : SampleFiles.World(10_000);
        path = Path.Combine(Path.GetTempPath(), $"simis-benchmark-{Sample}.txt");
        File.WriteAllText(path, text, Encoding.Unicode);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        File.Delete(path);
    }

    [Benchmark]
    public SimisDocument Parse()
    {
        return processor.Parse(text);
    }

    [Benchmark]
    public SimisDocument Load()
    {
        return processor.Load(path);
    }
}
