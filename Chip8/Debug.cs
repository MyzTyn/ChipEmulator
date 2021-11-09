namespace Chip8
{
#if DEBUG
    // Allow to config it
    public interface IDebugger
    {
        public void Output(string value);
    }

    // Debug to log the system
    public static class Debug
    {
        public static void Init(IDebugger debugger)
        {
            m_Debugger = debugger;
        }

        public static void Log(string value)
        {
            if (Enabled)
                m_Debugger.Output("[Log] " + value);
        }

        public static void Log(string value, object object0)
        {
            Log(string.Format(value, object0));
        }

        public static void LogWarning(string value)
        {
            if (Enabled)
                m_Debugger.Output("[Log Warning] " + value);
        }

        public static void LogWarning(string value, object object0)
        {
            LogWarning(string.Format(value, object0));
        }

        public static void LogError(string value)
        {
            if (Enabled)
                m_Debugger.Output("[Log Error] " + value);
        }

        public static void LogError(string value, object object0)
        {
            LogError(string.Format(value, object0));
        }

        public static bool Enabled = false;
        private static IDebugger m_Debugger;
    }
#endif
}
