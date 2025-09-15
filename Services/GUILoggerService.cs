#if WINDOWS
using DeviceAgent.GUI;
#endif

namespace DeviceAgent.Services;

public interface IGUILoggerService
{
    void LogMessage(string message);
#if WINDOWS
    void SetMainForm(MainForm? mainForm);
#endif
}

public class GUILoggerService : IGUILoggerService
{
#if WINDOWS
    private MainForm? _mainForm;
    private readonly Queue<string> _pendingMessages = new();

    public void SetMainForm(MainForm? mainForm)
    {
        _mainForm = mainForm;
        
        // Send any pending messages
        while (_pendingMessages.Count > 0)
        {
            var message = _pendingMessages.Dequeue();
            _mainForm?.LogMessage(message);
        }
    }

    public void LogMessage(string message)
    {
        if (_mainForm != null)
        {
            _mainForm.LogMessage(message);
        }
        else
        {
            _pendingMessages.Enqueue(message);
            
            // Keep only the last 100 messages to prevent memory issues
            while (_pendingMessages.Count > 100)
            {
                _pendingMessages.Dequeue();
            }
        }
    }
#else
    // Linux/Non-GUI implementation - just ignore GUI logging
    public void LogMessage(string message)
    {
        // No-op for non-GUI platforms
    }
#endif
}
