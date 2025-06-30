using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.StdLib.Math;

namespace Ouroboros.StdLib.UI
{
    /// <summary>
    /// Base class for all layout managers
    /// </summary>
    public abstract class Layout
    {
        protected List<Widget> widgets = new List<Widget>();
        public Margin Margin { get; set; } = new Margin(0);
        public Padding Padding { get; set; } = new Padding(0);
        
        public virtual void AddWidget(Widget widget)
        {
            widgets.Add(widget);
        }
        
        public virtual void RemoveWidget(Widget widget)
        {
            widgets.Remove(widget);
        }
        
        public virtual void Clear()
        {
            widgets.Clear();
        }
        
        public abstract void ApplyTo(Window window);
        public abstract void DoLayout(Vector containerSize);
    }
    
    /// <summary>
    /// Flow layout - arranges widgets in a flowing manner
    /// </summary>
    public class FlowLayout : Layout
    {
        public FlowDirection Direction { get; set; } = FlowDirection.LeftToRight;
        public double Spacing { get; set; } = 5;
        public bool WrapContent { get; set; } = true;
        public Alignment HorizontalAlignment { get; set; } = Alignment.Start;
        public Alignment VerticalAlignment { get; set; } = Alignment.Start;
        
        public override void ApplyTo(Window window)
        {
            DoLayout(window.Size);
        }
        
        public override void DoLayout(Vector containerSize)
        {
            double x = Padding.Left;
            double y = Padding.Top;
            double rowHeight = 0;
            double availableWidth = containerSize.X - Padding.Left - Padding.Right;
            
            foreach (var widget in widgets.Where(w => w.IsVisible))
            {
                var widgetWidth = widget.Size.X + widget.Margin.Left + widget.Margin.Right;
                var widgetHeight = widget.Size.Y + widget.Margin.Top + widget.Margin.Bottom;
                
                // Check if we need to wrap to next line
                if (WrapContent && x + widgetWidth > availableWidth && x > Padding.Left)
                {
                    x = Padding.Left;
                    y += rowHeight + Spacing;
                    rowHeight = 0;
                }
                
                // Position widget
                widget.Position = new Vector(
                    x + widget.Margin.Left,
                    y + widget.Margin.Top
                );
                
                // Update position for next widget
                if (Direction == FlowDirection.LeftToRight)
                {
                    x += widgetWidth + Spacing;
                    rowHeight = global::System.Math.Max(rowHeight, widgetHeight);
                }
                else
                {
                    y += widgetHeight + Spacing;
                }
            }
        }
    }
    
    /// <summary>
    /// Grid layout - arranges widgets in a grid
    /// </summary>
    public class GridLayout : Layout
    {
        public int Columns { get; set; } = 1;
        public int Rows { get; set; } = 0; // 0 means auto
        public double ColumnSpacing { get; set; } = 5;
        public double RowSpacing { get; set; } = 5;
        public List<double> ColumnWidths { get; set; } = new List<double>();
        public List<double> RowHeights { get; set; } = new List<double>();
        
        private Dictionary<Widget, GridPosition> widgetPositions = new Dictionary<Widget, GridPosition>();
        
        public void SetPosition(Widget widget, int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            widgetPositions[widget] = new GridPosition(column, row, columnSpan, rowSpan);
        }
        
        public override void AddWidget(Widget widget)
        {
            base.AddWidget(widget);
            if (!widgetPositions.ContainsKey(widget))
            {
                // Auto-position widget
                int nextPos = widgets.Count - 1;
                int row = nextPos / Columns;
                int col = nextPos % Columns;
                widgetPositions[widget] = new GridPosition(col, row);
            }
        }
        
        public override void RemoveWidget(Widget widget)
        {
            base.RemoveWidget(widget);
            widgetPositions.Remove(widget);
        }
        
        public override void ApplyTo(Window window)
        {
            DoLayout(window.Size);
        }
        
        public override void DoLayout(Vector containerSize)
        {
            // Calculate column widths
            var colWidths = CalculateColumnWidths(containerSize);
            var rowHeights = CalculateRowHeights(containerSize);
            
            // Position widgets
            foreach (var widget in widgets.Where(w => w.IsVisible))
            {
                if (widgetPositions.TryGetValue(widget, out var pos))
                {
                    double x = Padding.Left;
                    double y = Padding.Top;
                    
                    // Calculate x position
                    for (int i = 0; i < pos.Column; i++)
                    {
                        x += colWidths[i] + ColumnSpacing;
                    }
                    
                    // Calculate y position
                    for (int i = 0; i < pos.Row; i++)
                    {
                        y += rowHeights[i] + RowSpacing;
                    }
                    
                    // Calculate size if spanning multiple cells
                    double width = 0;
                    double height = 0;
                    
                    for (int i = 0; i < pos.ColumnSpan && pos.Column + i < colWidths.Count; i++)
                    {
                        width += colWidths[pos.Column + i];
                        if (i > 0) width += ColumnSpacing;
                    }
                    
                    for (int i = 0; i < pos.RowSpan && pos.Row + i < rowHeights.Count; i++)
                    {
                        height += rowHeights[pos.Row + i];
                        if (i > 0) height += RowSpacing;
                    }
                    
                    widget.Position = new Vector(x + widget.Margin.Left, y + widget.Margin.Top);
                    if (width > 0 && height > 0)
                    {
                        widget.Size = new Vector(
                            width - widget.Margin.Left - widget.Margin.Right,
                            height - widget.Margin.Top - widget.Margin.Bottom
                        );
                    }
                }
            }
        }
        
