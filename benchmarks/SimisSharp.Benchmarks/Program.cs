using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace KE.MSTS.SimisSharp.Benchmarks;

/// <summary>
/// Runs the benchmarks selected on the command line, for example <c>dotnet run -c Release -- --filter *Read*</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the benchmarks selected by <paramref name="args"/>, or asks which to run when there are none.
    /// </summary>
    /// <param name="args">BenchmarkDotNet command line arguments, such as <c>--filter *</c>.</param>
    public static void Main(string[] args)
    {
        // The library's wildcard assembly version cannot be built by the separate project BenchmarkDotNet generates, so the benchmarks run in this process.
        IConfig config = DefaultConfig.Instance.AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance).AsDefault());
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
