using System;
using System.Collections.Generic;
using Ouro.StdLib.Math;

namespace Ouro.StdLib.UI
{
    /// <summary>
    /// Base class for all UI widgets
    /// </summary>
    public abstract class Widget
    {
        public string Id { get; set; } = string.Empty;
        public Vector Position { get; set; }
        public Vector Size { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEnabled { get; set; }
        public object? Parent { get; set; }
        public Margin Margin { get; set; } = new Margin(0);
        public Padding Padding { get; set; } = new Padding(5);
        public Color? BackgroundColor { get; set; }
        public Color? ForegroundColor { get; set; }
        public Font Font { get; set; } = new Font("Arial", 12);
        
        protected List<Widget> children = new List<Widget>();
        
        public IReadOnlyList<Widget> Children => children;
        
#pragma warning disable CS0067 // Event is never used
        public event EventHandler<MouseEventArgs>? MouseEnter;
        public event EventHandler<MouseEventArgs>? MouseLeave;
        public event EventHandler<MouseEventArgs>? MouseMove;
        public event EventHandler<MouseEventArgs>? Click;
        public event EventHandler<MouseEventArgs>? DoubleClick;
        public event EventHandler<KeyEventArgs>? KeyDown;
        public event EventHandler<KeyEventArgs>? KeyUp;
        public event EventHandler<FocusEventArgs>? GotFocus;
        public event EventHandler<FocusEventArgs>? LostFocus;
#pragma warning restore CS0067 // Event is never used

        protected Widget()
        {
            Id = Guid.NewGuid().ToString();
            Position = Vector.Zero2;
            Size = new Vector(100, 30);
            IsVisible = true;
            IsEnabled = true;
        }

        public abstract void Render(GraphicsContext context);
        
        public virtual void ApplyTheme(Theme theme)
        {
            BackgroundColor = theme.BackgroundColor;
            ForegroundColor = theme.ForegroundColor;
            Font = theme.DefaultFont ?? new Font("Arial", 12);
            
            foreach (var child in children)
            {
                child.ApplyTheme(theme);
            }
        }
        
        public virtual bool HitTest(Vector point)
        {
            return point.X >= Position.X && point.X <= Position.X + Size.X &&
                   point.Y >= Position.Y && point.Y <= Position.Y + Size.Y;
        }
        
        protected virtual void OnClick(MouseEventArgs e)
        {
            Click?.Invoke(this, e);
        }
        
        protected virtual void OnMouseEnter(MouseEventArgs e)
        {
            MouseEnter?.Invoke(this, e);
        }
        
        protected virtual void OnMouseLeave(MouseEventArgs e)
        {
            MouseLeave?.Invoke(this, e);
        }
        
        public virtual void AddChild(Widget child)
        {
            children.Add(child);
            child.Parent = this;
        }
        
        public virtual void RemoveChild(Widget child)
        {
            if (children.Remove(child))
            {
                child.Parent = null;
            }
        }
    }

    /// <summary>
    /// Button control
    /// </summary>
    public class Button : Widget
    {
        public string Text { get; set; } = string.Empty;
        public Image? Icon { get; set; }
        public ButtonStyle Style { get; set; } = ButtonStyle.Default;
        public bool IsPressed { get; private set; }
        
        public Action? OnClickAction { get; set; }
        
        public Button(string text = "")
        {
            Text = text;
            Size = new Vector(100, 35);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw button background
            var bgColor = IsPressed ? Style.PressedColor : 
                         (IsEnabled ? Style.BackgroundColor : Style.DisabledColor);
            
            context.FillRoundedRectangle(Position, Size, Style.CornerRadius, bgColor);
            
            // Draw border
            context.DrawRoundedRectangle(Position, Size, Style.CornerRadius, 
                                       Style.BorderColor, Style.BorderWidth);
            
            // Draw icon if present
            if (Icon != null)
            {
                var iconPos = new Vector(
                    Position.X + Padding.Left,
                    Position.Y + (Size.Y - Icon.Height) / 2
                );
                context.DrawImage(Icon, iconPos);
            }
            
            // Draw text
            if (!string.IsNullOrEmpty(Text))
            {
                var textSize = context.MeasureText(Text, Font);
                var textPos = new Vector(
                    Position.X + (Size.X - textSize.X) / 2,
                    Position.Y + (Size.Y - textSize.Y) / 2
                );
                context.DrawText(Text, textPos, Font, ForegroundColor ?? Theme.Default.ForegroundColor);
            }
        }
        
        // Equivalent C# implementation of the high-level syntax
        public void SetupButton()
        {
            // High-level button configuration
            Text = "Click Me";
            Style = ButtonStyle.Primary;
            Icon = new Image("button-icon.png");
            
            Click += static (sender, e) =>
            {
                Console.WriteLine("Button clicked!");
                // Animation would be implemented here
            };
        }
    }

    /// <summary>
    /// Label control
    /// </summary>
    public class Label : Widget
    {
        public string Text { get; set; } = string.Empty;
        public TextAlignment Alignment { get; set; } = TextAlignment.Left;
        public bool WordWrap { get; set; } = false;
        
        public Label(string text = "")
        {
            Text = text;
        }
        
        public override void Render(GraphicsContext context)
        {
            if (string.IsNullOrEmpty(Text)) return;
            
            if (WordWrap)
            {
                context.DrawTextWrapped(Text, Position, Size, Font, ForegroundColor ?? Theme.Default.ForegroundColor, Alignment);
            }
            else
            {
                var textSize = context.MeasureText(Text, Font);
                var x = Alignment switch
                {
                    TextAlignment.Center => Position.X + (Size.X - textSize.X) / 2,
                    TextAlignment.Right => Position.X + Size.X - textSize.X,
                    _ => Position.X
                };
                
                context.DrawText(Text, new Vector(x, Position.Y), Font, ForegroundColor ?? Theme.Default.ForegroundColor);
            }
        }
    }

