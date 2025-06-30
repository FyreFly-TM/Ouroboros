using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.StdLib.Math;
using static Ouroboros.StdLib.Math.MathUtils;

namespace Ouroboros.StdLib.UI
{
    /// <summary>
    /// NumericUpDown control - numeric input with increment/decrement buttons
    /// </summary>
    public class NumericUpDown : Widget
    {
        private double value;
        private TextBox textBox;
        private Button upButton;
        private Button downButton;
        
        public double Minimum { get; set; } = 0;
        public double Maximum { get; set; } = 100;
        public double Step { get; set; } = 1;
        public int DecimalPlaces { get; set; } = 0;
        
        public double Value
        {
            get => value;
            set
            {
                var newValue = global::System.Math.Max(Minimum, global::System.Math.Min(Maximum, value));
                if (this.value != newValue)
                {
                    var oldValue = this.value;
                    this.value = newValue;
                    UpdateTextBox();
                    ValueChanged?.Invoke(this, new ValueChangedEventArgs(oldValue, newValue));
                }
            }
        }
        
        public event EventHandler<ValueChangedEventArgs> ValueChanged;
        
        public NumericUpDown()
        {
            Size = new Vector(120, 30);
            
            // Create text box
            textBox = new TextBox
            {
                Size = new Vector(90, 30),
                Position = Position
            };
            
            // Create up button
            upButton = new Button("▲")
            {
                Size = new Vector(30, 15),
                Position = Position + new Vector(90, 0)
            };
            upButton.Click += (s, e) => Increment();
            
            // Create down button
            downButton = new Button("▼")
            {
                Size = new Vector(30, 15),
                Position = Position + new Vector(90, 15)
            };
            downButton.Click += (s, e) => Decrement();
            
            UpdateTextBox();
        }
        
        public void Increment()
        {
            Value = Round(Value + Step, DecimalPlaces);
        }
        
        public void Decrement()
        {
            Value = Round(Value - Step, DecimalPlaces);
        }
        
        private void UpdateTextBox()
        {
            textBox.Text = DecimalPlaces > 0 
                ? value.ToString($"F{DecimalPlaces}")
                : value.ToString("F0");
        }
        
        public override void Render(GraphicsContext context)
        {
            textBox.Position = Position;
            textBox.Render(context);
            
            upButton.Position = Position + new Vector(Size.X - 30, 0);
            upButton.Render(context);
            
            downButton.Position = Position + new Vector(Size.X - 30, 15);
            downButton.Render(context);
        }
    }
    
    /// <summary>
    /// DatePicker control - date selection with calendar popup
    /// </summary>
    public class DatePicker : Widget
    {
        private DateTime selectedDate;
        private TextBox textBox;
        private Button calendarButton;
        private bool isCalendarOpen;
        private Calendar calendar;
        
        public DateTime SelectedDate
        {
            get => selectedDate;
            set
            {
                if (selectedDate != value)
                {
                    var oldDate = selectedDate;
                    selectedDate = value;
                    UpdateTextBox();
                    DateChanged?.Invoke(this, new DateChangedEventArgs(oldDate, selectedDate));
                }
            }
        }
        
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public DateTime MinDate { get; set; } = DateTime.MinValue;
        public DateTime MaxDate { get; set; } = DateTime.MaxValue;
        
        public event EventHandler<DateChangedEventArgs> DateChanged;
        
        public DatePicker()
        {
            Size = new Vector(200, 30);
            selectedDate = DateTime.Now.Date;
            
            // Create text box
            textBox = new TextBox
            {
                Size = new Vector(170, 30),
                Position = Position,
                IsReadOnly = true
            };
            
            // Create calendar button
            calendarButton = new Button("📅")
            {
                Size = new Vector(30, 30),
                Position = Position + new Vector(170, 0)
            };
            calendarButton.Click += (s, e) => ToggleCalendar();
            
            // Create calendar popup
            calendar = new Calendar
            {
                Position = Position + new Vector(0, 30),
                IsVisible = false
            };
            calendar.DateSelected += (s, e) =>
            {
                SelectedDate = e.Date;
                isCalendarOpen = false;
                calendar.IsVisible = false;
            };
            
            UpdateTextBox();
        }
        
