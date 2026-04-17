# Mime Type Finder

Console utility for adding file extensions to extensionless files by detecting their file signatures.

The application reads a list of GUID-based file names from `guid.txt`, searches for each file under a prefix folder, detects the file type, and renames the file by appending the detected extension.

## How It Works

For each GUID in `guid.txt`:

1. The first three characters of the GUID are used as the subfolder name.
2. The application searches that subfolder for a file whose name matches the GUID.
3. If the file has no extension, its content signature is checked.
4. The detected extension is appended to the file name.
5. Results are printed to the console and written to `output.txt`.

Example:

```text
Base directory:
D:\PROXY_STORE\DRIVE

GUID:
D2DFA0B7-47CA-4094-91E0-FC3000B8A359

Expected file location:
D:\PROXY_STORE\DRIVE\D2D\D2DFA0B7-47CA-4094-91E0-FC3000B8A359
```

If the file is detected as a PDF, it will be renamed to:

```text
D2DFA0B7-47CA-4094-91E0-FC3000B8A359.pdf
```

## Required Files

When running the published executable, keep these files in the same directory:

```text
ConsoleApp1.exe
guid.txt
```

`output.txt` is created automatically in the same directory on each run.

## guid.txt Format

Place one GUID per line:

```text
D2DFA0B7-47CA-4094-91E0-FC3000B8A359
5444D0C9-6593-4899-905C-831BA660C51F
AD8B6F6E-178B-483E-8C2D-4278E452729B
```

Lines may also contain quotes or trailing commas:

```text
'D2DFA0B7-47CA-4094-91E0-FC3000B8A359',
```

Invalid GUID lines are skipped and logged as warnings.

## Running

Run the executable and enter the base directory when prompted:

```powershell
.\ConsoleApp1.exe
```

Or pass the base directory as the first argument:

```powershell
.\ConsoleApp1.exe D:\PROXY_STORE\DRIVE
```

The console waits for a key press before closing so the result can be reviewed.

## Output

The application writes the same result to both:

```text
Console window
output.txt
```

`output.txt` is overwritten on every run.

## Supported Detections

The current signature-based detection supports:

- PDF
- DOCX, XLSX, PPTX
- ZIP
- MSG / DOC compound files
- JPG
- PNG
- GIF
- BMP
- RTF
- XML
- RAR
- 7Z
- EXE
- TXT

Files whose format cannot be detected are left unchanged and reported in the output.

## Publishing

This project is configured to publish as a Windows x64 self-contained single-file executable.

Build the distributable package with:

```powershell
dotnet publish -c Release
```

The output is generated under:

```text
bin\Release\net8.0\win-x64\publish
```

Expected published files:

```text
ConsoleApp1.exe
guid.txt
```

`output.txt` appears after the application is run.

## Requirements

Development:

- .NET 8 SDK

Runtime:

- Windows x64
- No separate .NET runtime installation is required for the published executable because it is self-contained.
