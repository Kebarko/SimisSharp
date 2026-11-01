using BenchmarkDotNet.Attributes;
using KE.MSTS.SimisSharp.IO;

namespace KE.MSTS.SimisSharp.Benchmarks;

/// <summary>
/// Measures writing a parsed tree back to text, with default options and with path patterns that are matched against every block.
/// </summary>
[MemoryDiagnoser]
public class WriteBenchmarks
{
    private readonly SimisProcessor processor = new();
    private readonly WriterOptions patterns = new()
    {
        Inline = ["Position", "QDirection", "/Wagon/Coupling"],
        Table = { new WriterOptions.TableConfig { Name = "Friction", Columns = "5" } },
    };

    private SimisDocument document = new();

    /// <summary>
    /// The sample to write: a small engine file, or a world file with many objects.
    /// </summary>
    [Params("Engine", "World")]
    public string Sample { get; set; } = "Engine";

    /// <summary>
    /// Parses the sample text into the tree that is written.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        document = processor.Parse(Sample == "Engine" ? SampleFiles.Engine() : SampleFiles.World(10_000));
    }

    /// <summary>
    /// Writes the tree with default options.
    /// </summary>
    [Benchmark]
    public string Save()
    {
        return processor.Save(document);
    }

    /// <summary>
    /// Writes the tree with inline and table path patterns, which are matched against every block.
    /// </summary>
    [Benchmark]
    public string SaveWithPatterns()
    {
        return processor.Save(document, patterns);
    }
}