        private void ToggleCalendar()
        {
            isCalendarOpen = !isCalendarOpen;
            calendar.IsVisible = isCalendarOpen;
            if (isCalendarOpen)
            {
                calendar.SelectedDate = selectedDate;
            }
        }
        
        private void UpdateTextBox()
        {
            textBox.Text = selectedDate.ToString(DateFormat);
        }
        
        public override void Render(GraphicsContext context)
        {
            textBox.Position = Position;
            textBox.Render(context);
            
            calendarButton.Position = Position + new Vector(Size.X - 30, 0);
            calendarButton.Render(context);
            
            if (isCalendarOpen)
            {
                calendar.Position = Position + new Vector(0, Size.Y);
                calendar.Render(context);
            }
        }
    }
    
    /// <summary>
    /// Calendar control - month view calendar
    /// </summary>
    public class Calendar : Widget
    {
        private DateTime currentMonth;
        private DateTime selectedDate;
        
        public DateTime SelectedDate
        {
            get => selectedDate;
            set
            {
                selectedDate = value;
                currentMonth = new DateTime(value.Year, value.Month, 1);
            }
        }
        
        public event EventHandler<DateSelectedEventArgs> DateSelected;
        
        public Calendar()
        {
            Size = new Vector(250, 200);
            selectedDate = DateTime.Now.Date;
            currentMonth = new DateTime(selectedDate.Year, selectedDate.Month, 1);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw background
            context.FillRectangle(Position, Size, BackgroundColor ?? Color.White);
            context.DrawRectangle(Position, Size, Theme.Default.BorderColor, 1);
            
            // Draw header
            var headerHeight = 30;
            var monthYear = currentMonth.ToString("MMMM yyyy");
            var textSize = context.MeasureText(monthYear, Font);
            
            context.DrawText(
                monthYear,
                Position + new Vector((Size.X - textSize.X) / 2, 5),
                Font,
                ForegroundColor ?? Color.Black
            );
            
            // Draw navigation buttons
            var prevButton = new Button("◀") { Position = Position + new Vector(5, 2), Size = new Vector(25, 25) };
            var nextButton = new Button("▶") { Position = Position + new Vector(Size.X - 30, 2), Size = new Vector(25, 25) };
            prevButton.Render(context);
            nextButton.Render(context);
            
            // Draw day headers
            var dayWidth = Size.X / 7;
            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for (int i = 0; i < 7; i++)
            {
                context.DrawText(
                    dayNames[i],
                    Position + new Vector(i * dayWidth + 5, headerHeight),
                    Font.Small,
                    Theme.Default.PlaceholderColor
                );
            }
            
            // Draw days
            var firstDay = currentMonth;
            var firstDayOfWeek = (int)firstDay.DayOfWeek;
            var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
            
            double dayY = headerHeight + 20;
            var dayHeight = (Size.Y - dayY) / 6;
            
            for (int week = 0; week < 6; week++)
            {
                for (int day = 0; day < 7; day++)
                {
                    var dayNumber = week * 7 + day - firstDayOfWeek + 1;
                    if (dayNumber > 0 && dayNumber <= daysInMonth)
                    {
                        var date = new DateTime(currentMonth.Year, currentMonth.Month, dayNumber);
                        var dayX = day * dayWidth;
                        
                        // Highlight selected date
                        if (date == selectedDate)
                        {
                            context.FillCircle(
                                Position + new Vector(dayX + dayWidth / 2, dayY + dayHeight / 2),
                                15,
                                Theme.Default.AccentColor
                            );
                        }
                        
                        // Draw day number
                        var dayText = dayNumber.ToString();
                        var dayTextSize = context.MeasureText(dayText, Font.Small);
                        context.DrawText(
                            dayText,
                            Position + new Vector(dayX + (dayWidth - dayTextSize.X) / 2, dayY + 5),
                            Font.Small,
                            date == selectedDate ? Color.White : (ForegroundColor ?? Color.Black)
                        );
                    }
                }
                dayY += dayHeight;
            }
        }
    }
    
