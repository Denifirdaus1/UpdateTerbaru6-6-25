using DataWizard.Core.Services;
using DataWizard.UI.Services;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Text;
using IOPath = System.IO.Path;

namespace DataWizard.UI.Pages
{
    public sealed partial class ChatPage : Page
    {
        private string selectedFilePath = "";
        private readonly string outputTextPath = @"C:\DataSample\hasil_output.txt";
        private Stopwatch _processTimer;

        // Animation variables
        private Compositor _compositor;
        private ShapeVisual _borderVisual;
        private CompositionLinearGradientBrush _animatedGradientBrush;
        private Vector2KeyFrameAnimation _flowAnimation;
        private ScalarKeyFrameAnimation _fadeInAnimation;
        private ScalarKeyFrameAnimation _fadeOutAnimation;
        private bool _isAuraInitialized = false;

        public ChatPage()
        {
            this.InitializeComponent();
            PromptBox.TextChanged += PromptBox_TextChanged;
            LoadUserPreferences();
            _processTimer = new Stopwatch();

            var outputDir = IOPath.GetDirectoryName(outputTextPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }

        private void InitializeAuraAnimation()
        {
            if (_isAuraInitialized) return;

            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

            _borderVisual = _compositor.CreateShapeVisual();
            _borderVisual.Size = new Vector2((float)InputFormBorder.ActualWidth, (float)InputFormBorder.ActualHeight);
            _borderVisual.Opacity = 0.0f;

            var borderGeometry = _compositor.CreateRoundedRectangleGeometry();
            borderGeometry.Size = new Vector2((float)InputFormBorder.ActualWidth, (float)InputFormBorder.ActualHeight);
            borderGeometry.CornerRadius = new Vector2((float)InputFormBorder.CornerRadius.TopLeft);

            var borderShape = _compositor.CreateSpriteShape(borderGeometry);
            borderShape.StrokeThickness = (float)InputFormBorder.BorderThickness.Left;

            _animatedGradientBrush = _compositor.CreateLinearGradientBrush();
            _animatedGradientBrush.StartPoint = new Vector2(0, 0);
            _animatedGradientBrush.EndPoint = new Vector2(1, 0);

            _animatedGradientBrush.ColorStops.Add(_compositor.CreateColorGradientStop(0.0f, Color.FromArgb(255, 255, 122, 0)));
            _animatedGradientBrush.ColorStops.Add(_compositor.CreateColorGradientStop(0.5f, Color.FromArgb(255, 229, 0, 122)));
            _animatedGradientBrush.ColorStops.Add(_compositor.CreateColorGradientStop(1.0f, Color.FromArgb(255, 109, 40, 217)));

            borderShape.StrokeBrush = _animatedGradientBrush;
            _borderVisual.Shapes.Add(borderShape);

            _flowAnimation = _compositor.CreateVector2KeyFrameAnimation();
            _flowAnimation.InsertKeyFrame(0.0f, new Vector2(-1, 0));
            _flowAnimation.InsertKeyFrame(1.0f, new Vector2(1, 0));
            _flowAnimation.Duration = TimeSpan.FromSeconds(3);
            _flowAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            _fadeInAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _fadeInAnimation.InsertKeyFrame(1.0f, 1.0f);
            _fadeInAnimation.Duration = TimeSpan.FromMilliseconds(400);

            _fadeOutAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _fadeOutAnimation.InsertKeyFrame(1.0f, 0.0f);
            _fadeOutAnimation.Duration = TimeSpan.FromMilliseconds(400);

            ElementCompositionPreview.SetElementChildVisual(InputFormBorder, _borderVisual);

            InputFormBorder.SizeChanged += (s, e) =>
            {
                if (_borderVisual != null)
                {
                    _borderVisual.Size = e.NewSize.ToVector2();
                    borderGeometry.Size = e.NewSize.ToVector2();
                }
            };

            _isAuraInitialized = true;
        }

        private void InputFormBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            InitializeAuraAnimation();
            _borderVisual.StartAnimation("Opacity", _fadeInAnimation);
            _animatedGradientBrush.StartAnimation("Offset", _flowAnimation);
        }

