using System;
using System.Collections.Generic;
using Ouroboros.StdLib.Math;
using System.Diagnostics;

namespace Ouroboros.StdLib.UI
{
    /// <summary>
    /// Graphics context for rendering UI elements
    /// </summary>
    public class GraphicsContext
    {
        // Platform-specific context for actual rendering
        private object platformContext;
        private Stack<Matrix> transformStack = new Stack<Matrix>();
        private Stack<Rectangle> clipStack = new Stack<Rectangle>();
        private Matrix currentTransform = Matrix.Identity3;
        
        public GraphicsContext(object context = null)
        {
            platformContext = context;
            transformStack.Push(currentTransform);
        }

        [global::System.Diagnostics.Conditional("DEBUG")]
        private void LogOperation(string operation)
        {
            Debug.WriteLine(operation);
        }
        
        public void Clear(Color color) 
        {
            // In a real implementation, this would clear the entire surface
            // For now, we'll just log the operation
            LogOperation($"Clear({color})");
        }
        
        public void DrawLine(Vector start, Vector end, Color color, double width = 1) 
        {
            // Apply current transform to points
            var transformedStart = currentTransform.Multiply(new Vector(start.X, start.Y, 1));
            var transformedEnd = currentTransform.Multiply(new Vector(end.X, end.Y, 1));
            
            LogOperation($"DrawLine({transformedStart}, {transformedEnd}, {color}, {width})");
        }
        
        public void DrawRectangle(Vector position, Vector size, Color color, double width = 1)
        {
            // Draw four lines to form a rectangle
            DrawLine(position, new Vector(position.X + size.X, position.Y), color, width);
            DrawLine(new Vector(position.X + size.X, position.Y), new Vector(position.X + size.X, position.Y + size.Y), color, width);
            DrawLine(new Vector(position.X + size.X, position.Y + size.Y), new Vector(position.X, position.Y + size.Y), color, width);
            DrawLine(new Vector(position.X, position.Y + size.Y), position, color, width);
        }
        
        public void FillRectangle(Vector position, Vector size, Color color)
        {
            var transformedPos = currentTransform.Multiply(new Vector(position.X, position.Y, 1));
            LogOperation($"FillRectangle({transformedPos}, {size}, {color})");
        }
        
        public void DrawRoundedRectangle(Vector position, Vector size, double radius, Color color, double width = 1)
        {
            // Simplified - draw a regular rectangle for now
            // A real implementation would draw arcs at the corners
            DrawRectangle(position, size, color, width);
            LogOperation($"DrawRoundedRectangle with radius {radius}");
        }
        
        public void FillRoundedRectangle(Vector position, Vector size, double radius, Color color)
        {
            FillRectangle(position, size, color);
            LogOperation($"FillRoundedRectangle with radius {radius}");
        }
        
        public void DrawCircle(Vector center, double radius, Color color, double width = 1)
        {
            // Draw a circle using line segments
            const int segments = 32;
            double angleStep = 2 * global::System.Math.PI / segments;
            
            for (int i = 0; i < segments; i++)
            {
                double angle1 = i * angleStep;
                double angle2 = (i + 1) * angleStep;
                
                var p1 = new Vector(
                    center.X + radius * global::System.Math.Cos(angle1),
                    center.Y + radius * global::System.Math.Sin(angle1)
                );
                var p2 = new Vector(
                    center.X + radius * global::System.Math.Cos(angle2),
                    center.Y + radius * global::System.Math.Sin(angle2)
                );
                
                DrawLine(p1, p2, color, width);
            }
        }
        
        public void FillCircle(Vector center, double radius, Color color)
        {
            var transformedCenter = currentTransform.Multiply(new Vector(center.X, center.Y, 1));
            LogOperation($"FillCircle({transformedCenter}, {radius}, {color})");
        }
        
        public void DrawEllipse(Vector center, Vector radii, Color color, double width = 1)
        {
            // Draw an ellipse using line segments
            const int segments = 32;
            double angleStep = 2 * global::System.Math.PI / segments;
            
            for (int i = 0; i < segments; i++)
            {
                double angle1 = i * angleStep;
                double angle2 = (i + 1) * angleStep;
                
                var p1 = new Vector(
                    center.X + radii.X * global::System.Math.Cos(angle1),
                    center.Y + radii.Y * global::System.Math.Sin(angle1)
                );
                var p2 = new Vector(
                    center.X + radii.X * global::System.Math.Cos(angle2),
                    center.Y + radii.Y * global::System.Math.Sin(angle2)
                );
                
                DrawLine(p1, p2, color, width);
            }
        }
        
