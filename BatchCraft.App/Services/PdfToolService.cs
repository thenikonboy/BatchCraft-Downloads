using System.IO;
using PDFtoImage;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace BatchCraft.App.Services;

public enum RotationPageScope
{
    All,
    Odd,
    Even,
    Custom
}

public sealed class PdfToolService
{
    public static int GetPageCount(string pdfPath)
    {
        using var document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);
        return document.PageCount;
    }

    public Task<int> PdfToImagesAsync(string inputPdf, string outputFolder, bool jpeg, IProgress<ToolProgress>? progress = null) => Task.Run(() =>
    {
        Directory.CreateDirectory(outputFolder);
        var baseName = Path.GetFileNameWithoutExtension(inputPdf);
        int pageCount;
        using (var document = PdfReader.Open(inputPdf, PdfDocumentOpenMode.Import)) pageCount = document.PageCount;
        for (var page = 0; page < pageCount; page++)
        {
            var output = Path.Combine(outputFolder, $"{baseName}-page-{page + 1:000}.{(jpeg ? "jpg" : "png")}");
            using var input = File.OpenRead(inputPdf);
            if (jpeg) Conversion.SaveJpeg(output, input, page: page);
            else Conversion.SavePng(output, input, page: page);
            progress?.Report(new ToolProgress(page + 1, pageCount));
        }
        if (pageCount == 0) throw new InvalidDataException("The PDF has no readable pages.");
        return pageCount;
    });

    public Task ImagesToPdfAsync(IReadOnlyList<string> images, string outputPdf, IProgress<ToolProgress>? progress = null) => Task.Run(() =>
    {
        if (images.Count == 0) throw new InvalidOperationException("Add at least one image.");
        using var document = new PdfDocument();
        document.Info.Title = Path.GetFileNameWithoutExtension(outputPdf);
        for (var index = 0; index < images.Count; index++)
        {
            using var image = XImage.FromFile(images[index]);
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(image.PointWidth);
            page.Height = XUnit.FromPoint(image.PointHeight);
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
            progress?.Report(new ToolProgress(index + 1, images.Count));
        }
        document.Save(outputPdf);
    });

    public Task MergePdfsAsync(IReadOnlyList<string> pdfs, string outputPdf, IProgress<ToolProgress>? progress = null) => Task.Run(() =>
    {
        if (pdfs.Count < 2) throw new InvalidOperationException("Add at least two PDF files.");
        using var output = new PdfDocument();
        for (var index = 0; index < pdfs.Count; index++)
        {
            using var input = PdfReader.Open(pdfs[index], PdfDocumentOpenMode.Import);
            foreach (var page in input.Pages) output.AddPage(page);
            progress?.Report(new ToolProgress(index + 1, pdfs.Count));
        }
        output.Save(outputPdf);
    });

    public Task<int> SplitPdfAllPagesAsync(string inputPdf, string outputFolder, IProgress<ToolProgress>? progress = null) => Task.Run(() =>
    {
        Directory.CreateDirectory(outputFolder);
        var baseName = Path.GetFileNameWithoutExtension(inputPdf);
        using var input = PdfReader.Open(inputPdf, PdfDocumentOpenMode.Import);
        var pageCount = input.PageCount;
        if (pageCount == 0) throw new InvalidDataException("The PDF has no readable pages.");

        for (var page = 0; page < pageCount; page++)
        {
            var outputPdf = Path.Combine(outputFolder, $"{baseName}-page-{page + 1:000}.pdf");
            using var singlePageDoc = new PdfDocument();
            singlePageDoc.AddPage(input.Pages[page]);
            singlePageDoc.Save(outputPdf);
            progress?.Report(new ToolProgress(page + 1, pageCount));
        }
        return pageCount;
    });

    public Task<int> ExtractPdfPagesAsync(string inputPdf, string pageRanges, string outputPdf, IProgress<ToolProgress>? progress = null) => Task.Run(() =>
    {
        using var input = PdfReader.Open(inputPdf, PdfDocumentOpenMode.Import);
        var pageCount = input.PageCount;
        if (pageCount == 0) throw new InvalidDataException("The PDF has no readable pages.");

        var selectedPages = ParsePageRanges(pageRanges, pageCount);
        if (selectedPages.Count == 0) throw new InvalidOperationException("No valid pages selected for extraction.");

        var outputDir = Path.GetDirectoryName(outputPdf);
        if (!string.IsNullOrWhiteSpace(outputDir)) Directory.CreateDirectory(outputDir);

        using var outputDoc = new PdfDocument();
        for (var i = 0; i < selectedPages.Count; i++)
        {
            outputDoc.AddPage(input.Pages[selectedPages[i]]);
            progress?.Report(new ToolProgress(i + 1, selectedPages.Count));
        }
        outputDoc.Save(outputPdf);
        return selectedPages.Count;
    });

    public Task<int> RotatePdfAsync(string inputPdf, string outputPdf, int angleDegrees, RotationPageScope scope, string? customRange = null, IProgress<ToolProgress>? progress = null) => Task.Run(() =>
    {
        using var input = PdfReader.Open(inputPdf, PdfDocumentOpenMode.Import);
        var pageCount = input.PageCount;
        if (pageCount == 0) throw new InvalidDataException("The PDF has no readable pages.");

        var customPages = scope == RotationPageScope.Custom && !string.IsNullOrWhiteSpace(customRange)
            ? ParsePageRanges(customRange, pageCount).ToHashSet()
            : [];

        var outputDir = Path.GetDirectoryName(outputPdf);
        if (!string.IsNullOrWhiteSpace(outputDir)) Directory.CreateDirectory(outputDir);

        using var outputDoc = new PdfDocument();
        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            var pageNumber = pageIndex + 1;
            var shouldRotate = scope switch
            {
                RotationPageScope.All => true,
                RotationPageScope.Odd => pageNumber % 2 != 0,
                RotationPageScope.Even => pageNumber % 2 == 0,
                RotationPageScope.Custom => customPages.Contains(pageIndex),
                _ => true
            };

            var newPage = outputDoc.AddPage(input.Pages[pageIndex]);
            if (shouldRotate)
            {
                var newAngle = (newPage.Rotate + angleDegrees) % 360;
                if (newAngle < 0) newAngle += 360;
                newPage.Rotate = newAngle;
            }
            progress?.Report(new ToolProgress(pageIndex + 1, pageCount));
        }
        outputDoc.Save(outputPdf);
        return pageCount;
    });

    public static List<int> ParsePageRanges(string expression, int maxPages)
    {
        if (string.IsNullOrWhiteSpace(expression)) return [];
        var result = new List<int>();
        var seen = new HashSet<int>();

        var parts = expression.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var rangeParts = part.Split('-', StringSplitOptions.TrimEntries);
                if (rangeParts.Length == 2)
                {
                    var startParsed = int.TryParse(rangeParts[0], out var start);
                    var endParsed = int.TryParse(rangeParts[1], out var end);

                    if (startParsed && endParsed)
                    {
                        var from = Math.Min(start, end);
                        var to = Math.Max(start, end);
                        for (var p = from; p <= to; p++)
                        {
                            if (p >= 1 && p <= maxPages && seen.Add(p - 1))
                                result.Add(p - 1);
                        }
                    }
                    else if (startParsed && string.IsNullOrEmpty(rangeParts[1]))
                    {
                        for (var p = start; p <= maxPages; p++)
                        {
                            if (p >= 1 && seen.Add(p - 1))
                                result.Add(p - 1);
                        }
                    }
                }
            }
            else if (int.TryParse(part, out var singlePage))
            {
                if (singlePage >= 1 && singlePage <= maxPages && seen.Add(singlePage - 1))
                    result.Add(singlePage - 1);
            }
        }
        return result;
    }
}

public sealed record ToolProgress(int Current, int Total);