    /// <summary>
    /// TextBox control for text input
    /// </summary>
    public class TextBox : Widget
    {
        public string Text { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public int MaxLength { get; set; } = int.MaxValue;
        public bool IsPassword { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;
        public int CursorPosition { get; set; } = 0;
        public int SelectionStart { get; set; } = 0;
        public int SelectionLength { get; set; } = 0;
        
        private bool hasFocus = false;
        private double cursorBlinkTime = 0;
        
        public TextBox(string text = "")
        {
            Text = text;
            Size = new Vector(200, 30);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            context.FillRectangle(Position, Size, BackgroundColor ?? Theme.Default.BackgroundColor);
            
            // Draw border
            var borderColor = hasFocus ? Theme.Default.AccentColor : Theme.Default.BorderColor;
            context.DrawRectangle(Position, Size, borderColor, 1);
            
            // Draw text or placeholder
            var displayText = Text;
            if (IsPassword && !string.IsNullOrEmpty(Text))
            {
                displayText = new string('•', Text.Length);
            }
            
            if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder))
            {
                context.DrawText(Placeholder, Position + new Vector(Padding.Left, Padding.Top),
                               Font, Theme.Default.PlaceholderColor);
            }
            else
            {
                context.DrawText(displayText, Position + new Vector(Padding.Left, Padding.Top),
                               Font, ForegroundColor ?? Theme.Default.ForegroundColor);
            }
            
            // Draw cursor
            if (hasFocus && global::System.Math.Sin(cursorBlinkTime * global::System.Math.PI * 2) > 0)
            {
                var cursorX = Position.X + Padding.Left + 
                            context.MeasureText(displayText.Substring(0, CursorPosition), Font).X;
                context.DrawLine(new Vector(cursorX, Position.Y + Padding.Top),
                               new Vector(cursorX, Position.Y + Size.Y - Padding.Bottom),
                               ForegroundColor ?? Theme.Default.ForegroundColor, 1);
            }
        }
        
        public void InsertText(string text)
        {
            if (IsReadOnly) return;
            
            Text = Text.Insert(CursorPosition, text);
            CursorPosition += text.Length;
            
            if (Text.Length > MaxLength)
            {
                Text = Text.Substring(0, MaxLength);
                CursorPosition = global::System.Math.Min(CursorPosition, MaxLength);
            }
        }
    }

    /// <summary>
    /// CheckBox control
    /// </summary>
    public class CheckBox : Widget
    {
        public bool IsChecked { get; set; } = false;
        public string Text { get; set; } = string.Empty;
        public CheckBoxStyle Style { get; set; } = CheckBoxStyle.Default;
        
#pragma warning disable CS0067 // Event is never used
        public event EventHandler<CheckedChangedEventArgs>? CheckedChanged;
#pragma warning restore CS0067 // Event is never used
        
        public CheckBox(string text = "")
        {
            Text = text;
            Size = new Vector(20, 20);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw checkbox box
            context.FillRectangle(Position, new Vector(20, 20), BackgroundColor ?? Theme.Default.BackgroundColor);
            context.DrawRectangle(Position, new Vector(20, 20), Style.BorderColor, 1);
            
            // Draw checkmark if checked
            if (IsChecked)
            {
                var checkPath = new Path();
                checkPath.MoveTo(Position + new Vector(4, 10));
                checkPath.LineTo(Position + new Vector(8, 14));
                checkPath.LineTo(Position + new Vector(16, 6));
                context.DrawPath(checkPath, Style.CheckColor, 2);
            }
            
            // Draw label
            if (!string.IsNullOrEmpty(Text))
            {
                context.DrawText(Text, Position + new Vector(25, 0), Font, ForegroundColor ?? Theme.Default.ForegroundColor);
            }
        }
        
        protected override void OnClick(MouseEventArgs e)
        {
            IsChecked = !IsChecked;
            CheckedChanged?.Invoke(this, new CheckedChangedEventArgs(IsChecked));
            base.OnClick(e);
        }
    }

    /// <summary>
    /// RadioButton control
    /// </summary>
    public class RadioButton : Widget
    {
        public bool IsChecked { get; set; } = false;
        public string Text { get; set; } = string.Empty;
        public string GroupName { get; set; } = "default";
        
#pragma warning disable CS0067 // Event is never used
        public event EventHandler<CheckedChangedEventArgs>? CheckedChanged;
#pragma warning restore CS0067 // Event is never used
        
        public RadioButton(string text = "", string group = "default")
        {
            Text = text;
            GroupName = group;
            Size = new Vector(20, 20);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw radio button circle
            var center = Position + new Vector(10, 10);
            context.DrawCircle(center, 9, Style.Default.BorderColor, 1);
            
            // Draw inner circle if checked
            if (IsChecked)
            {
                context.FillCircle(center, 5, Style.Default.AccentColor);
            }
            
            // Draw label
            if (!string.IsNullOrEmpty(Text))
            {
                context.DrawText(Text, Position + new Vector(25, 0), Font, ForegroundColor ?? Theme.Default.ForegroundColor);
            }
        }
    }

    /// <summary>
    /// ComboBox (dropdown) control
    /// </summary>
    public class ComboBox : Widget
    {
        public List<object> Items { get; } = new List<object>();
        public int SelectedIndex { get; set; } = -1;
        public object? SelectedItem => SelectedIndex >= 0 ? Items[SelectedIndex] : null;
        public bool IsDropDownOpen { get; set; } = false;
        
        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
        
