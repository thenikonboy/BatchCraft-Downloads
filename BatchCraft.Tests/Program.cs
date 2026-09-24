using System.Security.Cryptography;
using BatchCraft.App.Services;

var failures = new List<string>();
void Assert(bool condition, string name) { if (!condition) failures.Add(name); else Console.WriteLine($"PASS {name}"); }
void Throws<T>(Action action, string name) where T : Exception { try { action(); failures.Add(name); } catch (T) { Console.WriteLine($"PASS {name}"); } }

var bytes = "BatchCraft update integrity test"u8.ToArray();
var file = Path.Combine(Path.GetTempPath(), $"batchcraft-test-{Guid.NewGuid():N}.bin");
await File.WriteAllBytesAsync(file, bytes);
var digest = "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
UpdateService.ValidateDigest(digest);
await UpdateService.VerifyDigestAsync(file, digest);
Assert(File.Exists(file), "valid SHA-256 accepted");
Throws<InvalidDataException>(() => UpdateService.ValidateDigest(null), "missing digest rejected");
Throws<InvalidDataException>(() => UpdateService.ValidateDigest("sha256:1234"), "short digest rejected");

var releaseJson = $$"""{"tag_name":"v9.9.0","html_url":"https://example.test/release","assets":[{"name":"BatchCraft-Setup-v9.9.0-win-x64.exe","browser_download_url":"https://example.test/setup.exe","digest":"sha256:{{new string('a', 64)}}"},{"name":"BatchCraft-v9.9.0-win-x64.zip","browser_download_url":"https://example.test/portable.zip","digest":"sha256:{{new string('b', 64)}}"}]}""";
var uninstallerMarker = Path.Combine(AppContext.BaseDirectory, "unins000.exe");
try
{
    if (File.Exists(uninstallerMarker)) File.Delete(uninstallerMarker);
    var portableUpdate = await new UpdateService(new HttpClient(new JsonHandler(releaseJson))).CheckAsync();
    Assert(portableUpdate?.Kind == UpdatePackageKind.Portable && portableUpdate.AssetName.EndsWith(".zip"), "portable build selects ZIP");
    await File.WriteAllTextAsync(uninstallerMarker, "test marker");
    var installedUpdate = await new UpdateService(new HttpClient(new JsonHandler(releaseJson))).CheckAsync();
    Assert(installedUpdate?.Kind == UpdatePackageKind.Installer && installedUpdate.AssetName.Contains("Setup"), "installed build selects Setup EXE");
}
finally { try { File.Delete(uninstallerMarker); } catch { } }

await File.WriteAllBytesAsync(file, bytes);
try { await UpdateService.VerifyDigestAsync(file, "sha256:" + new string('0', 64)); failures.Add("mismatched digest rejected"); }
catch (InvalidDataException) { Assert(!File.Exists(file), "mismatched download deleted"); }

var service = new PdfToolService();
var work = Path.Combine(Path.GetTempPath(), $"batchcraft-pdf-test-{Guid.NewGuid():N}");
Directory.CreateDirectory(work);
try
{
    var image1 = Path.Combine(work, "ภาพ 1.png"); var image2 = Path.Combine(work, "image 2.png");
    using (var bitmap = new System.Drawing.Bitmap(120, 80)) { using var g = System.Drawing.Graphics.FromImage(bitmap); g.Clear(System.Drawing.Color.CornflowerBlue); bitmap.Save(image1); }
    using (var bitmap = new System.Drawing.Bitmap(80, 120)) { using var g = System.Drawing.Graphics.FromImage(bitmap); g.Clear(System.Drawing.Color.Orange); bitmap.Save(image2); }
    var pdf = Path.Combine(work, "รวมรูป.pdf"); await service.ImagesToPdfAsync([image1, image2], pdf);
    Assert(File.Exists(pdf) && new FileInfo(pdf).Length > 0, "images to PDF with Thai and spaces");
    var pngFolder = Path.Combine(work, "png"); var count = await service.PdfToImagesAsync(pdf, pngFolder, false);
    Assert(count == 2 && Directory.GetFiles(pngFolder, "*.png").Length == 2, "PDF to PNG page count");
    var merged = Path.Combine(work, "merged.pdf"); await service.MergePdfsAsync([pdf, pdf], merged);
    using var mergedDocument = PdfSharp.Pdf.IO.PdfReader.Open(merged, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
    Assert(mergedDocument.PageCount == 4, "PDF merge order and page count");

    // Test ParsePageRanges
    var parsed = PdfToolService.ParsePageRanges("1, 3-4", 4);
    Assert(parsed.SequenceEqual([0, 2, 3]), "parse page ranges 1, 3-4");

    // Test Split All Pages
    var splitFolder = Path.Combine(work, "split");
    var splitCount = await service.SplitPdfAllPagesAsync(merged, splitFolder);
    Assert(splitCount == 4 && Directory.GetFiles(splitFolder, "*.pdf").Length == 4, "split all pages into separate PDFs");

    // Test Extract Page Range
    var extractedPdf = Path.Combine(work, "extracted.pdf");
    var extractCount = await service.ExtractPdfPagesAsync(merged, "1, 3", extractedPdf);
    using var extractDoc = PdfSharp.Pdf.IO.PdfReader.Open(extractedPdf, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
    Assert(extractCount == 2 && extractDoc.PageCount == 2, "extract specific page range");

    // Test Rotate PDF (90 degrees clockwise for odd pages)
    var rotatedPdf = Path.Combine(work, "rotated.pdf");
    var rotateCount = await service.RotatePdfAsync(merged, rotatedPdf, 90, RotationPageScope.Odd);
    using var rotateDoc = PdfSharp.Pdf.IO.PdfReader.Open(rotatedPdf, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
    Assert(rotateCount == 4 && rotateDoc.Pages[0].Rotate == 90 && rotateDoc.Pages[1].Rotate == 0, "rotate PDF odd pages by 90 degrees");

    // Test Multiple PDFs to JPG
    var multiJpgFolder = Path.Combine(work, "multi_jpg");
    var multiJpgCount = await service.MultiplePdfsToImagesAsync([pdf, rotatedPdf], multiJpgFolder, true);
    Assert(multiJpgCount == 6 && Directory.GetFiles(multiJpgFolder, "*.jpg").Length == 6, "convert multiple PDFs to JPG");

    // Test Watermark PDF
    var watermarkedPdf = Path.Combine(work, "watermarked.pdf");
    var wmCount = await service.WatermarkPdfAsync(merged, watermarkedPdf, "สำเนาถูกต้อง", 48, 0.3, 45, PdfSharp.Drawing.XColors.Red, RotationPageScope.All);
    using var wmDoc = PdfSharp.Pdf.IO.PdfReader.Open(watermarkedPdf, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
    Assert(wmCount == 4 && wmDoc.PageCount == 4, "apply watermark with Thai text to PDF");

    // Test GetNonConflictingPath (duplicate prevention)
    var testDupPath = Path.Combine(work, "dup_test.txt");
    await File.WriteAllTextAsync(testDupPath, "first");
    var nonConflicting = PdfToolService.GetNonConflictingPath(testDupPath);
    Assert(nonConflicting.EndsWith("dup_test (1).txt"), "auto-rename duplicate filename");
}
finally { try { Directory.Delete(work, true); } catch { } }

if (failures.Count == 0) { Console.WriteLine("ALL TESTS PASSED"); return 0; }
Console.Error.WriteLine("FAILED: " + string.Join(", ", failures)); return 1;

file sealed class JsonHandler(string json) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json) });
}
