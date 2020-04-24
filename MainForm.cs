using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Reflection;

using Microsoft.Win32;

using Want.RecordEditor.View;
using Want.RecordEditor.Model;
using Want.RecordEditor.Properties;
using Want.Logging;
using Want.Components;
using Want.Patterns;

namespace Want.RecordEditor
{
	/// <summary>
	/// Main Fillred form.
    /// 
    /// For hints on using the expandable data grid:
    /// http://www.codeproject.com/KB/grid/usingdatagrid.aspx
    /// http://msdn.microsoft.com/en-us/magazine/cc301575.aspx
	/// </summary>
	/// <remarks>Note   To make your Windows Forms application support Windows XP visual styles, be sure to 
	/// set the FlatStyle property of your controls to FlatStyle.System and include a manifest with your 
	/// executable. A manifest is an XML file that is included either as a resource within your application 
	/// executable or as a seperate file that resides in the same directory as the executable file. For an 
	/// example of a manifest, see the Example section for the FlatStyle enumeration. For more information about 
	/// using the visual styles available in Windows XP, see the Using Windows XP Visual Styles in the 
	/// User Interface Design and Development section of the MSDN Library.</remarks>
	public class MainForm : System.Windows.Forms.Form, Observer
	{

		private const string REGISTRY_KEY = "SOFTWARE\\want gmbh\\FillRed";

		private static Logger logger = Logger.GetSingleton();
		private static MainForm theMainForm;

		private System.Windows.Forms.Panel navigationPanel;
		private System.Windows.Forms.Panel dataPanel;
		private System.Windows.Forms.StatusBar statusBar;
		private System.Windows.Forms.MenuItem menuGroupFile;
		private System.Windows.Forms.MenuItem menuItemExit;
		private System.Windows.Forms.MenuItem menuGroupEdit;
		private System.Windows.Forms.MenuItem menuGroupHelp;
		private System.Windows.Forms.MenuItem menuItemAbout;
		private System.Windows.Forms.MainMenu mainMenu;
		private System.Windows.Forms.MenuItem menuItemCut;
		private System.Windows.Forms.MenuItem menuItemCopy;
		private System.Windows.Forms.MenuItem menuItemPaste;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabPageSource;
		private System.Windows.Forms.TabPage tabPageFormatted;
		private System.Windows.Forms.TabPage tabPageLayout;
		private System.Windows.Forms.Panel panelFormattetData;
		private System.Windows.Forms.TextBox textBoxSource;
		private System.Windows.Forms.MenuItem menuItemSaveAll;
		private System.Windows.Forms.MenuItem menuItemOpenDatafile;
		private System.Windows.Forms.MenuItem menuItemLayoutfile;
		private System.Windows.Forms.OpenFileDialog openDataFileDialog;
		private System.Windows.Forms.OpenFileDialog openLayoutFileDialog;
		private System.Windows.Forms.StatusBarPanel statusBarPanelDatafile;
		private System.Windows.Forms.StatusBarPanel statusBarPanelLayoutfile;
        private System.Windows.Forms.MenuItem menuItem3; // separator
		private System.Windows.Forms.MenuItem menuItemWordwrap;
		private System.Windows.Forms.DataGrid dataGridRecordDefinition;
		private System.Windows.Forms.DataGrid dataGridRecordValues;
		private System.Windows.Forms.MenuItem menuItemReapplyLayout;
		private System.Windows.Forms.MenuItem menuItemNewLayout;
		private System.Windows.Forms.SaveFileDialog saveFileDialogLayout;
		private System.Windows.Forms.ErrorProvider errorProvider;
		private System.Windows.Forms.MenuItem menuItemSaveDataFile;
        private System.Windows.Forms.MenuItem menuItem1; // separator
		private System.Windows.Forms.MenuItem menuItemSaveLayout;
		private System.Windows.Forms.MenuItem menuItemSaveLayoutAs;
		private System.Windows.Forms.MenuItem menuItemNewDataFile;
		private System.Windows.Forms.MenuItem menuItemSaveDataFileAs;
        private System.Windows.Forms.SaveFileDialog saveFileDialogData; // separator
		private System.Windows.Forms.DataGridTableStyle dataGridStyleRecord;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTbRecordName;
		private System.Windows.Forms.DataGridTableStyle dataGridStyleField;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTbFieldIndex;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTbFieldName;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTbFieldFormat;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTbFieldLength;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTbDescription;
		private System.Windows.Forms.DataGridTableStyle dataGridTableStyle3;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTextBoxColumn9;
		private System.Windows.Forms.DataGridTableStyle dataGridTableStyle4;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTextBoxColumn10;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTextBoxColumn11;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTextBoxColumn12;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTbFieldCondition;
		private System.Windows.Forms.DataGridBoolColumn dataGridCbFieldIsIdentifier;
		private System.Windows.Forms.DataGridBoolColumn dataGridCbFieldOptional;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTextBoxColumn4;
		private System.Windows.Forms.DataGridTextBoxColumn dataGridTbPredfinedValue;
		private System.Windows.Forms.DataGridBoolColumn dataGridCbFieldEor;
		private System.Windows.Forms.MenuItem menuItem5;

        private CurrencyManager cmData;
        private DataViewManager dvmData;
        private CurrencyManager cmLayout;
        private DataViewManager dvmLayout;
		private Formatter formatter;
        private DataGridBoolColumn dataGridCbRecordIsHideLayout;
        private Panel panelFmtDataSubtab;
        private Label labelRecordStatus;
        private Button btnPrevRecord;
        private Button btnNextRecord;
        private Panel panelLayout;
        private Button btnLayoutPrev;
        private Button btnLayoutNext;
        private Label lblLayoutStatus;
        private TabPage tabFormattingLog;
        private TextBox textBoxFmtLog;
        private SplitContainer splitContainerLayout;
        private Panel panelMain;
        private TextBox textBox1;
        private MenuItem menuItem7;
        private MenuItem menuItemEncUtf8;
        private MenuItem menuItemEncLatin1;
        private IContainer components;

        /// <summary>
        /// Creates default MainForm instance. Note that grid layout is created in
        /// InitializeComponent()
        /// </summary>
		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			formatter = new Formatter();

            // setup layout dataset, binding, grid

            // .. use a data view manager her to define default sorting options
            // .. for the dataset's child table. consequently, bind the current 
            // .. manager to the view manager, and not to the dataset directly.
            dvmLayout = new DataViewManager(formatter.LayoutDataSet);
            dvmLayout.DataViewSettings["Field"].ApplyDefaultSort = true;
            dvmLayout.DataViewSettings["Field"].Sort = "Index";

            cmLayout = (CurrencyManager)BindingContext[dvmLayout, "Record"];
            if (cmLayout != null)
            {
                cmLayout.CurrentChanged += new EventHandler(CurrentLayoutRecordChanged);
            }

            dataGridRecordDefinition.SetDataBinding(dvmLayout, "Record");

            formatter.LayoutSubject.RegisterObserver(this);

            // setup (formatted) data binding, grid
            dvmData = new DataViewManager(formatter.DataSet);
            dvmData.DataViewSettings["Record"].RowFilter = "(Hidden = False)";
            dataGridRecordValues.SetDataBinding(dvmData, "Record");