        public void FillEllipse(Vector center, Vector radii, Color color)
        {
            var transformedCenter = currentTransform.Multiply(new Vector(center.X, center.Y, 1));
            LogOperation($"FillEllipse({transformedCenter}, {radii}, {color})");
        }
        
        public void DrawPolygon(Vector[] points, Color color, double width = 1)
        {
            if (points.Length < 2) return;
            
            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawLine(points[i], points[i + 1], color, width);
            }
            // Close the polygon
            DrawLine(points[points.Length - 1], points[0], color, width);
        }
        
        public void FillPolygon(Vector[] points, Color color)
        {
            LogOperation($"FillPolygon({points.Length} points, {color})");
        }
        
        public void DrawPath(Path path, Color color, double width = 1)
        {
            Vector lastPoint = new Vector(0, 0);
            bool hasLastPoint = false;
            
            foreach (var segment in path.Segments)
            {
                switch (segment)
                {
                    case MoveToSegment moveTo:
                        lastPoint = moveTo.Point;
                        hasLastPoint = true;
                        break;
                    case LineToSegment lineTo:
                        if (hasLastPoint)
                        {
                            DrawLine(lastPoint, lineTo.Point, color, width);
                        }
                        lastPoint = lineTo.Point;
                        hasLastPoint = true;
                        break;
                    case CloseSegment _:
                        // Path closing handled by Path class
                        break;
                }
            }
        }
        
        public void FillPath(Path path, Color color)
        {
            LogOperation($"FillPath({color})");
        }
        
        // Text rendering methods
        public void DrawText(string text, Vector position, Font font, Color color)
        {
            var transformedPos = currentTransform.Multiply(new Vector(position.X, position.Y, 1));
            LogOperation($"DrawText(\"{text}\", {transformedPos}, {font.Family} {font.Size}, {color})");
        }
        
