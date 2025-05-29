using System.Runtime.CompilerServices;

namespace PupKarma
{
    internal class Logger
    {
        public static void DTDebug(object obj)
        {
            if (ModManager.DevTools)
            {
                PupKarmaMain.LoggerPupKarma.LogDebug(obj);
            }
        }

        public static void Debug(object obj)
        {
            PupKarmaMain.LoggerPupKarma.LogDebug(obj);
        }

        public static void Error(object message, [CallerMemberName]string caller = "")
        {
            PupKarmaMain.LoggerPupKarma.LogError($"Error in {caller}\nMessage:\n{message}");
        }

        public static void Info(object obj)
        {
            PupKarmaMain.LoggerPupKarma.LogInfo(obj);
        }
    }
}
