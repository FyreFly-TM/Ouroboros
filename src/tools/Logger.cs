using Ouro.Core.AST;

namespace Ouro.src.tools
{
    static class Logger
    {
        private enum LoggerTypes {
            INFO,
            DEBUG,
            WARN,
            ERROR,
            FATAL
        }

        private static void LogBegin(LoggerTypes types)
        {
            string typeString = string.Empty;

            switch (types)
            {
                case LoggerTypes.INFO:
                    typeString = "INFO";
                    break;
                case LoggerTypes.DEBUG:
                    typeString = "DEBUG";
                    break;
                case LoggerTypes.WARN:
                    typeString = "WARN";
                    break;
                case LoggerTypes.ERROR:
                    typeString = "ERROR";
                    break;
                case LoggerTypes.FATAL:
                    typeString = "FATAL";
                    break;
                default:
                    typeString = "";
                    break;
            }
            Console.Write($"[{typeString}] [{DateTime.Now.ToString()}]: ");
        }

        private static bool _debugMode;
        public static void Init(bool debugMode)
        {
            _debugMode = debugMode;
        }

        public static void Info(string message)
        {
            Console.WriteLine(message);
        }

        public static void Info(Exception message)
        {
            Console.WriteLine(message);
        }

        public static void Debug(string message)
        {
            if (_debugMode)
            {
                LogBegin(LoggerTypes.DEBUG);
                Console.WriteLine("\x1b[36m" + message + "\x1b[0m");
            }
        }

        public static void Debug(Exception message)
        {
            if (_debugMode)
            {
                LogBegin(LoggerTypes.DEBUG);
                Console.WriteLine("\x1b[36m" + message + "\x1b[0m");
            }
        }

        public static void Warn(string message)
        {
            LogBegin(LoggerTypes.WARN);
            Console.WriteLine("\x1b[33m" + message + "\x1b[0m");
        }

        public static void Warn(Exception message)
        {
            LogBegin(LoggerTypes.WARN);
            Console.WriteLine("\x1b[33m" + message + "\x1b[0m");
        }

        public static void Error(string message)
        {
            LogBegin(LoggerTypes.ERROR);
            Console.WriteLine("\x1b[91m" + message + "\x1b[0m");
        }

        public static void Error(Exception message)
        {
            LogBegin(LoggerTypes.ERROR);
            Console.WriteLine("\x1b[91m" + message + "\x1b[0m");
        }

        public static void Fatal(string message)
        {
            LogBegin(LoggerTypes.FATAL);
            Console.WriteLine("\x1b[31m" + message + "\x1b[0m");
        }

        public static void Fatal(Exception message)
        {
            LogBegin(LoggerTypes.FATAL);
            Console.WriteLine("\x1b[31m" + message + "\x1b[0m");
        }

        // GPU SPECIFIC LOGGING //

        // -- Vulkan -- // TODO
        public static void VulkanDebug(string message)
        {

        }

        public static void VulkanWarn(string message)
        {

        }

        public static void VulkanError(string message)
        {

        }

        // -- CUDA -- //
        // TODO
    }
}
