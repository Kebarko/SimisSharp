using KE.MSTS.SimisSharp.IO;
using System.Text.Json;

namespace KE.MSTS.SimisSharp.Console;

public class Program
{
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
