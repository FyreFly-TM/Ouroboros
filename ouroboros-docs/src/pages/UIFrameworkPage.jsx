import CodeBlock from '../components/CodeBlock'
import Callout from '../components/Callout'

export default function UIFrameworkPage() {
  return (
    <div className="max-w-4xl">
      <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
        UI Framework
      </h1>
      <p className="text-xl text-gray-600 dark:text-gray-300 mb-8">
        Build beautiful desktop applications with Ouroboros's comprehensive UI toolkit. From simple windows to complex 
        controls, create modern, responsive interfaces with ease.
      </p>

      <Callout type="info" title="Getting Started">
        Import the UI module with <code className="bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded">using Ouroboros.StdLib.UI;</code> 
        to access all UI components and features.
      </Callout>

      {/* Windows */}
      <section className="mb-12">
        <h2 id="window" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Windows
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          The foundation of any desktop application. Create and configure windows with various properties.
        </p>
        <CodeBlock
          code={`// Basic window creation
var window = new Window("My Application", 800, 600);
window.Show();

// Window with properties
var mainWindow = new Window("Ouroboros App", 1024, 768)
{
    Theme = Theme.Material,
    Resizable = true,
    Maximizable = true,
    Minimizable = true,
    CenterOnScreen = true,
    Icon = new Icon("app-icon.png")
};

// Window events
mainWindow.Closing += (sender, e) =>
{
    if (HasUnsavedChanges())
    {
        var result = MessageBox.Show(
            "You have unsaved changes. Save before closing?",
            "Confirm Exit",
            MessageBoxButtons.YesNoCancel
        );
        
        if (result == DialogResult.Cancel)
        {
            e.Cancel = true;
        }
        else if (result == DialogResult.Yes)
        {
            SaveChanges();
        }
    }
};

mainWindow.Resized += (sender, e) =>
{
    Console.WriteLine($"Window resized to {e.Width}x{e.Height}");
};

// Window state management
mainWindow.WindowState = WindowState.Maximized;
mainWindow.Opacity = 0.95;  // Slightly transparent
mainWindow.TopMost = true;   // Always on top

// Modal dialogs
var dialog = new Window("Settings", 400, 300)
{
    Modal = true,
    Owner = mainWindow,
    ShowInTaskbar = false,
    StartPosition = WindowStartPosition.CenterOwner
};

// Multiple windows
var secondaryWindow = new Window("Tools", 300, 400);
secondaryWindow.Position = new Vector(
    mainWindow.Position.X + mainWindow.Width + 10,
    mainWindow.Position.Y
);`}
        />
      </section>

      {/* Menu System */}
      <section className="mb-12">
        <h2 id="menubar" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          MenuBar and Menus
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Create traditional application menus with shortcuts and icons.
        </p>
        <CodeBlock
          code={`// Create menu bar
var menuBar = new MenuBar();

// File menu
var fileMenu = menuBar.AddMenu("File");
fileMenu.AddItem("New", () => NewDocument())
    .WithShortcut(Keys.Control | Keys.N)
    .WithIcon("new-icon.png");

fileMenu.AddItem("Open...", () => OpenDocument())
    .WithShortcut(Keys.Control | Keys.O)
    .WithIcon("open-icon.png");

fileMenu.AddItem("Save", () => SaveDocument())
    .WithShortcut(Keys.Control | Keys.S)
    .WithIcon("save-icon.png")
    .Enabled = false;  // Disabled until document is opened

fileMenu.AddSeparator();

// Recent files submenu
var recentMenu = fileMenu.AddSubMenu("Recent Files");
foreach (var file in GetRecentFiles())
{
    recentMenu.AddItem(file.Name, () => OpenFile(file.Path));
}

fileMenu.AddSeparator();

fileMenu.AddItem("Exit", () => Application.Exit())
    .WithShortcut(Keys.Alt | Keys.F4);

// Edit menu
var editMenu = menuBar.AddMenu("Edit");
editMenu.AddItem("Undo", () => Undo())
    .WithShortcut(Keys.Control | Keys.Z);

editMenu.AddItem("Redo", () => Redo())
    .WithShortcut(Keys.Control | Keys.Y);

editMenu.AddSeparator();

editMenu.AddItem("Cut", () => Cut())
    .WithShortcut(Keys.Control | Keys.X);
    
editMenu.AddItem("Copy", () => Copy())
    .WithShortcut(Keys.Control | Keys.C);
    
editMenu.AddItem("Paste", () => Paste())
    .WithShortcut(Keys.Control | Keys.V);

// View menu with checkable items
var viewMenu = menuBar.AddMenu("View");
var toolbarItem = viewMenu.AddItem("Show Toolbar", () => ToggleToolbar())
    .AsCheckable();
toolbarItem.Checked = true;

var statusBarItem = viewMenu.AddItem("Show Status Bar", () => ToggleStatusBar())
    .AsCheckable();
statusBarItem.Checked = true;

viewMenu.AddSeparator();

// Theme submenu with radio items
var themeMenu = viewMenu.AddSubMenu("Theme");
var themeGroup = new MenuItemGroup();

themeMenu.AddItem("Light", () => SetTheme(Theme.Light))
    .InGroup(themeGroup);
    
themeMenu.AddItem("Dark", () => SetTheme(Theme.Dark))
    .InGroup(themeGroup)
    .Checked = true;
    
themeMenu.AddItem("Material", () => SetTheme(Theme.Material))
    .InGroup(themeGroup);

// Context menus
var contextMenu = new ContextMenu();
contextMenu.AddItem("Cut", () => Cut());
contextMenu.AddItem("Copy", () => Copy());
contextMenu.AddItem("Paste", () => Paste());
contextMenu.AddSeparator();
contextMenu.AddItem("Select All", () => SelectAll());

// Attach to control
textBox.ContextMenu = contextMenu;

window.AddChild(menuBar);`}
        />
      </section>

      {/* Toolbars */}
      <section className="mb-12">
        <h2 id="toolbar" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          ToolBar
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Quick access toolbars with buttons, separators, and dropdowns.
        </p>
        <CodeBlock
          code={`// Create toolbar
var toolBar = new ToolBar()
{
    Position = new Vector(0, menuBar.Height),
    Height = 40
};

// Add buttons
var newButton = toolBar.AddButton("New", () => NewDocument())
    .WithIcon("new-24.png")
    .WithToolTip("Create new document (Ctrl+N)");

var openButton = toolBar.AddButton("Open", () => OpenDocument())
    .WithIcon("open-24.png")
    .WithToolTip("Open existing document (Ctrl+O)");

var saveButton = toolBar.AddButton("Save", () => SaveDocument())
    .WithIcon("save-24.png")
    .WithToolTip("Save current document (Ctrl+S)")
    .Enabled = false;

toolBar.AddSeparator();

// Toggle buttons
var boldButton = toolBar.AddButton("B", () => ToggleBold())
    .AsToggle()
    .WithToolTip("Bold (Ctrl+B)")
    .WithFont(new Font("Arial", 12, FontStyle.Bold));

var italicButton = toolBar.AddButton("I", () => ToggleItalic())
    .AsToggle()
    .WithToolTip("Italic (Ctrl+I)")
    .WithFont(new Font("Arial", 12, FontStyle.Italic));

var underlineButton = toolBar.AddButton("U", () => ToggleUnderline())
    .AsToggle()
    .WithToolTip("Underline (Ctrl+U)")
    .WithFont(new Font("Arial", 12, FontStyle.Underline));

toolBar.AddSeparator();

// Dropdown button
var fontDropdown = toolBar.AddDropdown()
    .WithItems(new[] { "Arial", "Times New Roman", "Courier New", "Verdana" })
    .WithSelectedItem("Arial")
    .OnSelectionChanged((font) => SetFont(font));

var sizeDropdown = toolBar.AddDropdown()
    .WithItems(new[] { "8", "9", "10", "11", "12", "14", "16", "18", "20", "24", "28", "32" })
    .WithSelectedItem("12")
    .OnSelectionChanged((size) => SetFontSize(int.Parse(size)));

// Custom toolbar controls
var searchBox = new TextBox()
{
    Width = 200,
    PlaceholderText = "Search..."
};
toolBar.AddCustomControl(searchBox);

var searchButton = toolBar.AddButton("", () => Search(searchBox.Text))
    .WithIcon("search-16.png");

// Toolbar customization
toolBar.AllowCustomization = true;
toolBar.ShowText = ToolBarTextDisplay.TextBesideIcon;
toolBar.ButtonSize = new Size(32, 32);

// Multiple toolbars
var formatToolBar = new ToolBar()
{
    Position = new Vector(0, menuBar.Height + toolBar.Height),
    Visible = true
};

// Floating toolbar
var floatingTools = new ToolBar()
{
    Floating = true,
    Position = new Vector(100, 100),
    Draggable = true
};

window.AddChild(toolBar);
window.AddChild(formatToolBar);`}
        />
      </section>

      {/* Tab Controls */}
      <section className="mb-12">
        <h2 id="tabs" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          TabControl and TabPages
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Organize content into tabs for better space utilization.
        </p>
        <CodeBlock
          code={`// Create tab control
var tabControl = new TabControl()
{
    Position = new Vector(0, 100),
    Size = new Vector(800, 500),
    TabPosition = TabPosition.Top
};

// Add tabs
var generalTab = new TabPage("General");
generalTab.AddChild(new Label("General Settings") 
{ 
    Position = new Vector(10, 10),
    Font = new Font("Arial", 14, FontStyle.Bold)
});

// Build form in tab
var nameLabel = new Label("Name:") { Position = new Vector(10, 50) };
var nameBox = new TextBox() { Position = new Vector(100, 50), Width = 200 };
generalTab.AddChild(nameLabel);
generalTab.AddChild(nameBox);

var emailLabel = new Label("Email:") { Position = new Vector(10, 80) };
var emailBox = new TextBox() { Position = new Vector(100, 80), Width = 200 };
generalTab.AddChild(emailLabel);
generalTab.AddChild(emailBox);

tabControl.AddTab(generalTab);

// Advanced tab with icon
var advancedTab = new TabPage("Advanced")
{
    Icon = new Icon("settings-icon.png")
};

// Appearance tab
var appearanceTab = new TabPage("Appearance");
var themeLabel = new Label("Theme:") { Position = new Vector(10, 10) };
var themeCombo = new ComboBox() 
{ 
    Position = new Vector(100, 10),
    Width = 150,
    Items = { "Light", "Dark", "Material", "High Contrast" }
};
appearanceTab.AddChild(themeLabel);
appearanceTab.AddChild(themeCombo);

tabControl.AddTab(advancedTab);
tabControl.AddTab(appearanceTab);

// Tab events
tabControl.SelectedIndexChanged += (sender, e) =>
{
    Console.WriteLine($"Selected tab: {tabControl.SelectedTab.Text}");
};

// Closeable tabs
tabControl.AllowTabClosing = true;
tabControl.TabClosing += (sender, e) =>
{
    if (e.Tab.HasUnsavedChanges)
    {
        e.Cancel = !ConfirmClose(e.Tab);
    }
};

// Tab with custom header
var customTab = new TabPage("Custom")
{
    HeaderTemplate = new TabHeader
    {
        Icon = new Icon("custom-icon.png"),
        CloseButton = true,
        BackgroundColor = Color.Blue,
        TextColor = Color.White
    }
};

// Scrollable tabs
tabControl.TabScrolling = TabScrolling.Buttons;  // or TabScrolling.MouseWheel

window.AddChild(tabControl);`}
        />
      </section>

      {/* Basic Controls */}
      <section className="mb-12">
        <h2 id="button" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Basic Controls
        </h2>
        
        <h3 className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4">
          Button
        </h3>
        <CodeBlock
          code={`// Basic button
var button = new Button("Click Me!")
{
    Position = new Vector(10, 40),
    Size = new Vector(100, 30)
};

button.Click += (sender, e) =>
{
    Console.WriteLine("Button clicked!");
};

// Button with icon
var saveButton = new Button("Save")
{
    Icon = new Icon("save-icon.png"),
    IconPosition = IconPosition.Left,
    Position = new Vector(120, 40)
};

// Button styles
var primaryButton = new Button("Primary")
{
    Style = ButtonStyle.Primary,
    Position = new Vector(10, 80)
};

var dangerButton = new Button("Delete")
{
    Style = ButtonStyle.Danger,
    Position = new Vector(120, 80)
};

// Default button (responds to Enter key)
var okButton = new Button("OK")
{
    IsDefault = true,
    Position = new Vector(10, 120)
};

// Button with access key (Alt+S)
var submitButton = new Button("&Submit")
{
    Position = new Vector(120, 120)
};`}
        />

        <h3 id="textbox" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          TextBox
        </h3>
        <CodeBlock
          code={`// Basic text box
var textBox = new TextBox()
{
    Position = new Vector(10, 10),
    Width = 200,
    PlaceholderText = "Enter text here..."
};

// Password box
var passwordBox = new TextBox()
{
    Position = new Vector(10, 40),
    Width = 200,
    PasswordChar = '•',
    MaxLength = 20
};

// Multi-line text box
var textArea = new TextBox()
{
    Position = new Vector(10, 70),
    Size = new Vector(300, 100),
    Multiline = true,
    WordWrap = true,
    ScrollBars = ScrollBars.Vertical
};

// Text box with validation
var emailBox = new TextBox()
{
    Position = new Vector(10, 180),
    Width = 250
};

emailBox.Validating += (sender, e) =>
{
    if (!IsValidEmail(emailBox.Text))
    {
        e.Cancel = true;
        emailBox.BackgroundColor = Color.LightPink;
        ShowTooltip("Please enter a valid email address");
    }
    else
    {
        emailBox.BackgroundColor = Color.White;
    }
};

// Auto-complete text box
var cityBox = new TextBox()
{
    Position = new Vector(10, 210),
    Width = 200,
    AutoCompleteMode = AutoCompleteMode.Suggest,
    AutoCompleteSource = AutoCompleteSource.CustomSource,
    AutoCompleteCustomSource = GetCityNames()
};

// Read-only text box
var readOnlyBox = new TextBox()
{
    Position = new Vector(10, 240),
    Width = 200,
    Text = "This is read-only",
    ReadOnly = true,
    BackgroundColor = Color.LightGray
};`}
        />

        <h3 id="checkbox" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          CheckBox
        </h3>
        <CodeBlock
          code={`// Basic checkbox
var checkBox = new CheckBox("Enable notifications")
{
    Position = new Vector(10, 10),
    Checked = true
};

checkBox.CheckedChanged += (sender, e) =>
{
    Console.WriteLine($"Notifications: {checkBox.Checked}");
};

// Three-state checkbox
var triStateCheck = new CheckBox("Select All")
{
    Position = new Vector(10, 40),
    ThreeState = true,
    CheckState = CheckState.Indeterminate
};

// Checkbox with custom appearance
var customCheck = new CheckBox("Custom Style")
{
    Position = new Vector(10, 70),
    CheckAlign = ContentAlignment.MiddleRight,
    Font = new Font("Arial", 10, FontStyle.Bold),
    ForeColor = Color.Blue
};`}
        />

        <h3 id="radio" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          RadioButton
        </h3>
        <CodeBlock
          code={`// Radio button group
var genderGroup = new Panel()
{
    Position = new Vector(10, 10),
    Size = new Vector(200, 100),
    BorderStyle = BorderStyle.FixedSingle
};

var maleRadio = new RadioButton("Male")
{
    Position = new Vector(10, 10),
    Checked = true
};

var femaleRadio = new RadioButton("Female")
{
    Position = new Vector(10, 35)
};

var otherRadio = new RadioButton("Other")
{
    Position = new Vector(10, 60)
};

genderGroup.AddChild(maleRadio);
genderGroup.AddChild(femaleRadio);
genderGroup.AddChild(otherRadio);

// Multiple radio groups
var sizeGroup = "size";
var smallRadio = new RadioButton("Small", sizeGroup) { Position = new Vector(220, 10) };
var mediumRadio = new RadioButton("Medium", sizeGroup) { Position = new Vector(220, 35), Checked = true };
var largeRadio = new RadioButton("Large", sizeGroup) { Position = new Vector(220, 60) };`}
        />
      </section>

      {/* Advanced Controls */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Advanced Controls
        </h2>

        <h3 id="slider" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4">
          Slider
        </h3>
        <CodeBlock
          code={`// Basic slider
var slider = new Slider()
{
    Position = new Vector(10, 10),
    Size = new Vector(200, 30),
    Minimum = 0,
    Maximum = 100,
    Value = 50
};

slider.ValueChanged += (sender, e) =>
{
    Console.WriteLine($"Slider value: {e.NewValue}");
};

// Slider with ticks
var volumeSlider = new Slider()
{
    Position = new Vector(10, 50),
    Size = new Vector(200, 40),
    Minimum = 0,
    Maximum = 100,
    TickFrequency = 10,
    ShowTicks = true,
    ShowLabels = true
};

// Vertical slider
var verticalSlider = new Slider()
{
    Position = new Vector(250, 10),
    Size = new Vector(40, 200),
    Orientation = Orientation.Vertical,
    Minimum = 0,
    Maximum = 255,
    Value = 128
};

// Range slider (two handles)
var rangeSlider = new RangeSlider()
{
    Position = new Vector(10, 100),
    Size = new Vector(200, 30),
    Minimum = 0,
    Maximum = 100,
    LowerValue = 20,
    UpperValue = 80
};`}
        />

        <h3 id="combobox" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          ComboBox
        </h3>
        <CodeBlock
          code={`// Basic combo box
var comboBox = new ComboBox()
{
    Position = new Vector(10, 10),
    Width = 200,
    Items = { "Option 1", "Option 2", "Option 3" },
    SelectedIndex = 0
};

comboBox.SelectedIndexChanged += (sender, e) =>
{
    Console.WriteLine($"Selected: {comboBox.SelectedItem}");
};

// Editable combo box
var editableCombo = new ComboBox()
{
    Position = new Vector(10, 40),
    Width = 200,
    DropDownStyle = ComboBoxStyle.DropDown,
    Items = { "Red", "Green", "Blue" }
};

// Combo box with objects
class Country
{
    public string Name { get; set; }
    public string Code { get; set; }
    public override string ToString() => Name;
}

var countryCombo = new ComboBox()
{
    Position = new Vector(10, 70),
    Width = 200,
    DisplayMember = "Name",
    ValueMember = "Code"
};

countryCombo.Items.Add(new Country { Name = "United States", Code = "US" });
countryCombo.Items.Add(new Country { Name = "Canada", Code = "CA" });
countryCombo.Items.Add(new Country { Name = "Mexico", Code = "MX" });

// Auto-complete combo
var autoCombo = new ComboBox()
{
    Position = new Vector(10, 100),
    Width = 200,
    AutoCompleteMode = AutoCompleteMode.SuggestAppend,
    AutoCompleteSource = AutoCompleteSource.ListItems
};`}
        />

        <h3 id="datepicker" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          DatePicker
        </h3>
        <CodeBlock
          code={`// Basic date picker
var datePicker = new DatePicker()
{
    Position = new Vector(10, 10),
    Value = DateTime.Today,
    Format = DateTimePickerFormat.Short
};

datePicker.ValueChanged += (sender, e) =>
{
    Console.WriteLine($"Selected date: {datePicker.Value:yyyy-MM-dd}");
};

// Date range picker
var startDate = new DatePicker()
{
    Position = new Vector(10, 40),
    MaxDate = DateTime.Today
};

var endDate = new DatePicker()
{
    Position = new Vector(150, 40),
    MinDate = startDate.Value
};

startDate.ValueChanged += (s, e) =>
{
    endDate.MinDate = startDate.Value;
};

// Time picker
var timePicker = new DatePicker()
{
    Position = new Vector(10, 70),
    Format = DateTimePickerFormat.Time,
    ShowUpDown = true
};

// Custom format
var customPicker = new DatePicker()
{
    Position = new Vector(10, 100),
    Format = DateTimePickerFormat.Custom,
    CustomFormat = "MMMM dd, yyyy - HH:mm"
};

// Calendar control
var calendar = new MonthCalendar()
{
    Position = new Vector(10, 130),
    ShowToday = true,
    ShowWeekNumbers = true,
    MaxSelectionCount = 7
};

calendar.DateSelected += (s, e) =>
{
    Console.WriteLine($"Selected: {e.Start} to {e.End}");
};`}
        />

        <h3 id="colorpicker" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          ColorPicker
        </h3>
        <CodeBlock
          code={`// Basic color picker
var colorPicker = new ColorPicker()
{
    Position = new Vector(10, 10),
    Color = Color.Blue
};

colorPicker.ColorChanged += (sender, e) =>
{
    Console.WriteLine($"Selected color: {e.NewColor}");
    preview.BackgroundColor = e.NewColor;
};

// Color picker with custom palette
var customPicker = new ColorPicker()
{
    Position = new Vector(10, 40),
    ShowStandardColors = true,
    ShowCustomColors = true,
    CustomColors = new[]
    {
        Color.FromArgb(255, 128, 0),   // Orange
        Color.FromArgb(128, 0, 255),   // Purple
        Color.FromArgb(0, 255, 128)    // Mint
    }
};

// Inline color picker (button style)
var colorButton = new ColorPickerButton()
{
    Position = new Vector(10, 70),
    Size = new Vector(100, 30),
    Color = Color.Red,
    Text = "Choose Color"
};

// Advanced color dialog
var advancedPicker = new ColorPicker()
{
    Position = new Vector(10, 110),
    AllowFullOpen = true,
    ShowHelp = true,
    SolidColorOnly = false  // Allow transparent colors
};`}
        />
      </section>

      {/* Layout */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Layout Management
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Organize controls with flexible layout systems.
        </p>
        <CodeBlock
          code={`// Flow layout
var flowPanel = new FlowLayoutPanel()
{
    Position = new Vector(10, 10),
    Size = new Vector(300, 200),
    FlowDirection = FlowDirection.LeftToRight,
    WrapContents = true,
    AutoScroll = true
};

// Add buttons that automatically flow
for (int i = 1; i <= 10; i++)
{
    flowPanel.AddChild(new Button($"Button {i}") 
    { 
        Width = 80, 
        Height = 30,
        Margin = new Padding(5)
    });
}

// Table layout
var tablePanel = new TableLayoutPanel()
{
    Position = new Vector(320, 10),
    Size = new Vector(300, 200),
    ColumnCount = 3,
    RowCount = 3
};

// Configure columns and rows
tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

// Add controls to specific cells
tablePanel.Controls.Add(new Label("Name:"), 0, 0);
tablePanel.Controls.Add(new TextBox(), 1, 0);
tablePanel.SetColumnSpan(tablePanel.Controls[1], 2);

// Dock layout
var dockPanel = new Panel()
{
    Dock = DockStyle.Fill
};

var topPanel = new Panel()
{
    Dock = DockStyle.Top,
    Height = 50,
    BackgroundColor = Color.LightBlue
};

var leftPanel = new Panel()
{
    Dock = DockStyle.Left,
    Width = 200,
    BackgroundColor = Color.LightGreen
};

var centerPanel = new Panel()
{
    Dock = DockStyle.Fill,
    BackgroundColor = Color.White
};

// Splitter containers
var splitter = new SplitContainer()
{
    Position = new Vector(10, 220),
    Size = new Vector(600, 300),
    Orientation = Orientation.Vertical,
    SplitterDistance = 200
};

splitter.Panel1.AddChild(new TreeView() { Dock = DockStyle.Fill });
splitter.Panel2.AddChild(new RichTextBox() { Dock = DockStyle.Fill });

// Anchor and auto-size
var resizableButton = new Button("Resizable")
{
    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
    AutoSize = true
};

// Grid layout (custom)
var grid = new GridPanel()
{
    Position = new Vector(10, 530),
    Size = new Vector(400, 300),
    Columns = 4,
    Rows = 4,
    CellPadding = 5
};

grid.AddControl(new Label("Span"), 0, 0, 2, 1); // Span 2 columns`}
        />
      </section>

      {/* Dialogs */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Dialogs and Message Boxes
        </h2>
        <CodeBlock
          code={`// Simple message box
MessageBox.Show("Operation completed successfully!");

// Message box with title and buttons
var result = MessageBox.Show(
    "Do you want to save changes?",
    "Confirm Save",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question
);

if (result == DialogResult.Yes)
{
    SaveChanges();
}

// File dialogs
var openDialog = new OpenFileDialog()
{
    Title = "Select Image",
    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif|All Files|*.*",
    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
    Multiselect = true
};

if (openDialog.ShowDialog() == DialogResult.OK)
{
    foreach (var file in openDialog.FileNames)
    {
        LoadImage(file);
    }
}

// Save dialog
var saveDialog = new SaveFileDialog()
{
    Title = "Save Document",
    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
    DefaultExt = "txt",
    AddExtension = true
};

if (saveDialog.ShowDialog() == DialogResult.OK)
{
    SaveToFile(saveDialog.FileName);
}

// Folder browser
var folderDialog = new FolderBrowserDialog()
{
    Description = "Select output directory",
    ShowNewFolderButton = true
};

if (folderDialog.ShowDialog() == DialogResult.OK)
{
    SetOutputPath(folderDialog.SelectedPath);
}

// Custom dialogs
var inputDialog = new InputDialog("Enter Name", "Please enter your name:");
if (inputDialog.ShowDialog() == DialogResult.OK)
{
    string name = inputDialog.Value;
}

// Progress dialog
var progressDialog = new ProgressDialog("Processing Files")
{
    Maximum = 100,
    ShowTimeRemaining = true,
    AllowCancel = true
};

progressDialog.Show();
for (int i = 0; i <= 100; i++)
{
    progressDialog.Value = i;
    progressDialog.Message = $"Processing file {i} of 100...";
    
    if (progressDialog.Cancelled)
        break;
        
    Thread.Sleep(50);
}
progressDialog.Close();

// About dialog
var aboutDialog = new AboutDialog()
{
    ProductName = "Ouroboros Application",
    Version = "1.0.0",
    Copyright = "© 2024 Your Company",
    Description = "A powerful application built with Ouroboros",
    Logo = new Icon("app-logo.png")
};
aboutDialog.ShowDialog();`}
        />
      </section>

      {/* Complete Example */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Complete Example: Text Editor
        </h2>
        <CodeBlock
          code={`using Ouroboros.StdLib.UI;

public class TextEditor : Application
{
    private Window mainWindow;
    private RichTextBox textBox;
    private StatusBar statusBar;
    private string currentFile = null;
    private bool hasChanges = false;
    
    public TextEditor()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // Create main window
        mainWindow = new Window("Ouroboros Text Editor", 800, 600)
        {
            Theme = Theme.Material,
            CenterOnScreen = true
        };
        
        // Create menu bar
        var menuBar = CreateMenuBar();
        mainWindow.AddChild(menuBar);
        
        // Create toolbar
        var toolBar = CreateToolBar();
        mainWindow.AddChild(toolBar);
        
        // Create text editor
        textBox = new RichTextBox()
        {
            Position = new Vector(0, 65),
            Size = new Vector(800, 510),
            Font = new Font("Consolas", 11),
            WordWrap = false,
            ShowLineNumbers = true,
            SyntaxHighlighting = SyntaxHighlighter.CSharp
        };
        
        textBox.TextChanged += (s, e) => 
        {
            hasChanges = true;
            UpdateTitle();
            UpdateStatus();
        };
        
        mainWindow.AddChild(textBox);
        
        // Create status bar
        statusBar = new StatusBar();
        statusBar.AddPanel("Ready", StatusBarPanelAutoSize.Spring);
        statusBar.AddPanel("Ln 1, Col 1", 100);
        statusBar.AddPanel("UTF-8", 60);
        mainWindow.AddChild(statusBar);
        
        // Window closing event
        mainWindow.Closing += (s, e) =>
        {
            if (hasChanges)
            {
                var result = MessageBox.Show(
                    "Save changes before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel
                );
                
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (result == DialogResult.Yes)
                {
                    SaveFile();
                }
            }
        };
    }
    
    private MenuBar CreateMenuBar()
    {
        var menuBar = new MenuBar();
        
        // File menu
        var fileMenu = menuBar.AddMenu("File");
        fileMenu.AddItem("New", NewFile).WithShortcut(Keys.Control | Keys.N);
        fileMenu.AddItem("Open...", OpenFile).WithShortcut(Keys.Control | Keys.O);
        fileMenu.AddItem("Save", SaveFile).WithShortcut(Keys.Control | Keys.S);
        fileMenu.AddItem("Save As...", SaveFileAs).WithShortcut(Keys.Control | Keys.Shift | Keys.S);
        fileMenu.AddSeparator();
        fileMenu.AddItem("Exit", () => Application.Exit());
        
        // Edit menu
        var editMenu = menuBar.AddMenu("Edit");
        editMenu.AddItem("Undo", () => textBox.Undo()).WithShortcut(Keys.Control | Keys.Z);
        editMenu.AddItem("Redo", () => textBox.Redo()).WithShortcut(Keys.Control | Keys.Y);
        editMenu.AddSeparator();
        editMenu.AddItem("Cut", () => textBox.Cut()).WithShortcut(Keys.Control | Keys.X);
        editMenu.AddItem("Copy", () => textBox.Copy()).WithShortcut(Keys.Control | Keys.C);
        editMenu.AddItem("Paste", () => textBox.Paste()).WithShortcut(Keys.Control | Keys.V);
        editMenu.AddSeparator();
        editMenu.AddItem("Find...", ShowFindDialog).WithShortcut(Keys.Control | Keys.F);
        editMenu.AddItem("Replace...", ShowReplaceDialog).WithShortcut(Keys.Control | Keys.H);
        
        // View menu
        var viewMenu = menuBar.AddMenu("View");
        var wordWrapItem = viewMenu.AddItem("Word Wrap", ToggleWordWrap).AsCheckable();
        var lineNumbersItem = viewMenu.AddItem("Line Numbers", ToggleLineNumbers).AsCheckable();
        lineNumbersItem.Checked = true;
        
        return menuBar;
    }
    
    private ToolBar CreateToolBar()
    {
        var toolBar = new ToolBar()
        {
            Position = new Vector(0, 25),
            Height = 40
        };
        
        toolBar.AddButton("New", NewFile).WithIcon("new-24.png");
        toolBar.AddButton("Open", OpenFile).WithIcon("open-24.png");
        toolBar.AddButton("Save", SaveFile).WithIcon("save-24.png");
        toolBar.AddSeparator();
        toolBar.AddButton("Cut", () => textBox.Cut()).WithIcon("cut-24.png");
        toolBar.AddButton("Copy", () => textBox.Copy()).WithIcon("copy-24.png");
        toolBar.AddButton("Paste", () => textBox.Paste()).WithIcon("paste-24.png");
        
        return toolBar;
    }
    
    private void NewFile()
    {
        if (ConfirmSaveChanges())
        {
            textBox.Clear();
            currentFile = null;
            hasChanges = false;
            UpdateTitle();
        }
    }
    
    private void OpenFile()
    {
        if (!ConfirmSaveChanges()) return;
        
        var dialog = new OpenFileDialog()
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
        };
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            textBox.Text = File.ReadAllText(dialog.FileName);
            currentFile = dialog.FileName;
            hasChanges = false;
            UpdateTitle();
        }
    }
    
    private void SaveFile()
    {
        if (currentFile == null)
        {
            SaveFileAs();
        }
        else
        {
            File.WriteAllText(currentFile, textBox.Text);
            hasChanges = false;
            UpdateTitle();
        }
    }
    
    private void SaveFileAs()
    {
        var dialog = new SaveFileDialog()
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = "txt"
        };
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, textBox.Text);
            currentFile = dialog.FileName;
            hasChanges = false;
            UpdateTitle();
        }
    }
    
    private void UpdateTitle()
    {
        string fileName = currentFile ?? "Untitled";
        string modified = hasChanges ? "*" : "";
        mainWindow.Text = $"{Path.GetFileName(fileName)}{modified} - Ouroboros Text Editor";
    }
    
    private void UpdateStatus()
    {
        var position = textBox.CaretPosition;
        statusBar.Panels[1].Text = $"Ln {position.Line}, Col {position.Column}";
    }
    
    public void Run()
    {
        mainWindow.Show();
        Application.Run();
    }
    
    public static void Main()
    {
        var editor = new TextEditor();
        editor.Run();
    }
}`}
        />
      </section>

      {/* Best Practices */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Best Practices
        </h2>
        
        <Callout type="tip" title="UI Design Guidelines">
          Follow these principles for better user interfaces:
          <ul className="list-disc list-inside mt-2">
            <li>Keep layouts consistent and predictable</li>
            <li>Use appropriate spacing and alignment</li>
            <li>Group related controls together</li>
            <li>Provide keyboard shortcuts for common actions</li>
            <li>Include tooltips for complex controls</li>
            <li>Support both light and dark themes</li>
          </ul>
        </Callout>

        <Callout type="info" title="Performance Tips">
          Optimize UI performance:
          <ul className="list-disc list-inside mt-2">
            <li>Use virtual scrolling for large lists</li>
            <li>Suspend layout during bulk updates</li>
            <li>Cache frequently used resources</li>
            <li>Debounce rapid UI updates</li>
            <li>Load images asynchronously</li>
          </ul>
        </Callout>

        <CodeBlock
          code={`// Good: Responsive UI with async operations
private async void LoadDataButton_Click(object sender, EventArgs e)
{
    loadButton.Enabled = false;
    progressBar.Visible = true;
    
    try
    {
        var data = await LoadDataAsync();
        PopulateGrid(data);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error loading data: {ex.Message}");
    }
    finally
    {
        loadButton.Enabled = true;
        progressBar.Visible = false;
    }
}

// Good: Proper disposal of resources
public class ImageViewer : UserControl, IDisposable
{
    private Image currentImage;
    
    public void LoadImage(string path)
    {
        currentImage?.Dispose();
        currentImage = Image.FromFile(path);
        Invalidate();
    }
    
    public void Dispose()
    {
        currentImage?.Dispose();
        base.Dispose();
    }
}

// Good: Accessible UI
var button = new Button("&Save")  // Alt+S shortcut
{
    TabIndex = 1,
    ToolTip = "Save the current document (Ctrl+S)"
};

// Good: Validation feedback
textBox.Validating += (s, e) =>
{
    errorProvider.Clear();
    if (string.IsNullOrWhiteSpace(textBox.Text))
    {
        errorProvider.SetError(textBox, "This field is required");
        e.Cancel = true;
    }
};`}
        />
      </section>
    </div>
  )
} 