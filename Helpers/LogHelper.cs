using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Kurohana.Helpers
{
    public static class LogHelper
    {
        public static ILogger? Logger { get; set; } // initialize logger
        private static readonly List<string> LogBuffer = new(); // create a Buffer for bulk logging
        private static readonly object LockObject = new(); // create a lock

        public static async Task StartBufferFlusherAsync(CancellationToken Ctoken, int FlushingInvterval = 5000)
        {
            Logger?.LogInformation("Logging system started...");
            while (!Ctoken.IsCancellationRequested) // send buffer every 5 seconds
            {
                await Task.Delay(FlushingInvterval); // wait for given interval

                List<string> bufferCopy; // make a list for copied buffer

                lock (LockObject)
                {
                    if (LogBuffer.Count == 0) continue;
                    bufferCopy = new List<string>(LogBuffer);
                    LogBuffer.Clear();
                }
                Logger?.LogInformation("Flushing log buffer...");

                await WriteLogAsync(bufferCopy); // write logs in bulk if there are any pending logs
            }

            if (LogBuffer.Count > 0)
            {
                List<string> bufferCopy;
                lock (LockObject)
                {
                    bufferCopy = new List<string>(LogBuffer);
                    LogBuffer.Clear();
                }
                Logger?.LogInformation("Flushing remaining log buffer...");
                await WriteLogAsync(bufferCopy);
            }

        }
        public static async Task WriteLogAsync(List<string> logs)
        {
            try
            {
                string logsFolder = Path.Combine(AppContext.BaseDirectory, "Logs"); // combine the path to logs folder

                if (!Directory.Exists(logsFolder)) // check if the folder exists, if not we create a new one
                {
                    Directory.CreateDirectory(logsFolder);
                }

                string fileName = $"log_{DateTime.Now:yyyy-MM-dd}.txt"; // creates a new logging file everyday
                string logPath = Path.Combine(logsFolder, fileName); // direct the path from folder to logging file
                var now = DateTime.Now;

                var logLine = logs.Select(log => $"{now:yyyy-MM-dd HH:mm:ss} {log} {Environment.NewLine}").ToList(); // convert the log line to a list of strings
                await File.AppendAllTextAsync(logPath, string.Join("", logLine)); // join the strings together a bulk write logs
                Logger?.LogInformation("log written to log.txt");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to write log: {Message}", ex.Message);
            }
        }
        public static Task LogInfoAsync(string message, bool writeToLog = false)
        {
            Logger?.LogInformation(message);
            if (writeToLog)
            {
                lock (LockObject)
                {
                    LogBuffer.Add("[INFO] " + message);
                }
            }
            return Task.CompletedTask;
        }
        public static Task LogErrorAsync(string message, Exception? ex = null)
        {
            Logger?.LogError(ex, message);
            lock (LockObject)
            {
                LogBuffer.Add("[ERROR] " + message);
            }
            return Task.CompletedTask;
        }
        public static Task LogWarningAsync(string message)
        {
            Logger?.LogWarning(message);
            lock (LockObject)
            {
                LogBuffer.Add("[WARNING] " + message);
            }
            return Task.CompletedTask;
        }
    }
}

// ====================
// 📌 Design Notes:
// ====================
// - This class is designed to handle logging in a thread-safe manner.
// - Takes ILogger once and reusable everywhere.
// - Has 3 levels of severity: Info, Warning, and Error.
// - Uses a buffer to collect log messages and flush them to a file every 5 seconds.
// - Written carefully with async/await and lock to avoid deadlocks.
// - Uses bulk file writes to minimize I/O blocking.
// - TL;DR: Ready for use in multi-threaded environments.
