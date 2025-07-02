using System = global::System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Ouro.StdLib.Math;

namespace Ouro.StdLib.UI
{
    /// <summary>
    /// Windows Forms backend for the Ouroboros UI framework
    /// </summary>
    public static class UIBackend
    {
        private static Dictionary<Widget, Control> widgetToControl = new Dictionary<Widget, Control>();
        private static Dictionary<Control, Widget> controlToWidget = new Dictionary<Control, Widget>();
        private static bool initialized = false;
        private static Form mainForm = null;
        
        public static void Initialize()
        {
            if (!initialized)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                initialized = true;
            }
        }
        
        public static void ShowWindow(Window window)
        {
            Initialize();
            
            var form = new Form();
            form.Text = window.Title ?? "Ouroboros Window";
            form.Size = new Size((int)window.Size.X, (int)window.Size.Y);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.TopMost = true; // Force window to front
            form.WindowState = FormWindowState.Normal;
            form.ShowInTaskbar = true;
            
            // Track the first form as the main form
            if (mainForm == null)
            {
                mainForm = form;
            }
            
            // Map the window to the form
            widgetToControl[window] = form;
            controlToWidget[form] = window;
            
            // Add child controls
            foreach (var child in window.Children)
            {
                var control = CreateControl(child);
                if (control != null)
                {
                    form.Controls.Add(control);
                }
            }
            
            // Handle window events
            form.FormClosed += (s, e) => {
                window.OnClosed?.Invoke();
                UIBuiltins.OnWindowClosed(); // Signal to Ouroboros program
                global::System.Console.WriteLine("Window closed, signaling program to exit");
                // If this is the main form, clear the reference
                if (form == mainForm)
                {
                    mainForm = null;
                }
            };
            
            form.Show();
            form.Activate(); // Bring to front
        }
        
        public static void RunMessageLoop()
        {
            if (mainForm != null)
            {
                Application.Run(mainForm);
            }
            else
            {
                Application.Run();
            }
        }
        
        private static int processMessageCount = 0;
        
        public static void ProcessMessages()
        {
            // Process pending Windows messages without blocking
            Application.DoEvents();
            
            // Additional message processing to ensure events are handled
            if (mainForm != null && !mainForm.IsDisposed)
            {
                // Force the form to process any pending messages
                mainForm.Refresh();
            }
            
            // Debug output every 10000 calls to avoid spam
            processMessageCount++;
            if (processMessageCount % 10000 == 0)
            {
                global::System.Console.WriteLine($"[WindowsFormsBackend] ProcessMessages called {processMessageCount} times, mainForm exists: {mainForm != null}");
            }
        }
        
        private static Control CreateControl(Widget widget)
        {
            Control control = null;
            
            switch (widget)
            {
                case Button button:
                    var btn = new global::System.Windows.Forms.Button();
                    btn.Text = button.Text ?? "";
                    btn.Location = new Point((int)button.Position.X, (int)button.Position.Y);
                    btn.Size = new Size((int)button.Size.X, (int)button.Size.Y);
                    btn.Enabled = button.IsEnabled;
                    
                    global::System.Console.WriteLine($"[WindowsFormsBackend] Creating Windows Forms button '{btn.Text}', OnClickAction is: {(button.OnClickAction != null ? "SET" : "NULL")}");
                    
                    // Add multiple event handlers to see which ones fire
                    btn.MouseDown += (s, e) => global::System.Console.WriteLine($"[WindowsFormsBackend] Button '{btn.Text}' MouseDown!");
                    btn.MouseUp += (s, e) => global::System.Console.WriteLine($"[WindowsFormsBackend] Button '{btn.Text}' MouseUp!");
                    btn.MouseClick += (s, e) => global::System.Console.WriteLine($"[WindowsFormsBackend] Button '{btn.Text}' MouseClick!");
                    btn.Click += (s, e) => {
                        global::System.Console.WriteLine($"[WindowsFormsBackend] Button '{btn.Text}' was clicked!");
                        if (button.OnClickAction != null) {
                            global::System.Console.WriteLine($"[WindowsFormsBackend] Invoking OnClickAction for '{btn.Text}'");
                            button.OnClickAction.Invoke();
                        } else {
                            global::System.Console.WriteLine($"[WindowsFormsBackend] OnClickAction is NULL for '{btn.Text}'!");
                        }
                    };
                    control = btn;
                    break;
                    
                case Label label:
                    var lbl = new global::System.Windows.Forms.Label();
                    lbl.Text = label.Text ?? "";
                    lbl.Location = new Point((int)label.Position.X, (int)label.Position.Y);
                    lbl.Size = new Size((int)label.Size.X, (int)label.Size.Y);
                    lbl.AutoSize = true;
                    control = lbl;
                    break;
                    
                case TextBox textBox:
                    var txt = new global::System.Windows.Forms.TextBox();
                    txt.Text = textBox.Text ?? "";
                    txt.Location = new Point((int)textBox.Position.X, (int)textBox.Position.Y);
                    txt.Size = new Size((int)textBox.Size.X, (int)textBox.Size.Y);
                    if (!string.IsNullOrEmpty(textBox.Placeholder))
                    {
                        txt.PlaceholderText = textBox.Placeholder;
                    }
                    txt.UseSystemPasswordChar = textBox.IsPassword;
                    control = txt;
                    break;
                    
                case CheckBox checkBox:
                    var chk = new global::System.Windows.Forms.CheckBox();
                    chk.Text = checkBox.Text ?? "";
                    chk.Location = new Point((int)checkBox.Position.X, (int)checkBox.Position.Y);
                    chk.Size = new Size((int)checkBox.Size.X, (int)checkBox.Size.Y);
                    chk.Checked = checkBox.IsChecked;
                    chk.Enabled = checkBox.IsEnabled;
                    chk.AutoSize = true;
                    control = chk;
                    break;
                    
                case TabControl tabControl:
                    var tab = new global::System.Windows.Forms.TabControl();
                    tab.Location = new Point((int)tabControl.Position.X, (int)tabControl.Position.Y);
                    tab.Size = new Size((int)tabControl.Size.X, (int)tabControl.Size.Y);
                    
                    foreach (var tabPage in tabControl.TabPages)
                    {
                        var page = new global::System.Windows.Forms.TabPage(tabPage.Title ?? "Tab");
                        
                        foreach (var child in tabPage.Children)
                        {
                            var childControl = CreateControl(child);
                            if (childControl != null)
                            {
                                page.Controls.Add(childControl);
                            }
                        }
                        
                        tab.TabPages.Add(page);
                    }
                    control = tab;
                    break;
                    
                case Panel panel:
                    var pnl = new global::System.Windows.Forms.Panel();
                    pnl.Location = new Point((int)panel.Position.X, (int)panel.Position.Y);
                    pnl.Size = new Size((int)panel.Size.X, (int)panel.Size.Y);
                    pnl.BackColor = ConvertColor(panel.BackgroundColor);
                    
                    foreach (var child in panel.Children)
                    {
                        var childControl = CreateControl(child);
                        if (childControl != null)
                        {
                            pnl.Controls.Add(childControl);
                        }
                    }
                    control = pnl;
                    break;
                    
                default:
                    // Generic widget as panel
                    var genericPanel = new global::System.Windows.Forms.Panel();
                    genericPanel.Location = new Point((int)widget.Position.X, (int)widget.Position.Y);
                    genericPanel.Size = new Size((int)widget.Size.X, (int)widget.Size.Y);
                    genericPanel.BackColor = ConvertColor(widget.BackgroundColor);
                    
                    foreach (var child in widget.Children)
                    {
                        var childControl = CreateControl(child);
                        if (childControl != null)
                        {
                            genericPanel.Controls.Add(childControl);
                        }
                    }
                    control = genericPanel;
                    break;
            }
            
            if (control != null)
            {
                widgetToControl[widget] = control;
                controlToWidget[control] = widget;
            }
            
            return control;
        }
        
