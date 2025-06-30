using System;
using System.Text;
using System.Collections.Generic;
using Ouroboros.Core;

namespace Ouroboros.StdLib.System
{
    /// <summary>
    /// Console input/output operations
    /// </summary>
    public static class Console
    {
        private static readonly object ConsoleLock = new object();
        private static ConsoleColor currentForeground = ConsoleColor.Gray;
        private static ConsoleColor currentBackground = ConsoleColor.Black;
        
        /// <summary>
        /// Write value to console
        /// </summary>
        public static void Write(object value)
        {
            lock (ConsoleLock)
            {
                global::System.Console.Write(value?.ToString() ?? "");
            }
        }
        
        /// <summary>
        /// Write line to console
        /// </summary>
        public static void WriteLine(object value = null)
        {
            lock (ConsoleLock)
            {
                global::System.Console.WriteLine(value?.ToString() ?? "");
            }
        }
        
        /// <summary>
        /// Write formatted string to console
        /// </summary>
        public static void Write(string format, params object[] args)
        {
            lock (ConsoleLock)
            {
                global::System.Console.Write(format, args);
            }
        }
        
        /// <summary>
        /// Write formatted line to console
        /// </summary>
        public static void WriteLine(string format, params object[] args)
        {
            lock (ConsoleLock)
            {
                global::System.Console.WriteLine(format, args);
            }
        }
        
        /// <summary>
        /// Write error to console
        /// </summary>
        public static void WriteError(object value)
        {
            lock (ConsoleLock)
            {
                var oldColor = global::System.Console.ForegroundColor;
                global::System.Console.ForegroundColor = global::System.ConsoleColor.Red;
                global::System.Console.Error.Write(value?.ToString() ?? "");
                global::System.Console.ForegroundColor = oldColor;
            }
        }
        
        /// <summary>
        /// Write error line to console
        /// </summary>
        public static void WriteLineError(object value = null)
        {
            lock (ConsoleLock)
            {
                var oldColor = global::System.Console.ForegroundColor;
                global::System.Console.ForegroundColor = global::System.ConsoleColor.Red;
                global::System.Console.Error.WriteLine(value?.ToString() ?? "");
                global::System.Console.ForegroundColor = oldColor;
            }
        }
        
        /// <summary>
        /// Read line from console
        /// </summary>
        public static string ReadLine()
        {
            return global::System.Console.ReadLine();
        }
        
        /// <summary>
        /// Read key from console
        /// </summary>
        public static ConsoleKeyInfo ReadKey(bool intercept = false)
        {
            var systemKey = global::System.Console.ReadKey(intercept);
            return ConsoleKeyInfo.FromSystemConsoleKeyInfo(systemKey);
        }
        
        /// <summary>
        /// Read password from console (masked input)
        /// </summary>
        public static string ReadPassword(char mask = '*')
        {
            var password = new StringBuilder();
            global::System.ConsoleKeyInfo key;
            
            do
            {
                key = global::System.Console.ReadKey(true);
                
                if (key.Key == global::System.ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    global::System.Console.Write("\b \b");
                }
                else if (key.Key != global::System.ConsoleKey.Enter && key.Key != global::System.ConsoleKey.Backspace)
                {
                    password.Append(key.KeyChar);
                    global::System.Console.Write(mask);
                }
            } while (key.Key != global::System.ConsoleKey.Enter);
            
            global::System.Console.WriteLine();
            return password.ToString();
        }
        
        /// <summary>
        /// Set foreground color
        /// </summary>
        public static void SetForegroundColor(ConsoleColor color)
        {
            lock (ConsoleLock)
            {
                currentForeground = color;
                global::System.Console.ForegroundColor = (global::System.ConsoleColor)color;
            }
        }
        
        /// <summary>
        /// Set background color
        /// </summary>
        public static void SetBackgroundColor(ConsoleColor color)
        {
            lock (ConsoleLock)
            {
                currentBackground = color;
                global::System.Console.BackgroundColor = (global::System.ConsoleColor)color;
            }
        }
        
