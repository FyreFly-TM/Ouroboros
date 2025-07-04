using System;
using System.Collections.Generic;
using Ouro.StdLib.Math;

namespace Ouro.StdLib.UI
{
    /// <summary>
    /// Main window class for the Ouroboros UI framework
    /// </summary>
    public class Window : Widget
    {
        public string Title { get; set; }
        public bool IsResizable { get; set; }
        public bool IsFullscreen { get; set; }
        public Theme Theme { get; set; }
        
        private Layout layout;
        private bool isInitialized;
        
        public event EventHandler<ResizeEventArgs>? Resized;
        public event EventHandler<MouseEventArgs>? MouseMoved;
        public event EventHandler<MouseEventArgs>? MouseClicked;
        public event EventHandler<KeyEventArgs>? KeyPressed;
        public event EventHandler? Closed;
        
        public Action? OnClosed { get; set; }

        public Window(string title = "Ouroboros Window", int width = 800, int height = 600)
        {
            Title = title;
            Size = new Vector(width, height);
            Position = new Vector(100, 100);
            IsVisible = false;
            IsResizable = true;
            IsFullscreen = false;
            Theme = Theme.Default;
            layout = new FlowLayout();
        }

        public void Initialize()
        {
            if (isInitialized) return;
            
            // Platform-specific initialization
            PlatformInit();
            
            // Apply theme
            ApplyTheme(Theme);
            
            isInitialized = true;
        }

        public void Show()
        {
            if (!isInitialized) Initialize();
            IsVisible = true;
            PlatformShow();
            UIBackend.ShowWindow(this);
        }

        public void Hide()
        {
            IsVisible = false;
            PlatformHide();
        }

        public void Close()
        {
            Closed?.Invoke(this, EventArgs.Empty);
            PlatformClose();
        }

        public void SetLayout(Layout newLayout)
        {
            layout = newLayout;
            layout.ApplyTo(this);
            Invalidate();
        }

        public void Invalidate()
        {
            // Request redraw
            PlatformInvalidate();
        }

        public override void Render(GraphicsContext context)
        {
            // Clear background
            context.Clear(Theme.BackgroundColor);
            
            // Render all children
            foreach (var child in Children)
            {
                if (child.IsVisible)
                {
                    child.Render(context);
                }
            }
        }

        // Platform-specific methods (would be implemented per platform)
        private void PlatformInit() 
        {
            // Platform-specific initialization
            // UIBackend will be initialized when the window is shown
            UIBackend.Initialize();
        }
        
        private void PlatformShow() 
        {
            // Platform-specific show logic
            // Already handled by UIBackend.ShowWindow in Show() method
        }
        
        private void PlatformHide() 
        {
            // Platform-specific hide logic
            if (IsVisible)
            {
                IsVisible = false;
                
                // Notify the UI backend to hide the window
                UIBackend.SetWindowVisible(this, false);
                
                // Suspend render updates while hidden
                UIBackend.SuspendRendering(this);
            }
        }
        
        private void PlatformClose() 
        {
            // Platform-specific close logic
            if (IsVisible)
            {
                IsVisible = false;
                UIBackend.SetWindowVisible(this, false);
            }
            
            // Invoke close handler
            OnClosed?.Invoke();
            
            // Clean up window resources
            UIBackend.DestroyWindow(this);
            
            // Remove from window manager
            UIBackend.UnregisterWindow(this);
            
            // Clear event handlers to prevent memory leaks
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Resized = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            MouseMoved = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            MouseClicked = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            KeyPressed = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Closed = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private void PlatformInvalidate() 
        {
            // Platform-specific invalidation logic
            if (IsVisible && isInitialized)
            {
                // Mark the window as needing redraw
                UIBackend.InvalidateWindow(this);
                
                // Request a redraw in the next frame
                UIBackend.RequestRedraw(this);
                
                // Process any pending UI messages
                UIBackend.ProcessMessages();
            }
        }
        
        public override void ApplyTheme(Theme theme)
        {
            // Apply theme to window and all children
            foreach (var child in Children)
            {
                child.ApplyTheme(theme);
            }
        }

        // Equivalent C# implementation of the high-level syntax
        public void BuildUI()
        {
            // Natural language UI construction equivalent
            Title = "My Ouroboros App";
            Size = new Vector(1024, 768);
            // Center on screen would be implemented here
            
            // Add menu bar
            var menuBar = new MenuBar();
            var fileMenu = menuBar.AddMenu("File");
            fileMenu.AddItem("New");
            fileMenu.AddItem("Open");
            fileMenu.AddSeparator();
            fileMenu.AddItem("Exit");
            
            var editMenu = menuBar.AddMenu("Edit");
            editMenu.AddItem("Cut");
            editMenu.AddItem("Copy");
            editMenu.AddItem("Paste");
            
            // Add toolbar
            var toolbar = new ToolBar();
            toolbar.AddButton("New", new Image("new.png"));
            toolbar.AddButton("Open", new Image("open.png"));
            toolbar.AddButton("Save", new Image("save.png"));
            toolbar.AddSeparator();
            toolbar.AddButton("Cut", new Image("cut.png"));
            toolbar.AddButton("Copy", new Image("copy.png"));
            toolbar.AddButton("Paste", new Image("paste.png"));
            
            // Add split pane - implementation would continue here
            // Add status bar - implementation would continue here
        }
    }

    public class ResizeEventArgs : EventArgs
    {
        public Vector OldSize { get; }
        public Vector NewSize { get; }

        public ResizeEventArgs(Vector oldSize, Vector newSize)
        {
            OldSize = oldSize;
            NewSize = newSize;
        }
    }

    public class MouseEventArgs : EventArgs
    {
        public Vector Position { get; }
        public MouseButton Button { get; }
        public bool IsPressed { get; }
        public int ClickCount { get; }

        public MouseEventArgs(Vector position, MouseButton button = MouseButton.None, 
                            bool isPressed = false, int clickCount = 0)
        {
            Position = position;
            Button = button;
            IsPressed = isPressed;
            ClickCount = clickCount;
        }
    }

    public class KeyEventArgs : EventArgs
    {
        public Key Key { get; }
        public char Character { get; }
        public bool IsPressed { get; }
        public KeyModifiers Modifiers { get; }

        public KeyEventArgs(Key key, char character, bool isPressed, KeyModifiers modifiers)
        {
            Key = key;
            Character = character;
            IsPressed = isPressed;
            Modifiers = modifiers;
        }
    }

    public enum MouseButton
    {
        None,
        Left,
        Middle,
        Right,
        X1,
        X2
    }

    public enum Key
    {
        None,
        A, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        Space, Enter, Tab, Escape, Backspace, Delete,
        Left, Right, Up, Down,
        Home, End, PageUp, PageDown,
        Shift, Control, Alt, Command,
        CapsLock, NumLock, ScrollLock
    }

    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4,
        Command = 8
    }
} 