        private static global::System.Drawing.Color ConvertColor(Ouro.StdLib.UI.Color? color)
        {
            if (color is null || !color.HasValue) return global::System.Drawing.Color.Transparent;
            var c = color.Value;
            return global::System.Drawing.Color.FromArgb(
                (int)(c.A * 255),
                (int)(c.R * 255),
                (int)(c.G * 255),
                (int)(c.B * 255)
            );
        }
        
        public static void ShowMessage(string message, string title = "Message")
        {
            global::System.Windows.Forms.MessageBox.Show(message, title, global::System.Windows.Forms.MessageBoxButtons.OK, global::System.Windows.Forms.MessageBoxIcon.Information);
        }
        
        public static void UpdateTextBox(TextBox textBoxWidget, string newText)
        {
            if (widgetToControl.ContainsKey(textBoxWidget))
            {
                var control = widgetToControl[textBoxWidget];
                if (control is global::System.Windows.Forms.TextBox winFormsTextBox)
                {
                    // Update the text directly
                    winFormsTextBox.Text = newText;
                    global::System.Console.WriteLine($"TextBox updated to: '{newText}'");
                }
            }
        }
        
        public static void SetWindowVisible(Window window, bool visible)
        {
            if (widgetToControl.TryGetValue(window, out var control) && control is Form form)
            {
                if (visible)
                {
                    form.Show();
                    form.BringToFront();
                }
                else
                {
                    form.Hide();
                }
            }
        }
        
        public static void SuspendRendering(Window window)
        {
            if (widgetToControl.TryGetValue(window, out var control))
            {
                control.SuspendLayout();
            }
        }
        
        public static void ResumeRendering(Window window)
        {
            if (widgetToControl.TryGetValue(window, out var control))
            {
                control.ResumeLayout(true);
            }
        }
        
        public static void DestroyWindow(Window window)
        {
            if (widgetToControl.TryGetValue(window, out var control))
            {
                // Dispose the control and all its children
                control.Dispose();
                
                // Remove from tracking dictionaries
                widgetToControl.Remove(window);
                controlToWidget.Remove(control);
                
                // If this was the main form, clear the reference
                if (control == mainForm)
                {
                    mainForm = null;
                }
            }
        }
        
        public static void UnregisterWindow(Window window)
        {
            // Remove window from any global registries
            // This is called after DestroyWindow to ensure complete cleanup
            
            // Clean up any child widgets
            foreach (var child in window.Children)
            {
                if (widgetToControl.ContainsKey(child))
                {
                    var childControl = widgetToControl[child];
                    widgetToControl.Remove(child);
                    controlToWidget.Remove(childControl);
                }
            }
        }
        
        public static void InvalidateWindow(Window window)
        {
            if (widgetToControl.TryGetValue(window, out var control))
            {
                control.Invalidate();
            }
        }
        
        public static void RequestRedraw(Window window)
        {
            if (widgetToControl.TryGetValue(window, out var control))
            {
                // Force immediate redraw
                control.Update();
                
                // For forms, also refresh
                if (control is Form form)
                {
                    form.Refresh();
                }
            }
        }
    }
} 