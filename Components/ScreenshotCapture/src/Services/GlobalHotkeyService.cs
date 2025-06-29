using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenshotCapture.Services;

public class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
{
    private readonly ILogger<GlobalHotkeyService> _logger;
    private readonly object _lock = new();
    
    // Win32 API constants for hotkey registration
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000; // Unique ID for our hotkey
    
    // Modifier keys for Ctrl+Print Screen
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_SNAPSHOT = 0x2C; // Print Screen key
    
    // Fallback: Win+Shift+C
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_SHIFT = 0x0004;
    private const uint VK_C = 0x43;
    
    private bool _isRegistered;
    private bool _usingFallbackHotkey;
    private Action? _captureCallback;
    private HotkeyMessageWindow? _messageWindow;

    public bool IsActive => _isRegistered;

    public GlobalHotkeyService(ILogger<GlobalHotkeyService> logger)
    {
        _logger = logger;
    }

    public bool RegisterCaptureHotkey(Action onCaptureRequested)
    {
        lock (_lock)
        {
            if (_isRegistered)
            {
                _logger.LogWarning("Hotkey already registered. Unregistering first.");
                UnregisterCaptureHotkey();
            }

            _captureCallback = onCaptureRequested;
            _messageWindow = new HotkeyMessageWindow(OnHotkeyPressed);

            // Try primary hotkey: Ctrl+Print Screen
            if (RegisterHotkey(HOTKEY_ID, MOD_CONTROL, VK_SNAPSHOT))
            {
                _usingFallbackHotkey = false;
                _isRegistered = true;
                _logger.LogInformation("Successfully registered primary hotkey: Ctrl+Print Screen");
                return true;
            }

            _logger.LogWarning("Failed to register primary hotkey, trying fallback: Win+Shift+C");
            
            // Try fallback: Win+Shift+C
            if (RegisterHotkey(HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_C))
            {
                _usingFallbackHotkey = true;
                _isRegistered = true;
                _logger.LogInformation("Successfully registered fallback hotkey: Win+Shift+C");
                return true;
            }

            _logger.LogError("Failed to register any hotkey combination");
            _messageWindow?.Dispose();
            _messageWindow = null;
            return false;
        }
    }

    public void UnregisterCaptureHotkey()
    {
        lock (_lock)
        {
            if (_isRegistered)
            {
                UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
                _isRegistered = false;
                _logger.LogInformation("Hotkey unregistered successfully");
            }

            _messageWindow?.Dispose();
            _messageWindow = null;
            _captureCallback = null;
        }
    }

    public string GetHotkeyDisplayString()
    {
        if (!_isRegistered) return "None";
        return _usingFallbackHotkey ? "Win+Shift+C" : "Ctrl+Print Screen";
    }

    private bool RegisterHotkey(int id, uint modifiers, uint key)
    {
        try
        {
            return RegisterHotKey(IntPtr.Zero, id, modifiers, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during hotkey registration");
            return false;
        }
    }

    private void OnHotkeyPressed()
    {
        try
        {
            _logger.LogDebug("Global hotkey pressed, triggering capture");
            _captureCallback?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling hotkey press");
        }
    }

    public void Dispose()
    {
        UnregisterCaptureHotkey();
    }

    #region Win32 API

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    #endregion

    #region Message Window for Hotkey Handling

    private class HotkeyMessageWindow : NativeWindow, IDisposable
    {
        private readonly Action _hotkeyCallback;

        public HotkeyMessageWindow(Action hotkeyCallback)
        {
            _hotkeyCallback = hotkeyCallback;
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                _hotkeyCallback?.Invoke();
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                DestroyHandle();
            }
        }
    }

    #endregion
}