        /// <summary>
        /// Reset colors to default
        /// </summary>
        public static void ResetColor()
        {
            lock (ConsoleLock)
            {
                global::System.Console.ResetColor();
                currentForeground = ConsoleColor.Gray;
                currentBackground = ConsoleColor.Black;
            }
        }
        
        /// <summary>
        /// Clear console screen
        /// </summary>
        public static void Clear()
        {
            global::System.Console.Clear();
        }
        
        /// <summary>
        /// Set cursor position
        /// </summary>
        public static void SetCursorPosition(int x, int y)
        {
            global::System.Console.SetCursorPosition(x, y);
        }
        
        /// <summary>
        /// Get or set window width
        /// </summary>
        public static int WindowWidth
        {
            get => global::System.Console.WindowWidth;
            set => global::System.Console.WindowWidth = value;
        }
        
        /// <summary>
        /// Get or set window height
        /// </summary>
        public static int WindowHeight
        {
            get => global::System.Console.WindowHeight;
            set => global::System.Console.WindowHeight = value;
        }
        
        /// <summary>
        /// Get or set cursor visibility
        /// </summary>
        public static bool CursorVisible
        {
            get => global::System.Console.CursorVisible;
            set => global::System.Console.CursorVisible = value;
        }
        
        /// <summary>
        /// Get or set console title
        /// </summary>
        public static string Title
        {
            get => global::System.Console.Title;
            set => global::System.Console.Title = value;
        }
        
        /// <summary>
        /// Check if key is available
        /// </summary>
        public static bool KeyAvailable => global::System.Console.KeyAvailable;
        
        /// <summary>
        /// Beep sound
        /// </summary>
        public static void Beep()
        {
            global::System.Console.Beep();
        }
        
        /// <summary>
        /// Beep with frequency and duration
        /// </summary>
        public static void Beep(int frequency, int duration)
        {
            global::System.Console.Beep(frequency, duration);
        }
        
        /// <summary>
        /// Write with color
        /// </summary>
        public static void WriteColor(object value, ConsoleColor color)
        {
            lock (ConsoleLock)
            {
                var oldColor = global::System.Console.ForegroundColor;
                global::System.Console.ForegroundColor = (global::System.ConsoleColor)color;
                global::System.Console.Write(value);
                global::System.Console.ForegroundColor = oldColor;
            }
        }
        
        /// <summary>
        /// Write line with color
        /// </summary>
        public static void WriteLineColor(object value, ConsoleColor color)
        {
            lock (ConsoleLock)
            {
                var oldColor = global::System.Console.ForegroundColor;
                global::System.Console.ForegroundColor = (global::System.ConsoleColor)color;
                global::System.Console.WriteLine(value);
                global::System.Console.ForegroundColor = oldColor;
            }
        }
        
        /// <summary>
        /// Create a progress bar
        /// </summary>
        public static void WriteProgress(int current, int total, int width = 50)
        {
            lock (ConsoleLock)
            {
                double percentage = (double)current / total;
                int filled = (int)(width * percentage);
                
                global::System.Console.Write("\r[");
                global::System.Console.Write(new string('=', filled));
                global::System.Console.Write(new string(' ', width - filled));
                global::System.Console.Write($"] {percentage:P0}");
                
                if (current >= total)
                    global::System.Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Create a simple menu
        /// </summary>
        public static int ShowMenu(string title, params string[] options)
        {
            WriteLine(title);
            WriteLine(new string('-', title.Length));
            
            for (int i = 0; i < options.Length; i++)
            {
                WriteLine($"{i + 1}. {options[i]}");
            }
            
            Write("\nSelect option: ");
            
            while (true)
            {
                var input = ReadLine();
                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= options.Length)
                {
                    return choice - 1;
                }
                
                WriteLineError($"Invalid choice. Please enter 1-{options.Length}");
                Write("Select option: ");
            }
        }
        
        /// <summary>
        /// Prompt for yes/no
        /// </summary>
        public static bool Confirm(string message, bool defaultValue = true)
        {
            string defaultText = defaultValue ? "[Y/n]" : "[y/N]";
            Write($"{message} {defaultText}: ");
            
            var input = ReadLine()?.Trim().ToLower();
            
            if (string.IsNullOrEmpty(input))
                return defaultValue;
            
            return input == "y" || input == "yes";
        }
        
        /// <summary>
        /// Create a table display
        /// </summary>
        public static void WriteTable(string[] headers, List<string[]> rows)
        {
            // Calculate column widths
            var widths = new int[headers.Length];
            
            for (int i = 0; i < headers.Length; i++)
            {
                widths[i] = headers[i].Length;
            }
            
            foreach (var row in rows)
            {
                for (int i = 0; i < global::System.Math.Min(row.Length, headers.Length); i++)
                {
                    widths[i] = global::System.Math.Max(widths[i], row[i]?.Length ?? 0);
                }
            }
            
            // Print headers
            for (int i = 0; i < headers.Length; i++)
            {
                Write(headers[i].PadRight(widths[i] + 2));
            }
            WriteLine();
            
            // Print separator
            for (int i = 0; i < headers.Length; i++)
            {
                Write(new string('-', widths[i] + 2));
            }
            WriteLine();
            
            // Print rows
            foreach (var row in rows)
            {
                for (int i = 0; i < global::System.Math.Min(row.Length, headers.Length); i++)
                {
                    Write((row[i] ?? "").PadRight(widths[i] + 2));
                }
                WriteLine();
            }
        }
    }
    
