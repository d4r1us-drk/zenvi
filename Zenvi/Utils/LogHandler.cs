using NLog;
using NLog.Config;
using NLog.Targets;

namespace Zenvi.Utils;

/// <summary>
/// Class that handles event logging in the application. Allows logging of
/// informational, debug, error, warning, and fatal messages. Utilizes NLog
/// to configure and manage logs, with the capability to store logs
/// in a file and/or display them on the console.
/// </summary>
/// <typeparam name="T">The type associated with the event logging.</typeparam>
public class LogHandler<T>
{
    // NLog configuration used by the LogHandler class.
    private static readonly LoggingConfiguration _config = CreateConfiguration();

    // Logger instance used to log events, configured for the type T.
    private readonly Logger _log = LogManager.GetLogger(typeof(T).FullName);

    /// <summary>
    /// Creates and configures the LoggingConfiguration instance for NLog.
    /// </summary>
    /// <returns>Event logging configuration.</returns>
    private static LoggingConfiguration CreateConfiguration()
    {
        var config = new LoggingConfiguration();

        var logfile = new FileTarget("logfile") { FileName = "Zenvi_Server.log" };
        var logconsole = new ConsoleTarget("logconsole");

        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);
        config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logfile);

        return config;
    }

    /// <summary>
    /// Constructor of the LogHandler class. Assigns the static configuration to LogManager.
    /// </summary>
    public LogHandler()
    {
        LogManager.Configuration = _config;
    }

    /// <summary>
    /// Logs an informational message to the event log.
    /// </summary>
    /// <param name="message">The informational message to log.</param>
    public void LogInfo(string message)
    {
        _log.Info(message);
    }

    /// <summary>
    /// Logs a debug message to the event log.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    public void LogDebug(string message)
    {
        _log.Debug(message);
    }

    /// <summary>
    /// Logs an error message to the event log, along with the associated exception.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    /// <param name="ex">The exception associated with the error message.</param>
    public void LogError(string message, Exception ex)
    {
        _log.Error(ex, message);
    }

    /// <summary>
    /// Logs a warning message to the event log, along with the associated exception.
    /// </summary>
    /// <param name="message">The warning message to log.</param>
    /// <param name="ex">The exception associated with the warning message.</param>
    public void LogWarn(string message, Exception ex)
    {
        _log.Warn(ex, message);
    }

    /// <summary>
    /// Logs a fatal message to the event log, along with the associated exception.
    /// </summary>
    /// <param name="message">The fatal message to log.</param>
    /// <param name="ex">The exception associated with the fatal message.</param>
    public void LogFatal(string message, Exception ex)
    {
        _log.Fatal(ex, message);
    }
}