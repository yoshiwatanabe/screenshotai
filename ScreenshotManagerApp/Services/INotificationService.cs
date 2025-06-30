namespace ScreenshotManagerApp.Services;

public interface INotificationService
{
    /// <summary>
    /// Shows a success notification
    /// </summary>
    /// <param name="message">Success message</param>
    void ShowSuccess(string message);

    /// <summary>
    /// Shows an error notification
    /// </summary>
    /// <param name="message">Error message</param>
    void ShowError(string message);

    /// <summary>
    /// Shows an informational notification
    /// </summary>
    /// <param name="message">Information message</param>
    void ShowInfo(string message);

    /// <summary>
    /// Shows a warning notification
    /// </summary>
    /// <param name="message">Warning message</param>
    void ShowWarning(string message);

    /// <summary>
    /// Shows a notification when AI analysis is completed
    /// </summary>
    /// <param name="description">AI-generated description</param>
    void ShowAnalysisComplete(string description);
}