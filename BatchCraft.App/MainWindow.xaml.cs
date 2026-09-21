using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BatchCraft.App.Services;
using PDFtoImage;
using Forms = System.Windows.Forms;

namespace BatchCraft.App;

public partial class MainWindow : Window
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff"];
    private readonly UpdateService updater = new();
    private readonly UserSettingsService settingsService = new();
    private readonly UserSettings settings;
    private PdfToolService? pdfTools;
    private bool applyingSettings;
    private string? latestOutputFolder;

    private string? pdfToImagesPath;
    private int pdfToImagesPage;
    private int pdfToImagesTotalPages;

    private string? mergePdfPath;
    private int mergePdfPage;
    private int mergePdfTotalPages;

    private string? splitPdfPath;
    private int splitPdfPage;
    private int splitPdfTotalPages;

    private string? rotatePdfPath;
    private int rotatePdfPage;
    private int rotatePdfTotalPages;

    private PdfToolService PdfTools => pdfTools ??= new PdfToolService();
    private bool IsThai => settings.Language == "th";
    public ObservableCollection<string> ImageFiles { get; } = [];
    public ObservableCollection<string> PdfFiles { get; } = [];

    public MainWindow()
    {
        settings = settingsService.Load();
        if (settings.Language is not ("th" or "en")) settings.Language = "th";
        if (settings.Theme is not ("dark" or "light")) settings.Theme = "dark";
        InitializeComponent();
        SetDisplayedProductVersion();
        ImageList.ItemsSource = ImageFiles;
        PdfList.ItemsSource = PdfFiles;
        ApplyTheme();
        ApplyLanguage();
        SyncSettingsControls();
        Loaded += async (_, _) => await CheckForUpdatesAsync(true);
    }

    private string T(string thai, string english) => IsThai ? thai : english;

    private void SetDisplayedProductVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var label = $"BatchCraft {version?.Major}.{version?.Minor}.{version?.Build}";
        Title = label;
        VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
    }

    private void ApplyLanguage()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
        SubtitleText.Text = T("เครื่องมือ PDF ที่เรียบง่าย รวดเร็ว และเป็นส่วนตัว", "Simple, fast, and private PDF tools");
        SettingsButton.Content = T("⚙ ตั้งค่า", "⚙ Settings");

        // Tab Headers
        PdfToImagesTabHeader.Text = T("PDF → รูปภาพ", "PDF → Images");
        ImagesToPdfTabHeader.Text = T("รูปภาพ → PDF", "Images → PDF");
        MergePdfTabHeader.Text = T("รวม PDF", "Merge PDF");
        SplitPdfTabHeader.Text = T("แยก PDF", "Split PDF");
        RotatePdfTabHeader.Text = T("หมุน PDF", "Rotate PDF");
        SettingsTabHeader.Text = T("ตั้งค่า", "Settings");
        AboutTabHeader.Text = T("เกี่ยวกับ", "About");

        // 1. PDF -> Images
        PdfToImagesTitle.Text = T("แปลงทุกหน้า PDF เป็นรูปภาพ", "Convert every PDF page to images");
        PdfToImagesHint.Text = T("ลาก PDF มาวาง หรือกดเลือกไฟล์", "Drop a PDF here or select a file");
        DropPdfText.Text = T("วางไฟล์ PDF ที่นี่", "Drop a PDF here");
        PickPdfButton.Content = T("เลือก PDF", "Select PDF");
        SelectedFileLabel.Text = T("ไฟล์ที่เลือก", "Selected file");
        ImageOutputLabel.Text = T("โฟลเดอร์เก็บรูป", "Image output folder");
        PickImageOutputButton.Content = T("เลือกโฟลเดอร์…", "Select folder…");
        PngOption.Content = T("PNG (คมชัด)", "PNG (high quality)");
        JpegOption.Content = T("JPEG (ไฟล์เล็ก)", "JPEG (smaller files)");
        ConvertPdfButton.Content = T("เริ่มแปลง PDF", "Convert PDF");
        PdfPreviewEmpty.Text = T("เลือก PDF เพื่อดู\nตัวอย่างหน้า", "Select a PDF to preview\npages");

        // 2. Images -> PDF
        ImagesToPdfTitle.Text = T("รวมรูปหลายรูปเป็น PDF ไฟล์เดียว", "Combine images into one PDF");
        ImagesToPdfHint.Text = T("ลากรูปทั้งหมดมาวาง แล้วจัดลำดับด้วยปุ่มขึ้น/ลง", "Drop images here, then arrange them with the up/down buttons");
        DropImagesText.Text = T("วางไฟล์รูปภาพที่นี่", "Drop image files here");
        AddImagesButton.Content = T("เลือกหลายรูป", "Select images");
        ImagePreviewEmpty.Text = T("เลือกรูปจากรายการ\nเพื่อดูภาพตัวอย่าง", "Select an image from the list\nto preview it");

        // 3. Merge PDF
        MergePdfTitle.Text = T("รวม PDF หลายไฟล์เป็นไฟล์เดียว", "Combine multiple PDFs into one file");
        MergePdfHint.Text = T("ลาก PDF ทั้งหมดมาวาง แล้วจัดลำดับก่อนรวม", "Drop PDFs here and arrange them before merging");
        DropPdfsText.Text = T("วางไฟล์ PDF ที่นี่", "Drop PDF files here");
        AddPdfsButton.Content = T("เลือกหลายไฟล์ PDF", "Select PDFs");
        MergePreviewEmpty.Text = T("เลือก PDF เพื่อดู\nตัวอย่างหน้า", "Select a PDF to preview\npages");

        // 4. Split PDF (New)
        SplitPdfTitle.Text = T("แยกหน้าเอกสาร PDF", "Split or extract PDF pages");
        SplitPdfHint.Text = T("ลาก PDF มาวาง แล้วเลือกว่าจะแยกทุกหน้า หรือเลือกเฉพาะบางช่วงหน้า", "Drop a PDF here and choose to split all pages or extract specific ranges");
        DropSplitPdfText.Text = T("วางไฟล์ PDF ที่นี่", "Drop a PDF here");
        PickSplitPdfButton.Content = T("เลือก PDF", "Select PDF");
        SplitSelectedFileLabel.Text = T("ไฟล์ที่เลือก", "Selected file");
        SplitModeLabel.Text = T("รูปแบบการแยก", "Split mode");
        SplitAllOption.Content = T("แยกทุกหน้าเป็นไฟล์เดี่ยว (1 หน้า/ไฟล์)", "Split all pages (1 page/file)");
        ExtractRangeOption.Content = T("แยกเฉพาะช่วงหน้าที่ระบุ", "Extract specific page range");
        PageRangeLabel.Text = T("หน้าที่ต้องการแยก (เช่น 1-3, 5, 8-10)", "Pages to extract (e.g. 1-3, 5, 8-10)");
        PageRangeHint.Text = T("ระบุเลขหน้าหรือช่วง คั่นด้วยเครื่องหมายจุลภาค (,)", "Enter page numbers or ranges separated by commas");
        SplitOutputLabel.Text = T("โฟลเดอร์ผลลัพธ์", "Output folder");
        PickSplitOutputButton.Content = T("เลือกโฟลเดอร์…", "Select folder…");
        StartSplitButton.Content = T("เริ่มแยก PDF", "Start split");
        SplitPreviewEmpty.Text = T("เลือก PDF เพื่อดู\nตัวอย่างหน้า", "Select a PDF to preview\npages");

        // 5. Rotate PDF (New)
        RotatePdfTitle.Text = T("หมุนหน้าเอกสาร PDF", "Rotate PDF pages");
        RotatePdfHint.Text = T("ลาก PDF มาวาง เลือกมุมที่ต้องการหมุน และหน้าที่ต้องการปรับ", "Drop a PDF here, choose the rotation angle and target pages");
        DropRotatePdfText.Text = T("วางไฟล์ PDF ที่นี่", "Drop a PDF here");
        PickRotatePdfButton.Content = T("เลือก PDF", "Select PDF");
        RotateSelectedFileLabel.Text = T("ไฟล์ที่เลือก", "Selected file");
        RotateAngleLabel.Text = T("มุมที่ต้องการหมุน", "Rotation angle");
        Rotate90Option.Content = T("90° ตามเข็ม", "90° Clockwise");
        Rotate180Option.Content = T("180° กลับหัว", "180° Flip");
        Rotate270Option.Content = T("270° ทวนเข็ม (90° ซ้าย)", "270° Counter-clockwise");
        RotateScopeLabel.Text = T("หน้าที่ต้องการหมุน", "Pages to rotate");
        RotateAllPagesOption.Content = T("ทุกหน้า", "All pages");
        RotateOddPagesOption.Content = T("เฉพาะหน้าคี่ (1, 3, 5...)", "Odd pages (1, 3, 5...)");
        RotateEvenPagesOption.Content = T("เฉพาะหน้าคู่ (2, 4, 6...)", "Even pages (2, 4, 6...)");
        RotateCustomPagesOption.Content = T("หน้าที่กำหนด", "Custom pages");
        RotateCustomRangeLabel.Text = T("หน้าที่ต้องการหมุน (เช่น 1, 3-5)", "Pages to rotate (e.g. 1, 3-5)");
        StartRotateButton.Content = T("บันทึก PDF ที่หมุนแล้ว", "Save rotated PDF");
        RotatePreviewEmpty.Text = T("เลือก PDF เพื่อดู\nตัวอย่างหน้า", "Select a PDF to preview\npages");

        // Common Buttons
        RemoveImageButton.Content = RemovePdfButton.Content = T("ลบ", "Remove");
        ImageUpButton.Content = PdfUpButton.Content = T("↑ ขึ้น", "↑ Up");
        ImageDownButton.Content = PdfDownButton.Content = T("↓ ลง", "↓ Down");
        ClearImagesButton.Content = ClearPdfsButton.Content = T("ล้างทั้งหมด", "Clear all");
        CreatePdfButton.Content = T("สร้าง PDF", "Create PDF");
        MergePdfsButton.Content = T("รวมเป็น PDF เดียว", "Merge PDFs");

        // Settings
        SettingsTitle.Text = T("ตั้งค่า", "Settings");
        SettingsHint.Text = T("BatchCraft จะจำค่าที่เลือกไว้สำหรับครั้งถัดไป", "BatchCraft remembers these choices for the next launch");
        ThemeLabel.Text = T("ธีม", "Theme");
        LanguageLabel.Text = T("ภาษา", "Language");
        DefaultFolderLabel.Text = T("โฟลเดอร์ผลลัพธ์เริ่มต้น", "Default output folder");
        PickDefaultFolderButton.Content = T("เลือกโฟลเดอร์…", "Select folder…");
        SettingsUpdateButton.Content = T("ตรวจอัปเดต", "Check updates");
        DarkThemeButton.Content = T("🌙 โหมดมืด", "🌙 Dark mode");
        LightThemeButton.Content = T("☀ โหมดสว่าง", "☀ Light mode");

        // About
        AboutTitle.Text = T("เกี่ยวกับ BatchCraft", "About BatchCraft");
        AboutPurposeTitle.Text = T("จุดประสงค์", "Purpose");
        AboutPurposeText.Text = T("BatchCraft สร้างขึ้นเพื่อให้ผู้ใช้ทั่วไปจัดการงาน PDF ประจำวันได้ง่าย รวดเร็ว และไม่ต้องส่งเอกสารขึ้นเว็บไซต์ภายนอก รองรับการแปลง PDF เป็นรูปภาพ รวมรูปเป็น PDF รวม PDF หลายไฟล์ แยกหน้า PDF และหมุนหน้า PDF", "BatchCraft helps everyday users complete common PDF tasks simply and quickly without uploading documents to external websites. It converts PDF pages to images, combines images into a PDF, merges multiple PDFs, splits PDFs, and rotates PDF pages.");
        AboutPrivacyTitle.Text = T("ความเป็นส่วนตัว", "Privacy");
        AboutPrivacyText.Text = T("การแปลงไฟล์ทั้งหมดทำงานภายในเครื่อง โปรแกรมเชื่อมต่ออินเทอร์เน็ตเฉพาะเมื่อตรวจสอบอัปเดตจาก GitHub เท่านั้น", "All file processing happens locally. The app only connects to the internet when checking GitHub for updates.");
        ProjectPageButton.Content = T("เปิดหน้าโปรเจกต์", "Open project page");
        CoffeeTitle.Text = T("สนับสนุนผู้พัฒนา", "Support the developer");
        CoffeeText.Text = T("สแกน TrueMoney หรือ PromptPay เพื่อเลี้ยงกาแฟและสนับสนุนการพัฒนาฟีเจอร์ใหม่", "Scan TrueMoney or PromptPay to buy the developer a coffee and support future improvements.");
        CoffeeButton.Content = T("เปิดช่องทางสนับสนุนออนไลน์", "Open online support");
        OpenOutputButton.Content = T("เปิดโฟลเดอร์ผลลัพธ์", "Open output folder");

        UpdateChoiceButtons();
        if (string.IsNullOrWhiteSpace(GlobalStatus.Text)) GlobalStatus.Text = T("พร้อมใช้งาน", "Ready");
    }

    private void ApplyTheme()
    {
        var dark = settings.Theme == "dark";
        SetBrush("WindowBrush", dark ? "#111827" : "#F3F6FA"); SetBrush("PanelBrush", dark ? "#1F2937" : "#FFFFFF");
        SetBrush("InputBrush", dark ? "#273449" : "#E9EEF5"); SetBrush("TextBrush", dark ? "#F3F4F6" : "#172033");
        SetBrush("MutedBrush", dark ? "#AAB4C3" : "#637083"); SetBrush("BorderBrush", dark ? "#475569" : "#C7D2E0");
        SetBrush("AccentBrush", "#3B82F6"); SetBrush("DropBrush", dark ? "#17233A" : "#EFF6FF");
        SetBrush("StatusBrush", dark ? "#182438" : "#E6EDF9"); SetBrush("SuccessBrush", dark ? "#34D399" : "#059669");
        UpdateChoiceButtons();
    }

    private void SetBrush(string key, string color) => Resources[key] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    private void SyncSettingsControls() { applyingSettings = true; DefaultFolderText.Text = settings.DefaultOutputFolder; applyingSettings = false; UpdateChoiceButtons(); }
    private void SaveSettings() => settingsService.Save(settings);
    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != MainTabs) return;
        UpdateChoiceButtons();
    }
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (MainTabs.SelectedItem == SettingsTab) MainTabs.SelectedIndex = 0;
        else MainTabs.SelectedItem = SettingsTab;
        UpdateChoiceButtons();
    }
    private void DarkTheme_Click(object sender, RoutedEventArgs e) => SelectTheme("dark");
    private void LightTheme_Click(object sender, RoutedEventArgs e) => SelectTheme("light");
    private void Thai_Click(object sender, RoutedEventArgs e) => SelectLanguage("th");
    private void English_Click(object sender, RoutedEventArgs e) => SelectLanguage("en");
    private void SelectTheme(string value) { if (applyingSettings || settings.Theme == value) return; settings.Theme = value; SaveSettings(); ApplyTheme(); }
    private void SelectLanguage(string value) { if (applyingSettings || settings.Language == value) return; settings.Language = value; SaveSettings(); ApplyLanguage(); }
    private void UpdateChoiceButtons()
    {
        if (!IsInitialized) return;
        SetChoice(DarkThemeButton, settings.Theme == "dark"); SetChoice(HeaderDarkButton, settings.Theme == "dark");
        SetChoice(LightThemeButton, settings.Theme == "light"); SetChoice(HeaderLightButton, settings.Theme == "light");
        SetChoice(ThaiButton, IsThai); SetChoice(HeaderThaiButton, IsThai);
        SetChoice(EnglishButton, !IsThai); SetChoice(HeaderEnglishButton, !IsThai);
        SetChoice(SettingsButton, MainTabs?.SelectedItem == SettingsTab);
    }
    private void SetChoice(System.Windows.Controls.Button button, bool selected) { button.Background = (System.Windows.Media.Brush)FindResource(selected ? "AccentBrush" : "InputBrush"); button.Foreground = (System.Windows.Media.Brush)FindResource(selected ? "TextBrush" : "MutedBrush"); button.BorderBrush = (System.Windows.Media.Brush)FindResource(selected ? "AccentBrush" : "BorderBrush"); }
    private void ZoomOut_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: Slider slider }) slider.Value = Math.Max(slider.Minimum, slider.Value - 0.1); }
    private void ZoomIn_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: Slider slider }) slider.Value = Math.Min(slider.Maximum, slider.Value + 0.1); }
    private void Preview_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Slider slider }) return;
        slider.Value = Math.Clamp(slider.Value + (e.Delta > 0 ? 0.1 : -0.1), slider.Minimum, slider.Maximum); e.Handled = true;
    }
    private void PickDefaultFolder_Click(object sender, RoutedEventArgs e) { var path = PickFolder(settings.DefaultOutputFolder); if (path is null) return; settings.DefaultOutputFolder = path; DefaultFolderText.Text = path; SaveSettings(); }

    // 1. PDF -> Images
    private void PickPdf_Click(object s, RoutedEventArgs e) { var files = PickFiles("PDF files (*.pdf)|*.pdf", false, settings.LastInputFolder); if (files.Length > 0) SetPdfInput(files[0]); }
    private void PickImageOutput_Click(object s, RoutedEventArgs e) { var path = PickFolder(PreferredOutputFolder(ImageOutputText.Text)); if (path is null) return; ImageOutputText.Text = path; RememberOutputFolder(path); }
    private async void ConvertPdf_Click(object s, RoutedEventArgs e)
    {
        if (!File.Exists(PdfInputText.Text) || !Directory.Exists(ImageOutputText.Text)) { ShowError(T("กรุณาเลือก PDF และโฟลเดอร์เก็บรูป", "Select a PDF and an image output folder.")); return; }
        var outputFolder = ImageOutputText.Text;
        await RunTool(async progress => { var count = await PdfTools.PdfToImagesAsync(PdfInputText.Text, outputFolder, JpegOption.IsChecked == true, progress); return new ToolResult(count, outputFolder); });
    }
    private void PdfInput_Drop(object sender, System.Windows.DragEventArgs e) { var pdf = DroppedFiles(e).FirstOrDefault(IsPdf); if (pdf is null) { ShowError(T("พื้นที่นี้รับเฉพาะไฟล์ PDF", "Only PDF files are accepted here.")); return; } SetPdfInput(pdf); GlobalStatus.Text = T("รับไฟล์ PDF แล้ว พร้อมแปลง", "PDF ready to convert"); }
    private void SetPdfInput(string file)
    {
        PdfInputText.Text = file;
        RememberInputFolder(file);
        ImageOutputText.Text = PreferredOutputFolder(Path.GetDirectoryName(file));
        pdfToImagesPath = file;
        pdfToImagesPage = 0;
        _ = LoadPdfPreviewAsync(file, pdfToImagesPage, PdfPreviewImage, PdfPreviewEmpty, PdfToImagesPageIndicator, (p, t) => { pdfToImagesPage = p; pdfToImagesTotalPages = t; });
    }
    private async void PdfToImagesPrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (pdfToImagesTotalPages <= 1 || pdfToImagesPage <= 0) return;
        pdfToImagesPage--;
        await LoadPdfPreviewAsync(pdfToImagesPath, pdfToImagesPage, PdfPreviewImage, PdfPreviewEmpty, PdfToImagesPageIndicator, (p, t) => { pdfToImagesPage = p; pdfToImagesTotalPages = t; });
    }
    private async void PdfToImagesNextPage_Click(object sender, RoutedEventArgs e)
    {
        if (pdfToImagesTotalPages <= 1 || pdfToImagesPage >= pdfToImagesTotalPages - 1) return;
        pdfToImagesPage++;
        await LoadPdfPreviewAsync(pdfToImagesPath, pdfToImagesPage, PdfPreviewImage, PdfPreviewEmpty, PdfToImagesPageIndicator, (p, t) => { pdfToImagesPage = p; pdfToImagesTotalPages = t; });
    }

    // 2. Images -> PDF
    private void AddImages_Click(object s, RoutedEventArgs e) => AddImages(PickFiles("Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff", true, settings.LastInputFolder));
    private void RemoveImage_Click(object s, RoutedEventArgs e) => RemoveSelected(ImageFiles, ImageList.SelectedItem as string);
    private void ImageUp_Click(object s, RoutedEventArgs e) => MoveSelected(ImageFiles, ImageList.SelectedItem as string, -1);
    private void ImageDown_Click(object s, RoutedEventArgs e) => MoveSelected(ImageFiles, ImageList.SelectedItem as string, 1);
    private void ClearImages_Click(object s, RoutedEventArgs e) => ImageFiles.Clear();
    private async void CreatePdf_Click(object s, RoutedEventArgs e)
    {
        if (ImageFiles.Count == 0) { ShowError(T("กรุณาเพิ่มรูปอย่างน้อย 1 รูป", "Add at least one image.")); return; }
        var output = PickOutputPdf("images-combined.pdf"); if (output is null) return;
        await RunTool(async progress => { await PdfTools.ImagesToPdfAsync(ImageFiles.ToList(), output, progress); return new ToolResult(ImageFiles.Count, Path.GetDirectoryName(output)!); });
    }
    private void Images_Drop(object sender, System.Windows.DragEventArgs e) => AddImages(DroppedFiles(e));
    private void AddImages(IEnumerable<string> files) { var accepted = files.Where(x => File.Exists(x) && ImageExtensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase)).ToList(); foreach (var file in accepted) if (!ImageFiles.Contains(file, StringComparer.OrdinalIgnoreCase)) ImageFiles.Add(file); if (accepted.Count > 0) { RememberInputFolder(accepted[0]); ImageList.SelectedItem ??= accepted[0]; } GlobalStatus.Text = accepted.Count > 0 ? T($"เพิ่มรูปแล้ว {accepted.Count} ไฟล์", $"Added {accepted.Count} image(s)") : T("ไม่พบไฟล์รูปภาพที่รองรับ", "No supported images found"); }
    private void ImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ImageList.SelectedItem is not string path || !File.Exists(path)) { ImagePreview.Source = null; ImagePreviewEmpty.Visibility = Visibility.Visible; return; }
        try { ImagePreview.Source = LoadBitmap(path); ImagePreviewEmpty.Visibility = Visibility.Collapsed; }
        catch { ImagePreview.Source = null; ImagePreviewEmpty.Visibility = Visibility.Visible; }
    }

    // 3. Merge PDF
    private void AddPdfs_Click(object s, RoutedEventArgs e) => AddPdfs(PickFiles("PDF files (*.pdf)|*.pdf", true, settings.LastInputFolder));
    private void RemovePdf_Click(object s, RoutedEventArgs e) => RemoveSelected(PdfFiles, PdfList.SelectedItem as string);
    private void PdfUp_Click(object s, RoutedEventArgs e) => MoveSelected(PdfFiles, PdfList.SelectedItem as string, -1);
    private void PdfDown_Click(object s, RoutedEventArgs e) => MoveSelected(PdfFiles, PdfList.SelectedItem as string, 1);
    private void ClearPdfs_Click(object s, RoutedEventArgs e) => PdfFiles.Clear();
    private async void MergePdfs_Click(object s, RoutedEventArgs e)
    {
        if (PdfFiles.Count < 2) { ShowError(T("กรุณาเพิ่ม PDF อย่างน้อย 2 ไฟล์", "Add at least two PDF files.")); return; }
        var output = PickOutputPdf("combined.pdf"); if (output is null) return;
        if (PdfFiles.Any(x => string.Equals(Path.GetFullPath(x), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase))) { ShowError(T("ชื่อไฟล์ผลลัพธ์ต้องไม่ซ้ำกับไฟล์ต้นฉบับ", "The output file must differ from the source files.")); return; }
        await RunTool(async progress => { await PdfTools.MergePdfsAsync(PdfFiles.ToList(), output, progress); return new ToolResult(PdfFiles.Count, Path.GetDirectoryName(output)!); });
    }
    private void Pdfs_Drop(object sender, System.Windows.DragEventArgs e) => AddPdfs(DroppedFiles(e));
    private void AddPdfs(IEnumerable<string> files) { var accepted = files.Where(IsPdf).ToList(); foreach (var file in accepted) if (!PdfFiles.Contains(file, StringComparer.OrdinalIgnoreCase)) PdfFiles.Add(file); if (accepted.Count > 0) { RememberInputFolder(accepted[0]); PdfList.SelectedItem ??= accepted[0]; } GlobalStatus.Text = accepted.Count > 0 ? T($"เพิ่ม PDF แล้ว {accepted.Count} ไฟล์", $"Added {accepted.Count} PDF(s)") : T("ไม่พบไฟล์ PDF", "No PDF files found"); }
    private async void PdfList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PdfList.SelectedItem is string path)
        {
            mergePdfPath = path;
            mergePdfPage = 0;
            await LoadPdfPreviewAsync(path, mergePdfPage, MergePreviewImage, MergePreviewEmpty, MergePageIndicator, (p, t) => { mergePdfPage = p; mergePdfTotalPages = t; });
        }
        else
        {
            mergePdfPath = null;
            MergePreviewImage.Source = null;
            MergePreviewEmpty.Visibility = Visibility.Visible;
            MergePageIndicator.Text = "0 / 0";
        }
    }
    private async void MergePrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (mergePdfTotalPages <= 1 || mergePdfPage <= 0) return;
        mergePdfPage--;
        await LoadPdfPreviewAsync(mergePdfPath, mergePdfPage, MergePreviewImage, MergePreviewEmpty, MergePageIndicator, (p, t) => { mergePdfPage = p; mergePdfTotalPages = t; });
    }
    private async void MergeNextPage_Click(object sender, RoutedEventArgs e)
    {
        if (mergePdfTotalPages <= 1 || mergePdfPage >= mergePdfTotalPages - 1) return;
        mergePdfPage++;
        await LoadPdfPreviewAsync(mergePdfPath, mergePdfPage, MergePreviewImage, MergePreviewEmpty, MergePageIndicator, (p, t) => { mergePdfPage = p; mergePdfTotalPages = t; });
    }

    // 4. Split PDF (New)
    private void SplitMode_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized) return;
        PageRangePanel.Visibility = ExtractRangeOption.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }
    private void PickSplitPdf_Click(object s, RoutedEventArgs e) { var files = PickFiles("PDF files (*.pdf)|*.pdf", false, settings.LastInputFolder); if (files.Length > 0) SetSplitPdfInput(files[0]); }
    private void SplitPdf_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var pdf = DroppedFiles(e).FirstOrDefault(IsPdf);
        if (pdf is null) { ShowError(T("พื้นที่นี้รับเฉพาะไฟล์ PDF", "Only PDF files are accepted here.")); return; }
        SetSplitPdfInput(pdf);
        GlobalStatus.Text = T("รับไฟล์ PDF แล้ว พร้อมแยกหน้า", "PDF ready to split");
    }
    private void SetSplitPdfInput(string file)
    {
        SplitPdfInputText.Text = file;
        RememberInputFolder(file);
        SplitOutputText.Text = PreferredOutputFolder(Path.GetDirectoryName(file));
        splitPdfPath = file;
        splitPdfPage = 0;
        _ = LoadPdfPreviewAsync(file, splitPdfPage, SplitPreviewImage, SplitPreviewEmpty, SplitPageIndicator, (p, t) => { splitPdfPage = p; splitPdfTotalPages = t; });
    }
    private void PickSplitOutput_Click(object s, RoutedEventArgs e)
    {
        var path = PickFolder(PreferredOutputFolder(SplitOutputText.Text));
        if (path is null) return;
        SplitOutputText.Text = path;
        RememberOutputFolder(path);
    }
    private async void StartSplit_Click(object sender, RoutedEventArgs e)
    {
        var inputPdf = SplitPdfInputText.Text;
        if (!File.Exists(inputPdf)) { ShowError(T("กรุณาเลือกไฟล์ PDF ก่อน", "Please select a PDF file first.")); return; }

        if (SplitAllOption.IsChecked == true)
        {
            var outputFolder = PreferredOutputFolder(SplitOutputText.Text);
            if (!Directory.Exists(outputFolder)) { ShowError(T("กรุณาเลือกโฟลเดอร์ผลลัพธ์", "Please select an output folder.")); return; }
            await RunTool(async progress =>
            {
                var count = await PdfTools.SplitPdfAllPagesAsync(inputPdf, outputFolder, progress);
                return new ToolResult(count, outputFolder);
            });
        }
        else
        {
            var range = PageRangeText.Text.Trim();
            if (string.IsNullOrWhiteSpace(range)) { ShowError(T("กรุณาระบุหน้าที่ต้องการแยก เช่น 1-3, 5", "Please specify the pages to extract, e.g. 1-3, 5")); return; }
            var defaultName = $"{Path.GetFileNameWithoutExtension(inputPdf)}-extracted.pdf";
            var outputPdf = PickOutputPdf(defaultName);
            if (outputPdf is null) return;
            await RunTool(async progress =>
            {
                var count = await PdfTools.ExtractPdfPagesAsync(inputPdf, range, outputPdf, progress);
                return new ToolResult(count, Path.GetDirectoryName(outputPdf)!);
            });
        }
    }
    private async void SplitPrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (splitPdfTotalPages <= 1 || splitPdfPage <= 0) return;
        splitPdfPage--;
        await LoadPdfPreviewAsync(splitPdfPath, splitPdfPage, SplitPreviewImage, SplitPreviewEmpty, SplitPageIndicator, (p, t) => { splitPdfPage = p; splitPdfTotalPages = t; });
    }
    private async void SplitNextPage_Click(object sender, RoutedEventArgs e)
    {
        if (splitPdfTotalPages <= 1 || splitPdfPage >= splitPdfTotalPages - 1) return;
        splitPdfPage++;
        await LoadPdfPreviewAsync(splitPdfPath, splitPdfPage, SplitPreviewImage, SplitPreviewEmpty, SplitPageIndicator, (p, t) => { splitPdfPage = p; splitPdfTotalPages = t; });
    }

    // 5. Rotate PDF (New)
    private int GetSelectedRotationAngle()
    {
        if (Rotate180Option.IsChecked == true) return 180;
        if (Rotate270Option.IsChecked == true) return 270;
        return 90;
    }
    private RotationPageScope GetSelectedRotationScope()
    {
        if (RotateOddPagesOption.IsChecked == true) return RotationPageScope.Odd;
        if (RotateEvenPagesOption.IsChecked == true) return RotationPageScope.Even;
        if (RotateCustomPagesOption.IsChecked == true) return RotationPageScope.Custom;
        return RotationPageScope.All;
    }
    private void RotateOption_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized) return;
        RotatePreviewTransform.Angle = GetSelectedRotationAngle();
    }
    private void RotateScope_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized) return;
        RotateCustomRangePanel.Visibility = RotateCustomPagesOption.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }
    private void PickRotatePdf_Click(object s, RoutedEventArgs e) { var files = PickFiles("PDF files (*.pdf)|*.pdf", false, settings.LastInputFolder); if (files.Length > 0) SetRotatePdfInput(files[0]); }
    private void RotatePdf_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var pdf = DroppedFiles(e).FirstOrDefault(IsPdf);
        if (pdf is null) { ShowError(T("พื้นที่นี้รับเฉพาะไฟล์ PDF", "Only PDF files are accepted here.")); return; }
        SetRotatePdfInput(pdf);
        GlobalStatus.Text = T("รับไฟล์ PDF แล้ว พร้อมหมุนหน้า", "PDF ready to rotate");
    }
    private void SetRotatePdfInput(string file)
    {
        RotatePdfInputText.Text = file;
        RememberInputFolder(file);
        rotatePdfPath = file;
        rotatePdfPage = 0;
        RotatePreviewTransform.Angle = GetSelectedRotationAngle();
        _ = LoadPdfPreviewAsync(file, rotatePdfPage, RotatePreviewImage, RotatePreviewEmpty, RotatePageIndicator, (p, t) => { rotatePdfPage = p; rotatePdfTotalPages = t; });
    }
    private async void StartRotate_Click(object sender, RoutedEventArgs e)
    {
        var inputPdf = RotatePdfInputText.Text;
        if (!File.Exists(inputPdf)) { ShowError(T("กรุณาเลือกไฟล์ PDF ก่อน", "Please select a PDF file first.")); return; }

        var defaultName = $"{Path.GetFileNameWithoutExtension(inputPdf)}-rotated.pdf";
        var outputPdf = PickOutputPdf(defaultName);
        if (outputPdf is null) return;

        var angle = GetSelectedRotationAngle();
        var scope = GetSelectedRotationScope();
        var custom = RotateCustomRangeText.Text.Trim();

        await RunTool(async progress =>
        {
            var count = await PdfTools.RotatePdfAsync(inputPdf, outputPdf, angle, scope, custom, progress);
            return new ToolResult(count, Path.GetDirectoryName(outputPdf)!);
        });
    }
    private async void RotatePrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (rotatePdfTotalPages <= 1 || rotatePdfPage <= 0) return;
        rotatePdfPage--;
        await LoadPdfPreviewAsync(rotatePdfPath, rotatePdfPage, RotatePreviewImage, RotatePreviewEmpty, RotatePageIndicator, (p, t) => { rotatePdfPage = p; rotatePdfTotalPages = t; });
    }
    private async void RotateNextPage_Click(object sender, RoutedEventArgs e)
    {
        if (rotatePdfTotalPages <= 1 || rotatePdfPage >= rotatePdfTotalPages - 1) return;
        rotatePdfPage++;
        await LoadPdfPreviewAsync(rotatePdfPath, rotatePdfPage, RotatePreviewImage, RotatePreviewEmpty, RotatePageIndicator, (p, t) => { rotatePdfPage = p; rotatePdfTotalPages = t; });
    }

    // Preview and Helper Methods
    private async Task LoadPdfPreviewAsync(
        string? path,
        int pageIndex,
        System.Windows.Controls.Image target,
        TextBlock empty,
        TextBlock indicator,
        Action<int, int>? onPageCountUpdated = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            target.Source = null;
            empty.Visibility = Visibility.Visible;
            indicator.Text = "0 / 0";
            return;
        }

        try
        {
            var total = await Task.Run(() => PdfToolService.GetPageCount(path));
            if (total == 0) throw new InvalidDataException();

            var safePage = Math.Clamp(pageIndex, 0, total - 1);
            onPageCountUpdated?.Invoke(safePage, total);
            indicator.Text = $"{safePage + 1} / {total}";

            var previewFolder = Path.Combine(Path.GetTempPath(), "BatchCraft", "Preview");
            Directory.CreateDirectory(previewFolder);
            var preview = Path.Combine(previewFolder, $"preview-{Math.Abs(path.GetHashCode())}-p{safePage}.png");

            await Task.Run(() =>
            {
                using var input = File.OpenRead(path);
                Conversion.SavePng(preview, input, page: safePage);
            });

            target.Source = LoadBitmap(preview);
            empty.Visibility = Visibility.Collapsed;
        }
        catch
        {
            target.Source = null;
            empty.Visibility = Visibility.Visible;
            indicator.Text = "0 / 0";
        }
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void RememberInputFolder(string file) { settings.LastInputFolder = Path.GetDirectoryName(file) ?? settings.LastInputFolder; SaveSettings(); }
    private void RememberOutputFolder(string folder) { settings.LastOutputFolder = folder; SaveSettings(); }
    private string PreferredOutputFolder(string? fallback = null) { if (Directory.Exists(settings.DefaultOutputFolder)) return settings.DefaultOutputFolder; if (Directory.Exists(settings.LastOutputFolder)) return settings.LastOutputFolder; if (Directory.Exists(fallback)) return fallback!; return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e) => await CheckForUpdatesAsync(false);
    private async Task CheckForUpdatesAsync(bool silent)
    {
        try
        {
            if (!silent) GlobalStatus.Text = T("กำลังตรวจอัปเดต…", "Checking for updates…");
            var update = await updater.CheckAsync();
            if (update is null) { if (!silent) GlobalStatus.Text = T("คุณใช้เวอร์ชันล่าสุดแล้ว", "You have the latest version"); return; }
            var answer = System.Windows.MessageBox.Show(T($"มี BatchCraft เวอร์ชัน {update.Version} ใหม่\nต้องการดาวน์โหลดและติดตั้งตอนนี้หรือไม่?", $"BatchCraft {update.Version} is available.\nDownload and install it now?"), T("พบเวอร์ชันใหม่", "Update available"), MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer != MessageBoxResult.Yes) return;
            var progress = new Progress<int>(x => { WorkProgress.Maximum = 100; WorkProgress.Value = x; ProgressText.Text = $"{x}%"; });
            await updater.DownloadAndInstallAsync(update, progress);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex) { if (!silent) ShowError(T($"ตรวจอัปเดตไม่สำเร็จ: {ex.Message}", $"Update check failed: {ex.Message}")); }
    }

    private async Task RunTool(Func<IProgress<ToolProgress>, Task<ToolResult>> operation)
    {
        try
        {
            IsEnabled = false;
            OpenOutputButton.Visibility = Visibility.Collapsed;
            WorkProgress.Value = 0;
            var progress = new Progress<ToolProgress>(p =>
            {
                WorkProgress.Maximum = Math.Max(1, p.Total);
                WorkProgress.Value = p.Current;
                ProgressText.Text = T($"กำลังทำ {p.Current} / {p.Total}", $"Processing {p.Current} / {p.Total}");
            });
            GlobalStatus.Text = T("กำลังทำงาน…", "Working…");
            var result = await operation(progress);
            latestOutputFolder = result.OutputFolder;
            RememberOutputFolder(result.OutputFolder);
            GlobalStatus.Text = T($"✓ เสร็จแล้ว {result.SuccessCount} รายการ / ผิดพลาด 0", $"✓ Completed {result.SuccessCount} item(s) / errors 0");
            ProgressText.Text = T("เสร็จสมบูรณ์", "Complete");
            OpenOutputButton.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            GlobalStatus.Text = T("ทำงานสำเร็จ 0 รายการ / ผิดพลาด 1", "Completed 0 items / errors 1");
            ProgressText.Text = "";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void OpenOutput_Click(object sender, RoutedEventArgs e) { if (Directory.Exists(latestOutputFolder)) Process.Start(new ProcessStartInfo("explorer.exe", latestOutputFolder) { UseShellExecute = true }); }
    private void ProjectPage_Click(object sender, RoutedEventArgs e) => OpenWeb("https://github.com/thenikonboy/BatchCraft");
    private void Coffee_Click(object sender, RoutedEventArgs e) => OpenWeb("https://github.com/sponsors/thenikonboy");
    private static void OpenWeb(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    private void Files_DragOver(object sender, System.Windows.DragEventArgs e) { e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None; e.Handled = true; }
    private static string[] DroppedFiles(System.Windows.DragEventArgs e) => e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[] ?? [];
    private static bool IsPdf(string path) => File.Exists(path) && Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    private static string[] PickFiles(string filter, bool multiple, string initial) { var dialog = new Microsoft.Win32.OpenFileDialog { Filter = filter, Multiselect = multiple, InitialDirectory = Directory.Exists(initial) ? initial : "" }; return dialog.ShowDialog() == true ? dialog.FileNames : []; }
    private string? PickOutputPdf(string name) { var dialog = new Microsoft.Win32.SaveFileDialog { Filter = T("ไฟล์ PDF (*.pdf)|*.pdf", "PDF files (*.pdf)|*.pdf"), FileName = name, DefaultExt = ".pdf", InitialDirectory = PreferredOutputFolder() }; var result = dialog.ShowDialog() == true ? dialog.FileName : null; if (result is not null) RememberOutputFolder(Path.GetDirectoryName(result)!); return result; }
    private static string? PickFolder(string initial) { using var dialog = new Forms.FolderBrowserDialog { InitialDirectory = Directory.Exists(initial) ? initial : "" }; return dialog.ShowDialog() == Forms.DialogResult.OK ? dialog.SelectedPath : null; }
    private static void RemoveSelected(ObservableCollection<string> list, string? item) { if (item is not null) list.Remove(item); }
    private static void MoveSelected(ObservableCollection<string> list, string? item, int offset) { if (item is null) return; var old = list.IndexOf(item); var next = old + offset; if (next >= 0 && next < list.Count) list.Move(old, next); }
    private static void ShowError(string text) => System.Windows.MessageBox.Show(text, "BatchCraft", MessageBoxButton.OK, MessageBoxImage.Warning);
    private sealed record ToolResult(int SuccessCount, string OutputFolder);
}