        private void InputFormBorder_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isAuraInitialized) return;
            _borderVisual.StartAnimation("Opacity", _fadeOutAnimation);
            _animatedGradientBrush.StopAnimation("Offset");
        }

        private void LoadUserPreferences()
        {
            try
            {
                WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
                ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
                ExcelFormatButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
                OutputFormatBox.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user preferences: {ex.Message}");
                ExcelFormatButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
                OutputFormatBox.SelectedIndex = 1;
            }
        }

        private async Task ShowDialogAsync(string title, string content)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void PromptBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CharCountText.Text = $"{PromptBox.Text.Length}/1000";
        }

        private async Task<bool> SelectFileAsync()
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".xls");
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".docx");
            picker.FileTypeFilter.Add(".pdf");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                selectedFilePath = file.Path;

                // Update the UI to show selected file
                SelectedFileText.Text = IOPath.GetFileName(file.Path);
                SelectedFileDisplay.Visibility = Visibility.Visible;

                return true;
            }
            return false;
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectFileAsync();
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFilePath = "";
            SelectedFileDisplay.Visibility = Visibility.Collapsed;
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            string prompt = PromptBox.Text.Trim();
            string outputFormat = (OutputFormatBox.SelectedItem as ComboBoxItem)?.Content?.ToString().ToLower() ?? "txt";
            string mode = (ModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString().ToLower() ?? "file";

            if ((mode != "prompt-only" && string.IsNullOrWhiteSpace(selectedFilePath)) || string.IsNullOrWhiteSpace(prompt))
            {
                await ShowDialogAsync("Validation Error", "Harap pilih file (kecuali prompt-only) dan masukkan prompt terlebih dahulu.");
                return;
            }

            if (mode == "ocr" && !string.IsNullOrEmpty(selectedFilePath))
            {
                string[] validImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };
                string fileExtension = IOPath.GetExtension(selectedFilePath).ToLower();

                if (!validImageExtensions.Contains(fileExtension))
                {
                    await ShowDialogAsync("Validation Error",
                        $"File yang dipilih bukan format gambar yang didukung.\n" +
                        $"Format yang didukung: JPG, JPEG, PNG, BMP, TIFF\n" +
                        $"File Anda: {fileExtension}");
                    return;
                }
            }

            try
            {
                _processTimer.Restart();

                // Hide welcome and show preview
                WelcomePanel.Visibility = Visibility.Collapsed;
                PreviewWelcomeState.Visibility = Visibility.Collapsed;
                PreviewContentContainer.Visibility = Visibility.Visible;
                ProcessingBadge.Visibility = Visibility.Visible;
                CopyPreviewButton.Visibility = Visibility.Collapsed;
                SavePreviewButton.Visibility = Visibility.Collapsed;
                PreviewFooter.Visibility = Visibility.Visible;

                PreviewContent.Text = "Memproses data... Mohon tunggu.";
                Debug.WriteLine($"Starting Python process with mode: {mode}, format: {outputFormat}");

                string result = await PythonRunner.RunPythonScriptAsync(
                    mode == "prompt-only" ? "none" : selectedFilePath,
                    outputTextPath,
                    prompt,
                    outputFormat,
                    mode
                );

                _processTimer.Stop();
                int processingTimeMs = (int)_processTimer.ElapsedMilliseconds;
                Debug.WriteLine($"Python process completed in {processingTimeMs}ms with result: {result}");

                ProcessingBadge.Visibility = Visibility.Collapsed;

                if (result == "Success" && File.Exists(outputTextPath))
                {
                    string hasil = File.ReadAllText(outputTextPath);
                    Debug.WriteLine($"Output file content length: {hasil.Length}");

                    if (hasil.StartsWith("[ERROR]") || hasil.StartsWith("[GAGAL]"))
                    {
                        PreviewContent.Text = $"Proses gagal: {hasil}";
                        Debug.WriteLine($"Process failed with error: {hasil}");
                        return;
                    }

                    PreviewContent.Text = hasil;
                    CopyPreviewButton.Visibility = Visibility.Visible;
                    SavePreviewButton.Visibility = Visibility.Visible;

                    // Update processing time and word count
                    ProcessingTimeText.Text = $"Processing time: {processingTimeMs}ms";
                    WordCountText.Text = $"Words: {hasil.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length}";

                    await ProcessOutputFiles(outputFormat, processingTimeMs);
                }
                else
                {
                    PreviewContent.Text = $"❌ Gagal: {result}";
                    Debug.WriteLine($"Process failed with result: {result}");
                }
            }
            catch (Exception ex)
            {
                _processTimer.Stop();
                ProcessingBadge.Visibility = Visibility.Collapsed;
                Debug.WriteLine($"Error in RunButton_Click: {ex}");
                string errorMessage = $"Terjadi kesalahan aplikasi:\n{ex.Message}";
                PreviewContent.Text = errorMessage;
                await ShowDialogAsync("Application Error", errorMessage);
            }
        }

        private async Task ProcessOutputFiles(string outputFormat, int processingTimeMs)
        {
            try
            {
                string parsedExcelPath = PythonRunner.GetParsedExcelPath(outputTextPath);
                string outputFileName = string.Empty;
                string outputFilePath = string.Empty;

                if (outputFormat == "excel")
                {
                    await Task.Delay(2000);

                    if (File.Exists(parsedExcelPath))
                    {
                        outputFilePath = parsedExcelPath;
                        outputFileName = IOPath.GetFileName(parsedExcelPath);
                        ResultFileText.Text = outputFileName;

                        // Show file info
                        FileInfoDisplay.Visibility = Visibility.Visible;
                        FileInfoTitle.Text = "Excel File Created";
                        FileInfoDetails.Text = $"File: {outputFileName} | Size: {new FileInfo(outputFilePath).Length / 1024} KB";

                        Debug.WriteLine($"Excel file created successfully: {outputFileName}");
                    }
                    else
                    {
                        PreviewContent.Text += "\n\n⚠️ File hasil parsing Excel tidak ditemukan.";
                        Debug.WriteLine("Excel file not found after processing");
                    }
                }
                else if (outputFormat == "word")
                {
                    string basePath = IOPath.GetDirectoryName(outputTextPath);
                    string baseName = IOPath.GetFileNameWithoutExtension(outputTextPath);
                    string wordPath = IOPath.Combine(basePath, $"{baseName}_output.docx");

                    await Task.Delay(2000);

                    if (File.Exists(wordPath))
                    {
                        outputFilePath = wordPath;
                        outputFileName = IOPath.GetFileName(wordPath);
                        ResultFileText.Text = outputFileName;

                        // Show file info
                        FileInfoDisplay.Visibility = Visibility.Visible;
                        FileInfoTitle.Text = "Word Document Created";
                        FileInfoDetails.Text = $"File: {outputFileName} | Size: {new FileInfo(outputFilePath).Length / 1024} KB";

                        Debug.WriteLine($"Word file created successfully: {outputFileName}");
                    }
                    else
                    {
                        PreviewContent.Text += "\n\n⚠️ File Word tidak ditemukan";
                        Debug.WriteLine("Word file not found after processing");
                    }
                }

                if (!string.IsNullOrEmpty(outputFilePath) && File.Exists(outputFilePath))
                {
                    FileInfo fileInfo = new FileInfo(outputFilePath);
                    Debug.WriteLine($"Output file created: {outputFileName}, Size: {fileInfo.Length} bytes, Processing time: {processingTimeMs}ms");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing output files: {ex.Message}");
            }
        }

        private async void FileToFileButton_Click(object sender, RoutedEventArgs e)
        {
            ModeBox.SelectedIndex = 0;
            FileInputControls.Visibility = Visibility.Visible;
            await SelectFileAsync();
        }

        private async void PromptToFileButton_Click(object sender, RoutedEventArgs e)
        {
            ModeBox.SelectedIndex = 2;
            FileInputControls.Visibility = Visibility.Visible;
            await ShowDialogAsync("Reminder", "Please select your output format (Word or Excel) before proceeding.");
            PromptBox.Focus(FocusState.Programmatic);
        }

        private async void OcrToFileButton_Click(object sender, RoutedEventArgs e)
        {
            ModeBox.SelectedIndex = 1;
            FileInputControls.Visibility = Visibility.Visible;
            await SelectFileAsync();
        }

        private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowDialogAsync("History", "Fitur riwayat sementara dinonaktifkan.\nSemua proses berjalan tanpa menyimpan riwayat.");
        }

        private async void OutputFormatButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string format = clickedButton.Tag.ToString();

            WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
            ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;

            clickedButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
            OutputFormatBox.SelectedIndex = format == "word" ? 2 : 1;

            Debug.WriteLine($"User selected format: {format}");
        }

        private void RefreshPromptButton_Click(object sender, RoutedEventArgs e)
        {
            PromptBox.Text = "";
            selectedFilePath = "";
            SelectedFileDisplay.Visibility = Visibility.Collapsed;
            FileInputControls.Visibility = Visibility.Collapsed;

            OutputFormatBox.SelectedIndex = 0;
            ModeBox.SelectedIndex = 0;

            WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
            ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;

            // Reset preview to welcome state
            WelcomePanel.Visibility = Visibility.Visible;
            PreviewWelcomeState.Visibility = Visibility.Visible;
            PreviewContentContainer.Visibility = Visibility.Collapsed;
            ProcessingBadge.Visibility = Visibility.Collapsed;
            PreviewFooter.Visibility = Visibility.Collapsed;
            FileInfoDisplay.Visibility = Visibility.Collapsed;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(DataWizard.UI.Pages.HomePage));
        }

        private async void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectFileAsync();
        }

        private async void UseImageButton_Click(object sender, RoutedEventArgs e)
        {
            ModeBox.SelectedIndex = 1;
            await SelectFileAsync();
        }

        private async void CopyPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(PreviewContent.Text);
                Clipboard.SetContent(dataPackage);

                // Show brief confirmation
                PreviewTitle.Text = "Preview - Copied!";
                await Task.Delay(2000);
                PreviewTitle.Text = "Preview";
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Error", $"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Excel Files", new List<string>() { ".xlsx" });
            savePicker.FileTypeChoices.Add("Word Documents", new List<string>() { ".docx" });
            savePicker.FileTypeChoices.Add("Text Files", new List<string>() { ".txt" });
            savePicker.SuggestedFileName = ResultFileText.Text;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    // Copy the generated file to the selected location
                    string sourcePath = "";
                    string outputFormat = (OutputFormatBox.SelectedItem as ComboBoxItem)?.Content?.ToString().ToLower() ?? "txt";

                    if (outputFormat == "excel")
                    {
                        sourcePath = PythonRunner.GetParsedExcelPath(outputTextPath);
                    }
                    else if (outputFormat == "word")
                    {
                        string basePath = IOPath.GetDirectoryName(outputTextPath);
                        string baseName = IOPath.GetFileNameWithoutExtension(outputTextPath);
                        sourcePath = IOPath.Combine(basePath, $"{baseName}_output.docx");
                    }
                    else
                    {
                        sourcePath = outputTextPath;
                    }

                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, file.Path, true);

                        // Show brief confirmation
                        FileInfoTitle.Text = "File Saved Successfully";
                        await Task.Delay(2000);
                        FileInfoTitle.Text = "Output File Created";

                        Debug.WriteLine($"File saved to: {file.Path}");
                    }
                    else
                    {
                        await ShowDialogAsync("Error", "Source file not found. Please generate the output first.");
                    }
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Error", $"Error saving file: {ex.Message}");
                }
            }
        }
    }
}