    /// <summary>
    /// ColorPicker control - color selection widget
    /// </summary>
    public class ColorPicker : Widget
    {
        private Color selectedColor;
        private bool isDropDownOpen;
        private double hue = 0;
        private double saturation = 1;
        private double value = 1;
        
        public Color SelectedColor
        {
            get => selectedColor;
            set
            {
                if (!selectedColor.Equals(value))
                {
                    var oldColor = selectedColor;
                    selectedColor = value;
                    UpdateHSV();
                    ColorChanged?.Invoke(this, new ColorChangedEventArgs(oldColor, selectedColor));
                }
            }
        }
        
        public event EventHandler<ColorChangedEventArgs> ColorChanged;
        
        public ColorPicker()
        {
            Size = new Vector(200, 30);
            selectedColor = Color.White;
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw color preview box
            context.FillRectangle(Position, new Vector(30, 30), selectedColor);
            context.DrawRectangle(Position, new Vector(30, 30), Theme.Default.BorderColor, 1);
            
            // Draw color text
            var colorText = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
            context.DrawText(
                colorText,
                Position + new Vector(40, 5),
                Font,
                ForegroundColor ?? Color.Black
            );
            
            // Draw dropdown arrow
            var arrowPos = Position + new Vector(Size.X - 20, Size.Y / 2);
            DrawArrow(context, arrowPos, isDropDownOpen);
            
            // Draw color picker dropdown if open
            if (isDropDownOpen)
            {
                DrawColorPickerDropdown(context);
            }
        }
        
        private void DrawColorPickerDropdown(GraphicsContext context)
        {
            var dropdownPos = Position + new Vector(0, Size.Y);
            var dropdownSize = new Vector(250, 200);
            
            // Draw background
            context.FillRectangle(dropdownPos, dropdownSize, BackgroundColor ?? Color.White);
            context.DrawRectangle(dropdownPos, dropdownSize, Theme.Default.BorderColor, 1);
            
            // Draw hue slider
            var hueSliderPos = dropdownPos + new Vector(10, 10);
            var hueSliderSize = new Vector(230, 20);
            DrawHueSlider(context, hueSliderPos, hueSliderSize);
            
            // Draw saturation/value square
            var svSquarePos = dropdownPos + new Vector(10, 40);
            var svSquareSize = new Vector(150, 150);
            DrawSaturationValueSquare(context, svSquarePos, svSquareSize);
            
            // Draw preview
            var previewPos = dropdownPos + new Vector(170, 40);
            var previewSize = new Vector(70, 70);
            context.FillRectangle(previewPos, previewSize, selectedColor);
            context.DrawRectangle(previewPos, previewSize, Theme.Default.BorderColor, 1);
            
            // Draw RGB values
            var rgbY = previewPos.Y + previewSize.Y + 10;
            context.DrawText($"R: {selectedColor.R}", new Vector(previewPos.X, rgbY), Font.Small, ForegroundColor ?? Color.Black);
            context.DrawText($"G: {selectedColor.G}", new Vector(previewPos.X, rgbY + 20), Font.Small, ForegroundColor ?? Color.Black);
            context.DrawText($"B: {selectedColor.B}", new Vector(previewPos.X, rgbY + 40), Font.Small, ForegroundColor ?? Color.Black);
        }
        
        private void DrawHueSlider(GraphicsContext context, Vector position, Vector size)
        {
            // Draw hue gradient
            for (int x = 0; x < (int)size.X; x++)
            {
                var h = x / size.X * 360;
                var color = HSVToRGB(h, 1, 1);
                context.DrawLine(
                    position + new Vector(x, 0),
                    position + new Vector(x, size.Y),
                    color,
                    1
                );
            }
            
            // Draw thumb
            var thumbX = position.X + hue / 360 * size.X;
            context.FillCircle(new Vector(thumbX, position.Y + size.Y / 2), 8, Color.White);
            context.DrawCircle(new Vector(thumbX, position.Y + size.Y / 2), 8, Color.Black, 2);
        }
        