            cmData = (CurrencyManager)BindingContext[dvmData, "Record"];
            if (cmData != null)
            {
                cmData.CurrentChanged += new EventHandler(CurrentDataRecordChanged);
            }
            
			formatter.DataSubject.RegisterObserver(this);

			errorProvider.SetIconAlignment (this.dataGridRecordValues, ErrorIconAlignment.TopRight);
			errorProvider.SetIconPadding (this.dataGridRecordValues, -errorProvider.Icon.Width);
		}

		private void MainForm_Load(object sender, System.EventArgs e)
		{
            this.Size = Properties.Settings.Default.lastSize;
            this.Location = Properties.Settings.Default.lastLocation;

            string lastLayout = Properties.Settings.Default.lastLayout;
            if ((lastLayout != null) && (lastLayout.Length > 0))
            {
                formatter.LayoutFile = lastLayout;
                statusBarPanelLayoutfile.Text = formatter.LayoutFile;
            }
            logger.TextBox = this.textBoxFmtLog;
		}


		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.mainMenu = new System.Windows.Forms.MainMenu(this.components);
            this.menuGroupFile = new System.Windows.Forms.MenuItem();
            this.menuItemNewDataFile = new System.Windows.Forms.MenuItem();
            this.menuItemOpenDatafile = new System.Windows.Forms.MenuItem();
            this.menuItemSaveDataFile = new System.Windows.Forms.MenuItem();
            this.menuItemSaveDataFileAs = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItemNewLayout = new System.Windows.Forms.MenuItem();
            this.menuItemLayoutfile = new System.Windows.Forms.MenuItem();
            this.menuItemSaveLayout = new System.Windows.Forms.MenuItem();
            this.menuItemSaveLayoutAs = new System.Windows.Forms.MenuItem();
            this.menuItemSaveAll = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItemExit = new System.Windows.Forms.MenuItem();
            this.menuGroupEdit = new System.Windows.Forms.MenuItem();
            this.menuItemCut = new System.Windows.Forms.MenuItem();
            this.menuItemCopy = new System.Windows.Forms.MenuItem();
            this.menuItemPaste = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItemWordwrap = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.menuItemReapplyLayout = new System.Windows.Forms.MenuItem();
            this.menuGroupHelp = new System.Windows.Forms.MenuItem();
            this.menuItemAbout = new System.Windows.Forms.MenuItem();
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.statusBarPanelDatafile = new System.Windows.Forms.StatusBarPanel();
            this.statusBarPanelLayoutfile = new System.Windows.Forms.StatusBarPanel();
            this.navigationPanel = new System.Windows.Forms.Panel();
            this.dataPanel = new System.Windows.Forms.Panel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageSource = new System.Windows.Forms.TabPage();
            this.textBoxSource = new System.Windows.Forms.TextBox();
            this.tabPageFormatted = new System.Windows.Forms.TabPage();
            this.panelFormattetData = new System.Windows.Forms.Panel();
            this.dataGridRecordValues = new System.Windows.Forms.DataGrid();
            this.dataGridTableStyle3 = new System.Windows.Forms.DataGridTableStyle();
            this.dataGridTextBoxColumn9 = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTableStyle4 = new System.Windows.Forms.DataGridTableStyle();
            this.dataGridTextBoxColumn10 = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTextBoxColumn11 = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTextBoxColumn12 = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTextBoxColumn4 = new System.Windows.Forms.DataGridTextBoxColumn();
            this.panelFmtDataSubtab = new System.Windows.Forms.Panel();
            this.btnPrevRecord = new System.Windows.Forms.Button();
            this.btnNextRecord = new System.Windows.Forms.Button();
            this.labelRecordStatus = new System.Windows.Forms.Label();
            this.tabPageLayout = new System.Windows.Forms.TabPage();
            this.splitContainerLayout = new System.Windows.Forms.SplitContainer();
            this.dataGridRecordDefinition = new System.Windows.Forms.DataGrid();
            this.dataGridStyleRecord = new System.Windows.Forms.DataGridTableStyle();
            this.dataGridTbRecordName = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridCbRecordIsHideLayout = new System.Windows.Forms.DataGridBoolColumn();
            this.dataGridStyleField = new System.Windows.Forms.DataGridTableStyle();
            this.dataGridTbFieldIndex = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTbFieldName = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTbFieldLength = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTbFieldFormat = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridCbFieldIsIdentifier = new System.Windows.Forms.DataGridBoolColumn();
            this.dataGridCbFieldEor = new System.Windows.Forms.DataGridBoolColumn();
            this.dataGridCbFieldOptional = new System.Windows.Forms.DataGridBoolColumn();
            this.dataGridTbPredfinedValue = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTbFieldCondition = new System.Windows.Forms.DataGridTextBoxColumn();
            this.dataGridTbDescription = new System.Windows.Forms.DataGridTextBoxColumn();
            this.panelMain = new System.Windows.Forms.Panel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.panelLayout = new System.Windows.Forms.Panel();
            this.btnLayoutPrev = new System.Windows.Forms.Button();
            this.btnLayoutNext = new System.Windows.Forms.Button();
            this.lblLayoutStatus = new System.Windows.Forms.Label();
            this.tabFormattingLog = new System.Windows.Forms.TabPage();
            this.textBoxFmtLog = new System.Windows.Forms.TextBox();
            this.openDataFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.openLayoutFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialogLayout = new System.Windows.Forms.SaveFileDialog();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.saveFileDialogData = new System.Windows.Forms.SaveFileDialog();
            this.menuItemEncUtf8 = new System.Windows.Forms.MenuItem();
            this.menuItemEncLatin1 = new System.Windows.Forms.MenuItem();
            this.menuItem7 = new System.Windows.Forms.MenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelDatafile)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelLayoutfile)).BeginInit();
            this.navigationPanel.SuspendLayout();
            this.dataPanel.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabPageSource.SuspendLayout();
            this.tabPageFormatted.SuspendLayout();
            this.panelFormattetData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridRecordValues)).BeginInit();
            this.panelFmtDataSubtab.SuspendLayout();
            this.tabPageLayout.SuspendLayout();
            this.splitContainerLayout.Panel1.SuspendLayout();
            this.splitContainerLayout.Panel2.SuspendLayout();
            this.splitContainerLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridRecordDefinition)).BeginInit();
            this.panelMain.SuspendLayout();
            this.panelLayout.SuspendLayout();
            this.tabFormattingLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuGroupFile,
            this.menuGroupEdit,
            this.menuItem3,
            this.menuGroupHelp});
            // 
            // menuGroupFile
            // 
            this.menuGroupFile.Index = 0;
            this.menuGroupFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemNewDataFile,
            this.menuItemOpenDatafile,
            this.menuItemSaveDataFile,
            this.menuItemSaveDataFileAs,
            this.menuItem1,
            this.menuItemSaveAll,
            this.menuItem2,
            this.menuItemExit});
            this.menuGroupFile.Text = "&File";
            // 
            // menuItemNewDataFile
            // 
            this.menuItemNewDataFile.Index = 0;
            this.menuItemNewDataFile.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
            this.menuItemNewDataFile.Text = "&New data file";
            this.menuItemNewDataFile.Click += new System.EventHandler(this.menuItemNewDataFile_Click);
            // 
            // menuItemOpenDatafile
            // 
            this.menuItemOpenDatafile.Index = 1;
            this.menuItemOpenDatafile.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
            this.menuItemOpenDatafile.Text = "&Open data file ...";
            this.menuItemOpenDatafile.Click += new System.EventHandler(this.menuItemDatafile_Click);
            // 
            // menuItemSaveDataFile
            // 
            this.menuItemSaveDataFile.Index = 2;
            this.menuItemSaveDataFile.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
            this.menuItemSaveDataFile.Text = "&Save data file";
            this.menuItemSaveDataFile.Click += new System.EventHandler(this.menuItemSave_Click);
            // 
            // menuItemSaveDataFileAs
            // 
            this.menuItemSaveDataFileAs.Index = 3;
            this.menuItemSaveDataFileAs.Text = "Save data file &as ...";
            this.menuItemSaveDataFileAs.Click += new System.EventHandler(this.menuItemSaveDataFileAs_Click);
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 4;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemNewLayout,
            this.menuItemLayoutfile,
            this.menuItemSaveLayout,
            this.menuItemSaveLayoutAs});
            this.menuItem1.Text = "Layout file";
            // 
            // menuItemNewLayout
            // 
            this.menuItemNewLayout.Index = 0;
            this.menuItemNewLayout.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftN;
            this.menuItemNewLayout.Text = "&New";
            this.menuItemNewLayout.Click += new System.EventHandler(this.menuItemNewLayout_Click);
            // 
            // menuItemLayoutfile
            // 
            this.menuItemLayoutfile.Index = 1;
            this.menuItemLayoutfile.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftO;
            this.menuItemLayoutfile.Text = "&Open  ...";
            this.menuItemLayoutfile.Click += new System.EventHandler(this.menuItemLayoutfile_Click);
            // 
            // menuItemSaveLayout
            // 
            this.menuItemSaveLayout.Index = 2;
            this.menuItemSaveLayout.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftS;
            this.menuItemSaveLayout.Text = "&Save";
            this.menuItemSaveLayout.Click += new System.EventHandler(this.menuItemSaveLayout_Click);
            // 
            // menuItemSaveLayoutAs
            // 
            this.menuItemSaveLayoutAs.Index = 3;
            this.menuItemSaveLayoutAs.Text = "S&ave as ...";
            this.menuItemSaveLayoutAs.Click += new System.EventHandler(this.menuItemSaveLayoutAs_Click);
            // 
            // menuItemSaveAll
            // 
            this.menuItemSaveAll.Index = 5;
            this.menuItemSaveAll.Text = "Save &all";
            this.menuItemSaveAll.Click += new System.EventHandler(this.menuItemSaveAll_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 6;
            this.menuItem2.Text = "-";
            // 
            // menuItemExit
            // 
            this.menuItemExit.Index = 7;
            this.menuItemExit.Text = "E&xit";
            this.menuItemExit.Click += new System.EventHandler(this.menuItemExit_Click);
            // 
            // menuGroupEdit
            // 
            this.menuGroupEdit.Index = 1;
            this.menuGroupEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemCut,
            this.menuItemCopy,
            this.menuItemPaste});
            this.menuGroupEdit.Text = "&Edit";
            // 
            // menuItemCut
            // 
            this.menuItemCut.Index = 0;
            this.menuItemCut.Shortcut = System.Windows.Forms.Shortcut.CtrlX;
            this.menuItemCut.Text = "Cu&t";
            this.menuItemCut.Click += new System.EventHandler(this.menuItemCut_Click);
            // 
            // menuItemCopy
            // 
            this.menuItemCopy.Index = 1;
            this.menuItemCopy.Shortcut = System.Windows.Forms.Shortcut.CtrlC;
            this.menuItemCopy.Text = "Copy";
            this.menuItemCopy.Click += new System.EventHandler(this.menuItemCopy_Click);
            // 
            // menuItemPaste
            // 
            this.menuItemPaste.Index = 2;
            this.menuItemPaste.Shortcut = System.Windows.Forms.Shortcut.CtrlV;
            this.menuItemPaste.Text = "Paste";
            this.menuItemPaste.Click += new System.EventHandler(this.menuItemPaste_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 2;
            this.menuItem3.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemWordwrap,
            this.menuItem7,
            this.menuItemEncUtf8,
            this.menuItemEncLatin1,
            this.menuItem5,
            this.menuItemReapplyLayout});
            this.menuItem3.Text = "&Format";
            // 
            // menuItemWordwrap
            // 
            this.menuItemWordwrap.Index = 0;
            this.menuItemWordwrap.Text = "Word wrap";
            this.menuItemWordwrap.Click += new System.EventHandler(this.menuItemWordwrap_Click);
            // 
            // menuItem5
            // 
            this.menuItem5.Index = 4;
            this.menuItem5.Text = "-";
            // 
            // menuItemReapplyLayout
            // 
            this.menuItemReapplyLayout.Index = 5;
            this.menuItemReapplyLayout.Text = "Reapply layout";
            this.menuItemReapplyLayout.Click += new System.EventHandler(this.menuItemReapplyLayout_Click);
            // 
            // menuGroupHelp
            // 
            this.menuGroupHelp.Index = 3;
            this.menuGroupHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemAbout});
            this.menuGroupHelp.Text = "&Help";
            // 
            // menuItemAbout
            // 
            this.menuItemAbout.Index = 0;
            this.menuItemAbout.Text = "About ...";
            this.menuItemAbout.Click += new System.EventHandler(this.menuItemAbout_Click);
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 387);
            this.statusBar.Name = "statusBar";
            this.statusBar.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBarPanelDatafile,
            this.statusBarPanelLayoutfile});
            this.statusBar.ShowPanels = true;
            this.statusBar.Size = new System.Drawing.Size(680, 22);
            this.statusBar.TabIndex = 1;
            // 
            // statusBarPanelDatafile
            // 
            this.statusBarPanelDatafile.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
            this.statusBarPanelDatafile.Name = "statusBarPanelDatafile";
            this.statusBarPanelDatafile.Width = 331;
            // 
            // statusBarPanelLayoutfile
            // 
            this.statusBarPanelLayoutfile.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
            this.statusBarPanelLayoutfile.Name = "statusBarPanelLayoutfile";
            this.statusBarPanelLayoutfile.Width = 331;
            // 
            // navigationPanel
            // 
            this.navigationPanel.Controls.Add(this.dataPanel);
            this.navigationPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navigationPanel.Location = new System.Drawing.Point(0, 0);
            this.navigationPanel.Name = "navigationPanel";
            this.navigationPanel.Padding = new System.Windows.Forms.Padding(3);
            this.navigationPanel.Size = new System.Drawing.Size(680, 387);
            this.navigationPanel.TabIndex = 4;
            // 
            // dataPanel
            // 
            this.dataPanel.Controls.Add(this.tabControl);
            this.dataPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataPanel.Location = new System.Drawing.Point(3, 3);
            this.dataPanel.Name = "dataPanel";
            this.dataPanel.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.dataPanel.Size = new System.Drawing.Size(674, 381);
            this.dataPanel.TabIndex = 2;
            // 
            // tabControl
            // 
            this.tabControl.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabControl.Controls.Add(this.tabPageSource);
            this.tabControl.Controls.Add(this.tabPageFormatted);
            this.tabControl.Controls.Add(this.tabPageLayout);
            this.tabControl.Controls.Add(this.tabFormattingLog);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(3, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(671, 381);
            this.tabControl.TabIndex = 0;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
            // 
            // tabPageSource
            // 
            this.tabPageSource.Controls.Add(this.textBoxSource);
            this.tabPageSource.Location = new System.Drawing.Point(4, 4);
            this.tabPageSource.Name = "tabPageSource";
            this.tabPageSource.Size = new System.Drawing.Size(663, 355);
            this.tabPageSource.TabIndex = 0;
            this.tabPageSource.Text = "Source";
            this.tabPageSource.UseVisualStyleBackColor = true;
            // 
            // textBoxSource
            // 
            this.textBoxSource.AcceptsReturn = true;
            this.textBoxSource.AcceptsTab = true;
            this.textBoxSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxSource.Font = new System.Drawing.Font("Lucida Console", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxSource.Location = new System.Drawing.Point(0, 0);
            this.textBoxSource.MaxLength = 0;
            this.textBoxSource.Multiline = true;
            this.textBoxSource.Name = "textBoxSource";
            this.textBoxSource.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxSource.Size = new System.Drawing.Size(663, 355);
            this.textBoxSource.TabIndex = 0;
            this.textBoxSource.WordWrap = false;
            this.textBoxSource.TextChanged += new System.EventHandler(this.textBoxSource_TextChanged);
            // 
            // tabPageFormatted
            // 
            this.tabPageFormatted.Controls.Add(this.panelFormattetData);
            this.tabPageFormatted.Location = new System.Drawing.Point(4, 4);
            this.tabPageFormatted.Name = "tabPageFormatted";
            this.tabPageFormatted.Size = new System.Drawing.Size(663, 355);
            this.tabPageFormatted.TabIndex = 1;
            this.tabPageFormatted.Text = "Formatted";
            this.tabPageFormatted.UseVisualStyleBackColor = true;
            // 
            // panelFormattetData
            // 
            this.panelFormattetData.Controls.Add(this.dataGridRecordValues);
            this.panelFormattetData.Controls.Add(this.panelFmtDataSubtab);
            this.panelFormattetData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelFormattetData.Location = new System.Drawing.Point(0, 0);
            this.panelFormattetData.Name = "panelFormattetData";
            this.panelFormattetData.Size = new System.Drawing.Size(663, 355);
            this.panelFormattetData.TabIndex = 2;
            // 
            // dataGridRecordValues
            // 
            this.dataGridRecordValues.CaptionText = "Formatted data";
            this.dataGridRecordValues.DataMember = "";
            this.dataGridRecordValues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridRecordValues.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridRecordValues.Location = new System.Drawing.Point(0, 0);
            this.dataGridRecordValues.Name = "dataGridRecordValues";
            this.dataGridRecordValues.Size = new System.Drawing.Size(663, 315);
            this.dataGridRecordValues.TabIndex = 0;
            this.dataGridRecordValues.TableStyles.AddRange(new System.Windows.Forms.DataGridTableStyle[] {
            this.dataGridTableStyle3,
            this.dataGridTableStyle4});
            // 
            // dataGridTableStyle3
            // 
            this.dataGridTableStyle3.DataGrid = this.dataGridRecordValues;
            this.dataGridTableStyle3.GridColumnStyles.AddRange(new System.Windows.Forms.DataGridColumnStyle[] {
            this.dataGridTextBoxColumn9});
            this.dataGridTableStyle3.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridTableStyle3.MappingName = "Record";
            // 
            // dataGridTextBoxColumn9
            // 
            this.dataGridTextBoxColumn9.Format = "";
            this.dataGridTextBoxColumn9.FormatInfo = null;
            this.dataGridTextBoxColumn9.HeaderText = "Record name";
            this.dataGridTextBoxColumn9.MappingName = "Name";
            this.dataGridTextBoxColumn9.Width = 300;
            // 
            // dataGridTableStyle4
            // 
            this.dataGridTableStyle4.DataGrid = this.dataGridRecordValues;
            this.dataGridTableStyle4.GridColumnStyles.AddRange(new System.Windows.Forms.DataGridColumnStyle[] {
            this.dataGridTextBoxColumn10,
            this.dataGridTextBoxColumn11,
            this.dataGridTextBoxColumn12,
            this.dataGridTextBoxColumn4});
            this.dataGridTableStyle4.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridTableStyle4.MappingName = "Field";
            // 
            // dataGridTextBoxColumn10
            // 
            this.dataGridTextBoxColumn10.Format = "";
            this.dataGridTextBoxColumn10.FormatInfo = null;
            this.dataGridTextBoxColumn10.HeaderText = "Index";
            this.dataGridTextBoxColumn10.MappingName = "Index";
            this.dataGridTextBoxColumn10.ReadOnly = true;
            this.dataGridTextBoxColumn10.Width = 45;
            // 
            // dataGridTextBoxColumn11
            // 
            this.dataGridTextBoxColumn11.Format = "";
            this.dataGridTextBoxColumn11.FormatInfo = null;
            this.dataGridTextBoxColumn11.HeaderText = "Name";
            this.dataGridTextBoxColumn11.MappingName = "Name";
            this.dataGridTextBoxColumn11.ReadOnly = true;
            this.dataGridTextBoxColumn11.Width = 200;
            // 
            // dataGridTextBoxColumn12
            // 
            this.dataGridTextBoxColumn12.Format = "";
            this.dataGridTextBoxColumn12.FormatInfo = null;
            this.dataGridTextBoxColumn12.HeaderText = "Value";
            this.dataGridTextBoxColumn12.MappingName = "Value";
            this.dataGridTextBoxColumn12.Width = 150;
            // 
            // dataGridTextBoxColumn4
            // 
            this.dataGridTextBoxColumn4.Format = "";
            this.dataGridTextBoxColumn4.FormatInfo = null;
            this.dataGridTextBoxColumn4.HeaderText = "Description";
            this.dataGridTextBoxColumn4.MappingName = "Description";
            this.dataGridTextBoxColumn4.ReadOnly = true;
            this.dataGridTextBoxColumn4.Width = 200;
            // 
            // panelFmtDataSubtab
            // 
            this.panelFmtDataSubtab.Controls.Add(this.btnPrevRecord);
            this.panelFmtDataSubtab.Controls.Add(this.btnNextRecord);
            this.panelFmtDataSubtab.Controls.Add(this.labelRecordStatus);
            this.panelFmtDataSubtab.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelFmtDataSubtab.Location = new System.Drawing.Point(0, 315);
            this.panelFmtDataSubtab.Name = "panelFmtDataSubtab";
            this.panelFmtDataSubtab.Size = new System.Drawing.Size(663, 40);
            this.panelFmtDataSubtab.TabIndex = 1;
            // 
            // btnPrevRecord
            // 
            this.btnPrevRecord.Image = global::Want.RecordEditor.Properties.Resources.pageprevious;
            this.btnPrevRecord.Location = new System.Drawing.Point(3, 7);
            this.btnPrevRecord.Name = "btnPrevRecord";
            this.btnPrevRecord.Size = new System.Drawing.Size(30, 23);
            this.btnPrevRecord.TabIndex = 2;
            this.btnPrevRecord.UseVisualStyleBackColor = true;
            this.btnPrevRecord.Click += new System.EventHandler(this.btnPrevRecord_Click);
            // 
            // btnNextRecord
            // 
            this.btnNextRecord.Image = global::Want.RecordEditor.Properties.Resources.pagenext;
            this.btnNextRecord.Location = new System.Drawing.Point(39, 7);
            this.btnNextRecord.Name = "btnNextRecord";
            this.btnNextRecord.Size = new System.Drawing.Size(30, 23);
            this.btnNextRecord.TabIndex = 1;
            this.btnNextRecord.UseVisualStyleBackColor = true;
            this.btnNextRecord.Click += new System.EventHandler(this.btnNextRecord_Click);
            // 
            // labelRecordStatus
            // 
            this.labelRecordStatus.Location = new System.Drawing.Point(75, 12);
            this.labelRecordStatus.Name = "labelRecordStatus";
            this.labelRecordStatus.Size = new System.Drawing.Size(100, 13);
            this.labelRecordStatus.TabIndex = 0;
            // 
            // tabPageLayout
            // 
            this.tabPageLayout.Controls.Add(this.splitContainerLayout);
            this.tabPageLayout.Controls.Add(this.panelLayout);
            this.tabPageLayout.Location = new System.Drawing.Point(4, 4);
            this.tabPageLayout.Name = "tabPageLayout";
            this.tabPageLayout.Size = new System.Drawing.Size(663, 355);
            this.tabPageLayout.TabIndex = 2;
            this.tabPageLayout.Text = "Layout";
            this.tabPageLayout.UseVisualStyleBackColor = true;
            // 
            // splitContainerLayout
            // 
            this.splitContainerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerLayout.Location = new System.Drawing.Point(0, 0);
            this.splitContainerLayout.Name = "splitContainerLayout";
            // 
            // splitContainerLayout.Panel1
            // 
            this.splitContainerLayout.Panel1.Controls.Add(this.dataGridRecordDefinition);
            this.splitContainerLayout.Panel1MinSize = 400;
            // 
            // splitContainerLayout.Panel2
            // 
            this.splitContainerLayout.Panel2.Controls.Add(this.panelMain);
            this.splitContainerLayout.Size = new System.Drawing.Size(663, 315);
            this.splitContainerLayout.SplitterDistance = 450;
            this.splitContainerLayout.TabIndex = 8;
            // 
            // dataGridRecordDefinition
            // 
            this.dataGridRecordDefinition.CaptionText = "Records";
            this.dataGridRecordDefinition.DataMember = "";
            this.dataGridRecordDefinition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridRecordDefinition.FlatMode = true;
            this.dataGridRecordDefinition.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridRecordDefinition.Location = new System.Drawing.Point(0, 0);
            this.dataGridRecordDefinition.Name = "dataGridRecordDefinition";
            this.dataGridRecordDefinition.Size = new System.Drawing.Size(450, 315);
            this.dataGridRecordDefinition.TabIndex = 6;
            this.dataGridRecordDefinition.TableStyles.AddRange(new System.Windows.Forms.DataGridTableStyle[] {
            this.dataGridStyleRecord,
            this.dataGridStyleField});
            // 
            // dataGridStyleRecord
            // 
            this.dataGridStyleRecord.AllowSorting = false;
            this.dataGridStyleRecord.DataGrid = this.dataGridRecordDefinition;
            this.dataGridStyleRecord.GridColumnStyles.AddRange(new System.Windows.Forms.DataGridColumnStyle[] {
            this.dataGridTbRecordName,
            this.dataGridCbRecordIsHideLayout});
            this.dataGridStyleRecord.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridStyleRecord.MappingName = "Record";
            // 
            // dataGridTbRecordName
            // 
            this.dataGridTbRecordName.Format = "";
            this.dataGridTbRecordName.FormatInfo = null;
            this.dataGridTbRecordName.HeaderText = "Record name";
            this.dataGridTbRecordName.MappingName = "Name";
            this.dataGridTbRecordName.Width = 300;
            // 
            // dataGridCbRecordIsHideLayout
            // 
            this.dataGridCbRecordIsHideLayout.AllowNull = false;
            this.dataGridCbRecordIsHideLayout.HeaderText = "Hide in formatted view";
            this.dataGridCbRecordIsHideLayout.MappingName = "HideInFormatView";
            this.dataGridCbRecordIsHideLayout.NullValue = "False";
            this.dataGridCbRecordIsHideLayout.Width = 75;
            // 
            // dataGridStyleField
            // 
            this.dataGridStyleField.DataGrid = this.dataGridRecordDefinition;
            this.dataGridStyleField.GridColumnStyles.AddRange(new System.Windows.Forms.DataGridColumnStyle[] {
            this.dataGridTbFieldIndex,
            this.dataGridTbFieldName,
            this.dataGridTbFieldLength,
            this.dataGridTbFieldFormat,
            this.dataGridCbFieldIsIdentifier,
            this.dataGridCbFieldEor,
            this.dataGridCbFieldOptional,
            this.dataGridTbPredfinedValue,
            this.dataGridTbFieldCondition,
            this.dataGridTbDescription});
            this.dataGridStyleField.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridStyleField.MappingName = "Field";
            // 
            // dataGridTbFieldIndex
            // 
            this.dataGridTbFieldIndex.Format = "";
            this.dataGridTbFieldIndex.FormatInfo = null;
            this.dataGridTbFieldIndex.HeaderText = "Index";
            this.dataGridTbFieldIndex.MappingName = "Index";
            this.dataGridTbFieldIndex.Width = 40;
            // 
            // dataGridTbFieldName
            // 
            this.dataGridTbFieldName.Format = "";
            this.dataGridTbFieldName.FormatInfo = null;
            this.dataGridTbFieldName.HeaderText = "Name";
            this.dataGridTbFieldName.MappingName = "Name";
            this.dataGridTbFieldName.NullText = "";
            this.dataGridTbFieldName.Width = 170;
            // 
            // dataGridTbFieldLength
            // 
            this.dataGridTbFieldLength.Format = "";
            this.dataGridTbFieldLength.FormatInfo = null;
            this.dataGridTbFieldLength.HeaderText = "Length";
            this.dataGridTbFieldLength.MappingName = "Length";
            this.dataGridTbFieldLength.NullText = "0";
            this.dataGridTbFieldLength.Width = 45;
            // 
            // dataGridTbFieldFormat
            // 
            this.dataGridTbFieldFormat.Format = "";
            this.dataGridTbFieldFormat.FormatInfo = null;
            this.dataGridTbFieldFormat.HeaderText = "Format";
            this.dataGridTbFieldFormat.MappingName = "Format";
            this.dataGridTbFieldFormat.NullText = "";
            this.dataGridTbFieldFormat.Width = 45;
            // 
            // dataGridCbFieldIsIdentifier
            // 
            this.dataGridCbFieldIsIdentifier.AllowNull = false;
            this.dataGridCbFieldIsIdentifier.HeaderText = "Identity";
            this.dataGridCbFieldIsIdentifier.MappingName = "IsRecordIdentifier";
            this.dataGridCbFieldIsIdentifier.NullText = "False";
            this.dataGridCbFieldIsIdentifier.NullValue = "False";
            this.dataGridCbFieldIsIdentifier.Width = 45;
            // 
            // dataGridCbFieldEor
            // 
            this.dataGridCbFieldEor.AllowNull = false;
            this.dataGridCbFieldEor.HeaderText = "EOR";
            this.dataGridCbFieldEor.MappingName = "IsEOR";
            this.dataGridCbFieldEor.NullText = "False";
            this.dataGridCbFieldEor.NullValue = "False";
            this.dataGridCbFieldEor.Width = 40;
            // 
            // dataGridCbFieldOptional
            // 
            this.dataGridCbFieldOptional.AllowNull = false;
            this.dataGridCbFieldOptional.HeaderText = "Optional";
            this.dataGridCbFieldOptional.MappingName = "IsOptional";
            this.dataGridCbFieldOptional.NullText = "False";
            this.dataGridCbFieldOptional.NullValue = "False";
            this.dataGridCbFieldOptional.Width = 50;
            // 
            // dataGridTbPredfinedValue
            // 
            this.dataGridTbPredfinedValue.Format = "";
            this.dataGridTbPredfinedValue.FormatInfo = null;
            this.dataGridTbPredfinedValue.HeaderText = "Value";
            this.dataGridTbPredfinedValue.MappingName = "PredefinedValue";
            this.dataGridTbPredfinedValue.NullText = "";
            this.dataGridTbPredfinedValue.Width = 50;
            // 
            // dataGridTbFieldCondition
            // 
            this.dataGridTbFieldCondition.Format = "";
            this.dataGridTbFieldCondition.FormatInfo = null;
            this.dataGridTbFieldCondition.HeaderText = "Condition";
            this.dataGridTbFieldCondition.MappingName = "Condition";
            this.dataGridTbFieldCondition.NullText = "";
            this.dataGridTbFieldCondition.Width = 75;
            // 
            // dataGridTbDescription
            // 
            this.dataGridTbDescription.Format = "";
            this.dataGridTbDescription.FormatInfo = null;
            this.dataGridTbDescription.HeaderText = "Description";
            this.dataGridTbDescription.MappingName = "Description";
            this.dataGridTbDescription.NullText = "";
            this.dataGridTbDescription.Width = 150;
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.textBox1);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(209, 315);
            this.panelMain.TabIndex = 0;
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Location = new System.Drawing.Point(0, 0);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(209, 315);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = resources.GetString("textBox1.Text");
            // 
            // panelLayout
            // 
            this.panelLayout.Controls.Add(this.btnLayoutPrev);
            this.panelLayout.Controls.Add(this.btnLayoutNext);
            this.panelLayout.Controls.Add(this.lblLayoutStatus);
            this.panelLayout.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelLayout.Location = new System.Drawing.Point(0, 315);
            this.panelLayout.Name = "panelLayout";
            this.panelLayout.Size = new System.Drawing.Size(663, 40);
            this.panelLayout.TabIndex = 7;
            // 
            // btnLayoutPrev
            // 
            this.btnLayoutPrev.Image = global::Want.RecordEditor.Properties.Resources.pageprevious;
            this.btnLayoutPrev.Location = new System.Drawing.Point(3, 7);
            this.btnLayoutPrev.Name = "btnLayoutPrev";
            this.btnLayoutPrev.Size = new System.Drawing.Size(30, 23);
            this.btnLayoutPrev.TabIndex = 5;
            this.btnLayoutPrev.UseVisualStyleBackColor = true;
            this.btnLayoutPrev.Click += new System.EventHandler(this.btnLayoutPrev_Click);
            // 
            // btnLayoutNext
            // 
            this.btnLayoutNext.Image = global::Want.RecordEditor.Properties.Resources.pagenext;
            this.btnLayoutNext.Location = new System.Drawing.Point(39, 7);
            this.btnLayoutNext.Name = "btnLayoutNext";
            this.btnLayoutNext.Size = new System.Drawing.Size(30, 23);
            this.btnLayoutNext.TabIndex = 4;
            this.btnLayoutNext.UseVisualStyleBackColor = true;
            this.btnLayoutNext.Click += new System.EventHandler(this.btnLayoutNext_Click);
            // 
            // lblLayoutStatus
            // 
            this.lblLayoutStatus.Location = new System.Drawing.Point(75, 12);
            this.lblLayoutStatus.Name = "lblLayoutStatus";
            this.lblLayoutStatus.Size = new System.Drawing.Size(100, 13);
            this.lblLayoutStatus.TabIndex = 3;
            // 
            // tabFormattingLog
            // 
            this.tabFormattingLog.Controls.Add(this.textBoxFmtLog);
            this.tabFormattingLog.Font = new System.Drawing.Font("Lucida Console", 10F);
            this.tabFormattingLog.Location = new System.Drawing.Point(4, 4);
            this.tabFormattingLog.Name = "tabFormattingLog";
            this.tabFormattingLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabFormattingLog.Size = new System.Drawing.Size(663, 355);
            this.tabFormattingLog.TabIndex = 3;
            this.tabFormattingLog.Text = "Formatting log";
            this.tabFormattingLog.UseVisualStyleBackColor = true;
            // 
            // textBoxFmtLog
            // 
            this.textBoxFmtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxFmtLog.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxFmtLog.Location = new System.Drawing.Point(3, 3);
            this.textBoxFmtLog.Multiline = true;
            this.textBoxFmtLog.Name = "textBoxFmtLog";
            this.textBoxFmtLog.ReadOnly = true;
            this.textBoxFmtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxFmtLog.Size = new System.Drawing.Size(657, 349);
            this.textBoxFmtLog.TabIndex = 0;
            // 
            // openDataFileDialog
            // 
            this.openDataFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            this.openDataFileDialog.Title = "Select data file";
            // 
            // openLayoutFileDialog
            // 
            this.openLayoutFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            this.openLayoutFileDialog.Title = "Open layout file";
            // 
            // saveFileDialogLayout
            // 
            this.saveFileDialogLayout.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            this.saveFileDialogLayout.Title = "Save layout file";
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // saveFileDialogData
            // 
            this.saveFileDialogData.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            this.saveFileDialogData.Title = "Save data file";
            // 
            // menuItemEncUtf8
            // 
            this.menuItemEncUtf8.Checked = true;
            this.menuItemEncUtf8.Index = 2;
            this.menuItemEncUtf8.Text = "UTF-8";
            this.menuItemEncUtf8.Click += new System.EventHandler(this.menuItemEncUtf8_Click);
            // 
            // menuItemEncLatin1
            // 
            this.menuItemEncLatin1.Index = 3;
            this.menuItemEncLatin1.Text = "ISO-8859-1";
            this.menuItemEncLatin1.Click += new System.EventHandler(this.menuItemEncLatin1_Click);
            // 
            // menuItem7
            // 
            this.menuItem7.Index = 1;
            this.menuItem7.Text = "-";
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(680, 409);
            this.Controls.Add(this.navigationPanel);
            this.Controls.Add(this.statusBar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.mainMenu;
            this.Name = "MainForm";
            this.Text = "FillRed";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelDatafile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelLayoutfile)).EndInit();
            this.navigationPanel.ResumeLayout(false);
            this.dataPanel.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabPageSource.ResumeLayout(false);
            this.tabPageSource.PerformLayout();
            this.tabPageFormatted.ResumeLayout(false);
            this.panelFormattetData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridRecordValues)).EndInit();
            this.panelFmtDataSubtab.ResumeLayout(false);
            this.tabPageLayout.ResumeLayout(false);
            this.splitContainerLayout.Panel1.ResumeLayout(false);
            this.splitContainerLayout.Panel2.ResumeLayout(false);
            this.splitContainerLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridRecordDefinition)).EndInit();
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            this.panelLayout.ResumeLayout(false);
            this.tabFormattingLog.ResumeLayout(false);
            this.tabFormattingLog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
        static void Main(string[] args)
		{
            try
			{
				theMainForm = new MainForm();
                if (args.Length >= 1)
                {
                    theMainForm.openDataFile(args[0]);
                }
				Application.Run(theMainForm);
			}
			catch (Exception ex)
			{
				try
				{
					logger.Log(Logger.MessageType.FAIL, ex);
				}
				catch (Exception)
                {
                    Console.Out.WriteLine(ex.Message);
                    Console.Out.WriteLine(ex.StackTrace);
                }
			}
		}

		#region Menu handlers

		private void menuItemExit_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (CheckSourceAndContinue() && CheckLayoutAndContinue()) 
			{
                Properties.Settings.Default.lastSize = this.Size;
                Properties.Settings.Default.lastLocation = this.Location;
                Properties.Settings.Default.lastLayout = formatter.LayoutFile;
                Properties.Settings.Default.Save();
			}
			else
			{
				e.Cancel = true;
			}
		}


		private void menuItemAbout_Click(object sender, System.EventArgs e)
		{
			new AboutDialog().ShowDialog(this);
		}

		private void menuItemCut_Click(object sender, System.EventArgs e)
		{
			System.Windows.Forms.Control c = this.ActiveControl;
			Type t = c.GetType();
			MemberInfo[] myMembers = t.GetMember("Cut", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
			if(myMembers.Length > 0)
			{
				t.InvokeMember("Cut", 
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, 
					null, c, null);
			}
		}

		private void menuItemCopy_Click(object sender, System.EventArgs e)
		{
			System.Windows.Forms.Control c = this.ActiveControl;
			Type t = c.GetType();
			MemberInfo[] myMembers = t.GetMember("Copy", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
			if(myMembers.Length > 0)
			{
				t.InvokeMember("Copy", 
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, 
					null, c, null);
			}
		}

		private void menuItemPaste_Click(object sender, System.EventArgs e)
		{
			System.Windows.Forms.Control c = this.ActiveControl;
			Type t = c.GetType();
			MemberInfo[] myMembers = t.GetMember("Paste", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
			if(myMembers.Length > 0)
			{
				t.InvokeMember("Paste", 
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, 
					null, c, null);
			}
		}


		private void menuItemWordwrap_Click(object sender, System.EventArgs e)
		{
			textBoxSource.WordWrap = !textBoxSource.WordWrap;
			menuItemWordwrap.Checked = textBoxSource.WordWrap;
		}

        private void menuItemEncLatin1_Click(object sender, EventArgs e)
        {
            menuItemEncLatin1.Checked = true;
            menuItemEncUtf8.Checked = false;
            Formatter.DefaultEncoding = System.Text.Encoding.GetEncoding("ISO-8859-1");

        }

        private void menuItemEncUtf8_Click(object sender, EventArgs e)
        {
            menuItemEncLatin1.Checked = false;
            menuItemEncUtf8.Checked = true;
            Formatter.DefaultEncoding = System.Text.Encoding.UTF8;
        }


		private void menuItemReapplyLayout_Click(object sender, System.EventArgs e)
		{
			formatter.FormatDatafile();
			errorProvider.SetError(dataGridRecordValues, formatter.Error);
		}


		private void menuItemNewDataFile_Click(object sender, System.EventArgs e)
		{
			formatter.DataFile = "";
			textBoxSource.Text = "";
            formatter.EolDelimiter = "\r\n";
			formatter.DataSubject.SetState(State.NEW);
			formatter.FormatDatafile();
			errorProvider.SetError(dataGridRecordValues, formatter.Error);
		}


		private void menuItemDatafile_Click(object sender, System.EventArgs e)
		{
			if (!CheckSourceAndContinue()) 
			{
				return;
			}

			
			if (openDataFileDialog.ShowDialog(this) == DialogResult.OK) 
			{
                openDataFile(openDataFileDialog.FileName);
			}
		}

        private void openDataFile(String filename)
        {
            formatter.DataFile = filename;

            if (formatter.Source.IndexOf("\r\n") >= 0)
            {
                formatter.EolDelimiter = "\r\n";
            }
            else if (formatter.Source.IndexOf("\r") >= 0)
            {
                formatter.EolDelimiter = "\r";
            }
            else if (formatter.Source.IndexOf("\n") >= 0)
            {
                formatter.EolDelimiter = "\n";
            }

            textBoxSource.Enabled = false;
            textBoxSource.Lines = StringSplit(formatter.Source, formatter.EolDelimiter);
            textBoxSource.Enabled = true;

            statusBarPanelDatafile.Text = formatter.DataFile;
            errorProvider.SetError(dataGridRecordValues, formatter.Error);
        }

		private void menuItemSave_Click(object sender, System.EventArgs e)
		{
			if ((formatter.DataFile == null) || (formatter.DataFile.Length == 0))
			{
				menuItemSaveDataFileAs_Click(sender, e);
			}
			else 
			{
				if (tabControl.SelectedTab == tabPageSource)
				{
					// take data from source
					formatter.Source = textBoxSource.Text;
					formatter.SaveData();
				}
				else if (tabControl.SelectedTab == tabPageFormatted)
				{
					formatter.UpdateSourceFromDataset();
					formatter.SaveData();
				}
			}
		}


		private void menuItemSaveDataFileAs_Click(object sender, System.EventArgs e)
		{
			if (saveFileDialogData.ShowDialog(this) == DialogResult.OK) 
			{
				formatter.OverrideDataFile = saveFileDialogData.FileName;
				menuItemSave_Click(sender, e);
			}
		}


		private void menuItemNewLayout_Click(object sender, System.EventArgs e)
		{
			formatter.LayoutFile = "";
			statusBarPanelLayoutfile.Text = "";
			formatter.FormatDatafile();
			errorProvider.SetError(dataGridRecordValues, formatter.Error);
		}


		private void menuItemLayoutfile_Click(object sender, System.EventArgs e)
		{
			if (!CheckLayoutAndContinue()) 
			{
				return;
			}
			
			if (openLayoutFileDialog.ShowDialog(this) == DialogResult.OK) 
			{
				formatter.LayoutFile = openLayoutFileDialog.FileName;
				statusBarPanelLayoutfile.Text = formatter.LayoutFile;
				errorProvider.SetError(dataGridRecordValues, formatter.Error);
			}
		}

		private void menuItemSaveLayout_Click(object sender, System.EventArgs e)
		{
			if ((formatter.LayoutFile == null) || (formatter.LayoutFile.Length == 0))
			{
				menuItemSaveLayoutAs_Click(sender, e);
			}
			else 
			{
				formatter.SaveLayout();
			}
		}

		private void menuItemSaveLayoutAs_Click(object sender, System.EventArgs e)
		{
			if (saveFileDialogLayout.ShowDialog(this) == DialogResult.OK) 
			{
				formatter.OverrideLayoutFile = saveFileDialogLayout.FileName;
				formatter.SaveLayout();
			}
		}

		private void menuItemSaveAll_Click(object sender, System.EventArgs e)
		{
			formatter.SaveData();
			formatter.SaveLayout();
		}


		#endregion


		#region Control event handlers

		private void tabControl_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			System.Windows.Forms.TabControl tab = (System.Windows.Forms.TabControl)sender;
			if (tab.SelectedTab == tabPageFormatted) 
			{
				// this will trigger a PopulateDatasetFromSource
				if (!textBoxSource.Text.Equals(formatter.Source))
				{
                    if (!formatter.HasErrors)
                    {
                        formatter.Source = String.Join(formatter.EolDelimiter, textBoxSource.Lines);
                    }
				}
			}
			else if (tab.SelectedTab == tabPageSource) 
			{
                if (!formatter.HasErrors)
                {
                    formatter.UpdateSourceFromDataset();
                }

				textBoxSource.Enabled = false;
                textBoxSource.Lines = StringSplit(formatter.Source, formatter.EolDelimiter);
				textBoxSource.Enabled = true;

			}
		}


		private void textBoxSource_TextChanged(object sender, System.EventArgs e)
		{
			if (textBoxSource.Enabled) 
			{
				formatter.DataSubject.SetState(State.DIRTY);
			}
		}

		#endregion

		public void Update(Subject s)
		{
			if (s == formatter.LayoutSubject)
			{
				if (s.GetState() == State.DIRTY)
				{
					statusBarPanelLayoutfile.Text = formatter.LayoutFile + "*";
				}
				else 
				{
					statusBarPanelLayoutfile.Text = formatter.LayoutFile;
				}
			}
			else if (s == formatter.DataSubject)
			{
				if (s.GetState() == State.DIRTY) 
				{
					statusBarPanelDatafile.Text = formatter.DataFile + "*";
				}
				else 
				{
					statusBarPanelDatafile.Text = formatter.DataFile;
				}

			}
		}

		private bool CheckSourceAndContinue()
		{
			DialogResult dlgRes;
			
			if (formatter.DataSubject.GetState() == State.DIRTY) 
			{
				dlgRes = MessageBox.Show("The source has been changed.\nDo you want to save the changes?", "FillRed", 
					System.Windows.Forms.MessageBoxButtons.YesNoCancel);
				if (dlgRes == DialogResult.Yes)
				{
					formatter.SaveData();
				}
				else if (dlgRes == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;

		}

		private bool CheckLayoutAndContinue()
		{
			DialogResult dlgRes;
			
			if (formatter.LayoutSubject.GetState() == State.DIRTY) 
			{
				dlgRes = MessageBox.Show("The layout has been changed.\nDo you want to save the changes?", "FillRed", 
					System.Windows.Forms.MessageBoxButtons.YesNoCancel);
				if (dlgRes == DialogResult.Yes)
				{
					formatter.SaveLayout();
				}
				else if (dlgRes == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;

		}

		private static string[] StringSplit(string instr, string splitter)
		{
			ArrayList parts = new ArrayList();
			int pos;

			while ((pos = instr.IndexOf(splitter)) >= 0)
			{
				parts.Add(instr.Substring(0, pos));
				instr = instr.Remove(0, pos + splitter.Length);
			}

			if (instr.Length > 0)
			{
				parts.Add(instr);
			}

			string[] s = new string[parts.Count];

			for (int i = 0; i < parts.Count; i++)
			{
				s[i] = parts[i].ToString();
			}

			return s;
		}

        /// <summary>
        /// Move data grid to previous child (!) record
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevRecord_Click(object sender, EventArgs e)
        {
            if (cmData.Position > 0)
            {
                cmData.Position -= 1;
            }
            else if (cmData.List.Count > 0)
            {
                cmData.Position = cmData.List.Count - 1;
            }

        }

        private void btnNextRecord_Click(object sender, EventArgs e)
        {
            if ((cmData.Position >= 0) && ((cmData.Position + 1) < cmData.List.Count)) 
            {
                cmData.Position += 1;
            }
            else if (cmData.List.Count > 0)
            {
                cmData.Position = 0;
            }
            
        }

        /// <summary>
        /// Event handler for changes of the 'current' property of the currency manager
        /// bound to the data record table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentDataRecordChanged(Object sender, EventArgs e)
        {
            if (cmData.Position >= 0)
            {
                DataRowView drv = (DataRowView)cmData.Current;
                if (drv.Row is FormattedDataDataset.RecordRow)
                {
                    labelRecordStatus.Text = (cmData.Position + 1) + " / " + cmData.List.Count;
                }
            }
        }

        private void btnLayoutPrev_Click(object sender, EventArgs e)
        {
            if (cmLayout.Position > 0)
            {
                cmLayout.Position -= 1;
            }
            else if (cmLayout.List.Count > 0)
            {
                cmLayout.Position = cmLayout.List.Count - 1;
            }
        }

        private void btnLayoutNext_Click(object sender, EventArgs e)
        {
            if ((cmLayout.Position >= 0) && ((cmLayout.Position + 1) < cmLayout.List.Count))
            {
                cmLayout.Position += 1;
            }
            else if (cmLayout.List.Count > 0)
            {
                cmLayout.Position = 0;
            }
        }

        void CurrentLayoutRecordChanged(Object sender, EventArgs e)
        {
            if (cmLayout.Position >= 0)
            {
                DataRowView drv = (DataRowView)cmLayout.Current;
                if (drv.Row is LayoutDataset.RecordRow)
                {
                    lblLayoutStatus.Text = (cmLayout.Position + 1) + " / " + cmLayout.List.Count;
                }

                // TODO: sort if dataGridRecordDefinition.DataMember == "Record.RecordField"
                
            }
        }




	}
}