        public ComboBox()
        {
            Size = new Vector(150, 30);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw combo box background
            context.FillRectangle(Position, Size, BackgroundColor ?? Theme.Default.BackgroundColor);
            context.DrawRectangle(Position, Size, Theme.Default.BorderColor, 1);
            
            // Draw selected item text
            if (SelectedItem != null)
            {
                context.DrawText(SelectedItem?.ToString() ?? string.Empty, 
                               Position + new Vector(Padding.Left, Padding.Top),
                               Font, ForegroundColor ?? Theme.Default.ForegroundColor);
            }
            
            // Draw dropdown arrow
            var arrowPos = Position + new Vector(Size.X - 20, Size.Y / 2);
            DrawArrow(context, arrowPos, IsDropDownOpen ? Direction.Up : Direction.Down);
            
            // Draw dropdown list if open
            if (IsDropDownOpen)
            {
                var dropdownPos = Position + new Vector(0, Size.Y);
                var dropdownSize = new Vector(Size.X, Items.Count * 25);
                
                context.FillRectangle(dropdownPos, dropdownSize, BackgroundColor ?? Theme.Default.BackgroundColor);
                context.DrawRectangle(dropdownPos, dropdownSize, Theme.Default.BorderColor, 1);
                
                for (int i = 0; i < Items.Count; i++)
                {
                    var itemPos = dropdownPos + new Vector(Padding.Left, i * 25 + Padding.Top);
                    var itemBgColor = i == SelectedIndex ? Theme.Default.SelectionColor : (BackgroundColor ?? Theme.Default.BackgroundColor);
                    
                    context.FillRectangle(dropdownPos + new Vector(0, i * 25), 
                                        new Vector(Size.X, 25), itemBgColor);
                    context.DrawText(Items[i]?.ToString() ?? string.Empty, itemPos, Font, ForegroundColor ?? Theme.Default.ForegroundColor);
                }
            }
        }
        
        private void DrawArrow(GraphicsContext context, Vector position, Direction direction)
        {
            var path = new Path();
            switch (direction)
            {
                case Direction.Down:
                    path.MoveTo(position + new Vector(-5, -2));
                    path.LineTo(position + new Vector(0, 3));
                    path.LineTo(position + new Vector(5, -2));
                    break;
                case Direction.Up:
                    path.MoveTo(position + new Vector(-5, 2));
                    path.LineTo(position + new Vector(0, -3));
                    path.LineTo(position + new Vector(5, 2));
                    break;
            }
            context.DrawPath(path, ForegroundColor ?? Theme.Default.ForegroundColor, 1);
        }
    }

    /// <summary>
    /// Slider control
    /// </summary>
    public class Slider : Widget
    {
        public double Minimum { get; set; } = 0;
        public double Maximum { get; set; } = 100;
        public double Value { get; set; } = 0;
        public double Step { get; set; } = 1;
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
        
        public event EventHandler<ValueChangedEventArgs>? ValueChanged;
        
        public Slider()
        {
            Size = new Vector(200, 20);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw track
            var trackRect = Orientation == Orientation.Horizontal
                ? new Rectangle(Position.X, Position.Y + Size.Y / 2 - 2, Size.X, 4)
                : new Rectangle(Position.X + Size.X / 2 - 2, Position.Y, 4, Size.Y);
                
            context.FillRectangle(trackRect.Position, trackRect.Size, Theme.Default.TrackColor);
            
            // Calculate thumb position
            var percentage = (Value - Minimum) / (Maximum - Minimum);
            var thumbPos = Orientation == Orientation.Horizontal
                ? new Vector(Position.X + percentage * (Size.X - 20), Position.Y + Size.Y / 2)
                : new Vector(Position.X + Size.X / 2, Position.Y + percentage * (Size.Y - 20));
                
            // Draw thumb
            context.FillCircle(thumbPos, 10, Theme.Default.AccentColor);
            context.DrawCircle(thumbPos, 10, Theme.Default.BorderColor, 1);
        }
    }

    /// <summary>
    /// ProgressBar control
    /// </summary>
    public class ProgressBar : Widget
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Value { get; set; }
        public bool IsIndeterminate { get; set; }
        public ProgressBarStyle Style { get; set; }
        
        private double animationTime;
        
        public ProgressBar()
        {
            Minimum = 0;
            Maximum = 100;
            Value = 0;
            IsIndeterminate = false;
            Style = ProgressBarStyle.Default;
            Size = new Vector(200, 20);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            context.FillRoundedRectangle(Position, Size, 2, Style.BackgroundColor);
            context.DrawRoundedRectangle(Position, Size, 2, Style.BorderColor, 1);
            
            if (IsIndeterminate)
            {
                // Draw animated indeterminate progress
                var barWidth = Size.X * 0.3;
                var offset = (global::System.Math.Sin(animationTime) + 1) * 0.5 * (Size.X - barWidth);
                
                context.FillRoundedRectangle(
                    Position + new Vector(offset, 0),
                    new Vector(barWidth, Size.Y),
                    2, Style.FillColor
                );
            }
            else
            {
                // Draw determinate progress
                var percentage = global::System.Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1);
                var fillWidth = Size.X * percentage;
                
                if (fillWidth > 0)
                {
                    context.FillRoundedRectangle(
                        Position,
                        new Vector(fillWidth, Size.Y),
                        2, Style.FillColor
                    );
                }
            }
        }
        
