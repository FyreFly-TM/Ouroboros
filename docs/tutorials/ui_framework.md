# Ouroboros UI Framework Tutorial

The Ouroboros UI Framework provides a comprehensive set of widgets and layout managers for building modern user interfaces. This tutorial will guide you through using the various UI components.

## Table of Contents
1. [Getting Started](#getting-started)
2. [Basic Widgets](#basic-widgets)
3. [Advanced Widgets](#advanced-widgets)
4. [Layout Management](#layout-management)
5. [Event Handling](#event-handling)
6. [Themes and Styling](#themes-and-styling)
7. [Dialogs](#dialogs)
8. [Best Practices](#best-practices)

## Getting Started

To use the Ouroboros UI framework, import the UI namespace:

```ouroboros
using Ouroboros.StdLib.UI;
using Ouroboros.StdLib.Math;
```

Create a window and show it:

```ouroboros
var window = new Window("My App", 800, 600);
window.Show();
```

## Basic Widgets

### Button
A clickable button control.

```ouroboros
var button = new Button("Click Me");
button.Position = new Vector(10, 10);
button.Click += (sender, e) => Console.WriteLine("Button clicked!");
window.AddChild(button);
```

Button styles:
- `ButtonStyle.Default` - Standard button
- `ButtonStyle.Primary` - Emphasized button
- `ButtonStyle.Secondary` - Outlined button

### Label
Display text without user interaction.

```ouroboros
var label = new Label("Hello, World!");
label.Position = new Vector(10, 50);
label.Alignment = TextAlignment.Center;
window.AddChild(label);
```

### TextBox
Single-line text input.

```ouroboros
var textBox = new TextBox();
textBox.Placeholder = "Enter your name...";
textBox.Position = new Vector(10, 80);
textBox.Size = new Vector(200, 30);
window.AddChild(textBox);
```

Properties:
- `IsPassword` - Masks input for passwords
- `MaxLength` - Limits character count
- `IsReadOnly` - Prevents editing

### CheckBox
Toggle option with label.

```ouroboros
var checkBox = new CheckBox("Enable notifications");
checkBox.Position = new Vector(10, 120);
checkBox.CheckedChanged += (s, e) => 
    Console.WriteLine($"Checked: {e.IsChecked}");
window.AddChild(checkBox);
```

### RadioButton
Mutually exclusive options within a group.

```ouroboros
var radio1 = new RadioButton("Option A", "group1");
var radio2 = new RadioButton("Option B", "group1");
var radio3 = new RadioButton("Option C", "group1");
radio1.IsChecked = true; // Default selection
```

### ComboBox (Dropdown)
Select from a list of options.

```ouroboros
var comboBox = new ComboBox();
comboBox.Items.Add("Small");
comboBox.Items.Add("Medium");
comboBox.Items.Add("Large");
comboBox.SelectedIndex = 1;
comboBox.SelectionChanged += (s, e) => 
    Console.WriteLine($"Selected: {comboBox.SelectedItem}");
```

### Slider
Numeric value selection via dragging.

```ouroboros
var slider = new Slider();
slider.Minimum = 0;
slider.Maximum = 100;
slider.Value = 50;
slider.Step = 5;
slider.ValueChanged += (s, e) => 
    Console.WriteLine($"Value: {e.NewValue}");
```

### ProgressBar
Display task progress.

```ouroboros
var progressBar = new ProgressBar();
progressBar.Value = 75; // 75%

// Indeterminate progress
var loadingBar = new ProgressBar();
loadingBar.IsIndeterminate = true;
```

## Advanced Widgets

### TabControl
Organize content in tabs.

```ouroboros
var tabs = new TabControl();

var generalTab = new TabPage("General");
generalTab.AddChild(new Label("General settings"));
tabs.AddTab(generalTab);

var advancedTab = new TabPage("Advanced");
advancedTab.AddChild(new Label("Advanced settings"));
tabs.AddTab(advancedTab);

tabs.TabChanged += (s, e) => 
    Console.WriteLine($"Tab changed to index {e.NewIndex}");
```

### ListBox
Scrollable list of items.

```ouroboros
var listBox = new ListBox();
listBox.SelectionMode = SelectionMode.Multiple;

for (int i = 0; i < 100; i++)
{
    listBox.AddItem($"Item {i}");
}

listBox.ItemDoubleClick += (s, e) => 
    Console.WriteLine($"Double-clicked: {e.Item}");
```

### TreeView
Hierarchical data display.

```ouroboros
var treeView = new TreeView();
var root = treeView.RootNode;

var folder = root.AddChild("Documents");
folder.AddChild("Report.docx");
folder.AddChild("Presentation.pptx");
folder.IsExpanded = true;

treeView.SelectionChanged += (s, e) => 
    Console.WriteLine($"Selected: {e.NewNode?.Text}");
```

### MenuBar
Application menu system.

```ouroboros
var menuBar = new MenuBar();

var fileMenu = menuBar.AddMenu("File");
fileMenu.AddItem("New", (s, e) => NewFile());
fileMenu.AddItem("Open...", (s, e) => OpenFile());
fileMenu.AddSeparator();
fileMenu.AddItem("Exit", (s, e) => Application.Exit());

var editMenu = menuBar.AddMenu("Edit");
editMenu.AddItem("Cut", (s, e) => Cut());
editMenu.AddItem("Copy", (s, e) => Copy());
editMenu.AddItem("Paste", (s, e) => Paste());
```

### ToolBar
Quick access buttons.

```ouroboros
var toolBar = new ToolBar();

var newButton = toolBar.AddButton("New", new Image("new.png"));
newButton.ToolTip = "Create new document";

toolBar.AddSeparator();

var boldButton = toolBar.AddButton("B");
boldButton.IsToggle = true;
boldButton.Click += (s, e) => ToggleBold();
```

### StatusBar
Application status display.

```ouroboros
var statusBar = new StatusBar();
statusBar.AddPanel("Ready", 200);
statusBar.AddPanel("Line 1, Col 1", 150);
statusBar.AddPanel("UTF-8", 100);
```

### NumericUpDown
Numeric input with increment/decrement.

```ouroboros
var numeric = new NumericUpDown();
numeric.Minimum = 0;
numeric.Maximum = 100;
numeric.Step = 5;
numeric.DecimalPlaces = 2;
numeric.Value = 50.00;
```

### DatePicker
Date selection with calendar.

```ouroboros
var datePicker = new DatePicker();
datePicker.DateFormat = "yyyy-MM-dd";
datePicker.SelectedDate = DateTime.Now;
datePicker.DateChanged += (s, e) => 
    Console.WriteLine($"Date: {e.NewDate}");
```

### ColorPicker
Color selection control.

```ouroboros
var colorPicker = new ColorPicker();
colorPicker.SelectedColor = Color.MaterialBlue;
colorPicker.ColorChanged += (s, e) => 
    Console.WriteLine($"Color: {e.NewColor}");
```

## Layout Management

### FlowLayout
Arranges widgets in a flowing manner.

```ouroboros
var panel = new Widget();
var flowLayout = new FlowLayout();
flowLayout.Direction = FlowDirection.LeftToRight;
flowLayout.Spacing = 10;
flowLayout.WrapContent = true;
panel.SetLayout(flowLayout);

// Add widgets - they'll flow automatically
for (int i = 0; i < 10; i++)
{
    panel.AddChild(new Button($"Button {i}"));
}
```

### GridLayout
Arranges widgets in a grid.

```ouroboros
var grid = new GridLayout();
grid.Columns = 3;
grid.ColumnSpacing = 10;
grid.RowSpacing = 10;

var widget1 = new Label("Cell 0,0");
panel.AddChild(widget1);
grid.SetPosition(widget1, 0, 0, columnSpan: 2);

var widget2 = new Label("Cell 2,0");
panel.AddChild(widget2);
grid.SetPosition(widget2, 2, 0);
```

### StackLayout
Stacks widgets vertically or horizontally.

```ouroboros
var stackLayout = new StackLayout();
stackLayout.Orientation = Orientation.Vertical;
stackLayout.Spacing = 5;
stackLayout.HorizontalAlignment = Alignment.Stretch;

// All widgets will stretch horizontally
panel.AddChild(new Button("Button 1"));
panel.AddChild(new Button("Button 2"));
panel.AddChild(new TextBox());
```

### DockLayout
Docks widgets to container edges.

```ouroboros
var dockLayout = new DockLayout();

var menuBar = new MenuBar();
dockLayout.Dock(menuBar, DockPosition.Top);

var toolBar = new ToolBar();
dockLayout.Dock(toolBar, DockPosition.Top);

var statusBar = new StatusBar();
dockLayout.Dock(statusBar, DockPosition.Bottom);

var sidePanel = new Widget();
dockLayout.Dock(sidePanel, DockPosition.Left);

var mainContent = new Widget();
dockLayout.Dock(mainContent, DockPosition.Fill);
```

### AbsoluteLayout
Manual positioning (default).

```ouroboros
var widget = new Button("Absolute");
widget.Position = new Vector(100, 50);
widget.Size = new Vector(100, 30);
panel.AddChild(widget);
```

## Event Handling

All widgets support common events:

```ouroboros
widget.Click += (sender, e) => HandleClick(e);
widget.MouseEnter += (sender, e) => HandleMouseEnter(e);
widget.MouseLeave += (sender, e) => HandleMouseLeave(e);
widget.KeyDown += (sender, e) => HandleKeyDown(e);
widget.GotFocus += (sender, e) => HandleFocus(e);
widget.LostFocus += (sender, e) => HandleBlur(e);
```

Custom event arguments provide context:

```ouroboros
button.Click += (sender, e) =>
{
    var mousePos = e.Position;
    var clickCount = e.ClickCount;
    // Handle click
};

textBox.KeyDown += (sender, e) =>
{
    if (e.Key == Key.Enter)
    {
        SubmitForm();
    }
};
```

## Themes and Styling

Apply predefined themes:

```ouroboros
window.Theme = Theme.Default;  // Light theme
window.Theme = Theme.Dark;     // Dark theme
window.Theme = Theme.Material; // Material Design theme
```

Customize individual widgets:

```ouroboros
widget.BackgroundColor = Color.MaterialBlue;
widget.ForegroundColor = Color.White;
widget.Font = new Font("Arial", 14, FontStyle.Bold);
widget.Margin = new Margin(10);
widget.Padding = new Padding(5, 10); // horizontal, vertical
```

Create custom themes:

```ouroboros
var customTheme = new Theme
{
    BackgroundColor = new Color(250, 250, 250),
    ForegroundColor = new Color(33, 33, 33),
    AccentColor = Color.MaterialPurple,
    BorderColor = new Color(200, 200, 200),
    DefaultFont = new Font("Segoe UI", 12)
};
window.Theme = customTheme;
```

## Dialogs

### MessageBox
Simple message dialogs.

```ouroboros
// Information
MessageBox.Show(window, "Operation completed successfully!", 
    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

// Confirmation
var result = MessageBox.Show(window, "Are you sure?", 
    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
if (result == DialogResult.Yes)
{
    // Proceed
}

// Error
MessageBox.Show(window, "An error occurred!", 
    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
```

### InputDialog
Get user input.

```ouroboros
var name = InputDialog.Show(window, "Enter your name:", 
    "Name Required", "John Doe");
if (name != null)
{
    Console.WriteLine($"Hello, {name}!");
}
```

### Custom Dialogs
Create specialized dialogs.

```ouroboros
public class SettingsDialog : Dialog
{
    private TextBox nameBox;
    private CheckBox enableNotifications;
    
    public SettingsDialog(Window parent) : base(parent, "Settings")
    {
        Size = new Vector(400, 300);
        
        // Add controls
        nameBox = new TextBox();
        enableNotifications = new CheckBox("Enable notifications");
        
        // Add buttons
        AddButton("OK", DialogResult.OK);
        AddButton("Cancel", DialogResult.Cancel);
    }
    
    protected override void RenderContent(GraphicsContext context, 
        Rectangle contentArea)
    {
        // Position and render controls
        nameBox.Position = contentArea.Position;
        nameBox.Render(context);
        
        enableNotifications.Position = contentArea.Position + 
            new Vector(0, 40);
        enableNotifications.Render(context);
    }
}
```

## Best Practices

### 1. Responsive Design
Use layouts instead of absolute positioning:

```ouroboros
// Good - adapts to window size
var layout = new DockLayout();
panel.SetLayout(layout);

// Avoid - fixed positions
widget.Position = new Vector(100, 50);
```

### 2. Event Handler Cleanup
Remove event handlers to prevent memory leaks:

```ouroboros
// Attach
button.Click += OnButtonClick;

// Detach when no longer needed
button.Click -= OnButtonClick;
```

### 3. Async Operations
Keep UI responsive during long operations:

```ouroboros
@async
private async void LoadData()
{
    progressBar.IsIndeterminate = true;
    
    var data = await FetchDataAsync();
    
    progressBar.IsIndeterminate = false;
    DisplayData(data);
}
```

### 4. Input Validation
Validate user input:

```ouroboros
textBox.KeyDown += (s, e) =>
{
    // Allow only numbers
    if (!char.IsDigit(e.Character) && e.Key != Key.Backspace)
    {
        e.Handled = true; // Cancel the key press
    }
};
```

### 5. Accessibility
Provide tooltips and keyboard navigation:

```ouroboros
button.ToolTip = "Save the current document (Ctrl+S)";
button.TabIndex = 1; // Tab order

// Handle keyboard shortcuts
window.KeyDown += (s, e) =>
{
    if (e.Modifiers == KeyModifiers.Control && e.Key == Key.S)
    {
        SaveDocument();
    }
};
```

### 6. Resource Management
Dispose of resources properly:

```ouroboros
// Images
var icon = new Image("icon.png");
button.Icon = icon;
// ... later ...
icon.Dispose();

// Windows
var dialog = new CustomDialog(window);
dialog.ShowDialog();
dialog.Dispose();
```

## Example Application

Here's a complete example combining various UI elements:

```ouroboros
using Ouroboros.StdLib.UI;
using Ouroboros.StdLib.Math;

class TextEditor
{
    private Window window;
    private TextBox textArea;
    private StatusBar statusBar;
    private string currentFile;
    
    public void Run()
    {
        // Create main window
        window = new Window("Text Editor", 800, 600);
        window.Theme = Theme.Material;
        
        // Create menu
        CreateMenu();
        
        // Create toolbar
        CreateToolBar();
        
        // Create text area
        textArea = new TextBox();
        textArea.Position = new Vector(0, 55);
        textArea.Size = new Vector(800, 520);
        window.AddChild(textArea);
        
        // Create status bar
        CreateStatusBar();
        
        // Show window
        window.Show();
    }
    
    private void CreateMenu()
    {
        var menuBar = new MenuBar();
        
        var fileMenu = menuBar.AddMenu("File");
        fileMenu.AddItem("New", (s, e) => NewFile());
        fileMenu.AddItem("Open...", (s, e) => OpenFile());
        fileMenu.AddItem("Save", (s, e) => SaveFile());
        fileMenu.AddSeparator();
        fileMenu.AddItem("Exit", (s, e) => window.Close());
        
        window.AddChild(menuBar);
    }
    
    private void CreateToolBar()
    {
        var toolBar = new ToolBar();
        toolBar.Position = new Vector(0, 25);
        
        toolBar.AddButton("New", new Image("new.png"))
            .Click += (s, e) => NewFile();
        toolBar.AddButton("Open", new Image("open.png"))
            .Click += (s, e) => OpenFile();
        toolBar.AddButton("Save", new Image("save.png"))
            .Click += (s, e) => SaveFile();
            
        window.AddChild(toolBar);
    }
    
    private void CreateStatusBar()
    {
        statusBar = new StatusBar();
        statusBar.Position = new Vector(0, 575);
        statusBar.AddPanel("Ready", 200);
        statusBar.AddPanel("Line 1, Col 1", 150);
        
        window.AddChild(statusBar);
    }
    
    private void NewFile()
    {
        if (textArea.Text.Length > 0)
        {
            var result = MessageBox.Show(window,
                "Save current file?", "New File",
                MessageBoxButtons.YesNoCancel);
                
            if (result == DialogResult.Cancel) return;
            if (result == DialogResult.Yes) SaveFile();
        }
        
        textArea.Text = "";
        currentFile = null;
        UpdateStatus("New file created");
    }
    
    private void OpenFile()
    {
        // In real app, use file dialog
        var filename = InputDialog.Show(window,
            "Enter filename:", "Open File");
            
        if (filename != null)
        {
            // Load file content
            textArea.Text = File.ReadAllText(filename);
            currentFile = filename;
            UpdateStatus($"Opened {filename}");
        }
    }
    
    private void SaveFile()
    {
        if (currentFile == null)
        {
            currentFile = InputDialog.Show(window,
                "Enter filename:", "Save As");
        }
        
        if (currentFile != null)
        {
            File.WriteAllText(currentFile, textArea.Text);
            UpdateStatus($"Saved {currentFile}");
        }
    }
    
    private void UpdateStatus(string message)
    {
        statusBar.Panels[0].Text = message;
    }
}
```

## Conclusion

The Ouroboros UI Framework provides a complete set of tools for building modern user interfaces. By combining widgets, layouts, themes, and event handling, you can create professional applications with rich user experiences.

For more examples, see the `examples/UIDemo.ouro` file in the Ouroboros repository. 