    /// <summary>
    /// Console colors enumeration
    /// </summary>
    public enum ConsoleColor
    {
        Black = 0,
        DarkBlue = 1,
        DarkGreen = 2,
        DarkCyan = 3,
        DarkRed = 4,
        DarkMagenta = 5,
        DarkYellow = 6,
        Gray = 7,
        DarkGray = 8,
        Blue = 9,
        Green = 10,
        Cyan = 11,
        Red = 12,
        Magenta = 13,
        Yellow = 14,
        White = 15
    }
    
    /// <summary>
    /// Console key information
    /// </summary>
    public struct ConsoleKeyInfo
    {
        public char KeyChar { get; }
        public ConsoleKey Key { get; }
        public ConsoleModifiers Modifiers { get; }
        
        public ConsoleKeyInfo(char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
        {
            KeyChar = keyChar;
            Key = key;
            Modifiers = 0;
            
            if (shift) Modifiers |= ConsoleModifiers.Shift;
            if (alt) Modifiers |= ConsoleModifiers.Alt;
            if (control) Modifiers |= ConsoleModifiers.Control;
        }
        
        public static ConsoleKeyInfo FromSystemConsoleKeyInfo(global::System.ConsoleKeyInfo info)
        {
            return new ConsoleKeyInfo(
                info.KeyChar,
                (ConsoleKey)info.Key,
                (info.Modifiers & global::System.ConsoleModifiers.Shift) != 0,
                (info.Modifiers & global::System.ConsoleModifiers.Alt) != 0,
                (info.Modifiers & global::System.ConsoleModifiers.Control) != 0
            );
        }
    }
    
    /// <summary>
    /// Console key enumeration
    /// </summary>
    public enum ConsoleKey
    {
        Backspace = 8,
        Tab = 9,
        Enter = 13,
        Escape = 27,
        Spacebar = 32,
        PageUp = 33,
        PageDown = 34,
        End = 35,
        Home = 36,
        LeftArrow = 37,
        UpArrow = 38,
        RightArrow = 39,
        DownArrow = 40,
        Delete = 46,
        D0 = 48,
        D1 = 49,
        D2 = 50,
        D3 = 51,
        D4 = 52,
        D5 = 53,
        D6 = 54,
        D7 = 55,
        D8 = 56,
        D9 = 57,
        A = 65,
        B = 66,
        C = 67,
        D = 68,
        E = 69,
        F = 70,
        G = 71,
        H = 72,
        I = 73,
        J = 74,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        R = 82,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,
        F1 = 112,
        F2 = 113,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        F10 = 121,
        F11 = 122,
        F12 = 123
    }
    
    /// <summary>
    /// Console modifier keys
    /// </summary>
    [Flags]
    public enum ConsoleModifiers
    {
        None = 0,
        Alt = 1,
        Shift = 2,
        Control = 4
    }
} 