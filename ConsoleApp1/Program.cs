using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        using TextWriter? outputFile = CreateOutputFileWriter();
        if (outputFile != null)
        {
            Console.SetOut(new MultiTextWriter(Console.Out, outputFile));
        }

        Console.WriteLine("Dosya Uzantısı Ekleme Uygulaması (GUID Tabanlı)");
        Console.WriteLine("=================================================\n");

        string? basePath = GetBasePath(args);
        if (basePath == null)
        {
            WaitBeforeExit();
            return;
        }

        List<string> guids = GetGuidList();
        if (guids.Count == 0)
        {
            WaitBeforeExit();
            return;
        }

        Console.WriteLine($"\nKök Dizin : {basePath}");
        Console.WriteLine($"GUID Sayısı: {guids.Count}\n");
        Console.WriteLine(new string('-', 60));

        int successCount = 0;
        int failCount = 0;
        int notFoundCount = 0;

        foreach (var guid in guids)
        {
            string prefix = guid[..3].ToUpperInvariant();
            string subDir = Path.Combine(basePath, prefix);

            if (!Directory.Exists(subDir))
            {
                Console.WriteLine($"✗ Klasör bulunamadı: {subDir}  (GUID: {guid})");
                notFoundCount++;
                continue;
            }

            List<string> matchingFiles;
            try
            {
                matchingFiles = Directory.GetFiles(subDir)
                    .Where(f =>
                    {
                        string name = Path.GetFileNameWithoutExtension(f);
                        return name.Equals(guid, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âœ— KlasÃ¶r okunamadÄ±: {subDir}  (GUID: {guid}) - {ex.Message}");
                failCount++;
                continue;
            }

            if (matchingFiles.Count == 0)
            {
                Console.WriteLine($"✗ Dosya bulunamadı: {guid}  (Klasör: {prefix})");
                notFoundCount++;
                continue;
            }

            foreach (var filePath in matchingFiles)
            {
                if (!string.IsNullOrEmpty(Path.GetExtension(filePath)))
                {
                    Console.WriteLine($"⚠ Zaten uzantılı: {Path.GetFileName(filePath)}  (Klasör: {prefix})");
                    continue;
                }

                try
                {
                    string extension = DetectFileExtension(filePath);

                    if (!string.IsNullOrEmpty(extension))
                    {
                        string newFilePath = filePath + extension;

                        if (File.Exists(newFilePath))
                        {
                            Console.WriteLine($"⚠ Atlandı: {Path.GetFileName(filePath)} (hedef dosya zaten mevcut)");
                            failCount++;
                            continue;
                        }

                        File.Move(filePath, newFilePath);
                        Console.WriteLine($"✓ {prefix}\\{Path.GetFileName(filePath)} -> {Path.GetFileName(newFilePath)}");
                        successCount++;
                    }
                    else
                    {
                        Console.WriteLine($"✗ Format tespit edilemedi: {prefix}\\{Path.GetFileName(filePath)}");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Hata ({prefix}\\{Path.GetFileName(filePath)}): {ex.Message}");
                    failCount++;
                }
            }
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"\nÖzet: {successCount} başarılı, {failCount} başarısız, {notFoundCount} bulunamadı");
        WaitBeforeExit();
    }

    static void WaitBeforeExit()
    {
        if (Console.IsInputRedirected)
            return;

        Console.WriteLine();
        Console.Write("Cikmak icin bir tusa basin...");
        Console.ReadKey(intercept: true);
    }

    static TextWriter? CreateOutputFileWriter()
    {
        try
        {
            return new StreamWriter(Path.Combine(AppContext.BaseDirectory, "output.txt"), append: false, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Uyari: output.txt dosyasi olusturulamadi. Cikti sadece konsola yazilacak. Detay: {ex.Message}");
            return null;
        }
    }

    static string? GetBasePath(string[] args)
    {
        string? basePath = args.Length > 0 ? args[0] : null;

        if (string.IsNullOrWhiteSpace(basePath))
        {
            Console.Write("Kök dizin yolunu girin (ör: D:\\EDMS_PROXY_STORE\\DRIVE): ");
            basePath = Console.ReadLine();
        }

        if (string.IsNullOrWhiteSpace(basePath))
        {
            Console.WriteLine("Hata: Kök dizin belirtilmedi!");
            return null;
        }

        if (!Directory.Exists(basePath))
        {
            Console.WriteLine($"Hata: Dizin bulunamadı: {basePath}");
            return null;
        }

        return basePath;
    }

    static List<string> GetGuidList()
    {
        var guids = new List<string>();
        string guidFilePath = Path.Combine(AppContext.BaseDirectory, "guid.txt");

        if (!File.Exists(guidFilePath))
        {
            Console.WriteLine("Hata: guid.txt dosyasi bulunamadi!");
            Console.WriteLine($"Aranan konum: {guidFilePath}");
            return guids;
        }

        try
        {
            int lineNumber = 0;
            foreach (string line in File.ReadLines(guidFilePath))
            {
                lineNumber++;
                string candidate = line.Trim().Trim('\'', '"', ',');
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                if (Guid.TryParse(candidate, out Guid parsedGuid))
                {
                    guids.Add(parsedGuid.ToString("D").ToUpperInvariant());
                }
                else
                {
                    Console.WriteLine($"Uyari: guid.txt satir {lineNumber} gecersiz GUID, atlandi: {candidate}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: guid.txt okunamadi: {guidFilePath} - {ex.Message}");
            guids.Clear();
        }

        if (guids.Count == 0)
        {
            Console.WriteLine($"Hata: guid.txt dosyasinda GUID bulunamadi: {guidFilePath}");
        }

        return guids;
    }

    static string DetectFileExtension(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[32];
            int bytesRead = fs.Read(buffer, 0, buffer.Length);

            if (bytesRead < 4)
                return string.Empty;

            // PDF
            if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
                return ".pdf";

            // DOCX, XLSX, PPTX (ZIP based - Office Open XML)
            if (buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04)
            {
                // ZIP dosyası, Office dosyası mı kontrol et
                fs.Seek(0, SeekOrigin.Begin);
                using var reader = new BinaryReader(fs);
                byte[] fullBuffer = reader.ReadBytes((int)Math.Min(fs.Length, 8192));
                string content = System.Text.Encoding.ASCII.GetString(fullBuffer);

                if (content.Contains("word/"))
                    return ".docx";
                if (content.Contains("xl/"))
                    return ".xlsx";
                if (content.Contains("ppt/"))
                    return ".pptx";

                return ".zip";
            }

            // MSG (Outlook)
            if (buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0 &&
                buffer[4] == 0xA1 && buffer[5] == 0xB1 && buffer[6] == 0x1A && buffer[7] == 0xE1)
            {
                // Compound File Binary Format - MSG veya DOC olabilir
                fs.Seek(0, SeekOrigin.Begin);
                byte[] largeBuffer = new byte[Math.Min(512, (int)fs.Length)];
                fs.Read(largeBuffer, 0, largeBuffer.Length);
                string content = System.Text.Encoding.ASCII.GetString(largeBuffer);

                if (content.Contains("Microsoft") || content.Contains("Word"))
                    return ".doc";

                return ".msg";
            }

            // JPG/JPEG
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                return ".jpg";

            // PNG
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                return ".png";

            // GIF
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
                return ".gif";

            // BMP
            if (buffer[0] == 0x42 && buffer[1] == 0x4D)
                return ".bmp";

            // RTF
            if (buffer[0] == 0x7B && buffer[1] == 0x5C && buffer[2] == 0x72 && buffer[3] == 0x74)
                return ".rtf";

            // XML
            if (buffer[0] == 0x3C && buffer[1] == 0x3F && buffer[2] == 0x78 && buffer[3] == 0x6D)
                return ".xml";

            // RAR
            if (buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 && buffer[3] == 0x21)
                return ".rar";

            // 7Z
            if (buffer[0] == 0x37 && buffer[1] == 0x7A && buffer[2] == 0xBC && buffer[3] == 0xAF)
                return ".7z";

            // EXE/DLL
            if (buffer[0] == 0x4D && buffer[1] == 0x5A)
                return ".exe";

            // TXT/Log kontrolü (UTF-8 BOM)
            if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                return ".txt";

            // Son çare: metin dosyası mı kontrol et
            if (IsLikelyText(buffer))
                return ".txt";

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    static bool IsLikelyText(byte[] data)
    {
        foreach (var b in data)
        {
            // Kontrol karakterleri hariç (tab, newline, carriage return)
            if (b < 0x09) return false;
            // ASCII olmayan kontrol karakterleri
            if (b > 0x7E && b < 0xA0) return false;
        }
        return true;
    }
}

class MultiTextWriter : TextWriter
{
    private readonly TextWriter[] writers;

    public MultiTextWriter(params TextWriter[] writers)
    {
        this.writers = writers;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        foreach (var writer in writers)
            writer.Write(value);
    }

    public override void Write(string? value)
    {
        foreach (var writer in writers)
            writer.Write(value);
    }

    public override void WriteLine(string? value)
    {
        foreach (var writer in writers)
            writer.WriteLine(value);
    }

    public override void Flush()
    {
        foreach (var writer in writers)
            writer.Flush();
    }
}
