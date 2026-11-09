using System;
using System.IO;
using UnityEngine;

public class LogOutput : MonoBehaviour
{
    private string _fileName = "debug_log_world.txt";
    private StreamWriter _writer;

    private void OnEnable()
    {
        // Timestamped name avoids file-lock conflicts between runs
        string path = Path.Combine("D:\\", _fileName);

        try
        {
            var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(stream) { AutoFlush = true };
            Debug.Log("Logging to: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not open log file: {e}");
            return;
        }

        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
        _writer?.Dispose();
        _writer = null;
    }

    private void OnLogMessageReceived(string message, string stackTrace, LogType type)
    {
        if (_writer == null) return;

        try
        {
            _writer.WriteLine($"[{type}] {message}");
            if (type == LogType.Error || type == LogType.Exception)
                _writer.WriteLine(stackTrace);
        }
        catch (Exception) { /* don't log here, it can cause recursion */ }
    }
}