        private void DrawSaturationValueSquare(GraphicsContext context, Vector position, Vector size)
        {
            // This is a simplified representation
            // In a real implementation, you'd draw a proper gradient
            var baseColor = HSVToRGB(hue, 1, 1);
            context.FillRectangle(position, size, baseColor);
            
            // Draw selection circle
            var x = position.X + saturation * size.X;
            var y = position.Y + (1 - value) * size.Y;
            context.DrawCircle(new Vector(x, y), 5, Color.White, 2);
            context.DrawCircle(new Vector(x, y), 6, Color.Black, 1);
        }
        
        private void DrawArrow(GraphicsContext context, Vector position, bool isOpen)
        {
            var path = new Path();
            if (isOpen)
            {
                path.MoveTo(position + new Vector(-5, 2));
                path.LineTo(position + new Vector(0, -3));
                path.LineTo(position + new Vector(5, 2));
            }
            else
            {
                path.MoveTo(position + new Vector(-5, -2));
                path.LineTo(position + new Vector(0, 3));
                path.LineTo(position + new Vector(5, -2));
            }
            context.DrawPath(path, ForegroundColor ?? Color.Black, 1);
        }
        
        private void UpdateHSV()
        {
            // Convert RGB to HSV
            double r = selectedColor.R / 255.0;
            double g = selectedColor.G / 255.0;
            double b = selectedColor.B / 255.0;
            
            double max = global::System.Math.Max(r, global::System.Math.Max(g, b));
            double min = global::System.Math.Min(r, global::System.Math.Min(g, b));
            double delta = max - min;
            
            // Value
            value = max;
            
            // Saturation
            saturation = max == 0 ? 0 : delta / max;
            
            // Hue
            if (delta == 0)
            {
                hue = 0;
            }
            else if (max == r)
            {
                hue = 60 * (((g - b) / delta) % 6);
            }
            else if (max == g)
            {
                hue = 60 * (((b - r) / delta) + 2);
            }
            else
            {
                hue = 60 * (((r - g) / delta) + 4);
            }
            
            if (hue < 0) hue += 360;
        }
        
        private Color HSVToRGB(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Abs((h / 60) % 2 - 1));
            double m = v - c;
            
            double r, g, b;
            if (h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }
            
