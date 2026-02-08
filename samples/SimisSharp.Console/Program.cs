using KE.MSTS.SimisSharp.IO;
using System.Text.Json;

namespace KE.MSTS.SimisSharp.Console;

/// <summary>
/// Provides the entry point for the application and coordinates loading, processing, and saving of Simis documents using specified writer options.
/// </summary>
public class Program
{
    /// <summary>
    /// Serves as the entry point for the application. Loads a document from the specified path, processes it, and saves the result using provided options.
    /// </summary>
    /// <param name="args">An array of command-line arguments. The first element must specify the path to the document to load.</param>
    public static void Main(string[] args)
    {
        try
        {
            string loadPath = args[0];
            string savePath = GetSavePath(loadPath);

            WriterOptions options = LoadOptions();

            SimisProcessor processor = new();
            SimisDocument document = processor.Load(loadPath);
            processor.Save(document, savePath, options);
        }
        catch (Exception ex)
        {
            System.Console.Write(ex.ToString());
            System.Console.ReadKey();
        }
    }

    /// <summary>
    /// Generates a file path for saving that does not conflict with an existing file by appending a numeric suffix to the specified load path.
    /// </summary>
    /// <param name="loadPath">The original file path to use as a base for generating the save path. Cannot be null or empty.</param>
    /// <returns>A file path based on the specified load path with a numeric suffix appended, ensuring that the returned path does not correspond to an existing file.</returns>
    private static string GetSavePath(string loadPath)
    {
        string? directory = Path.GetDirectoryName(loadPath);
        string? filename = Path.GetFileNameWithoutExtension(loadPath);
        string? extension = Path.GetExtension(loadPath);

        string? result;
        int i = 1;
        do
        {
            result = Path.Combine(directory ?? string.Empty, $"{filename}_{i:D2}{extension}");
            i++;
        }
        while (File.Exists(result));

        return result;
    }

    /// <summary>
    /// Loads writer configuration options from a JSON file located in the application's base directory.
    /// </summary>
    /// <returns>A <see cref="WriterOptions"/> instance containing the loaded options. If the file does not exist or cannot be read, returns a default instance with standard values.</returns>
    private static WriterOptions LoadOptions()
    {
        const string optionsFileName = "writeroptions.json";
        string path = Path.Combine(AppContext.BaseDirectory, optionsFileName);

        WriterOptions options = new();

        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                WriterOptions? parsed = JsonSerializer.Deserialize<WriterOptions>(json);
                if (parsed is not null)
                    options = parsed;
            }
        }
        catch
        {
        }

        return options;
    }
}
