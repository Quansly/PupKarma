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

        public static void Error(string[] strs)
        {
            PupKarmaMain.LoggerPupKarma.LogError($"{strs[0]}\n{strs[1]}");
        }

        public static void Info(object obj)
        {
            PupKarmaMain.LoggerPupKarma.LogInfo(obj);
        }
    }
}
