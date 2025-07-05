
namespace Ouro.Tools
{
    public enum LogColors
    {
        Black,
        Red,
        Green,
        Yellow,
        Blue,
        Purple,
        Cyan,
        White,
        HighIntensityBlack,
        HighIntensityRed,
        HighIntensityGreen,
        HighIntensityYellow,
        HighIntensityBlue,
        HighIntensityPurple,
        HighIntensityCyan,
        HighIntensityWhite,
        Reset
    }

    public static class ANSIColors
    {
        public static readonly string Black  = "\x1b[30m";
        public static readonly string Red    = "\x1b[31m";
        public static readonly string Green  = "\x1b[32m";
        public static readonly string Yellow = "\x1b[33m";
        public static readonly string Blue   = "\x1b[34m";
        public static readonly string Purple = "\x1b[35m";
        public static readonly string Cyan   = "\x1b[36m";
        public static readonly string White  = "\x1b[37m";
                      
        public static readonly string HighIntensityBlack  = "\x1b[90m";
        public static readonly string HighIntensityRed    = "\x1b[91m";
        public static readonly string HighIntensityGreen  = "\x1b[92m";
        public static readonly string HighIntensityYellow = "\x1b[93m";
        public static readonly string HighIntensityBlue   = "\x1b[94m";
        public static readonly string HighIntensityPurple = "\x1b[95m";
        public static readonly string HighIntensityCyan   = "\x1b[96m";
        public static readonly string HighIntensityWhite  = "\x1b[97m";
                      
        public static readonly string Reset = "\x1b[0m";
        public static readonly string NULLCOLOR = string.Empty; // No color

        private static readonly Dictionary<LogColors, string> colorMap = new Dictionary<LogColors, string>
        {
            { LogColors.Black,  Black },
            { LogColors.Red,    Red },
            { LogColors.Green,  Green },
            { LogColors.Yellow, Yellow },
            { LogColors.Blue,   Blue },
            { LogColors.Purple, Purple },
            { LogColors.Cyan,   Cyan },
            { LogColors.White,  White },
            { LogColors.HighIntensityBlack,  HighIntensityBlack },
            { LogColors.HighIntensityRed,    HighIntensityRed },
            { LogColors.HighIntensityGreen,  HighIntensityGreen },
            { LogColors.HighIntensityYellow, HighIntensityYellow },
            { LogColors.HighIntensityBlue,   HighIntensityBlue },
            { LogColors.HighIntensityPurple, HighIntensityPurple },
            { LogColors.HighIntensityCyan,   HighIntensityCyan },
            { LogColors.HighIntensityWhite,  HighIntensityWhite },
            { LogColors.Reset, Reset }
        };
        public static string GetColor(LogColors color) => colorMap.GetValueOrDefault(color, Reset);
    }

    public static class Logger
    {
        public enum LogTypes {
            INFO,
            DEBUG,
            WARN,
            ERROR,
            FATAL
        }

        // VARABLES //

        private static bool _debugMode;

        // COLOR CONSTANTS
        private static readonly LogColors INFO_COLOR  = LogColors.White;
        private static readonly LogColors DEBUG_COLOR = LogColors.Cyan;
        private static readonly LogColors WARN_COLOR  = LogColors.Yellow;
        private static readonly LogColors ERROR_COLOR = LogColors.HighIntensityRed;
        private static readonly LogColors FATAL_COLOR = LogColors.Red;

        // -------- //
        private static void LogBegin(LogTypes types)
        {
            Console.Write($"[{types.ToString()}] [{DateTime.Now.ToString("HH:mm:ss.FFFF")}]: ");
        }

        public static void SetDebugMode(bool debugMode)
        {
            _debugMode = debugMode;
        }

        public static void PrintWithColor(string Message, LogColors color)
        {
            string code = ANSIColors.GetColor(color);
            Console.WriteLine(code + Message + ANSIColors.Reset);
        }

        public static void Info(string message)
        {
            LogBegin(LogTypes.INFO);
            PrintWithColor(message, INFO_COLOR);
        }

        public static void Debug(string message)
        {
            if (_debugMode)
            {
                LogBegin(LogTypes.DEBUG);
                PrintWithColor(message, DEBUG_COLOR);
            }
        }

        public static void DebugWarn(string message) // Warning function for only when debug is enabled
        {
            if (_debugMode)
            {
                LogBegin(LogTypes.WARN);
                PrintWithColor(message, WARN_COLOR);
            }
        }

        public static void Warn(string message)
        {
            LogBegin(LogTypes.WARN);
            PrintWithColor(message, WARN_COLOR);
        }

        public static void Error(string message)
        {
            LogBegin(LogTypes.ERROR);
            PrintWithColor(message, ERROR_COLOR);
        }

        public static void Fatal(string message)
        {
            LogBegin(LogTypes.FATAL);
            PrintWithColor(message, FATAL_COLOR);
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