        public void DrawTextWrapped(string text, Vector position, Vector maxSize, Font font, Color color, TextAlignment alignment = TextAlignment.Left)
        {
            // Simple word wrapping implementation
            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = "";
            
            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var testSize = MeasureText(testLine, font);
                
                if (testSize.X > maxSize.X)
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        lines.Add(word);
                        currentLine = "";
                    }
                }
                else
                {
                    currentLine = testLine;
                }
            }
            
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }
            
            // Draw each line
            double y = position.Y;
            foreach (var line in lines)
            {
                double x = position.X;
                
                if (alignment == TextAlignment.Center)
                {
                    var lineSize = MeasureText(line, font);
                    x = position.X + (maxSize.X - lineSize.X) / 2;
                }
                else if (alignment == TextAlignment.Right)
                {
                    var lineSize = MeasureText(line, font);
                    x = position.X + maxSize.X - lineSize.X;
                }
                
                DrawText(line, new Vector(x, y), font, color);
                y += font.Size * 1.2; // Line spacing
            }
        }
        
        public Vector MeasureText(string text, Font font)
        {
            // Calculate text dimensions based on font size and text length
            if (string.IsNullOrEmpty(text))
                return new Vector(0, 0);
                
            // Approximate width: average character width * text length
            double avgCharWidth = font.Size * 0.6; // Typical proportional font ratio
            double width = text.Length * avgCharWidth;
            
            // Height is typically font size plus some padding
            double height = font.Size * 1.2;
            
            return new Vector(width, height);
        }
        
        // Image rendering methods
        public void DrawImage(Image image, Vector position)
        {
            DrawImage(image, position, new Vector(image.Width, image.Height));
        }
        
        public void DrawImage(Image image, Vector position, Vector size)
        {
            var transformedPos = currentTransform.Multiply(new Vector(position.X, position.Y, 1));
            LogOperation($"DrawImage({image.Source}, {transformedPos}, {size})");
        }
        
        public void DrawImage(Image image, Rectangle source, Rectangle destination)
        {
            LogOperation($"DrawImage({image.Source}, src: {source.X},{source.Y},{source.Width}x{source.Height}, dst: {destination.X},{destination.Y},{destination.Width}x{destination.Height})");
        }
        
        // Transform methods
        public void PushTransform()
        {
            transformStack.Push(currentTransform);
        }
        
        public void PopTransform()
        {
            if (transformStack.Count > 1)
            {
                currentTransform = transformStack.Pop();
            }
        }
        
        public void Translate(Vector offset)
        {
            var translation = Matrix.Translation3D(offset.X, offset.Y, 0);
            currentTransform = currentTransform * translation;
        }
        
        public void Rotate(double angle)
        {
            var rotation = Matrix.RotationZ3D(angle);
            currentTransform = currentTransform * rotation;
        }
        
        public void Scale(Vector scale)
        {
            var scaling = Matrix.Scale3D(scale.X, scale.Y, 1);
            currentTransform = currentTransform * scaling;
        }
        
        // Clipping methods
        public void PushClip(Rectangle rect)
        {
            clipStack.Push(rect);
            LogOperation($"PushClip({rect.X},{rect.Y},{rect.Width}x{rect.Height})");
        }
        
        public void PopClip()
        {
            if (clipStack.Count > 0)
            {
                clipStack.Pop();
                LogOperation("PopClip()");
            }
        }
    }
    
    /// <summary>
    /// Color representation with RGBA components
    /// </summary>
    public struct Color
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }
        
        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        
        public Color(int rgb) : this((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF)) { }
        
        public Color(int argb, bool hasAlpha) : this(
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF),
            hasAlpha ? (byte)((argb >> 24) & 0xFF) : (byte)255) { }
        
        // Predefined colors
        public static Color Black => new Color(0, 0, 0);
        public static Color White => new Color(255, 255, 255);
        public static Color Red => new Color(255, 0, 0);
        public static Color Green => new Color(0, 255, 0);
        public static Color Blue => new Color(0, 0, 255);
        public static Color Yellow => new Color(255, 255, 0);
        public static Color Cyan => new Color(0, 255, 255);
        public static Color Magenta => new Color(255, 0, 255);
        public static Color Gray => new Color(128, 128, 128);
        public static Color DarkGray => new Color(64, 64, 64);
        public static Color LightGray => new Color(192, 192, 192);
        public static Color Orange => new Color(255, 165, 0);
        public static Color Purple => new Color(128, 0, 128);
        public static Color Brown => new Color(165, 42, 42);
        public static Color Pink => new Color(255, 192, 203);
        public static Color Transparent => new Color(0, 0, 0, 0);
        
        // Material Design colors
        public static Color MaterialRed => new Color(244, 67, 54);
        public static Color MaterialPink => new Color(233, 30, 99);
        public static Color MaterialPurple => new Color(156, 39, 176);
        public static Color MaterialDeepPurple => new Color(103, 58, 183);
        public static Color MaterialIndigo => new Color(63, 81, 181);
        public static Color MaterialBlue => new Color(33, 150, 243);
        public static Color MaterialLightBlue => new Color(3, 169, 244);
        public static Color MaterialCyan => new Color(0, 188, 212);
        public static Color MaterialTeal => new Color(0, 150, 136);
        public static Color MaterialGreen => new Color(76, 175, 80);
        public static Color MaterialLightGreen => new Color(139, 195, 74);
        public static Color MaterialLime => new Color(205, 220, 57);
        public static Color MaterialYellow => new Color(255, 235, 59);
        public static Color MaterialAmber => new Color(255, 193, 7);
        public static Color MaterialOrange => new Color(255, 152, 0);
        public static Color MaterialDeepOrange => new Color(255, 87, 34);
        public static Color MaterialBrown => new Color(121, 85, 72);
        public static Color MaterialGrey => new Color(158, 158, 158);
        public static Color MaterialBlueGrey => new Color(96, 125, 139);
        
        // Color manipulation
        public Color WithAlpha(byte alpha) => new Color(R, G, B, alpha);
        
        public Color Lighten(double amount)
        {
            amount = global::System.Math.Clamp(amount, 0, 1);
            return new Color(
                (byte)(R + (255 - R) * amount),
                (byte)(G + (255 - G) * amount),
                (byte)(B + (255 - B) * amount),
                A
            );
        }
        
        public Color Darken(double amount)
        {
            amount = global::System.Math.Clamp(amount, 0, 1);
            return new Color(
                (byte)(R * (1 - amount)),
                (byte)(G * (1 - amount)),
                (byte)(B * (1 - amount)),
                A
            );
        }
        
        public static Color Lerp(Color a, Color b, double t)
        {
            t = global::System.Math.Clamp(t, 0, 1);
            return new Color(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t),
                (byte)(a.A + (b.A - a.A) * t)
            );
        }
        
        public override string ToString() => $"Color({R}, {G}, {B}, {A})";
    }
    
    /// <summary>
    /// Font representation
    /// </summary>
    public class Font
    {
        public string Family { get; set; }
        public double Size { get; set; }
        public FontStyle Style { get; set; }
        public FontWeight Weight { get; set; }
        
        public Font(string family = "Arial", double size = 12, FontStyle style = FontStyle.Normal, FontWeight weight = FontWeight.Normal)
        {
            Family = family;
            Size = size;
            Style = style;
            Weight = weight;
        }
        
        // Common fonts
        public static Font Default => new Font("Arial", 12);
        public static Font Small => new Font("Arial", 10);
        public static Font Large => new Font("Arial", 16);
        public static Font Bold => new Font("Arial", 12, FontStyle.Normal, FontWeight.Bold);
        public static Font Monospace => new Font("Consolas", 12);
        public static Font Heading1 => new Font("Arial", 32, FontStyle.Normal, FontWeight.Bold);
        public static Font Heading2 => new Font("Arial", 24, FontStyle.Normal, FontWeight.Bold);
        public static Font Heading3 => new Font("Arial", 18, FontStyle.Normal, FontWeight.Bold);
    }
    
    [Flags]
    public enum FontStyle
    {
        Normal = 0,
        Italic = 1,
        Underline = 2,
        Strikethrough = 4
    }
    
    public enum FontWeight
    {
        Thin = 100,
        ExtraLight = 200,
        Light = 300,
        Normal = 400,
        Medium = 500,
        SemiBold = 600,
        Bold = 700,
        ExtraBold = 800,
        Black = 900
    }
    
    /// <summary>
    /// Image representation
    /// </summary>
    public class Image
    {
        public string Source { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] PixelData { get; set; }
        
        public Image(string source)
        {
            Source = source;
            // Load image data from source
        }
        
        public Image(int width, int height)
        {
            Width = width;
            Height = height;
            PixelData = new byte[width * height * 4]; // RGBA
        }
    }
    
    /// <summary>
    /// Path for complex shapes
    /// </summary>
    public class Path
    {
        private List<PathSegment> segments = new List<PathSegment>();
        
        public void MoveTo(Vector point)
        {
            segments.Add(new MoveToSegment(point));
        }
        
        public void LineTo(Vector point)
        {
            segments.Add(new LineToSegment(point));
        }
        
        public void CurveTo(Vector control1, Vector control2, Vector end)
        {
            segments.Add(new CurveToSegment(control1, control2, end));
        }
        
        public void QuadraticCurveTo(Vector control, Vector end)
        {
            segments.Add(new QuadraticCurveToSegment(control, end));
        }
        
        public void ArcTo(Vector center, double radius, double startAngle, double endAngle)
        {
            segments.Add(new ArcToSegment(center, radius, startAngle, endAngle));
        }
        
        public void Close()
        {
            segments.Add(new CloseSegment());
        }
        
        public void ClosePath()
        {
            Close();
        }
        
        internal IEnumerable<PathSegment> Segments => segments;
    }
    
    internal abstract class PathSegment { }
    
    internal class MoveToSegment : PathSegment
    {
        public Vector Point { get; }
        public MoveToSegment(Vector point) => Point = point;
    }
    
    internal class LineToSegment : PathSegment
    {
        public Vector Point { get; }
        public LineToSegment(Vector point) => Point = point;
    }
    
    internal class CurveToSegment : PathSegment
    {
        public Vector Control1 { get; }
        public Vector Control2 { get; }
        public Vector End { get; }
        public CurveToSegment(Vector control1, Vector control2, Vector end)
        {
            Control1 = control1;
            Control2 = control2;
            End = end;
        }
    }
    
    internal class QuadraticCurveToSegment : PathSegment
    {
        public Vector Control { get; }
        public Vector End { get; }
        public QuadraticCurveToSegment(Vector control, Vector end)
        {
            Control = control;
            End = end;
        }
    }
    
    internal class ArcToSegment : PathSegment
    {
        public Vector Center { get; }
        public double Radius { get; }
        public double StartAngle { get; }
        public double EndAngle { get; }
        public ArcToSegment(Vector center, double radius, double startAngle, double endAngle)
        {
            Center = center;
            Radius = radius;
            StartAngle = startAngle;
            EndAngle = endAngle;
        }
    }
    
    internal class CloseSegment : PathSegment { }
    
    /// <summary>
    /// UI Theme definition
    /// </summary>
    public class Theme
    {
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public Color AccentColor { get; set; }
        public Color BorderColor { get; set; }
        public Color SelectionColor { get; set; }
        public Color PlaceholderColor { get; set; }
        public Color TrackColor { get; set; }
        public Font DefaultFont { get; set; }
        
        // Default themes
        public static Theme Default => new Theme
        {
            BackgroundColor = Color.White,
            ForegroundColor = Color.Black,
            AccentColor = Color.MaterialBlue,
            BorderColor = Color.Gray,
            SelectionColor = Color.MaterialBlue.WithAlpha(64),
            PlaceholderColor = Color.Gray,
            TrackColor = Color.LightGray,
            DefaultFont = Font.Default
        };
        
        public static Theme Dark => new Theme
        {
            BackgroundColor = new Color(30, 30, 30),
            ForegroundColor = Color.White,
            AccentColor = Color.MaterialBlue,
            BorderColor = new Color(80, 80, 80),
            SelectionColor = Color.MaterialBlue.WithAlpha(64),
            PlaceholderColor = new Color(150, 150, 150),
            TrackColor = new Color(60, 60, 60),
            DefaultFont = Font.Default
        };
        
        public static Theme Material => new Theme
        {
            BackgroundColor = new Color(250, 250, 250),
            ForegroundColor = new Color(33, 33, 33),
            AccentColor = Color.MaterialIndigo,
            BorderColor = new Color(224, 224, 224),
            SelectionColor = Color.MaterialIndigo.WithAlpha(32),
            PlaceholderColor = new Color(117, 117, 117),
            TrackColor = new Color(189, 189, 189),
            DefaultFont = new Font("Roboto", 14)
        };
    }
    
    /// <summary>
    /// Button style
    /// </summary>
    public class ButtonStyle
    {
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public Color BorderColor { get; set; }
        public Color PressedColor { get; set; }
        public Color HoverColor { get; set; }
        public Color DisabledColor { get; set; }
        public double BorderWidth { get; set; }
        public double CornerRadius { get; set; }
        
        public static ButtonStyle Default => new ButtonStyle
        {
            BackgroundColor = Color.White,
            ForegroundColor = Color.Black,
            BorderColor = Color.Gray,
            PressedColor = Color.LightGray,
            HoverColor = new Color(245, 245, 245),
            DisabledColor = new Color(200, 200, 200),
            BorderWidth = 1,
            CornerRadius = 4
        };
        
        public static ButtonStyle Primary => new ButtonStyle
        {
            BackgroundColor = Color.MaterialBlue,
            ForegroundColor = Color.White,
            BorderColor = Color.MaterialBlue.Darken(0.2),
            PressedColor = Color.MaterialBlue.Darken(0.3),
            HoverColor = Color.MaterialBlue.Lighten(0.1),
            DisabledColor = Color.Gray,
            BorderWidth = 0,
            CornerRadius = 4
        };
        
        public static ButtonStyle Secondary => new ButtonStyle
        {
            BackgroundColor = Color.Transparent,
            ForegroundColor = Color.MaterialBlue,
            BorderColor = Color.MaterialBlue,
            PressedColor = Color.MaterialBlue.WithAlpha(32),
            HoverColor = Color.MaterialBlue.WithAlpha(16),
            DisabledColor = Color.Gray,
            BorderWidth = 2,
            CornerRadius = 4
        };
    }
    
    /// <summary>
    /// CheckBox style
    /// </summary>
    public class CheckBoxStyle
    {
        public Color BackgroundColor { get; set; }
        public Color BorderColor { get; set; }
        public Color CheckColor { get; set; }
        public double BorderWidth { get; set; }
        
        public static CheckBoxStyle Default => new CheckBoxStyle
        {
            BackgroundColor = Color.White,
            BorderColor = Color.Gray,
            CheckColor = Color.MaterialBlue,
            BorderWidth = 2
        };
    }
    
    /// <summary>
    /// ProgressBar style
    /// </summary>
    public class ProgressBarStyle
    {
        public Color BackgroundColor { get; set; }
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }
        public double BorderWidth { get; set; }
        
        public static ProgressBarStyle Default => new ProgressBarStyle
        {
            BackgroundColor = Color.LightGray,
            FillColor = Color.MaterialBlue,
            BorderColor = Color.Gray,
            BorderWidth = 1
        };
    }
    
    /// <summary>
    /// General widget style
    /// </summary>
    public class Style
    {
        private static Theme _default = Theme.Default;
        
        public static Theme Default
        {
            get => _default;
            set => _default = value;
        }
    }
} 