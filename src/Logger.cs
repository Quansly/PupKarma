using BepInEx.Logging;
using System.Runtime.CompilerServices;

namespace PupKarma
{
    internal class Logger
    {
        internal static ManualLogSource _logger;

        public static void DTDebug(object obj)
        {
            if (ModManager.DevTools)
            {
                _logger.LogDebug(obj);
            }
        }

        public static void Debug(object obj)
        {
            _logger.LogDebug(obj);
        }

        public static void Error(object message, [CallerMemberName]string caller = "")
        {
            _logger.LogError($"Error in {caller}\nMessage:\n{message}");
        }

        public static void Info(object obj)
        {
            _logger.LogInfo(obj);
        }
    }
}
