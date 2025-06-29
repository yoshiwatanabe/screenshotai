namespace ScreenshotCapture.Services;

public interface IGlobalHotkeyService
{
    /// <summary>
    /// Registers the global hotkey for screenshot capture (Ctrl+Print Screen)
    /// </summary>
    /// <param name="onCaptureRequested">Callback when hotkey is pressed</param>
    /// <returns>True if registration successful, false otherwise</returns>
    bool RegisterCaptureHotkey(Action onCaptureRequested);

    /// <summary>
    /// Unregisters the global hotkey
    /// </summary>
    void UnregisterCaptureHotkey();

    /// <summary>
    /// Gets the currently registered hotkey combination as a display string
    /// </summary>
    string GetHotkeyDisplayString();

    /// <summary>
    /// Checks if the hotkey service is currently active
    /// </summary>
    bool IsActive { get; }
}

public class HotkeyRegistrationException : Exception
{
    public HotkeyRegistrationException(string message) : base(message) { }
    public HotkeyRegistrationException(string message, Exception innerException) : base(message, innerException) { }
}