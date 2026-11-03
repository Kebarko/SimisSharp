# SimisSharp

## Overview

SimisSharp is a .NET library for reading, editing and writing the text files of Microsoft Train Simulator (MSTS) and Open Rails. These files use the SIMIS format: a header such as `SIMISA@@@@@@@@@@JINX0D0t______`, followed by named blocks in parentheses, as in `Wagon ( Mass ( 10t ) )`. Engines (`.eng`), wagons (`.wag`), consists (`.con`), activities (`.act`), paths (`.pat`), services (`.srv`), world files (`.w`), sound files (`.sms`) and many other files are written this way.

The library loads a file into a tree of nodes, which you can search with paths, read as numbers with units, and change. Saving writes the tree back as a file that MSTS and Open Rails read, keeping comment blocks, blank lines and quotes. Tools that only read can use a streaming reader instead, which reads a file token by token without building a tree and is several times faster.

Files are read the way Open Rails reads them, including comment blocks (`comment`, `skip`, and names that start with `#` or `_`), quoted strings with escape sequences, strings joined with `+`, and numbers with units such as `1000kW`.

### Supported files

- Text SIMIS files in UTF-16, as MSTS writes them, or in UTF-8, ASCII or an ANSI code page.
- Compressed text files, whose header starts with `SIMISA@F`. They are saved uncompressed.

Binary SIMIS files, such as most shapes (`.s`), terrain (`.t`) and textures (`.ace`), are not supported. Files that are not SIMIS documents are rejected.

### Limitations

- A file with an extra or a missing closing parenthesis, or with unbalanced quotes, fails to load, although MSTS and Open Rails read it. In a test on all 19,937 text SIMIS files of an MSTS installation, 63 files (0.3%) failed for these reasons, and every other file was saved back with the same content.
- Saving keeps the content of a file, but not its exact layout: indentation, spacing and line breaks are rewritten.
- The Open Rails `include` directive is read as an ordinary block. The included file is not loaded.

## Requirements

`.NET 10`

## Installation

SimisSharp is not published as a NuGet package yet. Add it to your solution from source:

1. Clone the repository:

   ```
   git clone https://github.com/Kebarko/SimisSharp.git
   ```

2. In the folder of your project, add a reference to the library, using the path to where you cloned it:

   ```
   dotnet add reference ../SimisSharp/src/SimisSharp/SimisSharp.csproj
   ```

Alternatively, build the library with `dotnet build src/SimisSharp -c Release` and reference `SimisSharp.dll` from `src/SimisSharp/bin/Release/net10.0`. Keep `SimisSharp.xml` next to it, so that the documentation shows in IntelliSense.

## Configuration

The library needs no configuration files or settings. Two things can be set in code.

### Files in an ANSI code page

A file with a byte order mark is read in the encoding that the mark selects. A file without one is read as UTF-16 when its first or second byte is zero, and as UTF-8 otherwise. An older file saved in an ANSI code page, such as Windows-1250 for Central European text, must be loaded with its encoding. Register the code page provider once when your application starts:

```csharp
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

SimisDocument document = processor.Load(path, Encoding.GetEncoding(1250));
```

### Writer options

`WriterOptions` controls how a document is formatted when it is saved. Pass it to `SimisProcessor.Save`. Every option has a default:

- **IndentCharacter** and **IndentSize**: indentation of each level. Default: one tab.
- **PreserveQuotes**: keep the quotes of values that were quoted in the file. When false, quotes are written only where a value needs them. Default: true.
- **PreserveComments**: write comment blocks. Default: true.
- **PreserveCommentPosition**: keep a comment block on the same line as the node before it, where it was on the same line in the file. Default: true.
- **PreserveEmptyLines**: write the blank lines of the file. Default: true.
- **CollapseEmptyLines**: write several blank lines in a row as one. Default: true.
- **Inline**: paths of blocks that are written on one line, such as `InertiaTensor`. Default: none.
- **Table**: blocks whose values are written in rows, such as `ThrottleRPMTab`. Default: none. Each entry has:
  - **Name**: path of the blocks.
  - **Columns**: number of columns, or a path relative to the block to the value that holds the number, such as `../[2]`.
  - **ColumnSeparator**: text between columns. Default: a space.
  - **Offset**: number of values written before the table, such as a row count. Default: 0.
  - **FixedWidth**: pad values so that the columns line up. Default: false.
- **IgnoreCase**: ignore letter case when matching the paths in **Inline** and **Table**. Default: true.

The options can be read from JSON with `System.Text.Json`. The sample application in `samples/SimisSharp.Console` reads them from `writeroptions.json`, which is a good starting point for engines and wagons:

```json
{
  "Inline": [ "InertiaTensor", "Conditions" ],
  "Table": [
    { "Name": "ThrottleRPMTab", "Columns": "2", "FixedWidth": true },
    { "Name": "CurvePoints/*", "Columns": "2", "FixedWidth": true, "Offset": 1 }
  ]
}
```

## Usage

All types are in the `KE.MSTS.SimisSharp` namespace, except `WriterOptions`, which is in `KE.MSTS.SimisSharp.IO`.

