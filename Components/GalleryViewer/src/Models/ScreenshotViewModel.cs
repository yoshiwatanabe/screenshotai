using AzureVisionAnalysis.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Domain.Enums;
using System.Windows.Input;

namespace GalleryViewer.Models;

public partial class ScreenshotViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid id;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string imagePath = string.Empty;

    [ObservableProperty]
    private string thumbnailPath = string.Empty;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private ScreenshotSource source;

    [ObservableProperty]
    private ScreenshotStatus status;

    [ObservableProperty]
    private string? aiDescription;

    [ObservableProperty]
    private double aiConfidence;

    [ObservableProperty]
    private bool hasAiAnalysis;

    [ObservableProperty]
    private bool isAnalyzing;

    [ObservableProperty]
    private string? analysisError;

    [ObservableProperty]
    private List<string> tags = new();

    [ObservableProperty]
    private string? extractedText;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private long fileSizeBytes;

    public string FormattedFileSize => FormatFileSize(FileSizeBytes);
    public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    public string StatusDisplayText => GetStatusDisplayText();
    public string SourceDisplayText => Source.ToString();

    public ICommand OpenImageCommand { get; }
    public ICommand OpenInExplorerCommand { get; }
    public ICommand CopyPathCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RenameCommand { get; }

    public ScreenshotViewModel()
    {
        OpenImageCommand = new RelayCommand(OpenImage, CanOpenImage);
        OpenInExplorerCommand = new RelayCommand(OpenInExplorer, CanOpenInExplorer);
        CopyPathCommand = new RelayCommand(CopyPath, CanCopyPath);
        DeleteCommand = new RelayCommand(RequestDelete);
        RenameCommand = new RelayCommand(RequestRename);
    }

    public static ScreenshotViewModel FromScreenshot(Screenshot screenshot, string imagePath, string thumbnailPath)
    {
        return new ScreenshotViewModel
        {
            Id = screenshot.Id,
            DisplayName = screenshot.DisplayName,
            ImagePath = imagePath,
            ThumbnailPath = thumbnailPath,
            CreatedAt = screenshot.CreatedAt,
            Source = screenshot.Source,
            Status = screenshot.Status,
            FileSizeBytes = File.Exists(imagePath) ? new FileInfo(imagePath).Length : 0
        };
    }

    public void UpdateAnalysisResult(VisionAnalysisResult result)
    {
        IsAnalyzing = false;
        
        if (result.Success)
        {
            AiDescription = result.GetComprehensiveDescription();
            AiConfidence = result.MainCaptionConfidence;
            HasAiAnalysis = true;
            Tags = result.Tags;
            ExtractedText = result.ExtractedText;
            AnalysisError = null;
        }
        else
        {
            AnalysisError = result.ErrorMessage;
            HasAiAnalysis = false;
        }
    }

    public void SetAnalyzing()
    {
        IsAnalyzing = true;
        HasAiAnalysis = false;
        AnalysisError = null;
    }

    private bool CanOpenImage() => File.Exists(ImagePath);
    private bool CanOpenInExplorer() => File.Exists(ImagePath);
    private bool CanCopyPath() => !string.IsNullOrEmpty(ImagePath);

    private void OpenImage()
    {
        try
        {
            if (File.Exists(ImagePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ImagePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
            System.Diagnostics.Debug.WriteLine($"Error opening image: {ex.Message}");
        }
    }

    private void OpenInExplorer()
    {
        try
        {
            if (File.Exists(ImagePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{ImagePath}\"");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening in explorer: {ex.Message}");
        }
    }

    private void CopyPath()
    {
        try
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                System.Windows.Clipboard.SetText(ImagePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error copying path: {ex.Message}");
        }
    }

    private void RequestDelete()
    {
        // This will be handled by the parent view model
        OnDeleteRequested?.Invoke(this);
    }

    private void RequestRename()
    {
        // This will be handled by the parent view model
        OnRenameRequested?.Invoke(this);
    }

    public event Action<ScreenshotViewModel>? OnDeleteRequested;
    public event Action<ScreenshotViewModel>? OnRenameRequested;

    private string GetStatusDisplayText()
    {
        if (IsAnalyzing)
            return "Analyzing...";
        
        if (HasAiAnalysis)
            return "Ready";
        
        if (!string.IsNullOrEmpty(AnalysisError))
            return "Analysis Failed";
        
        return Status switch
        {
            ScreenshotStatus.Processing => "Processing",
            ScreenshotStatus.Ready => "Ready",
            ScreenshotStatus.Failed => "Failed",
            ScreenshotStatus.Deleted => "Deleted",
            _ => "Unknown"
        };
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024:F1} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024 * 1024):F1} MB";
        
        return $"{bytes / (1024 * 1024 * 1024):F1} GB";
    }
}