        private List<double> CalculateColumnWidths(Vector containerSize)
        {
            var widths = new List<double>();
            double availableWidth = containerSize.X - Padding.Left - Padding.Right - (Columns - 1) * ColumnSpacing;
            
            if (ColumnWidths.Count > 0)
            {
                widths = new List<double>(ColumnWidths);
            }
            else
            {
                // Auto-calculate equal widths
                double colWidth = availableWidth / Columns;
                for (int i = 0; i < Columns; i++)
                {
                    widths.Add(colWidth);
                }
            }
            
            return widths;
        }
        
        private List<double> CalculateRowHeights(Vector containerSize)
        {
            var heights = new List<double>();
            
            if (RowHeights.Count > 0)
            {
                heights = new List<double>(RowHeights);
            }
            else
            {
                // Calculate based on widget requirements
                int maxRow = widgetPositions.Values.Max(p => p.Row + p.RowSpan - 1) + 1;
                for (int i = 0; i < maxRow; i++)
                {
                    double maxHeight = 0;
                    foreach (var kvp in widgetPositions)
                    {
                        if (kvp.Value.Row == i && kvp.Value.RowSpan == 1)
                        {
                            maxHeight = global::System.Math.Max(maxHeight, kvp.Key.Size.Y + kvp.Key.Margin.Top + kvp.Key.Margin.Bottom);
                        }
                    }
                    heights.Add(maxHeight > 0 ? maxHeight : 30); // Default height
                }
            }
            
            return heights;
        }
        
        private class GridPosition
        {
            public int Column { get; set; }
            public int Row { get; set; }
            public int ColumnSpan { get; set; }
            public int RowSpan { get; set; }
            
            public GridPosition(int column, int row, int columnSpan = 1, int rowSpan = 1)
            {
                Column = column;
                Row = row;
                ColumnSpan = columnSpan;
                RowSpan = rowSpan;
            }
        }
    }
    
    /// <summary>
    /// Stack layout - arranges widgets in a vertical or horizontal stack
    /// </summary>
    public class StackLayout : Layout
    {
        public Orientation Orientation { get; set; } = Orientation.Vertical;
        public double Spacing { get; set; } = 5;
        public Alignment HorizontalAlignment { get; set; } = Alignment.Stretch;
        public Alignment VerticalAlignment { get; set; } = Alignment.Start;
        
        public override void ApplyTo(Window window)
        {
            DoLayout(window.Size);
        }
        
        public override void DoLayout(Vector containerSize)
        {
            double position = Orientation == Orientation.Vertical ? Padding.Top : Padding.Left;
            double availableWidth = containerSize.X - Padding.Left - Padding.Right;
            double availableHeight = containerSize.Y - Padding.Top - Padding.Bottom;
            
            foreach (var widget in widgets.Where(w => w.IsVisible))
            {
                if (Orientation == Orientation.Vertical)
                {
                    // Calculate X position based on alignment
                    double x = Padding.Left + widget.Margin.Left;
                    if (HorizontalAlignment == Alignment.Center)
                    {
                        x = Padding.Left + (availableWidth - widget.Size.X - widget.Margin.Left - widget.Margin.Right) / 2 + widget.Margin.Left;
                    }
                    else if (HorizontalAlignment == Alignment.End)
                    {
                        x = containerSize.X - Padding.Right - widget.Size.X - widget.Margin.Right;
                    }
                    else if (HorizontalAlignment == Alignment.Stretch)
                    {
                        widget.Size = new Vector(availableWidth - widget.Margin.Left - widget.Margin.Right, widget.Size.Y);
                    }
                    
                    widget.Position = new Vector(x, position + widget.Margin.Top);
                    position += widget.Size.Y + widget.Margin.Top + widget.Margin.Bottom + Spacing;
                }
                else
                {
                    // Calculate Y position based on alignment
                    double y = Padding.Top + widget.Margin.Top;
                    if (VerticalAlignment == Alignment.Center)
                    {
                        y = Padding.Top + (availableHeight - widget.Size.Y - widget.Margin.Top - widget.Margin.Bottom) / 2 + widget.Margin.Top;
                    }
                    else if (VerticalAlignment == Alignment.End)
                    {
                        y = containerSize.Y - Padding.Bottom - widget.Size.Y - widget.Margin.Bottom;
                    }
                    else if (VerticalAlignment == Alignment.Stretch)
                    {
                        widget.Size = new Vector(widget.Size.X, availableHeight - widget.Margin.Top - widget.Margin.Bottom);
                    }
                    
                    widget.Position = new Vector(position + widget.Margin.Left, y);
                    position += widget.Size.X + widget.Margin.Left + widget.Margin.Right + Spacing;
                }
            }
        }
    }
    
