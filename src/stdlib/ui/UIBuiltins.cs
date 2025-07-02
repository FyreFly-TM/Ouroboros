using System;
using Ouro.StdLib.Math;

namespace Ouro.StdLib.UI
{
    /// <summary>
    /// Built-in UI functions that can be called from Ouroboros code
    /// </summary>
    public static class UIBuiltins
    {
        private static Window currentWindow;
        
        public static void CreateWindow(string title, double width, double height)
        {
            currentWindow = new Window(title, (int)width, (int)height);
        }
        
        public static void ShowWindow()
        {
            if (currentWindow != null)
            {
                currentWindow.Show();
            }
        }
        
        public static void AddButton(string text, double x, double y, double width, double height)
        {
            if (currentWindow != null)
            {
                var button = new Button(text);
                button.Position = new Vector(x, y);
                button.Size = new Vector(width, height);
                button.OnClickAction = () => HandleButtonClick(text);
                global::System.Console.WriteLine($"[UIBuiltins] Created button '{text}' with OnClickAction: {(button.OnClickAction != null ? "SET" : "NULL")}");
                currentWindow.AddChild(button);
            }
        }
        
        // Button click queue system for Ouroboros integration
        private static global::System.Collections.Generic.Queue<string> buttonClickQueue = new global::System.Collections.Generic.Queue<string>();
        private static readonly object queueLock = new object();
        private static bool windowClosed = false;
        
        private static void HandleButtonClick(string buttonText)
        {
            global::System.Console.WriteLine($"[UIBuiltins] HandleButtonClick called with: '{buttonText}'");
            lock (queueLock)
            {
                buttonClickQueue.Enqueue(buttonText);
                global::System.Console.WriteLine($"[UIBuiltins] Added '{buttonText}' to queue, queue size now: {buttonClickQueue.Count}");
            }
            global::System.Console.WriteLine($"[UIBuiltins] Button click handling completed for: {buttonText}");
        }
        
        public static string GetNextButtonClick()
        {
            lock (queueLock)
            {
                if (buttonClickQueue.Count > 0)
                {
                    string result = buttonClickQueue.Dequeue();
                    global::System.Console.WriteLine($"[UIBuiltins] GetNextButtonClick: returning '{result}', queue now has {buttonClickQueue.Count} items");
                    return result;
                }
                global::System.Console.WriteLine($"[UIBuiltins] GetNextButtonClick: queue is empty, returning empty string");
                return "";
            }
        }

        public static bool HasButtonClicks()
        {
            lock (queueLock)
            {
                bool hasClicks = buttonClickQueue.Count > 0;
                if (hasClicks) {
                    global::System.Console.WriteLine($"[UIBuiltins] HasButtonClicks: YES, queue has {buttonClickQueue.Count} items");
                }
                return hasClicks;
            }
        }
        
        public static void AddLabel(string text, double x, double y, double width, double height)
        {
            if (currentWindow != null)
            {
                var label = new Label(text);
                label.Position = new Vector(x, y);
                label.Size = new Vector(width, height);
                currentWindow.AddChild(label);
            }
        }
        
        private static TextBox displayTextBox = null;
        
        public static void AddTextBox(string text, double x, double y, double width, double height)
        {
            if (currentWindow != null)
            {
                var textBox = new TextBox(text);
                textBox.Position = new Vector(x, y);
                textBox.Size = new Vector(width, height);
                currentWindow.AddChild(textBox);
                
                // Store reference to the first textbox as the display
                if (displayTextBox == null)
                {
                    displayTextBox = textBox;
                }
            }
        }
        
        public static void UpdateDisplay(string text)
        {
            if (displayTextBox != null)
            {
                displayTextBox.Text = text;
                UIBackend.UpdateTextBox(displayTextBox, text);
                global::System.Console.WriteLine($"Display updated: {text}");
            }
        }
        
        public static void UpdateDisplay(object text)
        {
            UpdateDisplay(text?.ToString() ?? "");
        }
        
        public static void RunUI()
        {
            global::System.Console.WriteLine("[UIBuiltins] Starting Windows Forms message loop...");
            UIBackend.RunMessageLoop();
        }
        
        public static void ProcessMessages()
        {
            UIBackend.ProcessMessages();
        }
        
        public static bool IsWindowClosed()
        {
            return windowClosed;
        }
        
        public static void OnWindowClosed()
        {
            windowClosed = true;
        }
        
        public static void ShowMessage(string message)
        {
            UIBackend.ShowMessage(message);
        }
    }
} 