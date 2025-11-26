using System.Runtime.CompilerServices;

namespace CryptoPortfolio.Services
{
    public static class Logger
    {
        private static Action<string>? _writeEvent;
        private static readonly Lock _fileLock = new();

        public static void AddConsoleLogging()
            => _writeEvent += message =>
            {
                var logEntry = $"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} - {message}";
                Console.WriteLine(logEntry);
            };

        public static void AddFileLogging(string path)
            => _writeEvent += message =>
            {
                var logEntry = $"{DateTime.UtcNow:yy-MM-dd HH:mm:ss.fff} - {message}";

                lock(_fileLock)
                    File.AppendAllText(path, logEntry);
            };

        public static void WriteLine(string message, [CallerMemberName]string? topic = null)
            => Write(message + Environment.NewLine, topic);

        public static void Write(string message, [CallerMemberName] string? topic = null)
        {
            CheckForInitialized();
            ArgumentNullException.ThrowIfNull(message);

            _writeEvent?.Invoke($"[{topic}] {message}");
        }

        private static void CheckForInitialized()
        {
            if (_writeEvent is null)
                throw new InvalidOperationException("Logger is not initialized. Please call InitializeWithConsole or InitializeWithFile first.");
        }
    }
}