            return new Color(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255)
            );
        }
    }
    
    /// <summary>
    /// Dialog base class
    /// </summary>
    public abstract class Dialog : Widget
    {
        protected Window parentWindow;
        protected string title;
        protected List<Button> buttons = new List<Button>();
        
        public string Title
        {
            get => title;
            set => title = value;
        }
        
        public DialogResult Result { get; protected set; } = DialogResult.None;
        
        protected Dialog(Window parent, string title)
        {
            this.parentWindow = parent;
            this.title = title;
            
            Size = new Vector(400, 300);
            Position = parent != null 
                ? parent.Position + (parent.Size - Size) / 2 
                : new Vector(100, 100);
        }
        
        public virtual DialogResult ShowDialog()
        {
            IsVisible = true;
            // In a real implementation, this would block until closed
            return Result;
        }
        
        public virtual void Close(DialogResult result)
        {
            Result = result;
            IsVisible = false;
        }
        
        protected virtual void AddButton(string text, DialogResult result)
        {
            var button = new Button(text);
            button.Click += (s, e) => Close(result);
            buttons.Add(button);
        }
        
        public override void Render(GraphicsContext context)
        {
            // Draw dialog background
            context.FillRectangle(Position, Size, BackgroundColor ?? Color.White);
            context.DrawRectangle(Position, Size, Theme.Default.BorderColor, 2);
            
            // Draw title bar
            var titleBarHeight = 30;
            context.FillRectangle(Position, new Vector(Size.X, titleBarHeight), Theme.Default.AccentColor);
            
            // Draw title text
            var titleSize = context.MeasureText(title, Font.Bold);
            context.DrawText(
                title,
                Position + new Vector((Size.X - titleSize.X) / 2, 5),
                Font.Bold,
                Color.White
            );
            
            // Draw close button
            var closeButton = new Button("✕")
            {
                Position = Position + new Vector(Size.X - 30, 0),
                Size = new Vector(30, 30),
                BackgroundColor = Color.Transparent,
                ForegroundColor = Color.White
            };
            closeButton.Click += (s, e) => Close(DialogResult.Cancel);
            closeButton.Render(context);
            
            // Render content
            var contentArea = new Rectangle(
                Position.X + 10,
                Position.Y + titleBarHeight + 10,
                Size.X - 20,
                Size.Y - titleBarHeight - 60
            );
            RenderContent(context, contentArea);
            
            // Render buttons
            var buttonY = Position.Y + Size.Y - 40;
            var buttonX = Position.X + Size.X - 10;
            
            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                var button = buttons[i];
                button.Size = new Vector(80, 30);
                buttonX -= button.Size.X + 10;
                button.Position = new Vector(buttonX, buttonY);
                button.Render(context);
            }
        }
        
        protected abstract void RenderContent(GraphicsContext context, Rectangle contentArea);
    }
    
    /// <summary>
    /// MessageBox dialog
    /// </summary>
    public class MessageBox : Dialog
    {
        private string message;
        private MessageBoxButtons buttonType;
        private MessageBoxIcon icon;
        
        public MessageBox(Window parent, string title, string message, 
                         MessageBoxButtons buttons = MessageBoxButtons.OK,
                         MessageBoxIcon icon = MessageBoxIcon.None)
            : base(parent, title)
        {
            this.message = message;
            this.buttonType = buttons;
            this.icon = icon;
            
            // Add buttons based on type
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    AddButton("OK", DialogResult.OK);
                    break;
                case MessageBoxButtons.OKCancel:
                    AddButton("OK", DialogResult.OK);
                    AddButton("Cancel", DialogResult.Cancel);
                    break;
                case MessageBoxButtons.YesNo:
                    AddButton("Yes", DialogResult.Yes);
                    AddButton("No", DialogResult.No);
                    break;
                case MessageBoxButtons.YesNoCancel:
                    AddButton("Yes", DialogResult.Yes);
                    AddButton("No", DialogResult.No);
                    AddButton("Cancel", DialogResult.Cancel);
                    break;
                case MessageBoxButtons.RetryCancel:
                    AddButton("Retry", DialogResult.Retry);
                    AddButton("Cancel", DialogResult.Cancel);
                    break;
                case MessageBoxButtons.AbortRetryIgnore:
                    AddButton("Abort", DialogResult.Abort);
                    AddButton("Retry", DialogResult.Retry);
                    AddButton("Ignore", DialogResult.Ignore);
                    break;
            }
        }
        
        protected override void RenderContent(GraphicsContext context, Rectangle contentArea)
        {
            // Draw icon if present
            var textX = contentArea.X;
            if (icon != MessageBoxIcon.None)
            {
                var iconSize = 32;
                var iconPos = new Vector(contentArea.X, contentArea.Y);
                
                // Draw icon based on type
                switch (icon)
                {
                    case MessageBoxIcon.Information:
                        context.FillCircle(iconPos + new Vector(iconSize/2, iconSize/2), iconSize/2, Color.Blue);
                        context.DrawText("i", iconPos + new Vector(iconSize/2 - 3, 5), Font.Bold, Color.White);
                        break;
                    case MessageBoxIcon.Warning:
                        DrawTriangle(context, iconPos, iconSize, Color.Yellow);
                        context.DrawText("!", iconPos + new Vector(iconSize/2 - 3, 5), Font.Bold, Color.Black);
                        break;
                    case MessageBoxIcon.Error:
                        context.FillCircle(iconPos + new Vector(iconSize/2, iconSize/2), iconSize/2, Color.Red);
                        context.DrawText("✕", iconPos + new Vector(iconSize/2 - 5, 5), Font.Bold, Color.White);
                        break;
                    case MessageBoxIcon.Question:
                        context.FillCircle(iconPos + new Vector(iconSize/2, iconSize/2), iconSize/2, Color.Blue);
                        context.DrawText("?", iconPos + new Vector(iconSize/2 - 4, 5), Font.Bold, Color.White);
                        break;
                }
                
                textX += iconSize + 10;
            }
            
            // Draw message
            context.DrawText(
                message,
                new Vector(textX, contentArea.Y),
                Font,
                ForegroundColor ?? Color.Black
            );
        }
        
        private void DrawTriangle(GraphicsContext context, Vector position, double size, Color color)
        {
            var path = new Path();
            path.MoveTo(position + new Vector(size/2, 0));
            path.LineTo(position + new Vector(0, size));
            path.LineTo(position + new Vector(size, size));
            path.ClosePath();
            context.FillPath(path, color);
        }
        
        public static DialogResult Show(Window parent, string message, string title = "Message",
                                       MessageBoxButtons buttons = MessageBoxButtons.OK,
                                       MessageBoxIcon icon = MessageBoxIcon.None)
        {
            var msgBox = new MessageBox(parent, title, message, buttons, icon);
            return msgBox.ShowDialog();
        }
    }
    
    /// <summary>
    /// InputDialog for text input
    /// </summary>
    public class InputDialog : Dialog
    {
        private string prompt;
        private TextBox inputBox;
        
        public string InputText => inputBox?.Text ?? "";
        
        public InputDialog(Window parent, string title, string prompt, string defaultValue = "")
            : base(parent, title)
        {
            this.prompt = prompt;
            Size = new Vector(400, 200);
            
            AddButton("OK", DialogResult.OK);
            AddButton("Cancel", DialogResult.Cancel);
            
            inputBox = new TextBox
            {
                Text = defaultValue,
                Size = new Vector(350, 30)
            };
        }
        
        protected override void RenderContent(GraphicsContext context, Rectangle contentArea)
        {
            // Draw prompt
            context.DrawText(
                prompt,
                new Vector(contentArea.X, contentArea.Y),
                Font,
                ForegroundColor ?? Color.Black
            );
            
            // Position and render input box
            inputBox.Position = new Vector(contentArea.X, contentArea.Y + 30);
            inputBox.Render(context);
        }
        
        public static string Show(Window parent, string prompt, string title = "Input", 
                                 string defaultValue = "")
        {
            var dialog = new InputDialog(parent, title, prompt, defaultValue);
            return dialog.ShowDialog() == DialogResult.OK ? dialog.InputText : null;
        }
    }
    
    public class DateChangedEventArgs : EventArgs
    {
        public DateTime OldDate { get; }
        public DateTime NewDate { get; }
        
        public DateChangedEventArgs(DateTime oldDate, DateTime newDate)
        {
            OldDate = oldDate;
            NewDate = newDate;
        }
    }
    
    public class DateSelectedEventArgs : EventArgs
    {
        public DateTime Date { get; }
        
        public DateSelectedEventArgs(DateTime date)
        {
            Date = date;
        }
    }
    
    public class ColorChangedEventArgs : EventArgs
    {
        public Color OldColor { get; }
        public Color NewColor { get; }
        
        public ColorChangedEventArgs(Color oldColor, Color newColor)
        {
            OldColor = oldColor;
            NewColor = newColor;
        }
    }
    
    // Enums
    public enum DialogResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No,
        Abort,
        Retry,
        Ignore
    }
    
    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel,
        RetryCancel,
        AbortRetryIgnore
    }
    
    public enum MessageBoxIcon
    {
        None,
        Information,
        Warning,
        Error,
        Question
    }
} 