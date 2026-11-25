using System.IO.Compression;
using System.Xml.Linq;
using System.Globalization;
using System.Text;

namespace Study.Services;

/// <summary>Minimal OpenXML reader/writer using only .NET 8 BCL (no third-party package).</summary>
public static class XlsxService
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PkgRel = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static List<string[]> ReadFirstSheet(Stream input)
    {
        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        var workbook = Load(archive, "xl/workbook.xml");
        var rels = Load(archive, "xl/_rels/workbook.xml.rels");
        var first = workbook.Root?.Element(Main + "sheets")?.Elements(Main + "sheet").FirstOrDefault()
            ?? throw new InvalidDataException("Tệp Excel không có sheet.");
        var rid = (string?)first.Attribute(Rel + "id") ?? throw new InvalidDataException("Sheet không hợp lệ.");
        var target = rels.Root?.Elements(PkgRel + "Relationship").FirstOrDefault(x => (string?)x.Attribute("Id") == rid)?.Attribute("Target")?.Value
            ?? throw new InvalidDataException("Không tìm thấy dữ liệu sheet.");
        var sheetPath = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target.Replace("\\", "/");
        var sheet = Load(archive, sheetPath);
        var strings = new List<string>();
        var shared = archive.GetEntry("xl/sharedStrings.xml");
        if (shared != null)
        {
            using var sr = shared.Open(); var doc = XDocument.Load(sr);
            strings = doc.Root!.Elements(Main + "si").Select(si => string.Concat(si.Descendants(Main + "t").Select(t => t.Value))).ToList();
        }
        var rows = new List<string[]>();
        foreach (var row in sheet.Descendants(Main + "row"))
        {
            var cells = new SortedDictionary<int, string>();
            foreach (var cell in row.Elements(Main + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? "A1";
                int col = ColumnIndex(reference);
                var type = (string?)cell.Attribute("t");
                var value = cell.Element(Main + "v")?.Value ?? string.Concat(cell.Descendants(Main + "t").Select(t => t.Value));
                if (type == "s" && int.TryParse(value, out var si) && si >= 0 && si < strings.Count) value = strings[si];
                if (type == "b") value = value == "1" ? "TRUE" : "FALSE";
                cells[col] = value;
            }
            if (cells.Count == 0) continue;
            var arr = new string[cells.Keys.Max() + 1];
            foreach (var pair in cells) arr[pair.Key] = pair.Value;
            rows.Add(arr);
        }
        return rows;
    }

    public static byte[] Write(string sheetName, IReadOnlyList<string[]> rows)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            Add(zip, "[Content_Types].xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            Add(zip, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add(zip, "xl/workbook.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><sheets><sheet name=\"{Xml(sheetName)}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add(zip, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sb = new StringBuilder($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"{Main}\"><sheetData>");
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append($"<row r=\"{r + 1}\">");
                for (int c = 0; c < rows[r].Length; c++)
                {
                    var val = rows[r][c] ?? "";
                    sb.Append($"<c r=\"{ColumnName(c)}{r + 1}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Xml(val)}</t></is></c>");
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData></worksheet>");
            Add(zip, "xl/worksheets/sheet1.xml", sb.ToString());
        }
        return ms.ToArray();
    }

    private static XDocument Load(ZipArchive zip, string path)
    {
        var entry = zip.GetEntry(path) ?? throw new InvalidDataException($"Thiếu thành phần {path} trong Excel.");
        using var stream = entry.Open(); return XDocument.Load(stream);
    }
    private static void Add(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)); writer.Write(content);
    }
    private static int ColumnIndex(string cell)
    {
        int n = 0; foreach (char ch in cell.TakeWhile(char.IsLetter)) n = n * 26 + char.ToUpperInvariant(ch) - 'A' + 1; return Math.Max(0, n - 1);
    }
    private static string ColumnName(int index)
    {
        var s = ""; for (int n = index + 1; n > 0; n = (n - 1) / 26) s = (char)('A' + (n - 1) % 26) + s; return s;
    }
    private static string Xml(string value) => System.Security.SecurityElement.Escape(value) ?? "";
}