        public void Update(double deltaTime)
        {
            if (IsIndeterminate)
            {
                animationTime += deltaTime * 2; // Animation speed
            }
        }
    }

    // Supporting classes
    public class Margin
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
        
        public Margin(double all) : this(all, all, all, all) { }
        public Margin(double horizontal, double vertical) : this(horizontal, vertical, horizontal, vertical) { }
        public Margin(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    public class Padding : Margin
    {
        public Padding(double all) : base(all) { }
        public Padding(double horizontal, double vertical) : base(horizontal, vertical) { }
        public Padding(double left, double top, double right, double bottom) : base(left, top, right, bottom) { }
    }

    public class Rectangle
    {
        public Vector Position { get; set; }
        public Vector Size { get; set; }
        
        public double X => Position.X;
        public double Y => Position.Y;
        public double Width => Size.X;
        public double Height => Size.Y;
        
        public Rectangle(double x, double y, double width, double height)
        {
            Position = new Vector(x, y);
            Size = new Vector(width, height);
        }
    }

    public enum TextAlignment { Left, Center, Right, Justify }
    public enum Orientation { Horizontal, Vertical }
    public enum Direction { Up, Down, Left, Right }

    public class CheckedChangedEventArgs : EventArgs
    {
        public bool IsChecked { get; }
        public CheckedChangedEventArgs(bool isChecked) => IsChecked = isChecked;
    }

    public class SelectionChangedEventArgs : EventArgs
    {
        public int OldIndex { get; }
        public int NewIndex { get; }
        public SelectionChangedEventArgs(int oldIndex, int newIndex)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }

    public class ValueChangedEventArgs : EventArgs
    {
        public double OldValue { get; }
        public double NewValue { get; }
        public ValueChangedEventArgs(double oldValue, double newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class FocusEventArgs : EventArgs
    {
        public Widget OldFocus { get; }
        public Widget NewFocus { get; }
        public FocusEventArgs(Widget oldFocus, Widget newFocus)
        {
            OldFocus = oldFocus;
            NewFocus = newFocus;
        }
    }

    /// <summary>
    /// TabControl - container with tabbed pages
    /// </summary>
    public class TabControl : Widget
    {
        private List<TabPage> tabs = new List<TabPage>();
        private int selectedIndex = -1;
        
        public List<TabPage> Tabs => tabs;
        public List<TabPage> TabPages => tabs;
        public int SelectedIndex 
        { 
            get => selectedIndex;
            set 
            {
                if (value >= -1 && value < tabs.Count)
                {
                    var oldIndex = selectedIndex;
                    selectedIndex = value;
                    TabChanged?.Invoke(this, new TabChangedEventArgs(oldIndex, selectedIndex));
                }
            }
        }
        public TabPage? SelectedTab => selectedIndex >= 0 ? tabs[selectedIndex] : null;
        public TabHeaderPosition HeaderPosition { get; set; } = TabHeaderPosition.Top;
        
        public event EventHandler<TabChangedEventArgs>? TabChanged;
        
        public TabControl()
        {
            Size = new Vector(400, 300);
        }
        
        public void AddTab(TabPage tab)
        {
            tabs.Add(tab);
            tab.Parent = this;
            if (selectedIndex == -1)
                SelectedIndex = 0;
        }
        
        public void RemoveTab(TabPage tab)
        {
            int index = tabs.IndexOf(tab);
            if (index != -1)
            {
                tabs.RemoveAt(index);
                if (selectedIndex >= tabs.Count)
                    SelectedIndex = tabs.Count - 1;
            }
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw tab headers
            double headerHeight = 30;
            double x = Position.X;
            
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                var textSize = context.MeasureText(tab.Title, Font);
                var tabWidth = textSize.X + 20;
                
                // Draw tab header
                var bgColor = i == selectedIndex ? (BackgroundColor ?? Theme.Default.BackgroundColor) : Theme.Default.BackgroundColor.Darken(0.1);
                var borderColor = i == selectedIndex ? Theme.Default.AccentColor : Theme.Default.BorderColor;
                
                context.FillRectangle(new Vector(x, Position.Y), new Vector(tabWidth, headerHeight), bgColor);
                context.DrawRectangle(new Vector(x, Position.Y), new Vector(tabWidth, headerHeight), borderColor, 1);
                
                // Draw tab title
                context.DrawText(tab.Title, new Vector(x + 10, Position.Y + 5), Font, ForegroundColor ?? Theme.Default.ForegroundColor);
                
                x += tabWidth;
            }
            
            // Draw content area
            var contentPos = Position + new Vector(0, headerHeight);
            var contentSize = new Vector(Size.X, Size.Y - headerHeight);
            
            context.FillRectangle(contentPos, contentSize, BackgroundColor ?? Theme.Default.BackgroundColor);
            context.DrawRectangle(contentPos, contentSize, Theme.Default.BorderColor, 1);
            
            // Render selected tab content
            if (SelectedTab != null)
            {
                SelectedTab.Position = contentPos + new Vector(Padding.Left, Padding.Top);
                SelectedTab.Size = contentSize - new Vector(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
                SelectedTab.Render(context);
            }
        }
    }
    
    public class TabPage : Widget
    {
        public string Title { get; set; }
        public Image? Icon { get; set; }
        
        public TabPage(string title)
        {
            Title = title;
        }
        
        public override void Render(GraphicsContext context)
        {
            // Render children
            foreach (var child in children)
            {
                if (child.IsVisible)
                    child.Render(context);
            }
        }
    }
    
    /// <summary>
    /// ListBox control - displays a list of items
    /// </summary>
    public class ListBox : Widget
    {
        private List<object> items = new List<object>();
        private HashSet<int> selectedIndices = new HashSet<int>();
        private int focusedIndex = -1;
        private double scrollOffset = 0;
        
        public List<object> Items => items;
        public int SelectedIndex 
        { 
            get => selectedIndices.Count > 0 ? selectedIndices.First() : -1;
            set 
            {
                selectedIndices.Clear();
                if (value >= 0 && value < items.Count)
                {
                    selectedIndices.Add(value);
                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(-1, value));
                }
            }
        }
        public object? SelectedItem => SelectedIndex >= 0 ? items[SelectedIndex] : null;
        public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;
        public double ItemHeight { get; set; } = 25;
        
        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
        public event EventHandler<ItemEventArgs>? ItemDoubleClick;
        
        public ListBox()
        {
            Size = new Vector(200, 150);
        }
        
        public void AddItem(object item)
        {
            items.Add(item);
        }
        
        public void RemoveItem(object item)
        {
            int index = items.IndexOf(item);
            if (index != -1)
            {
                items.RemoveAt(index);
                selectedIndices.Remove(index);
                // Adjust selected indices
                var newIndices = new HashSet<int>();
                foreach (var i in selectedIndices)
                {
                    if (i > index) newIndices.Add(i - 1);
                    else newIndices.Add(i);
                }
                selectedIndices = newIndices;
            }
        }
        
        public void ClearSelection()
        {
            selectedIndices.Clear();
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(-1, -1));
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            context.FillRectangle(Position, Size, BackgroundColor ?? Theme.Default.BackgroundColor);
            context.DrawRectangle(Position, Size, Theme.Default.BorderColor, 1);
            
            // Calculate visible items
            int visibleItems = (int)(Size.Y / ItemHeight);
            int startIndex = (int)(scrollOffset / ItemHeight);
            
            // Draw items
            for (int i = startIndex; i < global::System.Math.Min(startIndex + visibleItems + 1, items.Count); i++)
            {
                double y = Position.Y + (i * ItemHeight) - scrollOffset;
                
                // Skip if outside visible area
                if (y < Position.Y || y > Position.Y + Size.Y - ItemHeight)
                    continue;
                
                // Draw selection background
                if (selectedIndices.Contains(i))
                {
                    context.FillRectangle(
                        new Vector(Position.X, y),
                        new Vector(Size.X, ItemHeight),
                        Theme.Default.SelectionColor
                    );
                }
                else if (i == focusedIndex)
                {
                    context.DrawRectangle(
                        new Vector(Position.X, y),
                        new Vector(Size.X, ItemHeight),
                        Theme.Default.AccentColor.WithAlpha(128),
                        1
                    );
                }
                
                // Draw item text
                context.DrawText(
                    items[i]?.ToString() ?? string.Empty,
                    new Vector(Position.X + Padding.Left, y + 2),
                    Font,
                    selectedIndices.Contains(i) ? Color.White : (ForegroundColor ?? Theme.Default.ForegroundColor)
                );
            }
            
            // Draw scrollbar if needed
            if (items.Count * ItemHeight > Size.Y)
            {
                DrawScrollbar(context);
            }
        }
        
        private void DrawScrollbar(GraphicsContext context)
        {
            double scrollbarWidth = 15;
            double scrollbarX = Position.X + Size.X - scrollbarWidth;
            double contentHeight = items.Count * ItemHeight;
            double scrollbarHeight = (Size.Y / contentHeight) * Size.Y;
            double scrollbarY = Position.Y + (scrollOffset / contentHeight) * Size.Y;
            
            // Draw scrollbar track
            context.FillRectangle(
                new Vector(scrollbarX, Position.Y),
                new Vector(scrollbarWidth, Size.Y),
                Theme.Default.TrackColor
            );
            
            // Draw scrollbar thumb
            context.FillRoundedRectangle(
                new Vector(scrollbarX + 2, scrollbarY),
                new Vector(scrollbarWidth - 4, scrollbarHeight),
                2,
                Theme.Default.BorderColor
            );
        }
    }
    
    /// <summary>
    /// TreeView control - displays hierarchical data
    /// </summary>
    public class TreeView : Widget
    {
        private TreeNode rootNode;
        private TreeNode? selectedNode;
        private double scrollOffset = 0;
        private double nodeHeight = 25;
        
        public TreeNode RootNode 
        { 
            get => rootNode;
            set 
            {
                rootNode = value;
                if (rootNode != null)
                    rootNode.TreeView = this;
            }
        }
        public TreeNode? SelectedNode 
        { 
            get => selectedNode;
            set 
            {
                var oldNode = selectedNode;
                selectedNode = value;
                SelectionChanged?.Invoke(this, new TreeNodeEventArgs(oldNode, selectedNode));
            }
        }
        public bool ShowRootNode { get; set; } = true;
        public bool ShowLines { get; set; } = true;
        
        public event EventHandler<TreeNodeEventArgs>? SelectionChanged;
        public event EventHandler<TreeNodeEventArgs>? NodeExpanded;
        public event EventHandler<TreeNodeEventArgs>? NodeCollapsed;
        
        public TreeView()
        {
            Size = new Vector(250, 300);
            rootNode = new TreeNode("Root") { TreeView = this };
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            context.FillRectangle(Position, Size, BackgroundColor ?? Theme.Default.BackgroundColor);
            context.DrawRectangle(Position, Size, Theme.Default.BorderColor, 1);
            
            // Render nodes
            double y = Position.Y - scrollOffset;
            if (ShowRootNode)
            {
                RenderNode(context, rootNode, Position.X, ref y, 0);
            }
            else
            {
                foreach (var child in rootNode.Children)
                {
                    RenderNode(context, child, Position.X, ref y, 0);
                }
            }
        }
        
        private void RenderNode(GraphicsContext context, TreeNode node, double x, ref double y, int level)
        {
            // Skip if outside visible area
            if (y > Position.Y + Size.Y || y + nodeHeight < Position.Y)
            {
                y += nodeHeight;
                if (node.IsExpanded)
                {
                    foreach (var child in node.Children)
                    {
                        RenderNode(context, child, x, ref y, level + 1);
                    }
                }
                return;
            }
            
            double indent = level * 20;
            
            // Draw selection background
            if (node == selectedNode)
            {
                context.FillRectangle(
                    new Vector(Position.X, y),
                    new Vector(Size.X, nodeHeight),
                    Theme.Default.SelectionColor
                );
            }
            
            // Draw expand/collapse icon
            if (node.Children.Count > 0)
            {
                var iconPos = new Vector(x + indent + 5, y + nodeHeight / 2);
                DrawExpandIcon(context, iconPos, node.IsExpanded);
            }
            
            // Draw node icon
            if (node.Icon != null)
            {
                context.DrawImage(node.Icon, new Vector(x + indent + 25, y + 2));
            }
            
            // Draw node text
            context.DrawText(
                node.Text,
                new Vector(x + indent + 45, y + 2),
                Font,
                node == selectedNode ? Color.White : (ForegroundColor ?? Theme.Default.ForegroundColor)
            );
            
            y += nodeHeight;
            
            // Render children if expanded
            if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                {
                    RenderNode(context, child, x, ref y, level + 1);
                }
            }
        }
        
        private void DrawExpandIcon(GraphicsContext context, Vector pos, bool expanded)
        {
            var path = new Path();
            if (expanded)
            {
                path.MoveTo(pos + new Vector(-4, -2));
                path.LineTo(pos + new Vector(4, -2));
                path.LineTo(pos + new Vector(0, 4));
            }
            else
            {
                path.MoveTo(pos + new Vector(-2, -4));
                path.LineTo(pos + new Vector(-2, 4));
                path.LineTo(pos + new Vector(4, 0));
            }
            path.Close();
            context.FillPath(path, ForegroundColor ?? Theme.Default.ForegroundColor);
        }
        
        internal void OnNodeExpanded(TreeNode node)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            NodeExpanded?.Invoke(this, new TreeNodeEventArgs(null, node));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        internal void OnNodeCollapsed(TreeNode node)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            NodeCollapsed?.Invoke(this, new TreeNodeEventArgs(null, node));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
    
    public class TreeNode
    {
        private List<TreeNode> children = new List<TreeNode>();
        private TreeNode? parent;
        private bool isExpanded = false;
        
        public string Text { get; set; }
        public object? Tag { get; set; }
        public Image? Icon { get; set; }
        public List<TreeNode> Children => children;
        public TreeNode? Parent => parent;
        public TreeView? TreeView { get; internal set; }
        
        public bool IsExpanded 
        { 
            get => isExpanded;
            set 
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    if (TreeView != null)
                    {
                        if (isExpanded)
                            TreeView.OnNodeExpanded(this);
                        else
                            TreeView.OnNodeCollapsed(this);
                    }
                }
            }
        }
        
        public TreeNode(string text)
        {
            Text = text;
        }
        
        public TreeNode AddChild(string text)
        {
            var child = new TreeNode(text);
            child.parent = this;
            child.TreeView = TreeView;
            children.Add(child);
            return child;
        }
        
        public void RemoveChild(TreeNode child)
        {
            if (children.Remove(child))
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                child.parent = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                child.TreeView = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        }
        
        public void Clear()
        {
            foreach (var child in children)
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                child.parent = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                child.TreeView = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
            children.Clear();
        }
    }
    
    /// <summary>
    /// MenuBar control
    /// </summary>
    public class MenuBar : Widget
    {
        private List<MenuItem> items = new List<MenuItem>();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        private MenuItem openMenu = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        
        public List<MenuItem> Items => items;
        
        public MenuBar()
        {
            Size = new Vector(800, 25);
        }
        
        public MenuItem AddMenu(string text)
        {
            var item = new MenuItem(text) { IsTopLevel = true };
            items.Add(item);
            return item;
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            context.FillRectangle(Position, Size, Theme.Default.BackgroundColor.Darken(0.05));
            
            // Draw menu items
            double x = Position.X + Padding.Left;
            foreach (var item in items)
            {
                var textSize = context.MeasureText(item.Text, Font);
                var itemWidth = textSize.X + 20;
                
                // Draw highlight if menu is open
                if (item == openMenu)
                {
                    context.FillRectangle(
                        new Vector(x, Position.Y),
                        new Vector(itemWidth, Size.Y),
                        Theme.Default.SelectionColor
                    );
                }
                
                // Draw text
                context.DrawText(
                    item.Text,
                    new Vector(x + 10, Position.Y + 2),
                    Font,
                    ForegroundColor ?? Theme.Default.ForegroundColor
                );
                
                x += itemWidth;
            }
            
            // Draw open submenu
            if (openMenu != null && openMenu.Children.Count > 0)
            {
                RenderSubmenu(context, openMenu);
            }
        }
        
        private void RenderSubmenu(GraphicsContext context, MenuItem menu)
        {
            double menuX = GetMenuX(menu);
            double menuY = Position.Y + Size.Y;
            double menuWidth = 200;
            double menuHeight = menu.Children.Count * 25;
            
            // Draw menu background
            context.FillRectangle(
                new Vector(menuX, menuY),
                new Vector(menuWidth, menuHeight),
                BackgroundColor ?? Theme.Default.BackgroundColor
            );
            context.DrawRectangle(
                new Vector(menuX, menuY),
                new Vector(menuWidth, menuHeight),
                Theme.Default.BorderColor,
                1
            );
            
            // Draw menu items
            double y = menuY;
            foreach (var item in menu.Children)
            {
                if (item.IsSeparator)
                {
                    context.DrawLine(
                        new Vector(menuX + 5, y + 12),
                        new Vector(menuX + menuWidth - 5, y + 12),
                        Theme.Default.BorderColor,
                        1
                    );
                }
                else
                {
                    // Draw item text
                    context.DrawText(
                        item.Text,
                        new Vector(menuX + 10, y + 2),
                        Font,
                        item.IsEnabled ? (ForegroundColor ?? Theme.Default.ForegroundColor) : Theme.Default.PlaceholderColor
                    );
                    
                    // Draw shortcut if present
                    if (!string.IsNullOrEmpty(item.Shortcut))
                    {
                        var shortcutSize = context.MeasureText(item.Shortcut, Font);
                        context.DrawText(
                            item.Shortcut,
                            new Vector(menuX + menuWidth - shortcutSize.X - 10, y + 2),
                            Font,
                            Theme.Default.PlaceholderColor
                        );
                    }
                }
                
                y += 25;
            }
        }
        
        private double GetMenuX(MenuItem menu)
        {
            double x = Position.X + Padding.Left;
            foreach (var item in items)
            {
                if (item == menu) return x;
                var textSize = Font.Default.Size * item.Text.Length * 0.6 + 20; // Approximation
                x += textSize;
            }
            return x;
        }
    }
    
    public class MenuItem
    {
        private List<MenuItem> children = new List<MenuItem>();
        
        public string Text { get; set; }
        public string? Shortcut { get; set; }
        public Image? Icon { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsChecked { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsTopLevel { get; internal set; }
        public List<MenuItem> Children => children;
        
        public event EventHandler? Click;
        
        public MenuItem(string text)
        {
            Text = text;
        }
        
        public static MenuItem Separator()
        {
            return new MenuItem("-") { IsSeparator = true };
        }
        
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public MenuItem AddItem(string text, EventHandler handler = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
            var item = new MenuItem(text);
            if (handler != null)
                item.Click += handler;
            children.Add(item);
            return item;
        }
        
        public void AddSeparator()
        {
            children.Add(MenuItem.Separator());
        }
        
        internal void OnClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// ToolBar control
    /// </summary>
    public class ToolBar : Widget
    {
        private List<ToolBarItem> items = new List<ToolBarItem>();
        
        public List<ToolBarItem> Items => items;
        public ToolBarStyle Style { get; set; } = ToolBarStyle.Default;
        
        public ToolBar()
        {
            Size = new Vector(800, 35);
        }
        
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public ToolBarButton AddButton(string text, Image icon = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
            var button = new ToolBarButton { Text = text, Icon = icon };
            items.Add(button);
            return button;
        }
        
        public void AddSeparator()
        {
            items.Add(new ToolBarSeparator());
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            context.FillRectangle(Position, Size, Style.BackgroundColor);
            
            // Draw items
            double x = Position.X + Padding.Left;
            foreach (var item in items)
            {
                item.Render(context, new Vector(x, Position.Y), Size.Y);
                x += item.GetWidth() + 5;
            }
        }
    }
    
    public abstract class ToolBarItem
    {
        public abstract void Render(GraphicsContext context, Vector position, double height);
        public abstract double GetWidth();
    }
    
    public class ToolBarButton : ToolBarItem
    {
        public string? Text { get; set; }
        public Image? Icon { get; set; }
        public string? ToolTip { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsToggle { get; set; }
        public bool IsChecked { get; set; }
        
        public event EventHandler? Click;
        
        public override void Render(GraphicsContext context, Vector position, double height)
        {
            double width = GetWidth();
            
            // Draw button background if checked or hovered
            if (IsChecked)
            {
                context.FillRoundedRectangle(position, new Vector(width, height), 2, Theme.Default.SelectionColor);
            }
            
            // Draw icon
            if (Icon != null)
            {
                var iconPos = new Vector(
                    position.X + (width - Icon.Width) / 2,
                    position.Y + (height - Icon.Height) / 2
                );
                context.DrawImage(Icon, iconPos);
            }
            
            // Draw text if no icon
            if (Icon == null && !string.IsNullOrEmpty(Text))
            {
                var textSize = Font.Default.Size * Text.Length * 0.6; // Approximation
                context.DrawText(
                    Text,
                    new Vector(position.X + 5, position.Y + 5),
                    Font.Default,
                    IsEnabled ? Color.Black : Color.Gray
                );
            }
        }
        
        public override double GetWidth()
        {
            if (Icon != null)
                return global::System.Math.Max(30, Icon.Width + 10);
            else if (!string.IsNullOrEmpty(Text))
                return Font.Default.Size * Text.Length * 0.6 + 10; // Approximation
            else
                return 30;
        }
    }
    
    public class ToolBarSeparator : ToolBarItem
    {
        public override void Render(GraphicsContext context, Vector position, double height)
        {
            context.DrawLine(
                position + new Vector(2, 5),
                position + new Vector(2, height - 5),
                Theme.Default.BorderColor,
                1
            );
        }
        
        public override double GetWidth() => 5;
    }
    
    public class ToolBarStyle
    {
        public Color BackgroundColor { get; set; }
        
        public static ToolBarStyle Default => new ToolBarStyle
        {
            BackgroundColor = new Color(240, 240, 240)
        };
    }
    
    /// <summary>
    /// StatusBar control
    /// </summary>
    public class StatusBar : Widget
    {
        private List<StatusBarPanel> panels = new List<StatusBarPanel>();
        
        public List<StatusBarPanel> Panels => panels;
        
        public StatusBar()
        {
            Size = new Vector(800, 25);
        }
        
        public StatusBarPanel AddPanel(string text, double width = 0)
        {
            var panel = new StatusBarPanel { Text = text, Width = width };
            panels.Add(panel);
            return panel;
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            context.FillRectangle(Position, Size, Theme.Default.BackgroundColor.Darken(0.05));
            context.DrawLine(
                new Vector(Position.X, Position.Y),
                new Vector(Position.X + Size.X, Position.Y),
                Theme.Default.BorderColor,
                1
            );
            
            // Draw panels
            double x = Position.X;
            for (int i = 0; i < panels.Count; i++)
            {
                var panel = panels[i];
                double panelWidth = panel.Width > 0 ? panel.Width : (Size.X - x) / (panels.Count - i);
                
                // Draw panel text
                context.DrawText(
                    panel.Text ?? string.Empty,
                    new Vector(x + 5, Position.Y + 2),
                    Font.Small,
                    ForegroundColor ?? Color.Black
                );
                
                // Draw separator
                if (i < panels.Count - 1)
                {
                    context.DrawLine(
                        new Vector(x + panelWidth, Position.Y + 2),
                        new Vector(x + panelWidth, Position.Y + Size.Y - 2),
                        Theme.Default.BorderColor,
                        1
                    );
                }
                
                x += panelWidth;
            }
        }
    }
    
    public class StatusBarPanel
    {
        public string? Text { get; set; }
        public double Width { get; set; }
        public Image? Icon { get; set; }
    }
    
    /// <summary>
    /// ScrollablePanel - container with scrollbars
    /// </summary>
    public class ScrollablePanel : Widget
    {
        private double horizontalScroll = 0;
        private double verticalScroll = 0;
        private Vector contentSize;
        
        public Vector ContentSize 
        { 
            get => contentSize;
            set 
            {
                contentSize = value;
                UpdateScrollbars();
            }
        }
        public bool ShowHorizontalScrollbar { get; set; } = true;
        public bool ShowVerticalScrollbar { get; set; } = true;
        public double ScrollbarWidth { get; set; } = 15;
        
        public ScrollablePanel()
        {
            Size = new Vector(400, 300);
            contentSize = Size;
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            if (BackgroundColor is not null)
            {
                context.FillRectangle(Position, Size, BackgroundColor.Value);
            }
            
            // Set up clipping for content
            var contentArea = new Rectangle(
                Position.X,
                Position.Y,
                Size.X - (NeedsVerticalScrollbar() ? ScrollbarWidth : 0),
                Size.Y - (NeedsHorizontalScrollbar() ? ScrollbarWidth : 0)
            );
            
            context.PushClip(contentArea);
            
            // Render children with scroll offset
            context.PushTransform();
            context.Translate(new Vector(-horizontalScroll, -verticalScroll));
            
            foreach (var child in children)
            {
                if (child.IsVisible)
                    child.Render(context);
            }
            
            context.PopTransform();
            context.PopClip();
            
            // Draw scrollbars
            if (NeedsVerticalScrollbar())
                DrawVerticalScrollbar(context);
            if (NeedsHorizontalScrollbar())
                DrawHorizontalScrollbar(context);
        }
        
        private bool NeedsHorizontalScrollbar()
        {
            return ShowHorizontalScrollbar && contentSize.X > Size.X;
        }
        
        private bool NeedsVerticalScrollbar()
        {
            return ShowVerticalScrollbar && contentSize.Y > Size.Y;
        }
        
        private void DrawVerticalScrollbar(GraphicsContext context)
        {
            double x = Position.X + Size.X - ScrollbarWidth;
            double height = Size.Y - (NeedsHorizontalScrollbar() ? ScrollbarWidth : 0);
            
            // Draw track
            context.FillRectangle(
                new Vector(x, Position.Y),
                new Vector(ScrollbarWidth, height),
                Theme.Default.TrackColor
            );
            
            // Calculate thumb size and position
            double thumbHeight = (height / contentSize.Y) * height;
            double thumbY = Position.Y + (verticalScroll / contentSize.Y) * height;
            
            // Draw thumb
            context.FillRoundedRectangle(
                new Vector(x + 2, thumbY),
                new Vector(ScrollbarWidth - 4, thumbHeight),
                2,
                Theme.Default.BorderColor
            );
        }
        
        private void DrawHorizontalScrollbar(GraphicsContext context)
        {
            double y = Position.Y + Size.Y - ScrollbarWidth;
            double width = Size.X - (NeedsVerticalScrollbar() ? ScrollbarWidth : 0);
            
            // Draw track
            context.FillRectangle(
                new Vector(Position.X, y),
                new Vector(width, ScrollbarWidth),
                Theme.Default.TrackColor
            );
            
            // Calculate thumb size and position
            double thumbWidth = (width / contentSize.X) * width;
            double thumbX = Position.X + (horizontalScroll / contentSize.X) * width;
            
            // Draw thumb
            context.FillRoundedRectangle(
                new Vector(thumbX, y + 2),
                new Vector(thumbWidth, ScrollbarWidth - 4),
                2,
                Theme.Default.BorderColor
            );
        }
        
        private void UpdateScrollbars()
        {
            // Clamp scroll values
            horizontalScroll = global::System.Math.Max(0, global::System.Math.Min(horizontalScroll, contentSize.X - Size.X));
            verticalScroll = global::System.Math.Max(0, global::System.Math.Min(verticalScroll, contentSize.Y - Size.Y));
        }
        
        public void ScrollTo(Vector position)
        {
            horizontalScroll = global::System.Math.Max(0, global::System.Math.Min(position.X, contentSize.X - Size.X));
            verticalScroll = global::System.Math.Max(0, global::System.Math.Min(position.Y, contentSize.Y - Size.Y));
        }
    }
    
    // Event argument classes
    public class TabChangedEventArgs : EventArgs
    {
        public int OldIndex { get; }
        public int NewIndex { get; }
        public TabChangedEventArgs(int oldIndex, int newIndex)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }
    
    public class ItemEventArgs : EventArgs
    {
        public object Item { get; }
        public int Index { get; }
        public ItemEventArgs(object item, int index)
        {
            Item = item;
            Index = index;
        }
    }
    
    public class TreeNodeEventArgs : EventArgs
    {
        public TreeNode? OldNode { get; }
        public TreeNode? NewNode { get; }
        public TreeNodeEventArgs(TreeNode? oldNode, TreeNode? newNode)
        {
            OldNode = oldNode;
            NewNode = newNode;
        }
    }
    
    public enum SelectionMode
    {
        None,
        Single,
        Multiple,
        Extended
    }
    
    public enum TabHeaderPosition
    {
        Top,
        Bottom,
        Left,
        Right
    }

    /// <summary>
    /// Panel control - a container for other widgets
    /// </summary>
    public class Panel : Widget
    {
        public Panel()
        {
            Size = new Vector(200, 200);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            if (BackgroundColor is not null)
            {
                context.FillRectangle(Position, Size, BackgroundColor.Value);
            }
            
            // Render all children
            foreach (var child in Children)
            {
                if (child.IsVisible)
                {
                    child.Render(context);
                }
            }
        }
    }
} 