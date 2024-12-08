using System.Diagnostics;


public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

public class Logger
{

    public static Logger Default = new Logger(LogLevel.Info);

    private List<TextWriter> _writers = [];

    private bool ShouldDebug = false;

    private readonly LogLevel _logLevel;
    public Logger(LogLevel logLevel = LogLevel.Info, string? logFilePath = null, IEnumerable<TextWriter>? writer = null)
    {
        _logLevel = logLevel;

        // We were not given a writer, so add the console
        if (writer == null)
        {
            _writers.Add(Console.Out);
        }
        else
        {
            _writers.AddRange(writer.Where(x => x != null));
        }

        // Determine if we should debug
        if (Debugger.IsLogging())
        {
            ShouldDebug = true;
        }

        // append a new writer
        if (logFilePath != null) _writers.Add(GetFileWriter(logFilePath));



    }

    public static TextWriter GetFileWriter(string path)
    {
        return new AppendingWriter(path);
    }


    private string Header => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";


    private void WriteLog(LogLevel level, string message)
    {
        if (level >= _logLevel)
        {
            string h = Header + $"[{level}]: ";
            string logMessage = $"{h}{message}";
            //print to all attached text writers
            foreach (TextWriter writer in _writers)
            {
                writer?.WriteLine(logMessage);
            }

            //and check if we should use debug output
            if (ShouldDebug)
            {
                System.Diagnostics.Debug.WriteLine(logMessage);
            }

        }
    }

    public void Log(string message, LogLevel level)
    {
        WriteLog(level, message);
    }

    public void Log(string message)
    {
        WriteLog(_logLevel, message);

    }

    public void Debug(string message)
    {
        WriteLog(LogLevel.Debug, message);
    }

    public void Info(string message)
    {
        WriteLog(LogLevel.Info, message);
    }

    public void Warn(string message)
    {
        WriteLog(LogLevel.Warn, message);
    }

    public void Error(string message)
    {
        WriteLog(LogLevel.Error, message);
    }

    public void Fatal(string message)
    {
        WriteLog(LogLevel.Fatal, message);
    }

    public void WriteBlock(string header, string message, LogLevel? level = null)
    {
        if (level == null) level = _logLevel;
        if (level < _logLevel) return;

        string h = Header + $" [{level}]: ";
        string logHeader = $"{h}{header}";
        string gap = "".PadLeft(h.Length + 1, ' ');
        var lines = message.Split('\n');

        //print to all attached text writers
        foreach (TextWriter writer in _writers)
        {
            writer?.WriteLine(logHeader);
            foreach (var line in lines) writer?.WriteLine($"{gap}* {line}");
        }

        if (ShouldDebug)
        {
            System.Diagnostics.Debug.WriteLine(logHeader);
            foreach (var line in lines) System.Diagnostics.Debug.WriteLine($"{gap} * {line}");

        }



    }




}