### Loading, changing and saving a file

```csharp
using KE.MSTS.SimisSharp;

SimisProcessor processor = new();
SimisDocument document = processor.Load(@"C:\MSTS\TRAINS\TRAINSET\MyLoco\MyLoco.eng");

// The value in the MaxPower block of the Engine block, such as 3080kW.
SimisNode? maxPower = SimisPathService.FindNodesByPath(document.Root, "/Engine/MaxPower/[1]").FirstOrDefault();
if (maxPower != null && maxPower.TryGetNumber(out double power, out string unit))
{
    maxPower.SetValue(Math.Round(power * 1.1), unit);  // 3388kW
}

processor.Save(document, @"C:\MSTS\TRAINS\TRAINSET\MyLoco\MyLoco.eng");
```

`Save` writes UTF-16 with a byte order mark and CRLF line endings, as MSTS does, on every operating system. It writes to a temporary file first, which then replaces the target, so a failed save leaves the original file unchanged. To get the text instead of writing a file, call `processor.Save(document)`.

`SimisProcessor` holds no state, so one instance can be shared, also between threads. Documents and nodes are not thread-safe.

### Finding nodes by path

`SimisPathService.FindNodesByPath` returns the nodes that match a path. A path is a list of node values separated by `/` or `\`. A path that starts with `/` starts at the root of the document, and other paths start at the given node. Letter case is ignored, as MSTS and Open Rails ignore it.

| Path | Finds |
|---|---|
| `/Wagon/Mass` | The `Mass` block in the top-level `Wagon` block. |
| `/Wagon/Mass/[1]` | The first value in that block, such as `10t`. |
| `/Wagon/Coupling[2]` | The second `Coupling` block of the wagon. |
| `/Train/TrainCfg/*/*Data` | The `EngineData` and `WagonData` blocks of all vehicles in a consist. `*` matches any text, and `?` any one character. |
| `../Name` | The `Name` block next to the given node. `.` is the node itself, and `..` its parent. |

### Reading and changing nodes

Each node has a `Value`: the name of a block, such as `Mass`, or a value, such as `10t`. The content of a block is in `Nodes`. The root of a document has no value, and its first node is the SIMIS header.

```csharp
SimisNode wagon = document.Root.GetNode("Wagon")!;

foreach (SimisNode node in wagon.Nodes)
{
    if (!node.IsLeaf && !node.IsComment)
        Console.WriteLine(node.Value);  // The name of each block in the wagon, such as Type, Size or Mass.
}

SimisNode name = wagon.AddNode("Name");
name.AddNode("My wagon");               // Saved as Name ( "My wagon" ), quoted because of the space.

wagon.RemoveNode("ORTSBearingType");    // Removes every ORTSBearingType block of the wagon.
```

Nodes can also be inserted with `InsertNode`, replaced with `ReplaceNode`, and looked up with `GetNode`, `GetNodes` and `IndexOf`. Values are read and written without regard to the culture of the computer:

- `TryGetNumber` reads a number with an optional unit, so `10t` gives `10` and `t`.
- `TryGetDouble`, `TryGetInt32` and `TryGetUInt32` read numbers without a unit.
- `TryGetHex` reads a hexadecimal number, such as the flags `00000001`.
- `TryGetBoolean` reads `true`, `false` or an integer, where any value other than 0 is true.
- `SetValue` writes a number with an optional unit, and `SetHexValue` a hexadecimal number.

### Reading without building a tree

`SimisReader` reads the text of a file token by token. It is several times faster than parsing the text into a document and allocates almost nothing, so it suits tools that read many files, such as all the activities or world files of a route:

```csharp
SimisReader reader = new(processor.LoadText(path));

while (reader.Read())
{
    if (reader.TokenType == SimisTokenType.StartBlock && reader.ValueEquals("MaxPower")
        && reader.Read() && reader.TryGetNumber(out double power, out string unit))
    {
        Console.WriteLine($"{power} {unit}");
    }
}
```

Each call to `Read` moves to the next token: `StartBlock` for the name of a block with its `(`, `Value`, or `EndBlock` for `)`. Comment blocks are skipped, unless the reader is created with `readComments: true`, which reports them as `Comment` tokens. `Depth` and `Line` tell where the reader is, and `Skip` skips the content of the block that has just started. `SimisReader` is a `ref struct`, so it cannot be stored in a field or used across `await`.

### Errors

| Exception | When |
|---|---|
| `SimisParseException` | The text is not well-formed: a block is not closed or not opened, or a quoted string is not terminated. `Line` is the line of the problem. |
| `NotSupportedException` | The file is not a SIMIS document, or it is a binary SIMIS file. |
| `FormatException` | The file is not valid text in its encoding, such as an ANSI file loaded without its encoding, or its compressed data is corrupt. |
| `IOException`, `UnauthorizedAccessException` | The file cannot be read or written. |

`SimisParseException` is a `FormatException`, so catch it first when you handle the two separately.

### Sample application

`samples/SimisSharp.Console` is a small console application. It loads the file given on its command line and saves it in the same folder under a new name, such as `MyLoco_01.eng`, formatted with the options in `writeroptions.json`.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