    /// <summary>
    /// Dock layout - docks widgets to edges of container
    /// </summary>
    public class DockLayout : Layout
    {
        private Dictionary<Widget, DockPosition> dockPositions = new Dictionary<Widget, DockPosition>();
        
        public void Dock(Widget widget, DockPosition position)
        {
            dockPositions[widget] = position;
        }
        
        public override void AddWidget(Widget widget)
        {
            base.AddWidget(widget);
            if (!dockPositions.ContainsKey(widget))
            {
                dockPositions[widget] = DockPosition.Fill;
            }
        }
        
        public override void RemoveWidget(Widget widget)
        {
            base.RemoveWidget(widget);
            dockPositions.Remove(widget);
        }
        
        public override void ApplyTo(Window window)
        {
            DoLayout(window.Size);
        }
        
        public override void DoLayout(Vector containerSize)
        {
            double left = Padding.Left;
            double top = Padding.Top;
            double right = containerSize.X - Padding.Right;
            double bottom = containerSize.Y - Padding.Bottom;
            
            // Process docked widgets in order: Top, Bottom, Left, Right, Fill
            var sortedWidgets = widgets.Where(w => w.IsVisible)
                .OrderBy(w => dockPositions.ContainsKey(w) ? (int)dockPositions[w] : 5)
                .ToList();
            
            foreach (var widget in sortedWidgets)
            {
                if (!dockPositions.TryGetValue(widget, out var dockPos))
                    continue;
                    
                switch (dockPos)
                {
                    case DockPosition.Top:
                        widget.Position = new Vector(left + widget.Margin.Left, top + widget.Margin.Top);
                        widget.Size = new Vector(
                            right - left - widget.Margin.Left - widget.Margin.Right,
                            widget.Size.Y
                        );
                        top += widget.Size.Y + widget.Margin.Top + widget.Margin.Bottom;
                        break;
                        
                    case DockPosition.Bottom:
                        widget.Position = new Vector(
                            left + widget.Margin.Left,
                            bottom - widget.Size.Y - widget.Margin.Bottom
                        );
                        widget.Size = new Vector(
                            right - left - widget.Margin.Left - widget.Margin.Right,
                            widget.Size.Y
                        );
                        bottom -= widget.Size.Y + widget.Margin.Top + widget.Margin.Bottom;
                        break;
                        
                    case DockPosition.Left:
                        widget.Position = new Vector(left + widget.Margin.Left, top + widget.Margin.Top);
                        widget.Size = new Vector(
                            widget.Size.X,
                            bottom - top - widget.Margin.Top - widget.Margin.Bottom
                        );
                        left += widget.Size.X + widget.Margin.Left + widget.Margin.Right;
                        break;
                        
                    case DockPosition.Right:
                        widget.Position = new Vector(
                            right - widget.Size.X - widget.Margin.Right,
                            top + widget.Margin.Top
                        );
                        widget.Size = new Vector(
                            widget.Size.X,
                            bottom - top - widget.Margin.Top - widget.Margin.Bottom
                        );
                        right -= widget.Size.X + widget.Margin.Left + widget.Margin.Right;
                        break;
                        
                    case DockPosition.Fill:
                        widget.Position = new Vector(left + widget.Margin.Left, top + widget.Margin.Top);
                        widget.Size = new Vector(
                            right - left - widget.Margin.Left - widget.Margin.Right,
                            bottom - top - widget.Margin.Top - widget.Margin.Bottom
                        );
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Absolute layout - widgets are positioned using absolute coordinates
    /// </summary>
    public class AbsoluteLayout : Layout
    {
        public override void ApplyTo(Window window)
        {
            // In absolute layout, widgets maintain their set positions
            // No automatic positioning is done
        }
        
        public override void DoLayout(Vector containerSize)
        {
            // Widgets use their existing Position property
            // No layout calculation needed
        }
    }
    
    /// <summary>
    /// Wrap panel layout - similar to flow but wraps content
    /// </summary>
    public class WrapLayout : FlowLayout
    {
        public WrapLayout()
        {
            WrapContent = true;
        }
    }
    
    public enum FlowDirection
    {
        LeftToRight,
        TopToBottom
    }
    
    public enum Alignment
    {
        Start,
        Center,
        End,
        Stretch
    }
    
    public enum DockPosition
    {
        Top = 0,
        Bottom = 1,
        Left = 2,
        Right = 3,
        Fill = 4
    }
} 