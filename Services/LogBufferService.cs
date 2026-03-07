using System.Collections.ObjectModel;

namespace AetherVault.Services;

/// <summary>
/// Single log line for binding in the in-app log viewer.
/// </summary>
public sealed class LogEntry
{
    public string Text { get; }
    public LogLevel Level { get; }

    public LogEntry(string text, LogLevel level)
    {
        Text = text;
        Level = level;
    }
}

/// <summary>
/// In-memory buffer of recent log entries for the debug Log View.
/// Subscribes to <see cref="Logger.LogEmitted"/> and keeps the last N entries on the main thread.
/// </summary>
public interface ILogBufferService
{
    ObservableCollection<LogEntry> Entries { get; }
    void Clear();
}

public sealed class LogBufferService : ILogBufferService
{
    private const int MaxEntries = 500;

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public LogBufferService()
    {
        Logger.LogEmitted += OnLogEmitted;
    }

    private void OnLogEmitted(string line, LogLevel level)
    {
        var entry = new LogEntry(line, level);
        if (MainThread.IsMainThread)
            AddEntry(entry);
        else
            MainThread.BeginInvokeOnMainThread(() => AddEntry(entry));
    }

    private void AddEntry(LogEntry entry)
    {
        Entries.Add(entry);
        while (Entries.Count > MaxEntries)
            Entries.RemoveAt(0);
    }

    public void Clear()
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(Clear);
            return;
        }
        Entries.Clear();
    }
}
