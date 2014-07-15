using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.CodeDom.Compiler;
using Model;

public partial class MainForm : Form
{
    #region 01. Framework
    ///////////////////////////////////////////////////////////////////////////////
    // TextBox has no Ctrl+A by default, so add it
    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is TextBoxBase)
        {
            TextBoxBase control = (sender as TextBoxBase);
            if (control != null)
            {
                if (e.KeyCode == Keys.Tab)
                {
                    control.Text.Insert(control.SelectionStart, "\t");
                    //e.Handled = true;
                }
                else
                {
                    if (ModifierKeys == Keys.Control)
                    {
                        if (e.KeyCode == Keys.A)
                        {
                            control.SelectAll();
                        }
                        else if (e.KeyCode == Keys.F)
                        {
                            // Find dialog
                        }
                        else if (e.KeyCode == Keys.H)
                        {
                            // Replace dialog
                        }
                        else if (e.KeyCode == Keys.S)
                        {
                            // Save As dialog
                        }
                    }
                }
            }
        }
    }
    private void FixMicrosoft(object sender, KeyPressEventArgs e)
    {
        // stop annoying beep due to parent not having an AcceptButton
        if ((e.KeyChar == (char)Keys.Enter) || (e.KeyChar == (char)Keys.Escape))
        {
            e.Handled = true;
        }
        // enable Ctrl+A to SelectAll
        if ((ModifierKeys == Keys.Control) && (e.KeyChar == (char)1))
        {
            TextBoxBase control = (sender as TextBoxBase);
            if (control != null)
            {
                control.SelectAll();
                e.Handled = true;
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 02. Constants
    ///////////////////////////////////////////////////////////////////////////////
    private const int DEFAULT_WINDOW_WIDTH = 996;
    private const int DEFAULT_WINDOW_HEIGHT = 755;
    private const int DEFAULT_INFORMATION_BOX_TOP = 498;
    private const int DEFAULT_INFORMATION_PAGE_INDEX = 0;
    private const int DEFAULT_AUDIO_VOLUME = 1000;
    private const float DEFAULT_SILENCE_BETWEEN_VERSES = 0.0F; // in multiples of verses
    private const string VERSE_ADDRESS_TRANSLATION_SEPARATOR = " ";
    private const string VERSE_ADDRESS_MAIN_SEPARATOR = "\t";
    private const string SPACE_GAP = "     ";
    private const string CAPTION_SEPARATOR = " ► ";
    private const int AUTO_COLORIZED_PHRASES = 100;
    private const float DEFAULT_FONT_SIZE = 14.0F;
    private const float DEFAULT_TEXT_ZOOM_FACTOR = 1.0F;
    private const float DEFAULT_GRAPHICS_ZOOM_FACTOR = 1.0F;
    private const int SELECTON_SCOPE_TEXT_MAX_LENGTH = 32;  // for longer text, use elipses (...)
    private const int DEFAULT_RADIX = 10;                   // base for current numbering system. decimal by default.
    private const float DEFAULT_DPI_X = 96.0F;              // 100% = 96.0F,   125% = 120.0F,   150% = 144.0F
    private const string GRAMMAR_EDITION_INSTRUCTION =
    "    Start QuranCode with the CTRL key for the Grammar Edition or" + "\r\n" +
    "    Start QuranCode with the CTRL+SHIFT keys for the Research Edition.";
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 03. MainForm
    ///////////////////////////////////////////////////////////////////////////////
    private float m_dpi_x = DEFAULT_DPI_X;
    private string m_ini_filename = null;
    private Client m_client = null;
    private PermissionSet m_permission_set = null;
    private AboutBox m_about_box = null;
    private Assembly m_resources_assembly = null;
    private Font m_font = null;
    private string m_current_text = null;
    // constructor
    public MainForm()
    {
        InitializeComponent();
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

        m_about_box = new AboutBox();

        m_permission_set = new PermissionSet(PermissionState.None);
        //m_permission_set.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
        //m_permission_set.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
        //m_permission_set.AddPermission(new UIPermission(UIPermissionWindow.SafeSubWindows));
        //m_permission_set.AddPermission(new IsolatedStorageFilePermission(PermissionState.Unrestricted));

        using (Graphics graphics = this.CreateGraphics())
        {
            m_dpi_x = graphics.DpiX;
            if (m_dpi_x != DEFAULT_DPI_X)
            {
                if (m_dpi_x == 120.0F)
                {
                    // adjust GUI to fit into 125%
                    MainSplitContainer.Height = (int)(MainSplitContainer.Height / (m_dpi_x / DEFAULT_DPI_X)) + 96;
                    MainSplitContainer.SplitterDistance = 215;
                }
            }
        }

        FindByTextButton.Enabled = true;
        FindBySimilarityButton.Enabled = false;
        FindByNumbersButton.Enabled = false;
        FindByFrequencySumButton.Enabled = false;

        m_ini_filename = AppDomain.CurrentDomain.FriendlyName.Replace(".exe", ".ini");

        // must initialize here as it is null
        m_active_textbox = MainTextBox;

        this.MainTextBox.HideSelection = false; // this won't shift the text to the left
        //this.MainTextBox.HideSelection = true; // this WILL shift the text to the left
        this.SearchResultTextBox.HideSelection = false; // this won't shift the text to the left
        //this.SearchResultTextBox.HideSelection = true; // this WILL shift the text to the left

        this.MainTextBox.MouseWheel += new MouseEventHandler(MainTextBox_MouseWheel);
        this.SearchResultTextBox.MouseWheel += new MouseEventHandler(MainTextBox_MouseWheel);
        this.PictureBoxEx.MouseWheel += new MouseEventHandler(PictureBoxEx_MouseWheel);
    }
    private void MainForm_Load(object sender, EventArgs e)
    {
        bool splash_screen_done = false;
        try
        {
            SplashForm splash_form = new SplashForm();
            ThreadPool.QueueUserWorkItem(delegate
            {
                using (splash_form)
                {
                    splash_form.Show();
                    while (!splash_screen_done)
                    {
                        Application.DoEvents();
                    }
                    splash_form.Close();
                }
            }, null);

            splash_form.Version += " - " + Globals.SHORT_VERSION;

            InitializeControls();
            splash_form.Information = "Initializing server ...";
            if ((Globals.EDITION == Edition.Grammar) || (Globals.EDITION == Edition.Research))
            {
                splash_form.Information = "Loading grammar information ...";
            }
            splash_form.Progress = 30;
            Thread.Sleep(100);

            LoadApplicationFolders();
            string machine = "local";
            string username = "QuranCode";
            string password = "help yourself by helping others ...";
            string numerology_system_name = null;
            numerology_system_name = LoadNumerologySystemName();
            m_client = new Client(machine, username, password, numerology_system_name);

            if (m_client != null)
            {
                splash_form.Information = "Loading research methods ...";
                LoadResearchMethods();
                splash_form.Progress = 35;
                Thread.Sleep(100);

                splash_form.Information = "Loading translations ...";
                PopulateTranslatorsCheckedListBox();
                PopulateTranslatorComboBox();
                splash_form.Progress = 40;
                Thread.Sleep(100);

                splash_form.Information = "Loading tafseers ...";
                PopulateTafseerComboBox();
                splash_form.Progress = 55;
                Thread.Sleep(100);

                splash_form.Information = "Loading recitations ...";
                PopulateRecitationsCheckedListBox();
                PopulateReciterComboBox();
                splash_form.Progress = 60;
                Thread.Sleep(100);

                // must be done before LoadApplicationSettings
                splash_form.Information = "Loading user bookmarks ...";
                m_client.LoadBookmarks();
                UpdateBookmarkHistoryButtons();
                splash_form.Progress = 70;
                Thread.Sleep(100);

                splash_form.Information = "Loading application settings ...";
                LoadApplicationSettings();
                splash_form.Progress = 80;
                Thread.Sleep(100);

                splash_form.Information = "Loading numerology systems ...";
                PopulateNumerologySystemComboBox();
                NumerologySystemComboBox.SelectedIndexChanged -= new EventHandler(NumerologySystemComboBox_SelectedIndexChanged);
                NumerologySystemComboBox.SelectedItem = numerology_system_name;
                if (m_client.NumerologySystem != null)
                {
                    UpdateKeyboard(m_client.NumerologySystem.TextMode);
                    GoldenRatioOrderLabel.Visible = true;
                    GoldenRatioScopeLabel.Visible = true;
                }
                NumerologySystemComboBox.SelectedIndexChanged += new EventHandler(NumerologySystemComboBox_SelectedIndexChanged);
                splash_form.Progress = 85;
                Thread.Sleep(100);

                splash_form.Information = "Loading browse history ...";
                m_client.LoadHistoryItems();
                UpdateBrowseHistoryButtons();
                splash_form.Progress = 90;
                Thread.Sleep(100);

                splash_form.Information = "Loading help messages ...";
                if (m_client.HelpMessages != null)
                {
                    if (m_client.HelpMessages.Count > 0)
                    {
                        HelpMessageLabel.Text = m_client.HelpMessages[0];
                    }
                }
                splash_form.Progress = 95;
                Thread.Sleep(100);

                splash_form.Information = "Generating prime numbers ...";
                UpdateFindByTextControls();
                splash_form.Progress = 100;
                Thread.Sleep(100);

                if (ReciterComboBox.SelectedItem != null)
                {
                    RecitationGroupBox.Text = ReciterComboBox.SelectedItem.ToString();
                }
                ToolTip.SetToolTip(PlayerVolumeTrackBar, "Volume " + (m_audio_volume / (1000 / PlayerVolumeTrackBar.Maximum)).ToString() + "%");

                PopulateChapterSortComboBox();

                RefreshCurrentNumerologySystem();
                UpdateObjectListView();

                FavoriteNumerologySystemButton.Visible = (numerology_system_name != NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM);
                SaveAsFavoriteNumerologySystemLabel.Visible = (numerology_system_name != NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM);
                RestoreFavoriteNumerologySystemLabel.Visible = (NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM != NumerologySystem.PRIMALOGY_NUMERORLOGY_SYSTEM);

                // prepare before Shown
                this.ClientSplitContainer.SplitterDistance = m_information_box_top;
                this.TabControl.SelectedIndex = m_information_page_index;

                switch (Chapter.PinTheKey)
                {
                    case null:
                        PinTheKeyCheckBox.CheckState = CheckState.Indeterminate;
                        break;
                    case false:
                        PinTheKeyCheckBox.CheckState = CheckState.Unchecked;
                        break;
                    case true:
                        PinTheKeyCheckBox.CheckState = CheckState.Checked;
                        break;
                }

                ApplyLoadedWordWrapSettings(); // must be before DisplaySelection for Verse.IncludeNumber to take effect
                DisplaySelection(false);
                GrammarTextBox.Text = GRAMMAR_EDITION_INSTRUCTION;

                this.Activate(); // bring to foreground
            }
        }
        catch
        {
            // silence exception
        }
        finally
        {
            splash_screen_done = true;
            Thread.Sleep(100);  // prevent race-condition to allow splashform.Close()
        }
    }
    private void MainForm_Shown(object sender, EventArgs e)
    {
        MainTextBox.AlignToStart();
        MainTextBox.Focus();
    }
    private void MainForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            if (AutoCompleteListBox.Focused)
            {
                AutoCompleteListBox_DoubleClick(null, null);
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }
        if (e.KeyCode == Keys.Tab)
        {
            e.Handled = false;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HandleEscapeKeyPress(null, null);
            e.Handled = true; // stop annoying beep
        }
        else if (e.Control && (e.KeyCode == Keys.Down)) // Redo
        {
            RedoGotoVerse();
        }
        else if (e.Control && (e.KeyCode == Keys.Up)) // Undo
        {
            UndoGotoVerse();
        }
        else if (e.Control && (e.KeyCode == Keys.A)) // SelectAll chapters
        {
            if (ChaptersListBox.Focused)
            {
                List<int> indexes = new List<int>(ChaptersListBox.Items.Count);
                for (int i = 0; i < ChaptersListBox.Items.Count; i++)
                {
                    indexes.Add(i);
                }

                if (m_client != null)
                {
                    m_client.Selection = new Selection(m_client.Book, SelectionScope.Chapter, indexes);
                }
                CopySelectionToChaptersListBoxIndexes();

                DisplaySelection(true);
            }
            else
            {
                e.Handled = false;
            }
        }
        else
        {
            if (!e.Alt && !e.Control && !e.Shift)
            {
                if ((e.KeyCode == Keys.Back) || (e.KeyCode == Keys.BrowserBack))
                {
                    if (
                        ((m_active_textbox.Focused) && (m_readonly_mode)) ||
                        (BrowseHistoryBackwardButton.Focused) ||
                        (BrowseHistoryForwardButton.Focused) ||
                        (BrowseHistoryCounterLabel.Focused)
                       )
                    {
                        BrowseHistoryBackwardButton_Click(null, null);
                        e.Handled = true; // stop annoying beep
                    }
                }
                else if ((e.KeyCode == Keys.BrowserForward))
                {
                    if (
                        ((m_active_textbox.Focused) && (m_readonly_mode)) ||
                        (BrowseHistoryBackwardButton.Focused) ||
                        (BrowseHistoryForwardButton.Focused) ||
                        (BrowseHistoryCounterLabel.Focused)
                       )
                    {
                        BrowseHistoryForwardButton_Click(null, null);
                        e.Handled = true; // stop annoying beep
                    }
                }
                else if (e.KeyCode == Keys.F1)
                {
                }
                else if (e.KeyCode == Keys.F2)
                {
                }
                else if (e.KeyCode == Keys.F3)
                {
                    if (m_found_verses_displayed)
                    {
                        SelectNextFindMatch();
                    }
                    else
                    {
                        NextBookmarkButton_Click(null, null);
                    }
                }
                else if (e.KeyCode == Keys.F4)
                {
                }
                else if (e.KeyCode == Keys.F5)
                {
                    if (m_active_textbox.Focused)
                    {
                        this.Cursor = Cursors.WaitCursor;
                        try
                        {
                            DoFindExactText(m_active_textbox);
                        }
                        finally
                        {
                            this.Cursor = Cursors.Default;
                        }
                    }
                }
                else if (e.KeyCode == Keys.F6)
                {
                    if (m_active_textbox.Focused)
                    {
                        this.Cursor = Cursors.WaitCursor;
                        try
                        {
                            DoFindSimilarWords(m_active_textbox);
                        }
                        finally
                        {
                            this.Cursor = Cursors.Default;
                        }
                    }
                }
                else if (e.KeyCode == Keys.F7)
                {
                    if (m_active_textbox.Focused)
                    {
                        this.Cursor = Cursors.WaitCursor;
                        try
                        {
                            DoFindRelatedWords(m_active_textbox);
                        }
                        finally
                        {
                            this.Cursor = Cursors.Default;
                        }
                    }
                }
                else if (e.KeyCode == Keys.F8)
                {
                }
                else if (e.KeyCode == Keys.F9)
                {
                    if (m_active_textbox.Focused)
                    {
                        this.Cursor = Cursors.WaitCursor;
                        try
                        {
                            DoFindSameValue(m_active_textbox);
                        }
                        finally
                        {
                            this.Cursor = Cursors.Default;
                        }
                    }
                }
                else if (e.KeyCode == Keys.F10)
                {
                }
                else if (e.KeyCode == Keys.F11)
                {
                    ToggleWordWrap(); // add/remove Verse.EndMark, wrap/unwrap and redisplay
                }
                else if (e.KeyCode == Keys.F12)
                {
                    if (this.WindowState != FormWindowState.Maximized)
                    {
                        this.WindowState = FormWindowState.Maximized;
                        this.FormBorderStyle = FormBorderStyle.None;
                    }
                    else
                    {
                        this.WindowState = FormWindowState.Normal;
                        this.FormBorderStyle = FormBorderStyle.Sizable;
                    }
                }
                else
                {
                    // let editor process key
                }
            }
            else if (!e.Alt && !e.Control && e.Shift)
            {
                if ((e.KeyCode == Keys.Back) || (e.KeyCode == Keys.BrowserBack))
                {
                    if (
                        ((m_active_textbox.Focused) && (m_readonly_mode)) ||
                        (BrowseHistoryBackwardButton.Focused) ||
                        (BrowseHistoryForwardButton.Focused) ||
                        (BrowseHistoryCounterLabel.Focused)
                       )
                    {
                        BrowseHistoryForwardButton_Click(null, null);
                        e.Handled = true; // stop annoying beep
                    }
                }
                else if ((e.KeyCode == Keys.BrowserForward))
                {
                    if (
                        ((m_active_textbox.Focused) && (m_readonly_mode)) ||
                        (BrowseHistoryBackwardButton.Focused) ||
                        (BrowseHistoryForwardButton.Focused) ||
                        (BrowseHistoryCounterLabel.Focused)
                       )
                    {
                        BrowseHistoryBackwardButton_Click(null, null);
                        e.Handled = true; // stop annoying beep
                    }
                }
                else if (e.KeyCode == Keys.F1)
                {
                }
                else if (e.KeyCode == Keys.F2)
                {
                }
                else if (e.KeyCode == Keys.F3)
                {
                    if (m_found_verses_displayed)
                    {
                        SelectPreviousFindMatch();
                    }
                    else
                    {
                        PreviousBookmarkButton_Click(null, null);
                    }
                }
                else if (e.KeyCode == Keys.F4)
                {
                }
                else if (e.KeyCode == Keys.F5)
                {
                }
                else if (e.KeyCode == Keys.F6)
                {
                }
                else if (e.KeyCode == Keys.F7)
                {
                }
                else if (e.KeyCode == Keys.F8)
                {
                }
                else if (e.KeyCode == Keys.F9)
                {
                }
                else if (e.KeyCode == Keys.F10)
                {
                }
                else if (e.KeyCode == Keys.F11)
                {
                }
                else if (e.KeyCode == Keys.F12)
                {
                }
                else
                {
                    // let editor process key
                }
            }
        }
    }
    private void MainForm_Resize(object sender, EventArgs e)
    {
        if (ScriptTextBox.Visible) return;

        if (PictureBoxPanel.Visible)
        {
            DisplayCurrentPage();
        }
        else if (PictureBoxEx.Visible)
        {
            RedrawCurrentGraph();
        }
        else
        {
            if (m_mp3player != null)
            {
                if (m_mp3player.Closed)
                {
                    if (m_active_textbox.SelectionLength == 0)
                    {
                        Verse verse = GetCurrentVerse();
                        m_active_textbox.AlignToLineStart();
                        GotoVerse(verse);
                    }
                }
            }
        }
    }
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        //// prevent user from closing from the X close button
        //if (e.CloseReason == CloseReason.UserClosing)
        //{
        //    e.Cancel = true;
        //    this.Visible = false;
        //}
    }
    private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        CloseApplication();
    }
    private void CloseApplication()
    {
        if (m_client != null)
        {
            try
            {
                // save current note (if any)
                BookmarkTextBox_Leave(null, null);

                // save bookmarks of all texts of current client
                m_client.SaveBookmarks();

                // save user history
                m_client.SaveHistoryItems();

                // save application options
                SaveApplicationOptions();
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }
        }
    }
    private void HandleEscapeKeyPress(object sender, KeyEventArgs e)
    {
        if (NumerologySystemComboBox.DroppedDown)
        {
            NumerologySystemComboBox.DroppedDown = false;
        }
        else if (ResearchMethodsComboBox.DroppedDown)
        {
            ResearchMethodsComboBox.DroppedDown = false;
        }
        else if (ChapterSortComboBox.DroppedDown)
        {
            ChapterSortComboBox.DroppedDown = false;
        }
        else if (TranslatorComboBox.DroppedDown)
        {
            TranslatorComboBox.DroppedDown = false;
        }
        else if (TafseerComboBox.DroppedDown)
        {
            TafseerComboBox.DroppedDown = false;
        }
        else if (ReciterComboBox.DroppedDown)
        {
            ReciterComboBox.DroppedDown = false;
        }
        else if (BookmarkTextBox.Focused)
        {
            BookmarkTextBox.Text = null;
        }
        else if (TranslatorsCheckedListBox.Visible)
        {
            TranslationsCancelSettingsLabel_Click(null, null);
        }
        else if (RecitationsDownloadGroupBox.Visible)
        {
            RecitationsCancelSettingsLabel_Click(null, null);
        }
        else if (TranslationTextBox.Focused)
        {
            DisplayTranslations(new List<Verse>(m_translated_verses));
        }
        else if (PictureBoxEx.Visible)
        {
            HideDrawPictureBox();
        }
        else if (PictureBoxPanel.Visible)
        {
            PictureBoxPanel.Visible = false;
            PictureBoxPanel.SendToBack();
        }
        else if (ScriptTextBox.Visible)
        {
            CloseScriptLabel_Click(null, null);
        }
        else if ((!LetterValuesPanel.Focused) && (LetterValuesPanel.Visible))
        {
            LetterValuesPanel.Visible = false;
        }
        else
        {
            if (m_client.NumerologySystem.TextMode.Contains("Images"))
            {
                PictureBoxPanel.BringToFront();
                PictureBoxPanel.Visible = true;
                DisplayCurrentPage();
            }
            else
            {
                SwitchActiveTextBox();
            }
        }

        UpdateHeaderLabel();
    }
    private void UpdateVersesToCurrentTextMode(ref List<Verse> verses)
    {
        List<Verse> temp = new List<Verse>();
        foreach (Verse verse in verses)
        {
            temp.Add(m_client.Book.Verses[verse.Number - 1]);
        }
        verses = temp;
    }
    private void LoadApplicationSettings()
    {
        try
        {
            PopulateChapterComboBox();
            PopulateChaptersListBox();
            PopulateReciterComboBox();
            PopulateTranslatorComboBox();

            LoadApplicationOptions();

            RadixValueLabel.Text = m_radix.ToString();

            // SetFontSize updates size BUT loses the font face in Right-to-left
            //SetFontSize(m_font_size);
            MainTextBox.ZoomFactor = m_text_zoom_factor;
            SearchResultTextBox.ZoomFactor = m_text_zoom_factor;

            PlayerVolumeTrackBar.Value = m_audio_volume / (1000 / PlayerVolumeTrackBar.Maximum);
            PlayerSilenceTrackBar.Value = (int)(m_silence_between_verses * (PlayerSilenceTrackBar.Maximum / 2));
        }
        catch (Exception ex)
        {
            while (ex != null)
            {
                //Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, Application.ProductName);
                ex = ex.InnerException;
            }
        }
    }
    private void LoadApplicationFolders()
    {
        if (File.Exists(m_ini_filename))
        {
            try
            {
                using (StreamReader reader = File.OpenText(m_ini_filename))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            switch (parts[0])
                            {
                                // [Folders]
                                case "NumbersFolder":
                                    {
                                        Numbers.NUMBERS_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "FontsFolder":
                                    {
                                        Globals.FONTS_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "ImagesFolder":
                                    {
                                        Globals.IMAGES_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "DataFolder":
                                    {
                                        Globals.DATA_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "AudioFolder":
                                    {
                                        Globals.AUDIO_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "TranslationsFolder":
                                    {
                                        Globals.TRANSLATIONS_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "TafseersFolder":
                                    {
                                        Globals.TAFSEERS_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "RulesFolder":
                                    {
                                        Globals.RULES_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "ValuesFolder":
                                    {
                                        Globals.VALUES_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "StatisticsFolder":
                                    {
                                        Globals.STATISTICS_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "ResearchFolder":
                                    {
                                        Globals.RESEARCH_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "DrawingsFolder":
                                    {
                                        Globals.DRAWINGS_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "BookmarksFolder":
                                    {
                                        Globals.BOOKMARKS_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "HistoryFolder":
                                    {
                                        Globals.HISTORY_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                                case "HelpFolder":
                                    {
                                        Globals.HELP_FOLDER = parts[1].Replace("\\", "/").Trim();
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // silence Parse exceptions
                // continue with next INI entry
            }
        }
    }
    private string LoadNumerologySystemName()
    {
        //string result = NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM;
        string result = "Original_Alphabet_Primes"; // for novice users
        if (File.Exists(m_ini_filename))
        {
            using (StreamReader reader = File.OpenText(m_ini_filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        if (parts[0] == "NumerologySystem")
                        {
                            try
                            {
                                result = parts[1].Trim();
                            }
                            catch
                            {
                                result = NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM;
                            }
                            break;
                        }
                    }
                }
            }
        }
        return result;
    }
    private void LoadApplicationOptions()
    {
        if (File.Exists(m_ini_filename))
        {
            try
            {
                if (m_client != null)
                {
                    // Selection.Scope and Selection.Indexes are immutable/readonly so create a new Selection to replace m_client.Selection 
                    SelectionScope selection_scope = SelectionScope.Book;
                    List<int> selection_indexes = new List<int>();

                    using (StreamReader reader = File.OpenText(m_ini_filename))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            string[] parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                switch (parts[0])
                                {
                                    // [Window]
                                    case "Top":
                                        {
                                            try
                                            {
                                                this.Top = int.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                this.Top = 100;
                                            }
                                        }
                                        break;
                                    case "Left":
                                        {
                                            try
                                            {
                                                this.Left = int.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                this.Left = 100;
                                            }
                                        }
                                        break;
                                    case "Width":
                                        {
                                            try
                                            {
                                                this.Width = int.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                this.Width = DEFAULT_WINDOW_WIDTH;
                                            }
                                        }
                                        break;
                                    case "Height":
                                        {
                                            try
                                            {
                                                this.Height = int.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                this.Height = DEFAULT_WINDOW_HEIGHT;
                                            }
                                        }
                                        break;
                                    case "InformationBoxTop":
                                        {
                                            try
                                            {
                                                m_information_box_top = int.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                m_information_box_top = DEFAULT_INFORMATION_BOX_TOP;
                                            }
                                        }
                                        break;
                                    case "InformationPageIndex":
                                        {
                                            try
                                            {
                                                m_information_page_index = int.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                m_information_page_index = DEFAULT_INFORMATION_PAGE_INDEX;
                                            }
                                        }
                                        break;
                                    // [Numbers]
                                    case "Radix":
                                        {
                                            try
                                            {
                                                m_radix = int.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                m_radix = DEFAULT_RADIX;
                                            }
                                            RadixValueLabel.Text = m_radix.ToString();
                                        }
                                        break;
                                    case "FavoriteNumerologySystem":
                                        {
                                            try
                                            {
                                                NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM = parts[1].Trim();
                                            }
                                            catch
                                            {
                                                NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM = NumerologySystem.PRIMALOGY_NUMERORLOGY_SYSTEM;
                                            }
                                        }
                                        break;
                                    case "NumerologySystem":
                                        {
                                            // already read separately by LoadNumerologySystemName method
                                            // continue with other NumerologySystem parameters
                                            if (m_client.NumerologySystem != null)
                                            {
                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.Scope = (NumerologySystemScope)Enum.Parse(typeof(NumerologySystemScope), parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToLetterLNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToLetterWNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToLetterVNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToLetterCNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToLetterLDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToLetterWDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToLetterVDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToLetterCDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToWordWNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToWordVNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToWordCNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToWordWDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToWordVDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToWordCDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToVerseVNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToVerseCNumber = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToVerseVDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToVerseCDistance = bool.Parse(parts[1].Trim());
                                                }

                                                line = reader.ReadLine();
                                                parts = line.Split('=');
                                                if (parts.Length == 2)
                                                {
                                                    m_client.NumerologySystem.AddToChapterCNumber = bool.Parse(parts[1].Trim());
                                                }
                                            }
                                        }
                                        break;
                                    // [Text]
                                    case "MainTextWordWrap":
                                        {
                                            try
                                            {
                                                m_word_wrap_main_textbox = bool.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                m_word_wrap_main_textbox = false;
                                            }
                                        }
                                        break;
                                    case "SearchTextWordWrap":
                                        {
                                            try
                                            {
                                                m_word_wrap_search_textbox = bool.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                m_word_wrap_search_textbox = false;
                                            }
                                        }
                                        break;
                                    case "SelectionScope":
                                        {
                                            try
                                            {
                                                selection_scope = (SelectionScope)Enum.Parse(typeof(SelectionScope), parts[1].Trim());
                                            }
                                            catch
                                            {
                                                selection_scope = SelectionScope.Chapter;
                                            }
                                        }
                                        break;
                                    case "SelectionIndexes":
                                        {
                                            try
                                            {
                                                string part = parts[1].Trim();
                                                string[] sub_parts = part.Split('+');
                                                selection_indexes.Clear();
                                                for (int i = 0; i < sub_parts.Length; i++)
                                                {
                                                    try
                                                    {
                                                        int index = int.Parse(sub_parts[i].Trim()) - 1;
                                                        selection_indexes.Add(index);
                                                    }
                                                    catch
                                                    {
                                                        // skip invalid index
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                selection_indexes.Add(0);
                                            }
                                        }
                                        break;
                                    case "TextZoomFactor":
                                        {
                                            try
                                            {
                                                m_text_zoom_factor = float.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                m_text_zoom_factor = DEFAULT_TEXT_ZOOM_FACTOR;
                                            }
                                        }
                                        break;
                                    case "Translator":
                                        {
                                            try
                                            {
                                                int index = int.Parse(parts[1].Trim());
                                                if (index < this.TranslatorComboBox.Items.Count)
                                                {
                                                    this.TranslatorComboBox.SelectedIndex = index;
                                                }
                                                else
                                                {
                                                    this.TranslatorComboBox.SelectedItem = -1;
                                                }
                                            }
                                            catch
                                            {
                                                if (this.TranslatorComboBox.Items.Count >= 3)
                                                {
                                                    this.TranslatorComboBox.SelectedItem = m_client.Book.TranslationInfos[Client.DEFAULT_NEW_TRANSLATION].Name;
                                                }
                                                else
                                                {
                                                    this.TranslatorComboBox.SelectedIndex = -1;
                                                }
                                            }
                                        }
                                        break;
                                    case "Tafseer":
                                        {
                                            try
                                            {
                                                int index = int.Parse(parts[1].Trim());
                                                if (index < this.TafseerComboBox.Items.Count)
                                                {
                                                    this.TafseerComboBox.SelectedIndex = index;
                                                }
                                                else
                                                {
                                                    this.TafseerComboBox.SelectedIndex = -1;
                                                }
                                            }
                                            catch
                                            {
                                                this.TafseerComboBox.SelectedIndex = -1;
                                            }
                                        }
                                        break;
                                    // [Audio]
                                    case "Volume":
                                        {
                                            try
                                            {
                                                m_audio_volume = int.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                m_audio_volume = DEFAULT_AUDIO_VOLUME;
                                            }
                                        }
                                        break;
                                    case "SilenceBetweenVerses":
                                        {
                                            try
                                            {
                                                m_silence_between_verses = float.Parse(parts[1].Trim());
                                            }
                                            catch
                                            {
                                                m_silence_between_verses = DEFAULT_SILENCE_BETWEEN_VERSES;
                                            }
                                        }
                                        break;
                                    case "Reciter":
                                        {
                                            try
                                            {
                                                int index = int.Parse(parts[1].Trim());
                                                if (index < this.ReciterComboBox.Items.Count)
                                                {
                                                    this.ReciterComboBox.SelectedIndex = index;
                                                }
                                                else
                                                {
                                                    this.ReciterComboBox.SelectedItem = -1;
                                                }
                                            }
                                            catch
                                            {
                                                this.ReciterComboBox.SelectedItem = m_client.Book.RecitationInfos[Client.DEFAULT_RECITATION].Reciter;
                                            }
                                        }
                                        break;
                                    // [Downloads]
                                    case "TranslationUrlPrefix":
                                        {
                                            try
                                            {
                                                TranslationInfo.UrlPrefix = parts[1].Trim();
                                            }
                                            catch
                                            {
                                                TranslationInfo.UrlPrefix = TranslationInfo.DEFAULT_URL_PREFIX;
                                            }
                                        }
                                        break;
                                    case "TranslationFileType":
                                        {
                                            try
                                            {
                                                TranslationInfo.FileType = parts[1].Trim();
                                            }
                                            catch
                                            {
                                                TranslationInfo.FileType = TranslationInfo.DEFAULT_FILE_TYPE;
                                            }
                                        }
                                        break;
                                    case "TranslationIconUrlPrefix":
                                        {
                                            try
                                            {
                                                TranslationInfo.IconUrlPrefix = parts[1].Trim();
                                            }
                                            catch
                                            {
                                                TranslationInfo.IconUrlPrefix = TranslationInfo.DEFAULT_ICON_URL_PREFIX;
                                            }
                                        }
                                        break;
                                    case "RecitationUrlPrefix":
                                        {
                                            try
                                            {
                                                RecitationInfo.UrlPrefix = parts[1].Trim();
                                            }
                                            catch
                                            {
                                                RecitationInfo.UrlPrefix = RecitationInfo.DEFAULT_URL_PREFIX;
                                            }
                                        }
                                        break;
                                    case "RecitationFileType":
                                        {
                                            try
                                            {
                                                RecitationInfo.FileType = parts[1].Trim();
                                            }
                                            catch
                                            {
                                                RecitationInfo.FileType = RecitationInfo.DEFAULT_FILE_TYPE;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }

                    m_client.Selection = new Selection(m_client.Book, selection_scope, selection_indexes);
                }
            }
            catch
            {
                // silence Parse exceptions
                // continue with next INI entry
            }
        }
        else // first Application launch
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = DEFAULT_WINDOW_WIDTH;
            this.Height = DEFAULT_WINDOW_HEIGHT;

            if (this.ChapterComboBox.Items.Count > 1)
            {
                this.ChapterComboBox.SelectedIndex = 0;
            }

            if (this.TranslatorComboBox.Items.Count >= 3)
            {
                this.TranslatorComboBox.SelectedItem = m_client.Book.TranslationInfos[Client.DEFAULT_NEW_TRANSLATION].Name;
            }

            if (this.TafseerComboBox.Items.Count > 0)
            {
                this.TafseerComboBox.SelectedItem = Client.DEFAULT_TAFSEER.Replace("/", " - ");
            }

            if (m_client != null)
            {
                try
                {
                    m_client.Selection = new Selection(m_client.Book, SelectionScope.Chapter, new List<int>() { 0 });
                }
                catch
                {
                    // log exception
                }
            }
        }
    }
    private void SaveApplicationOptions()
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(m_ini_filename, false, Encoding.Unicode))
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }

                writer.WriteLine("[Window]");
                if (this.WindowState == FormWindowState.Minimized)
                {
                    // restore or width/height will be saved as 0
                    writer.WriteLine("Top" + "=" + (Screen.PrimaryScreen.WorkingArea.Height - DEFAULT_WINDOW_HEIGHT) / 2);
                    writer.WriteLine("Left" + "=" + (Screen.PrimaryScreen.WorkingArea.Width - DEFAULT_WINDOW_WIDTH) / 2);
                    writer.WriteLine("Width" + "=" + DEFAULT_WINDOW_WIDTH);
                    writer.WriteLine("Height" + "=" + DEFAULT_WINDOW_HEIGHT);
                    writer.WriteLine("InformationBoxTop" + "=" + DEFAULT_INFORMATION_BOX_TOP);
                    writer.WriteLine("InformationPageIndex" + "=" + DEFAULT_INFORMATION_PAGE_INDEX);
                }
                else
                {
                    writer.WriteLine("Top" + "=" + this.Top);
                    writer.WriteLine("Left" + "=" + this.Left);
                    writer.WriteLine("Width" + "=" + this.Width);
                    writer.WriteLine("Height" + "=" + this.Height);
                    writer.WriteLine("InformationBoxTop" + "=" + m_information_box_top);
                    writer.WriteLine("InformationPageIndex" + "=" + m_information_page_index);
                }
                writer.WriteLine();

                writer.WriteLine("[Numbers]");
                writer.WriteLine("Radix" + "=" + m_radix);
                writer.WriteLine("FavoriteNumerologySystem" + "=" + NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM);
                writer.WriteLine("NumerologySystem" + "=" + m_client.NumerologySystem.Name);
                writer.WriteLine("NumerologySystemScope" + "=" + m_client.NumerologySystem.Scope.ToString());
                writer.WriteLine("AddToLetterLNumber" + "=" + m_client.NumerologySystem.AddToLetterLNumber.ToString());
                writer.WriteLine("AddToLetterWNumber" + "=" + m_client.NumerologySystem.AddToLetterWNumber.ToString());
                writer.WriteLine("AddToLetterVNumber" + "=" + m_client.NumerologySystem.AddToLetterVNumber.ToString());
                writer.WriteLine("AddToLetterCNumber" + "=" + m_client.NumerologySystem.AddToLetterCNumber.ToString());
                writer.WriteLine("AddToLetterLDistance" + "=" + m_client.NumerologySystem.AddToLetterLDistance.ToString());
                writer.WriteLine("AddToLetterWDistance" + "=" + m_client.NumerologySystem.AddToLetterWDistance.ToString());
                writer.WriteLine("AddToLetterVDistance" + "=" + m_client.NumerologySystem.AddToLetterVDistance.ToString());
                writer.WriteLine("AddToLetterCDistance" + "=" + m_client.NumerologySystem.AddToLetterCDistance.ToString());
                writer.WriteLine("AddToWordWNumber" + "=" + m_client.NumerologySystem.AddToWordWNumber.ToString());
                writer.WriteLine("AddToWordVNumber" + "=" + m_client.NumerologySystem.AddToWordVNumber.ToString());
                writer.WriteLine("AddToWordCNumber" + "=" + m_client.NumerologySystem.AddToWordCNumber.ToString());
                writer.WriteLine("AddToWordWDistance" + "=" + m_client.NumerologySystem.AddToWordWDistance.ToString());
                writer.WriteLine("AddToWordVDistance" + "=" + m_client.NumerologySystem.AddToWordVDistance.ToString());
                writer.WriteLine("AddToWordCDistance" + "=" + m_client.NumerologySystem.AddToWordCDistance.ToString());
                writer.WriteLine("AddToVerseVNumber" + "=" + m_client.NumerologySystem.AddToVerseVNumber.ToString());
                writer.WriteLine("AddToVerseCNumber" + "=" + m_client.NumerologySystem.AddToVerseCNumber.ToString());
                writer.WriteLine("AddToVerseVDistance" + "=" + m_client.NumerologySystem.AddToVerseVDistance.ToString());
                writer.WriteLine("AddToVerseCDistance" + "=" + m_client.NumerologySystem.AddToVerseCDistance.ToString());
                writer.WriteLine("AddToChapterCNumber" + "=" + m_client.NumerologySystem.AddToChapterCNumber.ToString());
                writer.WriteLine();

                writer.WriteLine("[Text]");
                writer.WriteLine("MainTextWordWrap" + "=" + m_word_wrap_main_textbox);
                writer.WriteLine("SearchTextWordWrap" + "=" + m_word_wrap_search_textbox);
                if (m_client != null)
                {
                    if (m_client.Selection != null)
                    {
                        writer.WriteLine("SelectionScope" + "=" + (int)m_client.Selection.Scope);
                        StringBuilder str = new StringBuilder("SelectionIndexes=");
                        if (m_client.Selection.Indexes.Count > 0)
                        {
                            foreach (int index in m_client.Selection.Indexes)
                            {
                                str.Append((index + 1).ToString() + "+");
                            }
                            if (str.Length > 1)
                            {
                                str.Remove(str.Length - 1, 1);
                            }
                        }
                        writer.WriteLine(str);
                    }
                }
                writer.WriteLine("TextZoomFactor" + "=" + m_text_zoom_factor);
                writer.WriteLine("Translator" + "=" + this.TranslatorComboBox.SelectedIndex);
                writer.WriteLine("Tafseer" + "=" + this.TafseerComboBox.SelectedIndex);
                writer.WriteLine();

                writer.WriteLine("[Audio]");
                writer.WriteLine("Volume" + "=" + m_audio_volume);
                writer.WriteLine("SilenceBetweenVerses" + "=" + m_silence_between_verses);
                writer.WriteLine("Reciter" + "=" + this.ReciterComboBox.SelectedIndex);
                writer.WriteLine();

                writer.WriteLine("[Downloads]");
                writer.WriteLine("TranslationUrlPrefix" + "=" + TranslationInfo.UrlPrefix);
                writer.WriteLine("TranslationFileType" + "=" + TranslationInfo.FileType);
                writer.WriteLine("TranslationIconUrlPrefix" + "=" + TranslationInfo.IconUrlPrefix);
                writer.WriteLine("RecitationUrlPrefix" + "=" + RecitationInfo.UrlPrefix);
                writer.WriteLine("RecitationFileType" + "=" + RecitationInfo.FileType);
                writer.WriteLine();

                writer.WriteLine("[Folders]");
                writer.WriteLine("NumbersFolder=" + Numbers.NUMBERS_FOLDER);
                writer.WriteLine("FontsFolder=" + Globals.FONTS_FOLDER);
                writer.WriteLine("ImagesFolder=" + Globals.IMAGES_FOLDER);
                writer.WriteLine("DataFolder=" + Globals.DATA_FOLDER);
                writer.WriteLine("AudioFolder=" + Globals.AUDIO_FOLDER);
                writer.WriteLine("TranslationsFolder=" + Globals.TRANSLATIONS_FOLDER);
                writer.WriteLine("TafseersFolder=" + Globals.TAFSEERS_FOLDER);
                writer.WriteLine("RulesFolder=" + Globals.RULES_FOLDER);
                writer.WriteLine("ValuesFolder=" + Globals.VALUES_FOLDER);
                writer.WriteLine("StatisticsFolder=" + Globals.STATISTICS_FOLDER);
                writer.WriteLine("ResearchFolder=" + Globals.RESEARCH_FOLDER);
                writer.WriteLine("DrawingsFolder=" + Globals.DRAWINGS_FOLDER);
                writer.WriteLine("BookmarksFolder=" + Globals.BOOKMARKS_FOLDER);
                writer.WriteLine("HistoryFolder=" + Globals.HISTORY_FOLDER);
                writer.WriteLine("HelpFolder=" + Globals.HELP_FOLDER);
            }
        }
        catch
        {
            // silence IO errors in case running from read-only media (CD/DVD)
        }
    }
    private void InitializeControls()
    {
        VersionLabel.Text = Globals.SHORT_VERSION;

        EditLetterValuesLabel.Visible = true;

        if (BookmarkTextBox.Text.Length > 0)
        {
            m_note_writing_instruction = BookmarkTextBox.Text;
        }

        // install font for first time
        InstallFont();

        // use Right-Click for going to Related Words instead of showing context menu
        RegisterContextMenu(MainTextBox);
        RegisterContextMenu(SearchResultTextBox);
        RegisterContextMenu(TranslationTextBox);
        RegisterContextMenu(RelatedWordsTextBox);
        RegisterContextMenu(GrammarTextBox);
        RegisterContextMenu(UserTextTextBox);
        RegisterContextMenu(FindByTextTextBox);
        RegisterContextMenu(FindByFrequencyPhraseTextBox);
        RegisterContextMenu(ValueTextBox);
        RegisterContextMenu(NthPrimeTextBox);
        RegisterContextMenu(NthAdditivePrimeTextBox);
        RegisterContextMenu(NthPurePrimeTextBox);

        PageNumericUpDown.Minimum = Page.MIN_NUMBER;
        PageNumericUpDown.Maximum = Page.MAX_NUMBER;
        StationNumericUpDown.Minimum = Station.MIN_NUMBER;
        StationNumericUpDown.Maximum = Station.MAX_NUMBER;
        PartNumericUpDown.Minimum = Part.MIN_NUMBER;
        PartNumericUpDown.Maximum = Part.MAX_NUMBER;
        GroupNumericUpDown.Minimum = Model.Group.MIN_NUMBER;
        GroupNumericUpDown.Maximum = Model.Group.MAX_NUMBER;
        QuarterNumericUpDown.Minimum = Quarter.MIN_NUMBER;
        QuarterNumericUpDown.Maximum = Quarter.MAX_NUMBER;
        BowingNumericUpDown.Minimum = Bowing.MIN_NUMBER;
        BowingNumericUpDown.Maximum = Bowing.MAX_NUMBER;
        PageNumericUpDown.Minimum = Page.MIN_NUMBER;
        PageNumericUpDown.Maximum = Page.MAX_NUMBER;
        VerseNumericUpDown.Minimum = Verse.MIN_NUMBER;
        VerseNumericUpDown.Maximum = Verse.MAX_NUMBER;

        SetupToolTips();
    }
    private void SetupToolTips()
    {
        this.ToolTip.SetToolTip(this.WebsiteLinkLabel, "اللهُمَّ صَلِّ على مُحَمَّدٍ وءالِ مُحَمَّدٍ");
        this.ToolTip.SetToolTip(this.VersionLabel, "Version");
        this.ToolTip.SetToolTip(this.EncryptedQuranLinkLabel, "Encrypted Quran");
        this.ToolTip.SetToolTip(this.PlayerPreviousLabel, "Previous verse");
        this.ToolTip.SetToolTip(this.PlayerPlayLabel, "Play");
        this.ToolTip.SetToolTip(this.PlayerNextLabel, "Next verse");
        this.ToolTip.SetToolTip(this.PlayerStopLabel, "Stop");
        this.ToolTip.SetToolTip(this.PlayerRepeatLabel, "Repeat verse");
        this.ToolTip.SetToolTip(this.PlayerRepeatSelectionLabel, "Repeat selection");
        this.ToolTip.SetToolTip(this.PlayerMuteLabel, "Mute");
        this.ToolTip.SetToolTip(this.PlayerSilenceTrackBar, "Silence between verses");
        this.ToolTip.SetToolTip(this.VerseByVerseNumberLabel, "Go to verse number = current value");
        this.ToolTip.SetToolTip(this.VerseByLetterNumberLabel, "Go to verse with letter number = current value");
        this.ToolTip.SetToolTip(this.VerseByWordNumberLabel, "Go to verse with word number = current value");
        this.ToolTip.SetToolTip(this.UndoValueNavigationLabel, "Back");
        this.ToolTip.SetToolTip(this.RedoValueNavigationLabel, "Forward");
        this.ToolTip.SetToolTip(this.NumerologySystemComboBox, "Letter valuation system  نظام تقييم الحروف");
        this.ToolTip.SetToolTip(this.VersesTextBox, "Verses in selection  عدد الءايات");
        this.ToolTip.SetToolTip(this.WordsTextBox, "Words in selection  عدد الكلمات");
        this.ToolTip.SetToolTip(this.LettersTextBox, "Letters in selection  عدد الحروف");
        this.ToolTip.SetToolTip(this.VerseNumberSumTextBox, "Sum of verse numbers  مجموع أرقام الءايات");
        this.ToolTip.SetToolTip(this.WordNumberSumTextBox, "Sum of word numbers  مجموع أرقام الكلمات");
        this.ToolTip.SetToolTip(this.LetterNumberSumTextBox, "Sum of letter numbers  مجموع أرقام الحروف");
        this.ToolTip.SetToolTip(this.ValueTextBox, "Value of selection  القيمة حسب نظام تقييم الحروف الحالي");
        this.ToolTip.SetToolTip(this.FindScopeLabel, "Entire Book");
        this.ToolTip.SetToolTip(this.FindByTextExactSearchTypeLabel, "search for exact word or phrase");
        this.ToolTip.SetToolTip(this.FindByTextProximitySearchTypeLabel, "search for any or all words in any order");
        this.ToolTip.SetToolTip(this.FindByTextRootSearchTypeLabel, "search for all words with given root(s)");
        this.ToolTip.SetToolTip(this.FindBySimilarityBookSourceLabel, "find similar verses to all verses in the Quran");
        this.ToolTip.SetToolTip(this.FindBySimilarityVerseSourceLabel, "find similar verses to current verse");
        this.ToolTip.SetToolTip(this.FindBySimilarityPercentageTrackBar, "similarity percentage");
        this.ToolTip.SetToolTip(this.FindBySimilaritySimilarWordsRadioButton, "verses with similar words");
        this.ToolTip.SetToolTip(this.FindBySimilaritySimilarTextRadioButton, "verses with similar letters");
        this.ToolTip.SetToolTip(this.FindBySimilaritySimilarFirstHalfRadioButton, "verses with similar words in first half");
        this.ToolTip.SetToolTip(this.FindBySimilaritySimilarLastHalfRadioButton, "verses with similar words in last half");
        this.ToolTip.SetToolTip(this.FindBySimilaritySimilarFirstWordRadioButton, "verses with similar first word");
        this.ToolTip.SetToolTip(this.FindBySimilaritySimilarLastWordRadioButton, "verses with similar last word");
        this.ToolTip.SetToolTip(this.FindByTextAtEndRadioButton, "find at the end of the verse");
        this.ToolTip.SetToolTip(this.FindByTextAtStartRadioButton, "find at the beginning of the verse");
        this.ToolTip.SetToolTip(this.FindByTextAnywhereRadioButton, "find anywhere in the verse");
        this.ToolTip.SetToolTip(this.FindByTextAtMiddleRadioButton, "find anywhere in the middle of the verse");
        this.ToolTip.SetToolTip(this.FindByTextMultiplicityCheckBox, "find verses with given number of word repetitions");
        this.ToolTip.SetToolTip(this.FindByTextAllWordsRadioButton, "find verses with all words in any order");
        this.ToolTip.SetToolTip(this.FindByTextAnyWordRadioButton, "find verses with at least one word");
        this.ToolTip.SetToolTip(this.DigitSumTextBox, "Value digit sum");
        this.ToolTip.SetToolTip(this.DigitalRootTextBox, "Value digital root");
        this.ToolTip.SetToolTip(this.NthPrimeTextBox, "Find prime by index");
        this.ToolTip.SetToolTip(this.NthAdditivePrimeTextBox, "Find additive prime by index");
        this.ToolTip.SetToolTip(this.NthPurePrimeTextBox, "Find pure prime by index");
        this.ToolTip.SetToolTip(this.AdjustValueByPositionsLabel, "Dynamic Primalogy System - ©2012 Ali Adams");
        this.ToolTip.SetToolTip(this.ScopeBookRadioButton, "Use letters of the whole book to re-build the valuation system");
        this.ToolTip.SetToolTip(this.ScopeSelectionRadioButton, "Use letters of current selection to re-build the valuation system");
        this.ToolTip.SetToolTip(this.ScopeHighlightedTextRadioButton, "Use letters of current line or highlighted text to re-build the valuation system");
        this.ToolTip.SetToolTip(this.AddToLetterLNumberCheckBox, "Increment each letter value by its number");
        this.ToolTip.SetToolTip(this.AddToLetterWNumberCheckBox, "Increment each letter value by its word number");
        this.ToolTip.SetToolTip(this.AddToLetterVNumberCheckBox, "Increment each letter value by its verse number");
        this.ToolTip.SetToolTip(this.AddToLetterCNumberCheckBox, "Increment each letter value by its chapter number");
        this.ToolTip.SetToolTip(this.AddToLetterLDistanceCheckBox, "Increment each letter value by number of letters to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToLetterWDistanceCheckBox, "Increment each letter value by number of words to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToLetterVDistanceCheckBox, "Increment each letter value by number of verses to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToLetterCDistanceCheckBox, "Increment each letter value by number of chapters to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToWordWNumberCheckBox, "Increment each word value by its number");
        this.ToolTip.SetToolTip(this.AddToWordVNumberCheckBox, "Increment each word value by its verse number");
        this.ToolTip.SetToolTip(this.AddToWordCNumberCheckBox, "Increment each word value by its chapter number");
        this.ToolTip.SetToolTip(this.AddToWordWDistanceCheckBox, "Increment each word value by number of words to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToWordVDistanceCheckBox, "Increment each word value by number of verses to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToWordCDistanceCheckBox, "Increment each word value by number of chapters to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToVerseVNumberCheckBox, "Increment each verse value by its number");
        this.ToolTip.SetToolTip(this.AddToVerseCNumberCheckBox, "Increment each verse value by its chapter number");
        this.ToolTip.SetToolTip(this.AddToVerseVDistanceCheckBox, "Increment each verse value by number of verses to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToVerseCDistanceCheckBox, "Increment each verse value by number of chapters to its previous occurrence");
        this.ToolTip.SetToolTip(this.AddToChapterCNumberCheckBox, "Increment each chapter value by its number");
        this.ToolTip.SetToolTip(this.ChapterComboBox, "CC, CC:VV, CC-CC, CC:VV-CC, CC-CC:VV, CC:VV-CC:VV");        // 11, 13-14, 15:55, 16:19-23, 13-14:19, 24:35-27:62
        this.ToolTip.SetToolTip(this.ChapterVerseNumericUpDown, "ءاية");
        this.ToolTip.SetToolTip(this.ChapterWordNumericUpDown, "كلمة");
        this.ToolTip.SetToolTip(this.ChapterLetterNumericUpDown, "حرف");
        this.ToolTip.SetToolTip(this.PartNumericUpDown, "جزء");
        this.ToolTip.SetToolTip(this.PageNumericUpDown, "صفحة");
        this.ToolTip.SetToolTip(this.StationNumericUpDown, "منزل");
        this.ToolTip.SetToolTip(this.GroupNumericUpDown, "حزب");
        this.ToolTip.SetToolTip(this.QuarterNumericUpDown, "ربع حزب");
        this.ToolTip.SetToolTip(this.BowingNumericUpDown, "ركوع");
        this.ToolTip.SetToolTip(this.VerseNumericUpDown, "ءاية");
        this.ToolTip.SetToolTip(this.WordNumericUpDown, "كلمة");
        this.ToolTip.SetToolTip(this.LetterNumericUpDown, "حرف");
        this.ToolTip.SetToolTip(this.VerseDiffTextBox, "فرق ءايات");
        this.ToolTip.SetToolTip(this.WordDiffTextBox, "فرق كلمات");
        this.ToolTip.SetToolTip(this.LetterDiffTextBox, "فرق حروف");
        this.ToolTip.SetToolTip(this.FindScopeLabel, "Search scope");
        this.ToolTip.SetToolTip(this.FindByTextTextBox, "text to search for");
        this.ToolTip.SetToolTip(this.FindByTextWordnessCheckBox, "find verses with whole word only");
        this.ToolTip.SetToolTip(this.FindByTextCaseSensitiveCheckBox, "case sensitive for non-Arabic");
        this.ToolTip.SetToolTip(this.FindByNumbersResultTypeWordsLabel, "find words within verses");
        this.ToolTip.SetToolTip(this.FindByNumbersResultTypeSentencesLabel, "find sentences across verses");
        this.ToolTip.SetToolTip(this.FindByNumbersResultTypeVersesLabel, "find verses");
        this.ToolTip.SetToolTip(this.FindByNumbersResultTypeChaptersLabel, "find chapters");
        this.ToolTip.SetToolTip(this.FindByFrequencyResultTypeWordsLabel, "find words within verses");
        this.ToolTip.SetToolTip(this.FindByFrequencyResultTypeSentencesLabel, "find sentences across verses");
        this.ToolTip.SetToolTip(this.FindByFrequencyResultTypeVersesLabel, "find verses");
        this.ToolTip.SetToolTip(this.FindByNumbersUniqueLettersLabel, "unique letters");
        this.ToolTip.SetToolTip(this.FindByNumbersValueDigitSumLabel, "value digit sum");
        this.ToolTip.SetToolTip(this.FindByNumbersValueDigitalRootLabel, "value digital root");
        this.ToolTip.SetToolTip(this.FindByFrequencySumTypeLabel, "include duplicate phrase letters");
        this.ToolTip.SetToolTip(this.LetterCountLabel, "Unique letters");
        this.ToolTip.SetToolTip(this.AminrezaLinkLabel, "©2009 Aminreza Ebrahimi Saba");
    }
    private void InstallFont()
    {
        AppDomain domain = AppDomain.CurrentDomain;
        if (domain != null)
        {
            m_resources_assembly = domain.Load("Resources");
            if (m_resources_assembly != null)
            {
                if (m_font == null)
                {
                    string font_resource_name = "Resources.Fonts.me_quran.ttf";
                    Stream font_stream = m_resources_assembly.GetManifestResourceStream(font_resource_name);
                    if (font_stream != null)
                    {
                        m_font = FontBuilder.Build(font_stream, font_resource_name, DEFAULT_FONT_SIZE);
                        MainTextBox.Font = m_font;
                        SearchResultTextBox.Font = m_font;
                        //RelatedWordsTextBox.Font = m_font;
                        //GrammarTextBox.Font = m_font;
                    }
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 04. ContextMenu
    ///////////////////////////////////////////////////////////////////////////////
    private string m_clipboard_text = null;
    private string RemovePunctuationMarks(string text)
    {
        if (!String.IsNullOrEmpty(text))
        {
            if (m_language_type == LanguageType.Translation)
            {
                text = text.Replace(".", "");
                text = text.Replace(",", "");
                text = text.Replace(";", "");
                text = text.Replace(":", "");
                text = text.Replace("?", "");
                text = text.Replace("/", "");
                text = text.Replace(")", "");
                text = text.Replace("(", "");
                text = text.Replace(">", "");
                text = text.Replace("<", "");
                text = text.Replace("[", "");
                text = text.Replace("]", "");
                text = text.Replace("{", "");
                text = text.Replace("}", "");
                text = text.Replace("-", "");
                text = text.Replace("\"", "");
                text = text.Replace("\'", "");
                text = text.Replace("!", "");
                text = text.Replace("`", "");
                text = text.Replace("@", "");
                text = text.Replace("#", "");
                text = text.Replace("$", "");
                text = text.Replace("%", "");
                text = text.Replace("^", "");
                text = text.Replace("&", "");
                text = text.Replace("|", "");
                text = text.Replace("*", "");
                text = text.Replace("=", "");
            }
        }
        return text;
    }
    private void SimplifyClipboardTextBeforePaste()
    {
        m_clipboard_text = Clipboard.GetText(TextDataFormat.UnicodeText);
        if ((m_clipboard_text != null) && (m_clipboard_text.Length > 0))
        {
            if (m_client != null)
            {
                string simplified_text = m_clipboard_text.SimplifyTo(m_client.NumerologySystem.TextMode);
                if ((simplified_text != null) && (simplified_text.Length > 0))
                {
                    Clipboard.SetText(simplified_text, TextDataFormat.UnicodeText);
                }
            }
        }
    }
    private void RestoreClipboardTextAfterPaste()
    {
        if ((m_clipboard_text != null) && (m_clipboard_text.Length > 0))
        {
            Clipboard.SetText(m_clipboard_text, TextDataFormat.UnicodeText);
        }
    }
    private void MenuItem_Undo(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                if (((sender as MenuItem).Parent as ContextMenu).SourceControl is TextBoxBase)
                {
                    (((sender as MenuItem).Parent as ContextMenu).SourceControl as TextBoxBase).Undo();
                }
            }
        }
    }
    private void MenuItem_Cut(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                if (((sender as MenuItem).Parent as ContextMenu).SourceControl is TextBoxBase)
                {
                    (((sender as MenuItem).Parent as ContextMenu).SourceControl as TextBoxBase).Cut();
                }
            }
        }
    }
    private void MenuItem_Copy(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                if (((sender as MenuItem).Parent as ContextMenu).SourceControl is TextBoxBase)
                {
                    TextBoxBase control = (((sender as MenuItem).Parent as ContextMenu).SourceControl as TextBoxBase);
                    bool nothing_selected = false;
                    if (control.SelectionLength == 0)
                    {
                        nothing_selected = true;
                        control.SelectAll();
                    }
                    (((sender as MenuItem).Parent as ContextMenu).SourceControl as TextBoxBase).Copy();
                    if (nothing_selected)
                    {
                        control.DeselectAll();
                    }
                }
            }
        }
    }
    private void MenuItem_Paste(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                if (((sender as MenuItem).Parent as ContextMenu).SourceControl is TextBoxBase)
                {
                    SimplifyClipboardTextBeforePaste();
                    Thread.Sleep(100); // must give chance for Clipboard to refresh its content before Paste
                    (((sender as MenuItem).Parent as ContextMenu).SourceControl as TextBoxBase).Paste();
                    RestoreClipboardTextAfterPaste();
                }
            }
        }
    }
    private void MenuItem_SelectAll(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                if (((sender as MenuItem).Parent as ContextMenu).SourceControl is TextBoxBase)
                {
                    (((sender as MenuItem).Parent as ContextMenu).SourceControl as TextBoxBase).SelectAll();
                    (((sender as MenuItem).Parent as ContextMenu).SourceControl as TextBoxBase).KeyDown += new KeyEventHandler(TextBox_KeyDown);
                }
            }
        }
    }
    private void MenuItem_ExactText(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                Control control = ((sender as MenuItem).Parent as ContextMenu).SourceControl;
                if ((control == MainTextBox) || (control == SearchResultTextBox))
                {
                    this.Cursor = Cursors.WaitCursor;
                    try
                    {
                        DoFindExactText(control);
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
        }
    }
    private void MenuItem_SimilarWords(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                Control control = ((sender as MenuItem).Parent as ContextMenu).SourceControl;
                if ((control == MainTextBox) || (control == SearchResultTextBox))
                {
                    this.Cursor = Cursors.WaitCursor;
                    try
                    {
                        DoFindSimilarWords(control);
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
        }
    }
    private void MenuItem_RelatedWords(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                Control control = ((sender as MenuItem).Parent as ContextMenu).SourceControl;
                if ((control == MainTextBox) || (control == SearchResultTextBox))
                {
                    this.Cursor = Cursors.WaitCursor;
                    try
                    {
                        DoFindRelatedWords(control);
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
        }
    }
    private void MenuItem_SameValue(object sender, EventArgs e)
    {
        if (sender is MenuItem)
        {
            if ((sender as MenuItem).Parent is ContextMenu)
            {
                Control control = ((sender as MenuItem).Parent as ContextMenu).SourceControl;
                if ((control == MainTextBox) || (control == SearchResultTextBox))
                {
                    this.Cursor = Cursors.WaitCursor;
                    try
                    {
                        DoFindSameValue(control);
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
        }
    }
    private void DoFindExactText(object sender)
    {
        if (m_client != null)
        {
            if (sender is TextBoxBase)
            {
                string text = (sender as TextBoxBase).SelectedText.Trim();
                if (text.Length == 0) // no selection, get word under mouse pointer
                {
                    m_active_word = GetWordAtCursor();
                    if (m_active_word == null)
                    {
                        return;
                    }
                    text = m_active_word.Text;
                }

                DoFindExactText(text);
            }
        }
    }
    private void DoFindExactText(string text)
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            m_client.FindPhrases(text, LanguageType.Arabic, null, TextLocation.Anywhere, false, TextWordness.Any, -1, false, false);
            if (m_client.FoundPhrases != null)
            {
                int matches = m_client.FoundPhrases.Count;
                if (m_client.FoundVerses != null)
                {
                    int verse_count = m_client.FoundVerses.Count;
                    m_find_result_header = matches + " matches in " + verse_count + ((verse_count == 1) ? " verse" : " verses") + " with " + text + " anywhere " + " in " + m_client.FindScope.ToString();
                    DisplayFoundVerses(true);
                }
            }
        }
    }
    private void DoFindSimilarWords(object sender)
    {
        if (m_client != null)
        {
            if (sender is TextBoxBase)
            {
                string text = (sender as TextBoxBase).SelectedText.Trim();
                if (text.Length == 0) // no selection, get word under mouse pointer
                {
                    m_active_word = GetWordAtCursor();
                    if (m_active_word == null)
                    {
                        return;
                    }
                    text = m_active_word.Text;
                }

                DoFindSimilarWords(text);
            }
        }
    }
    private void DoFindSimilarWords(string text)
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            //double similarity_percentage = ((double)FindBySimilarityPercentageTrackBar.Value) / 100.0D;
            double similarity_percentage = 0.66D;
            SimilarityMethod find_by_similarity_method = SimilarityMethod.SimilarWords;

            m_client.FindPhrases(text, similarity_percentage);
            if (m_client.FoundPhrases != null)
            {
                string similarity_source = " to " + text + " ";
                int matches = m_client.FoundPhrases.Count;
                m_find_result_header = m_client.FoundVerses.Count + ((m_client.FoundVerses.Count == 1) ? " verse" : " verses") + " with " + find_by_similarity_method.ToString() + similarity_source + " in " + m_client.FindScope.ToString();
                DisplayFoundVerses(true);
            }
        }
    }
    private void DoFindRelatedWords(object sender)
    {
        if (m_client != null)
        {
            if (sender is TextBoxBase)
            {
                string text = (sender as TextBoxBase).SelectedText.Trim();
                if (text.Length == 0)
                {
                    m_active_word = GetWordAtCursor();
                    if (m_active_word == null)
                    {
                        return;
                    }
                    text = m_active_word.Text;
                }
                text = RemovePunctuationMarks(text);

                FindByTextTextBox.Text = text;
                FindByTextTextBox.Refresh();

                int multiplicity = -1;
                FindByRoot(text, multiplicity, m_with_diacritics);
            }
        }
    }
    private void DoFindSameValue(object sender)
    {
        if (m_client != null)
        {
            if (sender is TextBoxBase)
            {
                string selected_text = (sender as TextBoxBase).SelectedText.Trim();
                if (selected_text.Length == 0) // no selection, get word under mouse pointer
                {
                    m_active_word = GetWordAtCursor();
                    if (m_active_word != null)
                    {
                        selected_text = m_active_word.Text;
                    }
                }

                long value = 0L;
                if (UserTextTextBox.Focused)
                {
                    value = CalculateValue(selected_text);
                }
                else
                {
                    try
                    {
                        value = long.Parse(ValueTextBox.Text);
                    }
                    catch
                    {
                        // leave value = 0L
                    }
                }
                DoFindSameValue(value);
            }
        }
    }
    private void DoFindSameValue(long value)
    {
        if (m_client != null)
        {
            int match_count = 0;
            List<Verse> found_verses = new List<Verse>();
            List<Phrase> found_phrases = new List<Phrase>();
            PrepareNewSearch();

            string text = "value" + "" + "=" + value.ToString();

            NumberQuery query = new NumberQuery();
            query.Value = value;

            match_count += m_client.FindWords(query);
            if (match_count > 0)
            {
                found_verses.InsertRange(0, new List<Verse>(m_client.FoundVerses));
                found_phrases.InsertRange(0, new List<Phrase>(m_client.FoundPhrases));
            }

            match_count += m_client.FindSentences(query);
            if (match_count > 0)
            {
                found_verses.InsertRange(0, new List<Verse>(m_client.FoundVerses));
                found_phrases.InsertRange(0, new List<Phrase>(m_client.FoundPhrases));
            }

            match_count += m_client.FindVerses(query);
            if (match_count > 0)
            {
                found_verses.InsertRange(0, new List<Verse>(m_client.FoundVerses));
                found_phrases.InsertRange(0, new List<Phrase>(m_client.FoundPhrases));
            }

            m_client.FoundVerses = found_verses;
            m_client.FoundPhrases = found_phrases;
            m_find_result_header = match_count + ((match_count == 1) ? " match" : " matches") + " in " + m_client.FoundVerses.Count + ((m_client.FoundVerses.Count == 1) ? " verse" : " verses") + " with " + text + " in " + m_client.FindScope.ToString();
            DisplayFoundVerses(true);
        }
    }
    private void RegisterContextMenu(TextBoxBase control)
    {
        ContextMenu ContextMenu = new ContextMenu();
        if ((control != MainTextBox) && (control != SearchResultTextBox))
        {
            MenuItem EditUndoMenuItem = new MenuItem("Undo\t\tCtrl+Z");
            EditUndoMenuItem.Click += new EventHandler(MenuItem_Undo);
            ContextMenu.MenuItems.Add(EditUndoMenuItem);

            MenuItem MenuItemSeparator1 = new MenuItem("-");
            ContextMenu.MenuItems.Add(MenuItemSeparator1);

            MenuItem EditCutMenuItem = new MenuItem("Cut\t\tCtrl+X");
            EditCutMenuItem.Click += new EventHandler(MenuItem_Cut);
            ContextMenu.MenuItems.Add(EditCutMenuItem);

            MenuItem EditCopyMenuItem = new MenuItem("Copy\t\tCtrl+C");
            EditCopyMenuItem.Click += new EventHandler(MenuItem_Copy);
            ContextMenu.MenuItems.Add(EditCopyMenuItem);

            MenuItem EditPasteMenuItem = new MenuItem("Paste\t\tCtrl+V");
            EditPasteMenuItem.Click += new EventHandler(MenuItem_Paste);
            ContextMenu.MenuItems.Add(EditPasteMenuItem);

            MenuItem MenuItemSeparator2 = new MenuItem("-");
            ContextMenu.MenuItems.Add(MenuItemSeparator2);

            MenuItem EditSelectAllMenuItem = new MenuItem("Select All\tCtrl+A");
            EditSelectAllMenuItem.Click += new EventHandler(MenuItem_SelectAll);
            ContextMenu.MenuItems.Add(EditSelectAllMenuItem);
        }
        else
        {
            MenuItem EditCopyAllMenuItem = new MenuItem("Copy All\t\tCtrl+C");
            EditCopyAllMenuItem.Click += new EventHandler(MenuItem_Copy);
            ContextMenu.MenuItems.Add(EditCopyAllMenuItem);

            MenuItem MenuItemSeparator1 = new MenuItem("-");
            ContextMenu.MenuItems.Add(MenuItemSeparator1);

            MenuItem FindExactTextMenuItem = new MenuItem("Exact Text\tF5");
            FindExactTextMenuItem.Click += new EventHandler(MenuItem_ExactText);
            ContextMenu.MenuItems.Add(FindExactTextMenuItem);

            MenuItem FindSimilarWordsMenuItem = new MenuItem("Similar Words\tF6");
            FindSimilarWordsMenuItem.Click += new EventHandler(MenuItem_SimilarWords);
            ContextMenu.MenuItems.Add(FindSimilarWordsMenuItem);

            MenuItem FindRelatedWordsMenuItem = new MenuItem("Related Words\tF7");
            FindRelatedWordsMenuItem.Click += new EventHandler(MenuItem_RelatedWords);
            ContextMenu.MenuItems.Add(FindRelatedWordsMenuItem);

            MenuItem MenuItemSeparator2 = new MenuItem("-");
            ContextMenu.MenuItems.Add(MenuItemSeparator2);

            MenuItem FindSameValueMenuItem = new MenuItem("Same Value\tF9");
            FindSameValueMenuItem.Click += new EventHandler(MenuItem_SameValue);
            ContextMenu.MenuItems.Add(FindSameValueMenuItem);
        }

        ContextMenu.Popup += new EventHandler(ContextMenu_Popup);
        ContextMenu.Collapse += new EventHandler(ContextMenu_Collapse);

        control.ContextMenu = ContextMenu;
    }
    private void ContextMenu_Popup(object sender, EventArgs e)
    {
        if (m_active_textbox.SelectionLength == 0)
        {
            m_active_textbox.ContextMenu.MenuItems[0].Text = "Copy All\t\tCtrl+C";
        }
        else
        {
            m_active_textbox.ContextMenu.MenuItems[0].Text = "Copy\t\tCtrl+C";
        }

        //this.Cursor = Cursors.Arrow;
        //this.Refresh();
    }
    private void ContextMenu_Collapse(object sender, EventArgs e)
    {
        //this.Cursor = Cursors.IBeam;
        //this.Refresh();
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 05. Research Methods
    ///////////////////////////////////////////////////////////////////////////////
    private string m_research_assembly_name = "Research";
    private Assembly m_research_methods_assembly = null;
    private void LoadResearchMethods()
    {
        try
        {
            ResearchMethodsComboBox.SelectedIndexChanged -= new EventHandler(ResearchMethodsComboBox_SelectedIndexChanged);

            AppDomain domain = AppDomain.CurrentDomain;
            if (domain != null)
            {
                m_research_methods_assembly = domain.Load(m_research_assembly_name);
                if (m_research_methods_assembly != null)
                {
                    Type assembly_type = m_research_methods_assembly.GetType(m_research_assembly_name);
                    if (assembly_type != null)
                    {
                        MethodInfo[] method_infos = null;
                        if (Globals.EDITION == Edition.Research)
                        {
                            method_infos = assembly_type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        }
                        else
                        {
                            method_infos = assembly_type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                        }

                        if (method_infos != null)
                        {
                            ResearchMethodsComboBox.Items.Clear();
                            foreach (MethodInfo method_info in method_infos)
                            {
                                string method_name = method_info.Name;

                                if (method_name.Contains("WordPart"))
                                {
                                    if ((Globals.EDITION != Edition.Grammar) && (Globals.EDITION != Edition.Research))
                                    {
                                        continue; // skip WordPart methods
                                    }
                                }

                                ParameterInfo[] parameters = method_info.GetParameters();
                                if ((parameters.Length == 2) && (parameters[0].ParameterType == typeof(Client)) && (parameters[1].ParameterType == typeof(string)))
                                {
                                    ResearchMethodsComboBox.Items.Add(method_name);
                                }
                            }
                        }
                    }

                    if (ResearchMethodsComboBox.Items.Count > 0)
                    {
                        ResearchMethodsComboBox.SelectedIndex = 0;
                        ResearchMethodsComboBox_SelectedIndexChanged(null, null);
                    }
                }
            }
        }
        catch
        {
            // cannot load Research assembly, so just ignore
        }
        finally
        {
            ResearchMethodsComboBox.SelectedIndexChanged += new EventHandler(ResearchMethodsComboBox_SelectedIndexChanged);
        }
    }
    private void RunResearchMethod()
    {
        if (m_client != null)
        {
            if (m_client.Selection != null)
            {
                if (ResearchMethodsComboBox.SelectedIndex > -1)
                {
                    if (ResearchMethodsComboBox.SelectedItem != null)
                    {
                        string method_name = ResearchMethodsComboBox.SelectedItem.ToString();
                        string extra = ResearchMethodParameterTextBox.Text;
                        if (!string.IsNullOrEmpty(method_name))
                        {
                            List<Verse> verses = m_client.Selection.Verses;
                            if (verses != null)
                            {
                                if (verses.Count > 0)
                                {
                                    InvokeResearchMethod(method_name, m_client, extra);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private void InvokeResearchMethod(string method_name, Client client, string extra)
    {
        try
        {
            if (m_research_methods_assembly != null)
            {
                Type assembly_type = m_research_methods_assembly.GetType(m_research_assembly_name);
                if (assembly_type != null)
                {
                    MethodInfo method_info = assembly_type.GetMethod(method_name);
                    if ((method_info != null) && (method_info.IsStatic))
                    {
                        object[] parameters = { client, extra };
                        object result = method_info.Invoke(null, parameters);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            while (ex != null)
            {
                //Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, Application.ProductName);
                ex = ex.InnerException;
            }
        }
    }
    private void ResearchMethodsComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ResearchMethodsComboBox.SelectedItem != null)
        {
            string method_name = ResearchMethodsComboBox.SelectedItem.ToString();

            ResearchMethodParameterTextBox.BringToFront();
            if (method_name == "NewResearchMethod")
            {
                ResearchMethodParameterTextBox.Text = "";
                ToolTip.SetToolTip(ResearchMethodParameterTextBox, "Target");
                ResearchMethodParameterTextBox.Visible = true;
            }
            else if ((method_name.Contains("ByX")) || (method_name.Contains("WithX")))
            {
                ResearchMethodParameterTextBox.Text = "7";
                ToolTip.SetToolTip(ResearchMethodParameterTextBox, "X");
                ResearchMethodParameterTextBox.Visible = true;
            }
            else if ((method_name.Contains("ByXY")) || (method_name.Contains("WithXY")))
            {
                ResearchMethodParameterTextBox.Text = "7,29";
                ToolTip.SetToolTip(ResearchMethodParameterTextBox, "X,Y");
                ResearchMethodParameterTextBox.Visible = true;
            }
            else if ((method_name.Contains("ByXYZ")) || (method_name.Contains("WithXYZ")))
            {
                ResearchMethodParameterTextBox.Text = "7,29,139";
                ToolTip.SetToolTip(ResearchMethodParameterTextBox, "X,Y,Z");
                ResearchMethodParameterTextBox.Visible = true;
            }
            else if (method_name.Contains("Sound"))
            {
                ResearchMethodParameterTextBox.Text = Globals.DEFAULT_FREQUENCY.ToString();
                ToolTip.SetToolTip(ResearchMethodParameterTextBox, "Hz");
                ResearchMethodParameterTextBox.Visible = true;
            }
            else if (method_name.Contains("Equals"))
            {
                ResearchMethodParameterTextBox.Text = "0";
                ToolTip.SetToolTip(ResearchMethodParameterTextBox, "to within");
                ResearchMethodParameterTextBox.Visible = true;
            }
            else // method doesn't need parameters
            {
                ResearchMethodParameterTextBox.Text = "";
                ToolTip.SetToolTip(ResearchMethodParameterTextBox, null);
                ResearchMethodParameterTextBox.Visible = false;
            }

            // must be done at end so we can trim Research methods
            ToolTip.SetToolTip(ResearchMethodsComboBox, method_name);
        }
    }
    private void ResearchMethodsComboBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            RunResearchMethod();
        }
    }
    private void ResearchMethodParameterTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            RunResearchMethod();
        }
    }
    private void ResearchMethodsRunButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (ResearchMethodsComboBox.SelectedItem != null)
            {
                if (ResearchMethodsComboBox.SelectedItem.ToString() == "NewResearchMethod")
                {
                    HeaderLabel.Text = "New Research Method";
                    HeaderLabel.Refresh();

                    if (ScriptTextBox.Text == "")
                    {
                        ScriptTextBox.Text = ScriptRunner.LoadScript("Template.cs");
                    }

                    if (ScriptTextBox.Visible)
                    {
                        RunScriptLabel_Click(sender, e);
                    }
                    else
                    {
                        ScriptTextBox.BringToFront();
                        ScriptTextBox.Visible = true;
                        ScriptOutputGroupBox.Visible = true;
                        CompileScriptLabel.Visible = true;
                        RunScriptLabel.Visible = true;
                        ScriptSamplesLabel.Visible = true;
                        NewScriptLabel.Visible = true;
                        CloseScriptLabel.Visible = true;

                        ZoomInLabel.Visible = false;
                        ZoomOutLabel.Visible = false;
                        ColorizeMatchesLabel.Visible = false;
                        GoldenRatioOrderLabel.Visible = false;
                        GoldenRatioScopeLabel.Visible = false;
                        WordWrapLabel.Visible = false;
                    }
                }
                else
                {
                    RunResearchMethod();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    Assembly m_compiled_assembly = null;
    private void NewScriptLabel_Click(object sender, EventArgs e)
    {
        ScriptTextBox.Text = ScriptRunner.LoadScript("Template.cs");
        ScriptOutputTextBox.Text = "";
    }
    private void ScriptSamplesLabel_Click(object sender, EventArgs e)
    {
        ScriptTextBox.Text = ScriptRunner.LoadScript("Samples.cs");
        ScriptOutputTextBox.Text = "";
    }
    private void CompileScriptLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            string source_code = ScriptTextBox.Text;
            if (source_code.Length > 0)
            {
                CompilerResults compiler_results = ScriptRunner.CompileCode(source_code);
                ScriptOutputTextBox.Text = "";
                if (compiler_results != null)
                {
                    if ((compiler_results.Errors.HasWarnings) || (compiler_results.Errors.HasErrors))
                    {
                        int error_count = 0;
                        if (compiler_results.Errors.HasErrors)
                        {
                            foreach (CompilerError error in compiler_results.Errors)
                            {
                                if (!error.IsWarning)
                                {
                                    ScriptOutputTextBox.Text += ("Error at line " + error.Line + ": " + error.ErrorText) + "\r\n";
                                    error_count++;
                                }
                            }
                        }

                        ScriptOutputTextBox.Text += "\r\n";

                        int warning_count = 0;
                        if (compiler_results.Errors.HasWarnings)
                        {
                            foreach (CompilerError error in compiler_results.Errors)
                            {
                                if (error.IsWarning)
                                {
                                    ScriptOutputTextBox.Text += ("Warning at line " + error.Line + ": " + error.ErrorText) + "\r\n";
                                    warning_count++;
                                }
                            }
                        }

                        ScriptOutputGroupBox.Text = " Output    " + error_count.ToString() + " Errors,   " + warning_count.ToString() + " Warnings. ";
                    }
                    else
                    {
                        m_compiled_assembly = compiler_results.CompiledAssembly;
                        if (m_compiled_assembly != null)
                        {
                            ScriptOutputTextBox.Text = "Script was compiled successfully.";
                            ScriptOutputTextBox.Refresh();
                        }

                        ScriptOutputGroupBox.Text = " Output    Success. ";
                    }

                    // in all cases, clear for next complation run
                    compiler_results.Errors.Clear();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void RunScriptLabel_Click(object sender, EventArgs e)
    {
        CompileScriptLabel_Click(sender, e);

        // to stop race conditions
        Thread.Sleep(1000);

        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_compiled_assembly != null)
            {

                string extra = ResearchMethodParameterTextBox.Text;
                object[] args = new object[] { m_client, extra };
                object result = ScriptRunner.Run(m_compiled_assembly, args, m_permission_set);

                // display result
                if ((bool)result == true)
                {
                    CloseScriptLabel_Click(sender, e);

                    m_find_result_header = m_client.FoundVerses.Count + ((m_client.FoundVerses.Count == 1) ? " verse" : " verses") + " found";
                    DisplayFoundVerses(false);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void CloseScriptLabel_Click(object sender, EventArgs e)
    {
        if (ScriptTextBox.Visible)
        {
            ScriptTextBox.SendToBack();
            ScriptTextBox.Visible = false;
            ScriptOutputGroupBox.Visible = false;
            CompileScriptLabel.Visible = false;
            RunScriptLabel.Visible = false;
            ScriptSamplesLabel.Visible = false;
            NewScriptLabel.Visible = false;
            CloseScriptLabel.Visible = false;

            ZoomInLabel.Visible = true;
            ZoomOutLabel.Visible = true;
            ColorizeMatchesLabel.Visible = m_found_verses_displayed;
            GoldenRatioOrderLabel.Visible = !m_found_verses_displayed;
            GoldenRatioScopeLabel.Visible = !m_found_verses_displayed;
            WordWrapLabel.Visible = true;
        }
    }
    private void ScriptTextBox_MouseHover(object sender, EventArgs e)
    {
        if (sender != ScriptTextBox)
        {
            this.AcceptButton = null;
            this.CancelButton = null;
            ScriptTextBox.Focus();
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 06. MainTextBox
    ///////////////////////////////////////////////////////////////////////////////
    private float m_text_zoom_factor = DEFAULT_TEXT_ZOOM_FACTOR;
    private float m_graphics_zoom_factor = DEFAULT_GRAPHICS_ZOOM_FACTOR;
    private Point m_previous_location = new Point(0, 0);
    private int m_previous_clicked_verse_number = 1;
    private int m_previous_clicked_word_number = 1;
    private int m_previous_clicked_letter_number = 1;
    private float m_min_zoom_factor = 0.1F;
    private float m_max_zoom_factor = 2.0F;
    private float m_zoom_factor_increment = 0.1F;
    private float m_error_margin = 0.001F;
    private void MainTextBox_TextChanged(object sender, EventArgs e)
    {
        //ApplyFont(); // don't do as it disables Undo/Redo

        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                if (
                    (m_client.NumerologySystem.Scope == NumerologySystemScope.Selection)
                    ||
                    (m_client.NumerologySystem.Scope == NumerologySystemScope.HighlightedText)
                   )
                {
                    RebuildCurrentNumerologySystem();
                }
            }
        }
    }
    private void MainTextBox_SelectionChanged(object sender, EventArgs e)
    {
        if (
             (sender == m_active_textbox) &&
             (
               (m_active_textbox.Focused) ||
               (ChapterWordNumericUpDown.Focused) ||
               (ChapterLetterNumericUpDown.Focused) ||
               (WordNumericUpDown.Focused) ||
               (LetterNumericUpDown.Focused)
             )
           )
        {
            if (m_client != null)
            {
                m_is_selection_mode = false;
                // set verse pointer to current verse
                Verse old_verse = GetCurrentVerse();
                Verse new_verse = GetVerseAtCursor();
                // don't goto start of verse OR colorize verse
                // let user select words/letters as desired
                if (old_verse != new_verse)
                {
                    SetCurrentVerse();
                }

                if (m_client.NumerologySystem != null)
                {
                    if (
                        (m_client.NumerologySystem.Scope == NumerologySystemScope.Selection)
                        ||
                        (m_client.NumerologySystem.Scope == NumerologySystemScope.HighlightedText)
                       )
                    {
                        RebuildCurrentNumerologySystem();
                    }
                    else
                    {
                        // calculate numerology value
                        CalculateCurrentValue();

                        CalculateLetterStatistics();
                        DisplayLetterStatistics();

                        CalculatePhraseLetterStatistics();
                        DisplayPhraseLetterStatistics();
                    }
                }

                DisplayCurrentPositions();
            }
        }
    }
    private void MainTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if ((e.Control) && (e.KeyCode == Keys.V))
            {
                if ((e.Control) && (e.KeyCode == Keys.V))
                {
                    SimplifyClipboardTextBeforePaste();
                    Thread.Sleep(100); // must give chance for Clipboard to refresh its content before Paste
                    m_active_textbox.Paste();
                    RestoreClipboardTextAfterPaste();
                    e.Handled = true;
                }
            }
        }
        finally
        {
            UpdateMouseCursor();
        }
    }
    private void MainTextBox_KeyUp(object sender, KeyEventArgs e)
    {
        try
        {
            bool NavigationKeys = (
            e.KeyCode == Keys.Up ||
            e.KeyCode == Keys.Right ||
            e.KeyCode == Keys.Down ||
            e.KeyCode == Keys.Left ||
            e.KeyCode == Keys.Home ||
            e.KeyCode == Keys.End);

            if (NavigationKeys)
            {
                PrepareVerseToPlay();

                // this code has been moved out of SelectionChanged and brought to MouseClick and KeyUp
                // to keep all verse translations visible until the user clicks a verse then show one verse translation
                if (m_active_textbox.SelectionLength == 0)
                {
                    Verse verse = GetCurrentVerse();
                    if (verse != null)
                    {
                        DisplayTranslations(verse);
                        DisplayTafseer(verse);
                    }
                }
                else
                {
                    // selected text is dealt with by DisplayVersesWordsLetters 
                }

                // in all cases
                m_active_word = GetWordAtCursor();
                if (m_active_word != null)
                {
                    DisplayWordGrammar(m_active_word);
                    DisplayRelatedWords(m_active_word);
                }
            }
        }
        finally
        {
            UpdateMouseCursor();
        }
    }
    private void MainTextBox_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == ' ')
        {
            if ((MainTextBox.Focused) && (!m_readonly_mode)) return;
            if ((SearchResultTextBox.Focused) && (!m_readonly_mode)) return;
            if (FindByTextTextBox.Focused) return;
            if (ChapterComboBox.Focused) return;
            if (BookmarkTextBox.Focused) return;
            if (FindByFrequencyPhraseTextBox.Focused) return;

            if (m_mp3player != null)
            {
                if ((m_mp3player.Playing) || (m_mp3player.Paused))
                {
                    PlayerPlayLabel_Click(null, null);
                }
            }
        }

        e.Handled = true; // stop annoying beep
    }
    private void MainTextBox_Enter(object sender, EventArgs e)
    {
        SearchGroupBox_Leave(null, null);
        this.AcceptButton = null;
        UpdateMouseCursor();
    }
    private void MainTextBox_MouseEnter(object sender, EventArgs e)
    {
        //m_active_textbox.Focus();
    }
    private void MainTextBox_MouseLeave(object sender, EventArgs e)
    {
        // stop cursor flicker
        if (m_active_textbox.Cursor != Cursors.Default)
        {
            m_active_textbox.Cursor = Cursors.Default;
        }
    }
    private void MainTextBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            // in case we come from UserTextTextBox
            m_active_textbox.Focus();
            MainTextBox_SelectionChanged(m_active_textbox, null);

            // set cursor at mouse RIGHT-click location so we know which word to get related words for
            int start = m_active_textbox.GetCharIndexFromPosition(e.Location);
            if (
                 (start <= m_active_textbox.SelectionStart)
                 ||
                 (start > (m_active_textbox.SelectionStart + m_active_textbox.SelectionLength))
               )
            {
                m_active_textbox.Select(start, 0);
            }
            // reset find_by_similarity current verse
            m_similarity_current_verse = GetVerseAtCursor();
        }
    }
    private void MainTextBox_MouseMove(object sender, MouseEventArgs e)
    {
        // stop flickering
        if (
            (Math.Abs(m_previous_location.X - e.X) < 4)
            &&
            (Math.Abs(m_previous_location.Y - e.Y) < 4)
           )
        {
            return;
        }
        m_previous_location = e.Location;

        Word word = GetWordAtPointer(e);
        if (word != null)
        {
            // always diplay word info at application caption
            this.Text = Application.ProductName + " | " + GetSummarizedFindScope();
            UpdateFindMatchCaption();

            this.Text += SPACE_GAP +
            (
                word.Verse.Chapter.Name + SPACE_GAP +
                "verse " + word.Verse.NumberInChapter + "-" + word.Verse.Number + SPACE_GAP +
                "word " + word.NumberInVerse + "-" + word.NumberInChapter + "-" + word.Number + SPACE_GAP +
                word.Transliteration + SPACE_GAP +
                word.Text + SPACE_GAP +
                word.Meaning + SPACE_GAP +
                word.Occurrence.ToString() + "/" + word.Occurrences.ToString()
            );

            // and optionally in the related word tabpage
            string word_info = null;
            if (ModifierKeys == Keys.Control)
            {
                // only update m_active_word on CTRL key
                m_active_word = GetWordAtPointer(e);
                if (m_active_word != null)
                {
                    word_info = GetWordInformation(m_active_word) + "\r\n\r\n";
                    if ((Globals.EDITION == Edition.Grammar) || (Globals.EDITION == Edition.Research))
                    {
                        word_info += GetWordGrammar(m_active_word) + "\r\n\r\n";
                    }
                    word_info += GetWordRelatedWords(m_active_word);
                }
            }
            ToolTip.SetToolTip(m_active_textbox, word_info);
        }
    }
    private void MainTextBox_MouseUp(object sender, MouseEventArgs e)
    {
        try
        {
            m_active_textbox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            m_active_textbox.BeginUpdate();

            if (ModifierKeys == Keys.Control)
            {
                // go to related words to word under mouse pointer
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    DoFindRelatedWords(sender);
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
            else
            {
                // calculate the Verse/Word/Letter distances
                int current_verse_number = (int)VerseNumericUpDown.Value;
                int current_word_number = (int)WordNumericUpDown.Value;
                int current_letter_number = (int)LetterNumericUpDown.Value;
                int verse_diff = Math.Abs(current_verse_number - m_previous_clicked_verse_number);
                int word_diff = Math.Abs(current_word_number - m_previous_clicked_word_number);
                int letter_diff = Math.Abs(current_letter_number - m_previous_clicked_letter_number);

                VerseDiffTextBox.Text = verse_diff.ToString();
                WordDiffTextBox.Text = word_diff.ToString();
                LetterDiffTextBox.Text = letter_diff.ToString();
                VerseDiffTextBox.ForeColor = GetNumberTypeColor(verse_diff);
                WordDiffTextBox.ForeColor = GetNumberTypeColor(word_diff);
                LetterDiffTextBox.ForeColor = GetNumberTypeColor(letter_diff);

                m_previous_clicked_verse_number = current_verse_number;
                m_previous_clicked_word_number = current_word_number;
                m_previous_clicked_letter_number = current_letter_number;

                // in all cases
                m_active_word = GetWordAtPointer(e);
                if (m_active_word != null)
                {
                    DisplayWordGrammar(m_active_word);
                    DisplayRelatedWords(m_active_word);
                }

                Verse verse = GetCurrentVerse();
                if (verse != null)
                {
                    // Let DisplayVersesWordsLetters display translations of all selected verses
                    //DisplayTranslations(verse);

                    DisplayTafseer(verse);
                }
            }
        }
        finally
        {
            m_active_textbox.EndUpdate();
            m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private Word m_active_word = null;
    private Word m_info_word = null;
    private string GetWordInformation(Word word)
    {
        if (word != null)
        {
            if (m_client != null)
            {
                StringBuilder roots = new StringBuilder();
                if (word.Roots != null)
                {
                    if (word.Roots.Count > 0)
                    {
                        foreach (string root in word.Roots)
                        {
                            roots.Append(root + " | ");
                        }
                        roots.Remove(roots.Length - 3, 3);
                    }
                }

                return
                    word.Transliteration + SPACE_GAP +
                    word.Text + SPACE_GAP +
                    word.Meaning + "\r\n" +
                    word.Verse.Chapter.Number + ". " + word.Verse.Chapter.Name + SPACE_GAP +
                    "verse  " + word.Verse.NumberInChapter + "-" + word.Verse.Number + SPACE_GAP +
                    "word  " + word.NumberInVerse + "-" + word.NumberInChapter + "-" + word.Number + SPACE_GAP +
                    "occurance " + word.Occurrence.ToString() + "/" + word.Occurrences.ToString() + SPACE_GAP + SPACE_GAP +
                    "roots  " + roots;
            }
        }
        return null;
    }
    private string GetWordRelatedWords(Word word)
    {
        if (word != null)
        {
            if (m_client != null)
            {
                string related_words_lines = null;
                int words_per_line = 0;
                int max_words_per_line = 10;
                List<Word> related_words = m_client.Book.GetRelatedWords(word, m_with_diacritics);
                related_words = related_words.RemoveDuplicates();
                if (related_words != null)
                {
                    StringBuilder str = new StringBuilder();
                    if (related_words.Count > 0)
                    {
                        str.AppendLine("Related words = " + related_words.Count.ToString());
                        foreach (Word related_word in related_words)
                        {
                            words_per_line++;
                            str.Append(related_word.Text + (((words_per_line % max_words_per_line) == 0) ? "\r\n" : "\t"));
                        }
                        if (str.Length > 1)
                        {
                            str.Remove(str.Length - 1, 1); // \t
                        }
                        related_words_lines = str.ToString();
                    }
                }
                return related_words_lines;
            }
        }
        return null;
    }
    private string GetWordGrammar(Word word)
    {
        if (word != null)
        {
            if (m_client != null)
            {
                if ((Globals.EDITION == Edition.Grammar) || (Globals.EDITION == Edition.Research))
                {
                    string grammar_info = "";
                    if (word.CorpusLemma != null)
                    {
                        grammar_info += "Lemma\t" + word.CorpusLemma + "\t";
                    }
                    if (word.CorpusRoot != null)
                    {
                        grammar_info += "Root\t" + word.CorpusRoot + "\t";
                    }
                    if (word.CorpusSpecialGroup != null)
                    {
                        grammar_info += "Special Group\t" + word.CorpusSpecialGroup + "\t";
                    }
                    if (grammar_info.Length > 0)
                    {
                        grammar_info += "\r\n";
                    }

                    grammar_info += word.ArabicGrammar;
                    grammar_info += "\r\n";
                    grammar_info += word.EnglishGrammar;

                    return grammar_info;
                }
                else
                {
                    return GRAMMAR_EDITION_INSTRUCTION;
                }
            }
        }
        return null;
    }

    private void DisplayRelatedWords(Word word)
    {
        if (word != null)
        {
            //if (TabControl.SelectedTab == RelatedWordsTabPage)
            {
                RelatedWordsTextBox.Text = GetWordInformation(m_active_word) + "\r\n\r\n" + GetWordRelatedWords(m_active_word);
                RelatedWordsTextBox.Refresh();

                m_info_word = word;
            }
        }
    }
    private void DisplayWordGrammar(Word word)
    {
        if (word != null)
        {
            //if (TabControl.SelectedTab == GrammarTabPage)
            {
                GrammarTextBox.Text = GetWordGrammar(m_active_word);
                GrammarTextBox.Refresh();

                m_info_word = word;
            }
        }
    }
    //private Word m_display_verb_forms_word = null;
    //private void DisplayVerbForms(Word word)
    //{
    //    if (word != null)
    //    {
    //        if (m_client != null)
    //        {
    //            if (TabControl.SelectedTab == VerbFormsTabPage)
    //            {
    //                string verb = m_client.Book.GetBestRoot(word.Text, m_with_diacritics);

    //                StringBuilder str = new StringBuilder();
    //                if (verb.Length == 3)
    //                {
    //                    char Faa = verb[0];
    //                    char Ain = verb[1];
    //                    char Laam = verb[2];

    //                    string form1_perfect = Faa + "َ" + Ain + "َ" + Laam + "َ";
    //                    string form1_imperfect = "يَ" + Faa + "ْ" + Ain + "َ" + Laam + "ُ";
    //                    string form1_active_participle = Faa + "َ" + Ain + "ِ" + Laam + "ٌ";
    //                    string form1_passive_participle = "مَ" + Faa + "ْ" + Ain + "ُ" + Laam + "ٌ";
    //                    string form1_verbal_noun = Faa + "ِ" + Ain + "َ" + Laam + "ٌ";

    //                    string form2_perfect = Faa + "َ" + Ain + "َّ" + Laam + "َ";
    //                    string form2_imperfect = "يُ" + Faa + "َ" + Ain + "ِّ" + Laam + "ُ";
    //                    string form2_active_participle = "مُ" + Faa + "َ" + Ain + "ِّ" + Laam + "ٌ";
    //                    string form2_passive_participle = "مُ" + Faa + "َ" + Ain + "َّ" + Laam + "ٌ";
    //                    string form2_verbal_noun = "تَ" + Faa + "ْ" + Ain + "ِ" + "ي" + Laam + "ٌ";

    //                    string form3_perfect = Faa + "َ" + "ا" + Ain + "َ" + Laam + "َ";
    //                    string form3_imperfect = "يُ" + Faa + "َ" + "ا" + Ain + "ِ" + Laam + "ُ";
    //                    string form3_active_participle = "مُ" + Faa + "" + "ا" + Ain + "ِ" + Laam + "ٌ";
    //                    string form3_passive_participle = "مُ" + Faa + "" + "ا" + Ain + "َ" + Laam + "ٌ";
    //                    string form3_verbal_noun = "مُ" + Faa + "" + "ا" + Ain + "َ" + Laam + "َ" + "ة" + " / " + Faa + "ِ" + Ain + "" + "ا" + Laam + "ٌ";

    //                    string form4_perfect = "اَ" + Faa + "ْ" + Ain + "َ" + Laam + "َ";
    //                    string form4_imperfect = "يُ" + Faa + "ْ" + Ain + "ِ" + Laam + "ُ";
    //                    string form4_active_participle = "مُ" + Faa + "ْ" + Ain + "ِ" + Laam + "ٌ";
    //                    string form4_passive_participle = "مُ" + Faa + "ْ" + Ain + "َ" + Laam + "ٌ";
    //                    string form4_verbal_noun = "اِ" + Faa + "ْ" + Ain + "َ" + Laam + "ٌ";

    //                    string form5_perfect = "تَ" + Faa + "َ" + Ain + "َّ" + Laam + "َ";
    //                    string form5_imperfect = "يَتَ" + Faa + "َ" + Ain + "ِّ" + Laam + "ُ";
    //                    string form5_active_participle = "مُتَ" + Faa + "َ" + Ain + "ِّ" + Laam + "ٌ";
    //                    string form5_passive_participle = "مُتَ" + Faa + "َ" + Ain + "َّ" + Laam + "ٌ";
    //                    string form5_verbal_noun = "تَ" + Faa + "َ" + Ain + "ُّ" + Laam + "ٌ";

    //                    string form6_perfect = "تَ" + Faa + "َ" + "ا" + Ain + "َ" + Laam + "َ";
    //                    string form6_imperfect = "تَ" + Faa + "َ" + "ا" + Ain + "َ" + Laam + "ٌ";
    //                    string form6_active_participle = "مُتَ" + Faa + "َ" + "ا" + Ain + "ِ" + Laam + "ٌ";
    //                    string form6_passive_participle = "مُتَ" + Faa + "َ" + "ا" + Ain + "َ" + Laam + "ٌ";
    //                    string form6_verbal_noun = "تَ" + Faa + "َ" + "ا" + Ain + "ُ" + Laam + "ٌ";

    //                    string form7_perfect = "اِنْ" + Faa + "َ" + Ain + "َ" + Laam + "َ";
    //                    string form7_imperfect = "يَنْ" + Faa + "َ" + Ain + "ِ" + Laam + "ُ";
    //                    string form7_active_participle = "مُنْ" + Faa + "َ" + Ain + "ِ" + Laam + "ٌ";
    //                    string form7_passive_participle = "مُنْ" + Faa + "َ" + Ain + "َ" + Laam + "ٌ";
    //                    string form7_verbal_noun = "اِنْ" + Faa + "ِ" + Ain + "" + "ا" + Laam + "ٌ";

    //                    string form8_perfect = "إِ" + Faa + "ْ" + "تَ" + Ain + "َ" + Laam + "َ";
    //                    string form8_imperfect = "يَ" + Faa + "ْ" + "تَ" + Ain + "ِ" + Laam + "ُ";
    //                    string form8_active_participle = "مُ" + Faa + "ْ" + "تَ" + Ain + "ِ" + Laam + "ٌ";
    //                    string form8_passive_participle = "مُ" + Faa + "ْ" + "تَ" + Ain + "َ" + Laam + "ٌ";
    //                    string form8_verbal_noun = "إ" + Faa + "ْ" + "تِ" + Ain + "َ" + Laam + "ٌ";

    //                    string form9_perfect = "إِ" + Faa + "ْ" + Ain + "َ" + Laam + "َّ";
    //                    string form9_imperfect = "يَ" + Faa + "ْ" + Ain + "َ" + Laam + "ُّ";
    //                    string form9_active_participle = "مُ" + Faa + "ْ" + Ain + "َ" + Laam + "ٌّ";
    //                    string form9_passive_participle = "";
    //                    string form9_verbal_noun = "إِ" + Faa + "ْ" + Ain + "ِ" + Laam + "ا" + Laam + "ٌ";

    //                    string form10_perfect = "إِسْتَ" + Faa + "ْ" + Ain + "َ" + Laam + "َ";
    //                    string form10_imperfect = "يَسْتَ" + Faa + "ْ" + Ain + "ِ" + Laam + "ُ";
    //                    string form10_active_participle = "مُسْتَ" + Faa + "ْ" + Ain + "ِ" + Laam + "ٌ";
    //                    string form10_passive_participle = "مُسْتَ" + Faa + "ْ" + Ain + "َ" + Laam + "ٌ";
    //                    string form10_verbal_noun = "اِسْتِ" + Faa + "ْ" + Ain + "" + "ا" + Laam + "ٌ";

    //                    //str.AppendLine("Form\t" + "Perfect" + "\t" + "Imperfect" + "\t" + "ActiveParticiple" + "\t" + "PassiveParticiple" + "\t" + "VerbalNoun");
    //                    str.AppendLine("الصيغة\tماضي\tمضارع\tإسم فاعل\tإسم مفعول\tمصدر");
    //                    str.AppendLine("I\t" + form1_perfect + "\t" + form1_imperfect + "\t" + form1_active_participle + "\t" + form1_passive_participle + "\t" + form1_verbal_noun);
    //                    str.AppendLine("II\t" + form2_perfect + "\t" + form2_imperfect + "\t" + form2_active_participle + "\t" + form2_passive_participle + "\t" + form2_verbal_noun);
    //                    str.AppendLine("III\t" + form3_perfect + "\t" + form3_imperfect + "\t" + form3_active_participle + "\t" + form3_passive_participle + "\t" + form3_verbal_noun);
    //                    str.AppendLine("IV\t" + form4_perfect + "\t" + form4_imperfect + "\t" + form4_active_participle + "\t" + form4_passive_participle + "\t" + form4_verbal_noun);
    //                    str.AppendLine("V\t" + form5_perfect + "\t" + form5_imperfect + "\t" + form5_active_participle + "\t" + form5_passive_participle + "\t" + form5_verbal_noun);
    //                    str.AppendLine("VI\t" + form6_perfect + "\t" + form6_imperfect + "\t" + form6_active_participle + "\t" + form6_passive_participle + "\t" + form6_verbal_noun);
    //                    str.AppendLine("VII\t" + form7_perfect + "\t" + form7_imperfect + "\t" + form7_active_participle + "\t" + form7_passive_participle + "\t" + form7_verbal_noun);
    //                    str.AppendLine("VIII\t" + form8_perfect + "\t" + form8_imperfect + "\t" + form8_active_participle + "\t" + form8_passive_participle + "\t" + form8_verbal_noun);
    //                    str.AppendLine("IX\t" + form9_perfect + "\t" + form9_imperfect + "\t" + form9_active_participle + "\t" + form9_passive_participle + "\t" + form9_verbal_noun);
    //                    str.Append("X\t" + form10_perfect + "\t" + form10_imperfect + "\t" + form10_active_participle + "\t" + form10_passive_participle + "\t" + form10_verbal_noun);
    //                }

    //                ToolTip.SetToolTip(m_active_textbox, null);
    //                VerbFormsTextBox.Text = str.ToString();
    //                VerbFormsTextBox.Refresh();

    //                m_display_verb_forms_word = word;
    //            }
    //        }
    //    }
    //}
    private void RelatedWordsTextBox_TextChanged(object sender, EventArgs e)
    {
        RelatedVersesButton.Enabled = (RelatedWordsTextBox.Text.Length > 0);
    }
    private void RelatedVersesButton_Click(object sender, EventArgs e)
    {
        FindRelatedWords(m_info_word);
    }
    private void FindRelatedWords(Word word)
    {
        if (m_active_word != null)
        {
            string text = word.Text;
            text = RemovePunctuationMarks(text);

            FindByTextTextBox.Text = text;
            FindByTextTextBox.Refresh();

            int multiplicity = -1;
            FindByRoot(text, multiplicity, m_with_diacritics);
        }
    }
    private void UpdateMouseCursor()
    {
        if (ModifierKeys == Keys.Control)
        {
            // stop cursor flicker
            if (m_active_textbox.Cursor != Cursors.Hand)
            {
                m_active_textbox.Cursor = Cursors.Hand;
            }
        }
        else
        {
            // stop cursor flicker
            if (m_active_textbox.Cursor != Cursors.IBeam)
            {
                m_active_textbox.Cursor = Cursors.IBeam;
            }
        }
    }
    private void MainTextBox_Click(object sender, EventArgs e)
    {
        PrepareVerseToPlay();

        // this code has been moved out of SelectionChanged and brought to MouseClick and KeyUp
        // to keep all verse translations visible until the user clicks a verse then show one verse translation
        if (m_active_textbox.SelectionLength == 0)
        {
            Verse verse = GetCurrentVerse();
            if (verse != null)
            {
                DisplayTranslations(verse);
            }
        }
        else
        {
            // selected text is dealt with by DisplayVersesWordsLetters 
        }
    }
    private void MainTextBox_DoubleClick(object sender, EventArgs e)
    {
        if (ModifierKeys == Keys.None)
        {
            if (m_found_verses_displayed)
            {
                Verse verse = GetCurrentVerse();
                if (verse != null)
                {
                    GotoDoubleClickedVerseInItsChapter(verse);
                }
            }
        }
    }
    private void GotoDoubleClickedVerseInItsChapter(Verse verse)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            MainTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            MainTextBox.BeginUpdate();

            if (verse != null)
            {
                if (verse.Chapter != null)
                {
                    if (m_client != null)
                    {
                        // select chapter and display it and colorize target verse
                        m_client.Selection = new Selection(m_client.Book, SelectionScope.Chapter, new List<int>() { verse.Chapter.Number - 1 });
                        if (m_client.Selection != null)
                        {
                            if ((m_mp3player.Playing) || (m_mp3player.Paused))
                            {
                                PlayerStopLabel_Click(null, null);
                            }

                            SwitchToMainTextBox();

                            BookmarkTextBox.Enabled = true;
                            // display selection's note (if any)
                            DisplayNote(m_client.GetBookmark(m_client.Selection));

                            m_is_selection_mode = false;

                            AutoCompleteHeaderLabel.Visible = false;
                            AutoCompleteListBox.Visible = false;
                            AutoCompleteListBox.SendToBack();

                            this.Text = Application.ProductName + " | " + GetSummarizedFindScope();
                            UpdateFindScope();

                            UpdateHeaderLabel();

                            DisplaySelectionText();

                            CalculateCurrentValue();

                            CalculateLetterStatistics();
                            DisplayLetterStatistics();

                            CalculatePhraseLetterStatistics();
                            DisplayPhraseLetterStatistics();

                            MainTextBox.ClearHighlight();
                            MainTextBox.AlignToStart();

                            m_current_selection_verse_index = 0;
                            PrepareVerseToPlay();

                            DisplayVersesWordsLetters(verse);

                            m_similarity_current_verse = verse;
                            DisplayTranslations(verse);

                            HighlightVerse(verse);

                            if (m_client.NumerologySystem.TextMode.Contains("Images"))
                            {
                                PictureBoxPanel.BringToFront();
                                PictureBoxPanel.Visible = true;
                                DisplayCurrentPage();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            MainTextBox.EndUpdate();
            MainTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
            this.Cursor = Cursors.Default;
        }
    }
    private void MainTextBox_Resize(object sender, EventArgs e)
    {
        if (m_active_textbox.SelectionStart == 0)
        {
            m_active_textbox.AlignToLineStart();
        }
    }
    private void MainTextBox_MouseWheel(object sender, MouseEventArgs e)
    {
        if (ModifierKeys == Keys.Control)
        {
            ZoomOutLabel.Enabled = true;
            ZoomInLabel.Enabled = true;
            if (MainTextBox.ZoomFactor <= (m_min_zoom_factor + m_error_margin))
            {
                MainTextBox.ZoomFactor = m_min_zoom_factor;
                SearchResultTextBox.ZoomFactor = m_min_zoom_factor;
                ZoomOutLabel.Enabled = false;
                ZoomInLabel.Enabled = true;
            }
            else if (MainTextBox.ZoomFactor >= (m_max_zoom_factor - m_error_margin))
            {
                MainTextBox.ZoomFactor = m_max_zoom_factor;
                SearchResultTextBox.ZoomFactor = m_max_zoom_factor;
                ZoomOutLabel.Enabled = true;
                ZoomInLabel.Enabled = false;
            }
            m_text_zoom_factor = MainTextBox.ZoomFactor;
            //SetTranslationFontSize(m_translation_font_size * m_text_zoom_factor);
        }
    }
    private void ZoomInLabel_Click(object sender, EventArgs e)
    {
        if (PictureBoxEx.Visible)
        {
            if (m_graphics_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin))
            {
                PictureBoxEx.ZoomIn();
                m_graphics_zoom_factor = PictureBoxEx.ZoomFactor;
            }
            // re-check same condition after zoom_factor update
            ZoomInLabel.Enabled = (m_graphics_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin));
            ZoomOutLabel.Enabled = true;
            RedrawCurrentGraph();
        }
        else
        {
            if (m_text_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin))
            {
                m_text_zoom_factor += m_zoom_factor_increment;
                MainTextBox.ZoomFactor = m_text_zoom_factor;
                SearchResultTextBox.ZoomFactor = m_text_zoom_factor;
                //SetTranslationFontSize(m_translation_font_size * m_text_zoom_factor);
            }
            // re-check same condition after zoom_factor update
            ZoomInLabel.Enabled = (m_text_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin));
            ZoomOutLabel.Enabled = true;
        }
    }
    private void ZoomOutLabel_Click(object sender, EventArgs e)
    {
        if (PictureBoxEx.Visible)
        {
            if (m_graphics_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin))
            {
                PictureBoxEx.ZoomOut();
                m_graphics_zoom_factor = PictureBoxEx.ZoomFactor;
            }
            // re-check same condition after zoom_factor update
            ZoomOutLabel.Enabled = (m_graphics_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin));
            ZoomInLabel.Enabled = true;
            RedrawCurrentGraph();
        }
        else
        {
            if (m_text_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin))
            {
                m_text_zoom_factor -= m_zoom_factor_increment;
                MainTextBox.ZoomFactor = m_text_zoom_factor;
                SearchResultTextBox.ZoomFactor = m_text_zoom_factor;
                //SetTranslationFontSize(m_translation_font_size * m_text_zoom_factor);
            }
            // re-check same condition after zoom_factor update
            ZoomOutLabel.Enabled = (m_text_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin));
            ZoomInLabel.Enabled = true;
        }
    }
    // wordwrap mode
    private bool m_word_wrap_main_textbox = false;
    private bool m_word_wrap_search_textbox = false;
    private void ApplyLoadedWordWrapSettings()
    {
        try
        {
            MainTextBox.BeginUpdate();
            SearchResultTextBox.BeginUpdate();

            MainTextBox.WordWrap = m_word_wrap_main_textbox;
            SearchResultTextBox.WordWrap = m_word_wrap_search_textbox;

            Verse.IncludeNumber = m_word_wrap_main_textbox;

            UpdateWordWrapLabel(m_word_wrap_main_textbox);
        }
        finally
        {
            MainTextBox.EndUpdate();
            SearchResultTextBox.EndUpdate();
        }
    }
    private void UpdateWordWrapLabel(bool word_wrap)
    {
        if (word_wrap)
        {
            if (File.Exists(Globals.IMAGES_FOLDER + "/" + "arrow_left.png"))
            {
                WordWrapLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "arrow_left.png");
            }
            ToolTip.SetToolTip(WordWrapLabel, "Unwrap text");
        }
        else
        {
            if (File.Exists(Globals.IMAGES_FOLDER + "/" + "arrow_rotate_anticlockwise.png"))
            {
                WordWrapLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "arrow_rotate_anticlockwise.png");
            }
            ToolTip.SetToolTip(WordWrapLabel, "Wrap text");
        }
    }
    private void WordWrapLabel_Click(object sender, EventArgs e)
    {
        if (m_found_verses_displayed)
        {
            m_word_wrap_search_textbox = SearchResultTextBox.WordWrap;
        }
        else
        {
            m_word_wrap_main_textbox = MainTextBox.WordWrap;
        }
        ToggleWordWrap();
    }
    private void ToggleWordWrap() // F11
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            m_active_textbox.BeginUpdate();
            Verse backup_verse = GetCurrentVerse();

            if (m_found_verses_displayed)
            {
                m_word_wrap_search_textbox = !m_word_wrap_search_textbox;
                Verse.IncludeNumber = false;
                m_active_textbox.WordWrap = m_word_wrap_search_textbox;
                UpdateWordWrapLabel(m_word_wrap_search_textbox);
                DisplayFoundVerses(false);
            }
            else
            {
                m_word_wrap_main_textbox = !m_word_wrap_main_textbox;
                Verse.IncludeNumber = m_word_wrap_main_textbox;
                m_active_textbox.WordWrap = m_word_wrap_main_textbox;
                UpdateWordWrapLabel(m_word_wrap_main_textbox);
                DisplaySelection(false);
            }

            GotoVerse(backup_verse);
        }
        finally
        {
            m_active_textbox.EndUpdate();
            this.Cursor = Cursors.Default;
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 07. Verse Navigator
    ///////////////////////////////////////////////////////////////////////////////
    // navigation
    private int m_current_selection_verse_index = 0;
    private int m_current_found_verse_index = 0;
    private int CurrentVerseIndex
    {
        get
        {
            if (m_found_verses_displayed)
            {
                if (m_current_found_verse_index == -1)
                {
                    m_current_found_verse_index = 0;
                }
                return m_current_found_verse_index;
            }
            else
            {
                if (m_current_selection_verse_index == -1)
                {
                    m_current_selection_verse_index = 0;
                }
                return m_current_selection_verse_index;
            }
        }
        set
        {
            if (m_found_verses_displayed)
            {
                if (m_client.FoundVerses != null)
                {
                    if ((value >= 0) && (value < m_client.FoundVerses.Count))
                    {
                        m_current_found_verse_index = value;
                    }
                    else if (value < 0)
                    {
                        // round robin
                        m_current_found_verse_index = m_client.FoundVerses.Count - 1;
                    }
                    else if (value >= m_client.FoundVerses.Count)
                    {
                        // round robin
                        m_current_found_verse_index = 0;
                    }
                    else
                    {
                        // do nothing
                    }
                }
            }
            else
            {
                if (m_client.Selection != null)
                {
                    if (m_client.Selection.Verses != null)
                    {
                        if ((value >= 0) && (value < m_client.Selection.Verses.Count))
                        {
                            m_current_selection_verse_index = value;
                        }
                        else if (value < 0)
                        {
                            // round robin
                            m_current_selection_verse_index = m_client.Selection.Verses.Count - 1;
                        }
                        else if (value >= m_client.Selection.Verses.Count)
                        {
                            // round robin
                            m_current_selection_verse_index = 0;
                        }
                        else
                        {
                            // do nothing
                        }
                    }
                }
            }
        }
    }
    private Verse SetPreviousVerse()
    {
        CurrentVerseIndex--;
        return GetCurrentVerse();
    }
    private Verse SetCurrentVerse()
    {
        Verse verse = GetVerseAtCursor();
        if (verse != null)
        {
            int index = GetVerseIndex(verse);
            CurrentVerseIndex = index;
        }
        return verse;
    }
    private void SetCurrentVerse(Verse verse)
    {
        if (verse != null)
        {
            int index = GetVerseIndex(verse);
            CurrentVerseIndex = index;
        }
    }
    private Verse SetNextVerse()
    {
        CurrentVerseIndex++;
        return GetCurrentVerse();
    }
    private Verse GetCurrentVerse()
    {
        int index = CurrentVerseIndex;
        return GetVerse(index);
    }
    private Verse GetVerse(int index)
    {
        if (m_client != null)
        {
            List<Verse> verses = null;
            if (m_found_verses_displayed)
            {
                verses = m_client.FoundVerses;
            }
            else // m_curent_verses displayed
            {
                if (m_client.Selection != null)
                {
                    verses = m_client.Selection.Verses;
                }
            }

            if (verses != null)
            {
                if ((index >= 0) && (index < verses.Count))
                {
                    return verses[index];
                }
            }
        }
        return null;
    }
    private int GetVerseIndex(Verse verse)
    {
        List<Verse> verses = null;
        if (m_found_verses_displayed)
        {
            verses = m_client.FoundVerses;
        }
        else
        {
            if (m_client.Selection != null)
            {
                verses = m_client.Selection.Verses;
            }
        }
        if (verses != null)
        {
            int verse_index = -1;
            foreach (Verse v in verses)
            {
                verse_index++;
                if (v == verse)
                {
                    return verse_index;
                }
            }
        }
        return -1;
    }
    private int GetVerseDisplayStart(Verse verse)
    {
        int start = 0;
        if (m_client != null)
        {
            List<Verse> verses = null;
            if (m_found_verses_displayed)
            {
                verses = m_client.FoundVerses;
            }
            else
            {
                if (m_client.Selection != null)
                {
                    verses = m_client.Selection.Verses;
                }
            }

            if (verses != null)
            {
                if (verse != null)
                {
                    foreach (Verse v in verses)
                    {
                        if (v == verse) break;

                        if (m_found_verses_displayed)
                        {//                            \t                  \n
                            start += v.Address.Length + 1 + v.Text.Length + 1;
                        }
                        else
                        {
                            start += v.Text.Length + v.Endmark.Length;
                        }
                    }
                }
            }
        }
        return start;
    }
    private int GetVerseDisplayLength(Verse verse)
    {
        int length = 0;
        if (verse != null)
        {
            if (m_found_verses_displayed)
            {//                                \t                       \n
                length = verse.Address.Length + 1 + verse.Text.Length + 1;
            }
            else
            {//                                 { # }  or  \n
                length = verse.Text.Length + verse.Endmark.Length;
            }
        }
        return length;
    }
    private int GetWordDisplayStart(Word word)
    {
        int start = 0;
        if (m_client != null)
        {
            if (word != null)
            {
                Verse verse = word.Verse;
                if (verse != null)
                {
                    start = GetVerseDisplayStart(verse);
                    start += word.Position;
                }
            }
        }
        return start;
    }
    private int GetWordDisplayLength(Word word)
    {
        if (word != null)
        {
            return word.Text.Length + 1;
        }
        return 0;
    }
    // page downloader
    private void DownloadAllPageImages()
    {
        for (int i = 1; i <= Page.MAX_NUMBER; i++)
        {
            DownloadPageImage(i);
        }
    }
    private void DownloadPageImage(int page_number)
    {
        string path = "Pages" + "/" + page_number.ToString("000") + "." + Page.FileType;
        string url = Page.UrlPrefix + page_number.ToString("000") + "." + Page.FileType;
        if (!File.Exists(path))
        {
            DownloadFile(url, path);
        }
    }
    private void DisplayPageImage(int page_number)
    {
        try
        {
            string path = "Pages" + "/" + page_number.ToString("000") + ".jpg";
            if (!File.Exists(path))
            {
                DownloadPageImage(page_number);
            }

            if (File.Exists(path))
            {
                if (ScrollablePictureBox.ImagePath != path)
                {
                    ScrollablePictureBox.ImagePath = path;
                    ScrollablePictureBox.Refresh();
                }
            }
        }
        catch
        {
            ScrollablePictureBox.ImagePath = null;
            ScrollablePictureBox.Refresh();
        }
    }
    // highlighting verse/word
    private Verse m_previous_highlighted_verse = null;
    private Word m_previous_highlighted_word = null;
    private void HighlightWord(Word word)
    {
        try
        {
            m_active_textbox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            m_active_textbox.BeginUpdate();

            if (m_client != null)
            {
                int backup_selection_start = m_active_textbox.SelectionStart;
                int backup_selection_length = m_active_textbox.SelectionLength;

                // de-highlight previous word
                if (m_previous_highlighted_word != null)
                {
                    int start = GetWordDisplayStart(m_previous_highlighted_word);
                    int length = GetWordDisplayLength(m_previous_highlighted_word);
                    m_active_textbox.ClearHighlight(start, length);
                }

                // highlight this word
                if (word != null)
                {
                    int start = GetWordDisplayStart(word);
                    int length = GetWordDisplayLength(word);
                    m_active_textbox.Highlight(start, length - 1, Color.Lavender); // -1 so de-highlighting can clean the last \n at the end of all text

                    // backup highlighted word
                    m_previous_highlighted_word = word;
                }

                //??? WRONG DESIGN: if backup_selection is outside visible area, then this line will scroll to it and loses highlight above
                m_active_textbox.Select(backup_selection_start, backup_selection_length);
            }
        }
        finally
        {
            m_active_textbox.EndUpdate();
            m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void GotoVerse(Verse verse)
    {
        try
        {
            m_active_textbox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            m_active_textbox.BeginUpdate();

            if (m_client != null)
            {
                if (verse != null)
                {
                    int start = GetVerseDisplayStart(verse);

                    // re-wire MainTextBox_SelectionChanged event to
                    // updates verse position and value when cursor goes to start of verse
                    // #######
                    m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
                    m_active_textbox.Select(start, 0);

                    SetCurrentVerse(verse);
                }
                else
                {
                    m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
                }
            }
        }
        finally
        {
            m_active_textbox.EndUpdate();
            // already re-wired above, see #######
            //m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void HighlightVerse(Verse verse)
    {
        try
        {
            m_active_textbox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            m_active_textbox.BeginUpdate();

            if (m_client != null)
            {
                // de-highlight previous verse
                if (m_previous_highlighted_verse != null)
                {
                    int start = GetVerseDisplayStart(m_previous_highlighted_verse);
                    int length = GetVerseDisplayLength(m_previous_highlighted_verse);
                    m_active_textbox.ClearHighlight(start, length);
                }

                // highlight this verse
                if (verse != null)
                {
                    int start = GetVerseDisplayStart(verse);
                    int length = GetVerseDisplayLength(verse);
                    m_active_textbox.Highlight(start, length - 1, Color.Lavender); // -1 so de-highlighting can clean the last \n at the end of all text

                    // re-wire MainTextBox_SelectionChanged event to
                    // updates verse position and value when cursor goes to start of verse
                    // #######
                    m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
                    m_active_textbox.Select(start, 0);

                    SetCurrentVerse(verse);

                    // backup highlighted verse
                    m_previous_highlighted_verse = verse;
                }
                else
                {
                    m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
                }
            }
        }
        finally
        {
            m_active_textbox.EndUpdate();
            // already re-wired above, see #######
            //m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private GoldenRatioOrder m_golden_ratio_order = GoldenRatioOrder.LongShort;
    private GoldenRatioScope m_golden_ratio_scope = GoldenRatioScope.None;
    private void GoldenRatioOrderLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            m_active_textbox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            m_active_textbox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.NumerologySystem != null)
                {
                    switch (m_golden_ratio_order)
                    {
                        case GoldenRatioOrder.LongShort:
                            {
                                m_golden_ratio_order = GoldenRatioOrder.ShortLong;
                                if (File.Exists("Images/golden_sl.png"))
                                {
                                    GoldenRatioOrderLabel.Image = new Bitmap("Images/golden_sl.png");
                                    ToolTip.SetToolTip(GoldenRatioOrderLabel, "Golden ratio ~= 0.618 + 1");
                                }
                            }
                            break;
                        case GoldenRatioOrder.ShortLong:
                            {
                                m_golden_ratio_order = GoldenRatioOrder.LongShort;
                                if (File.Exists("Images/golden_ls.png"))
                                {
                                    GoldenRatioOrderLabel.Image = new Bitmap("Images/golden_ls.png");
                                    ToolTip.SetToolTip(GoldenRatioOrderLabel, "Golden ratio ~= 1 + 0.618");
                                }
                            }
                            break;
                    }

                    DisplaySelection(false);
                }
            }
        }
        finally
        {
            m_active_textbox.EndUpdate();
            m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
            this.Cursor = Cursors.Default;
        }
    }
    private void GoldenRatioScopeLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            m_active_textbox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            m_active_textbox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.NumerologySystem != null)
                {
                    if (ModifierKeys == Keys.Shift)
                    {
                        switch (m_golden_ratio_scope)
                        {
                            case GoldenRatioScope.None:
                                {
                                    if (m_client.NumerologySystem.TextMode.Contains("Original"))
                                    {
                                        m_golden_ratio_scope = GoldenRatioScope.Sentence;
                                        if (File.Exists("Images/golden_sentence.png"))
                                        {
                                            GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_sentence.png");
                                            ToolTip.SetToolTip(GoldenRatioScopeLabel, "Sentence-level golden ratio");
                                        }
                                    }
                                    else
                                    {
                                        m_golden_ratio_scope = GoldenRatioScope.Word;
                                        if (File.Exists("Images/golden_word.png"))
                                        {
                                            GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_word.png");
                                            ToolTip.SetToolTip(GoldenRatioScopeLabel, "Word-level golden ratio");
                                        }
                                    }
                                }
                                break;
                            case GoldenRatioScope.Letter:
                                {
                                    m_golden_ratio_scope = GoldenRatioScope.None;
                                    if (File.Exists("Images/golden_none.png"))
                                    {
                                        GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_none.png");
                                        ToolTip.SetToolTip(GoldenRatioScopeLabel, "No golden ratio colorization");
                                    }
                                }
                                break;
                            case GoldenRatioScope.Word:
                                {
                                    m_golden_ratio_scope = GoldenRatioScope.Letter;
                                    if (File.Exists("Images/golden_letter.png"))
                                    {
                                        GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_letter.png");
                                        ToolTip.SetToolTip(GoldenRatioScopeLabel, "Letter-level golden ratio");
                                    }
                                }
                                break;
                            case GoldenRatioScope.Sentence:
                                {
                                    m_golden_ratio_scope = GoldenRatioScope.Word;
                                    if (File.Exists("Images/golden_word.png"))
                                    {
                                        GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_word.png");
                                        ToolTip.SetToolTip(GoldenRatioScopeLabel, "Word-level golden ratio");
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        switch (m_golden_ratio_scope)
                        {
                            case GoldenRatioScope.None:
                                {
                                    m_golden_ratio_scope = GoldenRatioScope.Letter;
                                    if (File.Exists("Images/golden_letter.png"))
                                    {
                                        GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_letter.png");
                                        ToolTip.SetToolTip(GoldenRatioScopeLabel, "Letter-level golden ratio");
                                    }
                                }
                                break;
                            case GoldenRatioScope.Letter:
                                {
                                    m_golden_ratio_scope = GoldenRatioScope.Word;
                                    if (File.Exists("Images/golden_word.png"))
                                    {
                                        GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_word.png");
                                        ToolTip.SetToolTip(GoldenRatioScopeLabel, "Word-level golden ratio");
                                    }
                                }
                                break;
                            case GoldenRatioScope.Word:
                                {
                                    if (m_client.NumerologySystem.TextMode.Contains("Original"))
                                    {
                                        m_golden_ratio_scope = GoldenRatioScope.Sentence;
                                        if (File.Exists("Images/golden_sentence.png"))
                                        {
                                            GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_sentence.png");
                                            ToolTip.SetToolTip(GoldenRatioScopeLabel, "Sentence-level golden ratio");
                                        }
                                    }
                                    else
                                    {
                                        m_golden_ratio_scope = GoldenRatioScope.None;
                                        if (File.Exists("Images/golden_none.png"))
                                        {
                                            GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_none.png");
                                            ToolTip.SetToolTip(GoldenRatioScopeLabel, "No golden ratio colorization");
                                        }
                                    }
                                }
                                break;
                            case GoldenRatioScope.Sentence:
                                {
                                    m_golden_ratio_scope = GoldenRatioScope.None;
                                    if (File.Exists("Images/golden_none.png"))
                                    {
                                        GoldenRatioScopeLabel.Image = new Bitmap("Images/golden_none.png");
                                        ToolTip.SetToolTip(GoldenRatioScopeLabel, "No golden ratio colorization");
                                    }
                                }
                                break;
                        }
                    }

                    DisplaySelection(false);
                }
            }
        }
        finally
        {
            m_active_textbox.EndUpdate();
            m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
            this.Cursor = Cursors.Default;
        }
    }
    private void ColorizeGoldenRatios()
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            MainTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            MainTextBox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.Selection != null)
                {
                    foreach (Verse verse in m_client.Selection.Verses)
                    {
                        if (verse != null)
                        {
                            int start = GetVerseDisplayStart(verse);

                            if (m_client.NumerologySystem != null)
                            {
                                int length = verse.LetterCount;

                                int golden_letters = 0;
                                switch (m_golden_ratio_order)
                                {
                                    case GoldenRatioOrder.LongShort:
                                        {
                                            golden_letters = (int)Math.Round(((double)length / Numbers.PHI), 0);
                                        }
                                        break;
                                    case GoldenRatioOrder.ShortLong:
                                        {
                                            golden_letters = length - (int)Math.Round(((double)length / Numbers.PHI), 0);
                                        }
                                        break;
                                }

                                int golden_space_stopmarks_diacritics = 0;
                                bool colorize = false;
                                int count = 0;
                                for (int i = 0; i < verse.Text.Length; i++)
                                {
                                    if (Constants.ARABIC_LETTERS.Contains(verse.Text[i]))
                                    {
                                        count++;
                                        if (count == golden_letters)
                                        {
                                            switch (m_golden_ratio_scope)
                                            {
                                                case GoldenRatioScope.None:
                                                    {
                                                        colorize = false;
                                                    }
                                                    break;
                                                case GoldenRatioScope.Letter:
                                                    {
                                                        colorize = true;
                                                    }
                                                    break;
                                                case GoldenRatioScope.Word:
                                                    {
                                                        for (int j = 1; j < verse.Text.Length - i; j++)
                                                        {
                                                            if (Constants.ARABIC_LETTERS.Contains(verse.Text[i + j]))
                                                            {
                                                                break;
                                                            }
                                                            if (verse.Text[i + j] == ' ')
                                                            {
                                                                colorize = true;
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case GoldenRatioScope.Sentence:
                                                    {
                                                        for (int j = 1; j < verse.Text.Length - i; j++)
                                                        {
                                                            if (Constants.ARABIC_LETTERS.Contains(verse.Text[i + j]))
                                                            {
                                                                break;
                                                            }
                                                            if (Constants.STOPMARKS.Contains(verse.Text[i + j]))
                                                            {
                                                                colorize = true;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }

                                            // in all cases
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        golden_space_stopmarks_diacritics++;
                                    }
                                }
                                if (!colorize) continue; // skip verse

                                int golden_length = golden_letters + golden_space_stopmarks_diacritics;

                                MainTextBox.Colorize(start, golden_length, Color.Navy);
                                MainTextBox.Colorize(start + golden_length, verse.Text.Length - golden_length, Color.Red);

                                // reset color back to Navy for subsequent display
                                if (MainTextBox.Text.Length > 0)
                                {
                                    MainTextBox.Colorize(0, 1, Color.Navy);
                                }

                                MainTextBox.AlignToStart();
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            MainTextBox.EndUpdate();
            MainTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
            this.Cursor = Cursors.Default;
        }
    }
    // helpers
    private Verse GetVerseAtCursor()
    {
        int start = m_active_textbox.SelectionStart;
        return GetVerseAtChar(start);
    }
    private Word GetWordAtCursor()
    {
        int char_index = m_active_textbox.SelectionStart;
        if (char_index > 0)
        {
            return GetWordAtChar(char_index);
        }
        return null;
    }
    private Letter GetLetterAtCursor()
    {
        int char_index = m_active_textbox.SelectionStart;
        if (char_index > 0)
        {
            return GetLetterAtChar(char_index);
        }
        return null;
    }
    private Verse GetVerseAtPointer(MouseEventArgs e)
    {
        return GetVerseAtLocation(e.Location);
    }
    private Word GetWordAtPointer(MouseEventArgs e)
    {
        return GetWordAtLocation(e.Location);
    }
    private Letter GetLetterAtPointer(MouseEventArgs e)
    {
        return GetLetterAtLocation(e.Location);
    }
    private Verse GetVerseAtLocation(Point mouse_location)
    {
        int char_index = m_active_textbox.GetCharIndexFromPosition(mouse_location);
        if (char_index > 0)
        {
            return GetVerseAtChar(char_index);
        }
        return null;
    }
    private Word GetWordAtLocation(Point mouse_location)
    {
        int char_index = m_active_textbox.GetCharIndexFromPosition(mouse_location);
        if (char_index > 0)
        {
            return GetWordAtChar(char_index);
        }
        return null;
    }
    private Letter GetLetterAtLocation(Point mouse_location)
    {
        int char_index = m_active_textbox.GetCharIndexFromPosition(mouse_location);
        if (char_index > 0)
        {
            return GetLetterAtChar(char_index);
        }
        return null;
    }
    // helper helpers
    private Verse GetVerseAtChar(int char_index)
    {
        if (m_client != null)
        {
            List<Verse> verses = null;
            if (m_found_verses_displayed)
            {
                verses = m_client.FoundVerses;
            }
            else
            {
                if (m_client.Selection != null)
                {
                    verses = m_client.Selection.Verses;
                }
            }

            if (verses != null)
            {
                Verse scanned_verse = null;
                foreach (Verse verse in verses)
                {
                    int start = GetVerseDisplayStart(verse);
                    if (char_index < start)
                    {
                        return scanned_verse;
                    }
                    scanned_verse = verse;
                }
                return scanned_verse;
            }
        }
        return null;
    }
    private Word GetWordAtChar(int char_index)
    {
        Word word = null;
        if (m_client != null)
        {
            if (m_found_verses_displayed)
            {
                List<Verse> verses = m_client.FoundVerses;
                if (verses != null)
                {
                    foreach (Verse verse in verses)
                    {
                        int length = GetVerseDisplayLength(verse);
                        if (char_index >= length)
                        {
                            char_index -= length;
                        }
                        else
                        {
                            // verse found, remove verse address
                            char_index -= verse.Address.Length + 1; // \t

                            int word_index = CalculateWordIndex(verse, char_index);
                            if ((word_index >= 0) && (word_index < verse.Words.Count))
                            {
                                word = verse.Words[word_index];
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (m_client.Selection != null)
                {
                    List<Verse> verses = m_client.Selection.Verses;
                    if (verses != null)
                    {
                        foreach (Verse verse in verses)
                        {
                            if ((char_index >= verse.Text.Length) && (char_index < (verse.Text.Length + verse.Endmark.Length - 1)))
                            {
                                return null; // don't return a word at verse Endmark
                            }

                            int length = GetVerseDisplayLength(verse);
                            if (char_index >= length)
                            {
                                char_index -= length;
                            }
                            else
                            {
                                int word_index = CalculateWordIndex(verse, char_index);
                                if ((word_index >= 0) && (word_index < verse.Words.Count))
                                {
                                    word = verse.Words[word_index];
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        return word;
    }
    private Letter GetLetterAtChar(int char_index)
    {
        if (m_client != null)
        {
            if (m_found_verses_displayed)
            {
                List<Verse> verses = m_client.FoundVerses;
                if (verses != null)
                {
                    foreach (Verse verse in verses)
                    {
                        int length = GetVerseDisplayLength(verse);
                        if (char_index >= length)
                        {
                            char_index -= length;
                        }
                        else
                        {
                            // remove verse address
                            char_index -= verse.Address.Length + 1; // \t

                            int letter_index = CalculateLetterIndex(verse, char_index);
                            if ((letter_index >= 0) && (letter_index < verse.LetterCount))
                            {
                                return verse.GetLetter(letter_index);
                            }
                        }
                    }
                }
            }
            else
            {
                if (m_client.Selection != null)
                {
                    List<Verse> verses = m_client.Selection.Verses;
                    if (verses != null)
                    {
                        foreach (Verse verse in verses)
                        {
                            if ((char_index >= verse.Text.Length) && (char_index < (verse.Text.Length + verse.Endmark.Length - 1)))
                            {
                                return null; // no word at verse Endmark
                            }

                            int length = GetVerseDisplayLength(verse);
                            if (char_index >= length)
                            {
                                char_index -= length;
                            }
                            else
                            {
                                int letter_index = CalculateLetterIndex(verse, char_index);
                                if ((letter_index >= 0) && (letter_index < verse.LetterCount))
                                {
                                    return verse.GetLetter(letter_index);
                                }
                            }
                        }
                    }
                }
            }
        }
        return null;
    }
    // helper helper helpers
    private int CalculateWordIndex(Verse verse, int column_index)
    {
        int word_index = -1;
        if (m_client != null)
        {
            if (verse != null)
            {
                string[] word_texts = verse.Text.Split();
                foreach (string word_text in word_texts)
                {
                    // skip stopmarks (1-letter words), except real Quranic 1-letter words
                    if (
                         (word_text.Length == 1)
                         &&
                         !((word_text == "ص") || (word_text == "ق") || (word_text == "ن") || (word_text == "و"))
                       )
                    {
                        // skip stopmark words
                        column_index -= word_text.Length + 1; // 1 for space
                    }
                    else
                    {
                        word_index++;

                        if (column_index < word_text.Length)
                        {
                            break;
                        }
                        column_index -= word_text.Length + 1; // 1 for space
                    }
                }
            }
        }
        return word_index;
    }
    private int CalculateLetterIndex(Verse verse, int column_index)
    {
        int letter_index = -1;
        if (m_client != null)
        {
            if (verse != null)
            {
                if ((column_index >= 0) && (column_index < verse.Text.Length))
                {
                    for (int i = 0; i <= column_index; i++)
                    {
                        char character = verse.Text[i];
                        if (Constants.ARABIC_LETTERS.Contains(character))
                        {
                            letter_index++;
                        }
                    }
                }
                else if (column_index == verse.Text.Length)
                {
                    letter_index = verse.LetterCount;
                }
                else
                {
                    letter_index = -1; // for Original text
                }
            }
        }
        return letter_index;
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 08. Verse Translation/Tafseer
    ///////////////////////////////////////////////////////////////////////////////
    private int m_information_page_index = DEFAULT_INFORMATION_PAGE_INDEX;
    private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
    {
        m_information_page_index = TabControl.SelectedIndex;
    }
    private int m_information_box_top = DEFAULT_INFORMATION_BOX_TOP;
    // translation
    private List<string> m_checked_keys = new List<string>();
    private void PopulateTranslatorsCheckedListBox()
    {
        try
        {
            // to disable item in a list, just ignore user check using this trick
            TranslatorsCheckedListBox.ItemCheck += new ItemCheckEventHandler(TranslatorsCheckedListBox_ItemCheck);

            TranslatorsCheckedListBox.SelectedIndexChanged -= new EventHandler(TranslatorsCheckedListBox_SelectedIndexChanged);
            TranslatorsCheckedListBox.BeginUpdate();
            TranslatorsCheckedListBox.Items.Clear();

            if (m_client.Book != null)
            {
                if (m_client.Book.Verses != null)
                {
                    if (m_client.Book.Verses.Count > 0)
                    {
                        m_checked_keys.Clear();
                        foreach (string key in m_client.Book.Verses[0].Translations.Keys)
                        {
                            m_checked_keys.Add(key);
                        }

                        // populate TranslatorsCheckedListBox
                        if (m_client.Book.TranslationInfos != null)
                        {
                            foreach (string key in m_client.Book.TranslationInfos.Keys)
                            {
                                string name = m_client.Book.TranslationInfos[key].Name;
                                bool is_checked = m_checked_keys.Contains(key);
                                TranslatorsCheckedListBox.Items.Add(name, is_checked);
                            }

                            // disable list item if default so user cannot uncheck it
                            for (int i = 0; i < TranslatorsCheckedListBox.Items.Count; i++)
                            {
                                string item_text = TranslatorsCheckedListBox.Items[i].ToString();
                                foreach (string key in m_client.Book.TranslationInfos.Keys)
                                {
                                    string name = m_client.Book.TranslationInfos[key].Name;
                                    if (name == item_text)
                                    {
                                        if (
                                            (key == Client.DEFAULT_EMLAAEI_TEXT) ||
                                            (key == Client.DEFAULT_NEW_TRANSLATION) ||
                                            (key == Client.DEFAULT_OLD_TRANSLATION) ||
                                            (key == Client.DEFAULT_WORD_TRANSLATION) ||
                                            (key == Client.DEFAULT_TRANSLITERATION)
                                           )
                                        {
                                            TranslatorsCheckedListBox.SetItemCheckState(i, CheckState.Indeterminate);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            TranslatorsCheckedListBox.Sorted = true;
            TranslatorsCheckedListBox.EndUpdate();
            TranslatorsCheckedListBox.SelectedIndexChanged += new EventHandler(TranslatorsCheckedListBox_SelectedIndexChanged);
        }
    }
    private void TranslatorsCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        if (e.CurrentValue == CheckState.Indeterminate)
        {
            e.NewValue = e.CurrentValue;
        }
    }
    private void PopulateTranslatorComboBox()
    {
        try
        {
            TranslatorComboBox.SelectedIndexChanged -= new EventHandler(TranslatorComboBox_SelectedIndexChanged);
            TranslatorComboBox.BeginUpdate();

            if (m_client.Book != null)
            {
                if (m_client.Book.Verses.Count > 0)
                {
                    if (m_client.Book.Verses[0].Translations != null)
                    {
                        if (m_client.Book.Verses[0].Translations.Count == 0)
                        {
                            DownloadTranslations();
                        }

                        string backup_translation_name = null;
                        if (TranslatorComboBox.SelectedItem != null)
                        {
                            backup_translation_name = TranslatorComboBox.SelectedItem.ToString();
                        }

                        TranslatorComboBox.Items.Clear();
                        foreach (string key in m_client.Book.Verses[0].Translations.Keys)
                        {
                            string name = m_client.Book.TranslationInfos[key].Name;
                            TranslatorComboBox.Items.Add(name);
                        }

                        if (!String.IsNullOrEmpty(backup_translation_name))
                        {
                            bool found = false;
                            for (int i = 0; i < TranslatorComboBox.Items.Count; i++)
                            {
                                if (TranslatorComboBox.Items[i].ToString() == backup_translation_name)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                this.TranslatorComboBox.SelectedItem = backup_translation_name;
                            }
                            else
                            {
                                this.TranslatorComboBox.SelectedItem = m_client.Book.TranslationInfos[Client.DEFAULT_NEW_TRANSLATION].Name;
                            }
                        }
                        else // if all translations were cleared, we still have the 3 mandatory ones at minimum
                        {
                            if (this.TranslatorComboBox.Items.Count >= 3)
                            {
                                this.TranslatorComboBox.SelectedItem = m_client.Book.TranslationInfos[Client.DEFAULT_NEW_TRANSLATION].Name;
                            }
                            else // if user deleted one or more of the 3 mandatory translations manually
                            {
                                if (this.TranslatorComboBox.Items.Count > 0)
                                {
                                    this.TranslatorComboBox.SelectedItem = 0;
                                }
                                else // if no transaltion at all was left
                                {
                                    TranslatorComboBox.SelectedIndex = -1;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            TranslatorComboBox.SelectedIndex = -1;
        }
        finally
        {
            TranslatorComboBox.Sorted = true;
            TranslatorComboBox.EndUpdate();
            TranslatorComboBox.SelectedIndexChanged += new EventHandler(TranslatorComboBox_SelectedIndexChanged);
        }
    }
    private void TranslatorsCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
    }
    private void TranslationsApplySettingsLabel_Click(object sender, EventArgs e)
    {
        TranslatorsCheckedListBox.Visible = !TranslatorsCheckedListBox.Visible;
        TranslationsCancelSettingsLabel.Visible = TranslatorsCheckedListBox.Visible;

        if (TranslatorsCheckedListBox.Visible)
        {
            TranslatorsCheckedListBox.BringToFront();
            TranslationsCancelSettingsLabel.BringToFront();

            if (File.Exists(Globals.IMAGES_FOLDER + "/" + "apply.png"))
            {
                TranslationsApplySettingsLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "apply.png");
            }
            ToolTip.SetToolTip(TranslationsApplySettingsLabel, "Download translations");
        }
        else // download any newly checked translation(s)
        {
            TranslatorsCheckedListBox.SendToBack();
            TranslationsCancelSettingsLabel.SendToBack();

            if (File.Exists(Globals.IMAGES_FOLDER + "/" + "settings.png"))
            {
                TranslationsApplySettingsLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "settings.png");
            }
            ToolTip.SetToolTip(TranslationsApplySettingsLabel, "Add/Remove translations");

            int index_of_first_new_translation = DownloadTranslations();
            if ((index_of_first_new_translation >= 0) && (index_of_first_new_translation < TranslatorComboBox.Items.Count))
            {
                TranslatorComboBox.SelectedIndex = index_of_first_new_translation;
            }
            this.AcceptButton = null;
        }
    }
    private void TranslationsCancelSettingsLabel_Click(object sender, EventArgs e)
    {
        TranslatorsCheckedListBox.Visible = false;
        TranslatorsCheckedListBox.SendToBack();
        TranslationsCancelSettingsLabel.SendToBack();
        TranslationsCancelSettingsLabel.Visible = false;

        if (File.Exists(Globals.IMAGES_FOLDER + "/" + "settings.png"))
        {
            TranslationsApplySettingsLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "settings.png");
        }
        ToolTip.SetToolTip(TranslationsApplySettingsLabel, "Add/Remove translations");

        // remove any new user checkes 
        PopulateTranslatorsCheckedListBox();

        this.AcceptButton = null;
    }
    private void ClientSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
    {
        if ((ClientSplitContainer.Height - ClientSplitContainer.SplitterDistance) > 40)
        {
            m_information_box_top = this.ClientSplitContainer.SplitterDistance;
        }
    }
    private void TranslatorComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        Verse verse = GetCurrentVerse();
        if (verse != null)
        {
            DisplayTranslationAllowingEdit(verse);
        }
    }
    private int DownloadTranslations()
    {
        int index_of_first_new_transaltion = -1;

        try
        {
            m_checked_keys.Clear();
            if (m_client.Book.TranslationInfos != null)
            {
                foreach (string key in m_client.Book.TranslationInfos.Keys)
                {
                    if (
                        (key == Client.DEFAULT_EMLAAEI_TEXT) ||
                        (key == Client.DEFAULT_NEW_TRANSLATION) ||
                        (key == Client.DEFAULT_OLD_TRANSLATION) ||
                        (key == Client.DEFAULT_WORD_TRANSLATION) ||
                        (key == Client.DEFAULT_TRANSLITERATION)
                       )
                    {
                        m_checked_keys.Add(key);
                    }
                    else
                    {
                        foreach (int index in TranslatorsCheckedListBox.CheckedIndices)
                        {
                            if (m_client.Book.TranslationInfos[key].Name == TranslatorsCheckedListBox.Items[index].ToString())
                            {
                                m_checked_keys.Add(key);
                                break;
                            }
                        }
                    }
                }

                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = m_checked_keys.Count;
                ProgressBar.Value = 0;
                ProgressBar.Refresh();

                string[] keys = new string[m_client.Book.TranslationInfos.Keys.Count];
                m_client.Book.TranslationInfos.Keys.CopyTo(keys, 0);
                foreach (string key in keys)
                {
                    if (m_checked_keys.Contains(key))
                    {
                        ProgressBar.Value++;
                        ProgressBar.Refresh();

                        string translations_path = Globals.TRANSLATIONS_FOLDER + "/" + key + ".txt";
                        string offline_path = Globals.TRANSLATIONS_FOLDER + "/" + "Offline" + "/" + key + ".txt";

                        // delete file in translations_path if invalid
                        if (File.Exists(translations_path))
                        {
                            long filesize = (new FileInfo(translations_path)).Length;
                            if (filesize < 1024) // < 1kb invalid file
                            {
                                File.Delete(translations_path);
                            }
                        }

                        // delete file in offline_path if invalid
                        if (File.Exists(offline_path))
                        {
                            long filesize = (new FileInfo(offline_path)).Length;
                            if (filesize < 1024) // < 1kb invalid file
                            {
                                File.Delete(offline_path);
                            }
                        }

                        if (!File.Exists(translations_path))
                        {
                            // download file to offline_path
                            if (!File.Exists(offline_path))
                            {
                                DownloadFile(TranslationInfo.UrlPrefix + m_client.Book.TranslationInfos[key].Url, offline_path);
                            }

                            // copy to translations_path
                            if (File.Exists(offline_path))
                            {
                                long filesize = (new FileInfo(offline_path)).Length;
                                if (filesize < 1024) // < 1kb invalid file
                                {
                                    File.Delete(offline_path);
                                    m_client.UnloadTranslation(key);
                                }
                                else // copy valid file
                                {
                                    File.Copy(offline_path, translations_path);
                                    m_client.LoadTranslation(key);
                                }
                            }

                            // get index of first new translation
                            if (index_of_first_new_transaltion == -1)
                            {
                                int index_of_new_transaltion = -1;
                                foreach (int index in TranslatorsCheckedListBox.CheckedIndices)
                                {
                                    index_of_new_transaltion++;
                                    if (m_client.Book.TranslationInfos[key].Name == TranslatorsCheckedListBox.Items[index].ToString())
                                    {
                                        index_of_first_new_transaltion = index_of_new_transaltion;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else // unload translation
                    {
                        if (File.Exists(Globals.TRANSLATIONS_FOLDER + "/" + key + ".txt"))
                        {
                            m_client.UnloadTranslation(key);
                            File.Delete(Globals.TRANSLATIONS_FOLDER + "/" + key + ".txt");
                        }
                    }

                    Application.DoEvents();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            // stack overflow if verse.Translations is lost after copying
            PopulateTranslatorsCheckedListBox();
            PopulateTranslatorComboBox();

            if (m_client != null)
            {
                if (m_client.Selection != null)
                {
                    List<Verse> verses = m_client.Selection.Verses;
                    if (verses.Count > 0)
                    {
                        ProgressBar.Minimum = Verse.MIN_NUMBER;
                        ProgressBar.Maximum = Verse.MAX_NUMBER;
                        ProgressBar.Value = verses[0].Number;
                        ProgressBar.Refresh();
                    }
                }
            }
        }

        return index_of_first_new_transaltion;
    }
    private List<Verse> m_translated_verses = new List<Verse>();
    private void DisplayTranslations(Verse verse)
    {
        if (verse != null)
        {
            if (EditVerseTranslationLabel.Visible)
            {
                // display in edit mode
                DisplayTranslationAllowingEdit(verse);
            }
            else
            {
                //[ar.emlaaei   ]	001:006 اهْدِنَا الصِّرَاطَ الْمُسْتَقِيمَ
                //[en.pickthall ]	001:006 Show us the straight path,
                //[en.qarai     ]	001:006 Guide us on the straight path,
                //[en.transliter]	001:006 Ihdina alssirata almustaqeema
                //[en.wordbyword]	001:006 Guide us	(to) the path,	the straight,
                StringBuilder str = new StringBuilder();
                if (m_checked_keys.Count > 0)
                {
                    foreach (string key in m_checked_keys)
                    {
                        if (verse.Translations.ContainsKey(key))
                        {
                            str.AppendLine("[" + key.Pad(13) + "]\t" + verse.PaddedAddress + VERSE_ADDRESS_TRANSLATION_SEPARATOR + verse.Translations[key]);
                        }
                    }
                    if (str.Length > 2)
                    {
                        str.Remove(str.Length - 2, 2);
                    }
                }

                m_translated_verses.Clear();
                m_translated_verses.Add(verse);

                TranslationTextBox.WordWrap = false;
                TranslationTextBox.Text = str.ToString();
                TranslationTextBox.Refresh();

                m_readonly_mode = false;
                TranslationTextBox_ToggleReadOnly();
                EditVerseTranslationLabel.Visible = false;
            }
        }
        else
        {
            TranslationTextBox.WordWrap = false;
            TranslationTextBox.Text = "";
            TranslationTextBox.Refresh();

            m_readonly_mode = false;
            TranslationTextBox_ToggleReadOnly();
            EditVerseTranslationLabel.Visible = false;
        }
    }
    private void DisplayTranslations(List<Verse> verses)
    {
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                if ((verses.Count == 1) && (EditVerseTranslationLabel.Visible))
                {
                    // display in edit mode
                    DisplayTranslationAllowingEdit(verses[0]);
                }
                else
                {
                    StringBuilder str = new StringBuilder();
                    if (verses.Count > 0)
                    {
                        if (verses.Count == 1)
                        {
                            foreach (string key in m_checked_keys)
                            {
                                if (verses[0].Translations.ContainsKey(key))
                                {
                                    str.AppendLine("[" + key.Pad(13) + "]\t" + verses[0].PaddedAddress + VERSE_ADDRESS_TRANSLATION_SEPARATOR + verses[0].Translations[key]);
                                }
                            }
                            if (str.Length > 2)
                            {
                                str.Remove(str.Length - 2, 2);
                            }
                        }
                        else
                        {
                            if (TranslatorComboBox.SelectedItem != null)
                            {
                                string name = TranslatorComboBox.SelectedItem.ToString();
                                string key = m_client.GetTranslationKey(name);
                                if (verses[0].Translations.ContainsKey(key))
                                {
                                    foreach (Verse verse in verses)
                                    {
                                        str.AppendLine(verse.PaddedAddress + VERSE_ADDRESS_TRANSLATION_SEPARATOR + verse.Translations[key]);
                                    }
                                    if (str.Length > 2)
                                    {
                                        str.Remove(str.Length - 2, 2);
                                    }
                                }
                            }
                        }
                    }

                    TranslationTextBox.WordWrap = false;
                    TranslationTextBox.Text = str.ToString();
                    TranslationTextBox.Refresh();

                    m_translated_verses.Clear();
                    m_translated_verses.AddRange(verses);

                    m_readonly_mode = false;
                    TranslationTextBox_ToggleReadOnly();
                    EditVerseTranslationLabel.Visible = false;
                }
            }
            else
            {
                TranslationTextBox.WordWrap = false;
                TranslationTextBox.Text = "";
                TranslationTextBox.Refresh();

                m_readonly_mode = false;
                TranslationTextBox_ToggleReadOnly();
                EditVerseTranslationLabel.Visible = false;
            }
        }
    }
    private void DisplayTranslationAllowingEdit(Verse verse)
    {
        if (verse != null)
        {
            StringBuilder str = new StringBuilder();
            if (TranslatorComboBox.SelectedItem != null)
            {
                string name = TranslatorComboBox.SelectedItem.ToString();
                string key = m_client.GetTranslationKey(name);
                if (verse.Translations.ContainsKey(key))
                {
                    str.Append(verse.PaddedAddress + VERSE_ADDRESS_TRANSLATION_SEPARATOR + verse.Translations[key]);
                }
            }
            TranslationTextBox.WordWrap = true;
            TranslationTextBox.Text = str.ToString();
            TranslationTextBox.Refresh();
            m_translated_verses.Clear();
            m_translated_verses.Add(verse);

            m_readonly_mode = false;
            TranslationTextBox_ToggleReadOnly();
            EditVerseTranslationLabel.Visible = true;
        }
    }
    // tafseer
    private string m_tafseer = null;
    private List<Verse> m_tafseer_verses = new List<Verse>();
    private void PopulateTafseerComboBox()
    {
        try
        {
            TafseerComboBox.SelectedIndexChanged -= new EventHandler(TafseerComboBox_SelectedIndexChanged);
            TafseerComboBox.BeginUpdate();

            if (TafseerComboBox.Items.Count == 0)
            {
                string[] folders = Directory.GetDirectories(Globals.TAFSEERS_FOLDER, "*.*", SearchOption.TopDirectoryOnly);
                if (folders.Length > 0)
                {
                    TafseerComboBox.Items.Clear();
                    foreach (string folder in folders)
                    {
                        string language_folder = Path.GetFileNameWithoutExtension(folder);
                        string[] sub_folders = Directory.GetDirectories(folder, "*.*", SearchOption.TopDirectoryOnly);
                        if (sub_folders.Length > 0)
                        {
                            foreach (string sub_folder in sub_folders)
                            {
                                string tafseer_folder = Path.GetFileNameWithoutExtension(sub_folder);
                                TafseerComboBox.Items.Add(language_folder + " - " + tafseer_folder);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            TafseerComboBox.SelectedIndex = -1;
        }
        finally
        {
            TafseerComboBox.EndUpdate();
            TafseerComboBox.SelectedIndexChanged += new EventHandler(TafseerComboBox_SelectedIndexChanged);

            // trigger TafseerComboBox_SelectedIndexChanged
            if (TafseerComboBox.Items.Count > 0)
            {
                TafseerComboBox.SelectedIndex = 0;
            }
        }
    }
    private void TafseerComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (TafseerComboBox.SelectedIndex != -1)
        {
            m_tafseer = TafseerComboBox.SelectedItem.ToString();

            if (m_tafseer_verses.Count == 1)
            {
                Verse verse = m_tafseer_verses[0];
                DisplayTafseer(verse);
            }
            else if (m_tafseer_verses.Count > 1)
            {
                List<Verse> verses = new List<Verse>(m_tafseer_verses);
                DisplayTafseer(verses);
            }
            else // not set yet
            {
                Verse verse = GetCurrentVerse();
                if (verse != null)
                {
                    DisplayTafseer(verse);
                }
            }
        }
    }
    private void DisplayTafseer(Verse verse)
    {
        //if (TabControl.SelectedTab == TafseerTabPage)
        {
            this.Cursor = Cursors.WaitCursor;
            if (verse != null)
            {
                string tafseers_folder = Directory.GetCurrentDirectory() + "/" + Globals.TAFSEERS_FOLDER;
                string filename = (verse.Chapter.Number.ToString("000") + verse.NumberInChapter.ToString("000") + ".htm");
                string tafseer_langauge = m_tafseer.Substring(0, m_tafseer.IndexOf(" - "));
                string tafseer_folder = m_tafseer.Substring(m_tafseer.IndexOf(" - ") + 3);
                string path = tafseers_folder + "/" + tafseer_langauge + "/" + tafseer_folder + "/" + filename;
                try
                {
                    TafseerWebBrowser.Url = new Uri(path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\r\n" + path, Application.ProductName);
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }

                m_tafseer_verses.Clear();
                m_tafseer_verses.Add(verse);
            }
        }
    }
    private void DisplayTafseer(List<Verse> verses)
    {
        //if (TabControl.SelectedTab == TafseerTabPage)
        {
            this.Cursor = Cursors.WaitCursor;
            if (verses != null)
            {
                if (verses.Count > 0)
                {
                    List<Chapter> chapters = m_client.Book.GetChapters(verses);
                    if (chapters != null)
                    {
                        if (chapters.Count > 0)
                        {
                            string tafseers_folder = Directory.GetCurrentDirectory() + "/" + Globals.TAFSEERS_FOLDER;
                            string filename = (chapters[0].Number.ToString("000") + "000" + ".htm");
                            string tafseer_langauge = m_tafseer.Substring(0, m_tafseer.IndexOf(" - "));
                            string tafseer_folder = m_tafseer.Substring(m_tafseer.IndexOf(" - ") + 3);
                            string path = tafseers_folder + "/" + tafseer_langauge + "/" + tafseer_folder + "/" + filename;
                            try
                            {
                                TafseerWebBrowser.Url = new Uri(path);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message + "\r\n" + path, Application.ProductName);
                            }
                            finally
                            {
                                this.Cursor = Cursors.Default;
                            }

                            m_tafseer_verses.Clear();
                            m_tafseer_verses.AddRange(verses);
                        }
                        else
                        {
                            DisplayTafseer(verses[0]);
                        }
                    }
                }
            }
        }
    }
    // readonly mode
    private bool m_readonly_mode = true;
    private void EditVerseTranslationLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (!m_readonly_mode)
            {
                SaveTranslation();

                if (m_mp3player != null)
                {
                    if (m_mp3player.Paused)
                    {
                        PlayerPlayLabel_Click(null, null);
                    }
                }
            }
            else
            {
                if (m_mp3player != null)
                {
                    if (m_mp3player.Playing)
                    {
                        PlayerPlayLabel_Click(null, null);
                    }
                }
            }

            TranslationTextBox_ToggleReadOnly();
        }
        finally
        {
            Thread.Sleep(100);
            this.Cursor = Cursors.Default;
        }
    }
    private void TranslationTextBox_ToggleReadOnly()
    {
        m_readonly_mode = !m_readonly_mode;

        TranslationTextBox.ReadOnly = m_readonly_mode;
        TranslationTextBox.BackColor = m_readonly_mode ? Color.LightGray : SystemColors.Window;

        if (m_readonly_mode)
        {
            if (File.Exists(Globals.IMAGES_FOLDER + "/" + "text_unlocked.png"))
            {
                EditVerseTranslationLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "text_unlocked.png");
            }
            ToolTip.SetToolTip(EditVerseTranslationLabel, "Edit translation");
        }
        else
        {
            if (File.Exists(Globals.IMAGES_FOLDER + "/" + "save.png"))
            {
                EditVerseTranslationLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "save.png");
            }
            ToolTip.SetToolTip(EditVerseTranslationLabel, "Save translation");
        }
    }
    private void SaveTranslation()
    {
        if (m_client != null)
        {
            Verse verse = GetCurrentVerse();
            if (verse != null)
            {
                string translation = Client.DEFAULT_NEW_TRANSLATION;
                if (TranslatorComboBox.SelectedItem != null)
                {
                    translation = m_client.GetTranslationKey(TranslatorComboBox.SelectedItem.ToString());
                }

                int index = TranslationTextBox.Text.IndexOf(VERSE_ADDRESS_TRANSLATION_SEPARATOR);
                verse.Translations[translation] = TranslationTextBox.Text.Substring(index + 1);

                m_client.SaveTranslation(translation);
            }
        }
    }
    private void TranslationTextBox_KeyPress(object sender, KeyPressEventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (ModifierKeys == Keys.Control)
            {
                if (e.KeyChar == 19) // Ctrl+S == 19 
                {
                    if (!m_readonly_mode)
                    {
                        SaveTranslation();

                        if (m_mp3player != null)
                        {
                            if (m_mp3player.Paused)
                            {
                                PlayerPlayLabel_Click(null, null);
                            }
                        }

                        TranslationTextBox_ToggleReadOnly();

                        e.Handled = true; // stop annoying beep for no default button defined
                    }
                }
            }
        }
        finally
        {
            Thread.Sleep(100);
            this.Cursor = Cursors.Default;
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 09. Chapter Selection
    ///////////////////////////////////////////////////////////////////////////////
    private void PopulateChapterSortComboBox()
    {
        try
        {
            ChapterSortComboBox.SelectedIndexChanged -= new EventHandler(ChapterSortComboBox_SelectedIndexChanged);
            ChapterSortComboBox.BeginUpdate();

            ChapterSortComboBox.Items.Clear();
            ChapterSortComboBox.Items.Add("By Compilation");
            ChapterSortComboBox.Items.Add("By Revelation");
            ChapterSortComboBox.Items.Add("By Verses");
            ChapterSortComboBox.Items.Add("By Words");
            ChapterSortComboBox.Items.Add("By Letters");
            ChapterSortComboBox.Items.Add("By Value");

            ChapterSortComboBox.SelectedIndex = 0;
        }
        finally
        {
            ChapterSortComboBox.EndUpdate();
            ChapterSortComboBox.SelectedIndexChanged += new EventHandler(ChapterSortComboBox_SelectedIndexChanged);
        }
    }
    private void PopulateChapterComboBox()
    {
        try
        {
            ChapterComboBox.SelectedIndexChanged -= new EventHandler(ChapterComboBox_SelectedIndexChanged);
            if (m_client != null)
            {
                if (m_client.Book != null)
                {
                    ChapterComboBox.BeginUpdate();
                    ChapterComboBox.Items.Clear();
                    foreach (Chapter chapter in m_client.Book.Chapters)
                    {
                        ChapterComboBox.Items.Add(chapter.Number + " - " + chapter.Name);
                    }
                }
            }
        }
        finally
        {
            ChapterComboBox.EndUpdate();
            ChapterComboBox.SelectedIndexChanged += new EventHandler(ChapterComboBox_SelectedIndexChanged);
        }
    }
    private void PopulateChaptersListBox()
    {
        ChapterGroupBox.Text = " Chapters ";
        try
        {
            ChaptersListBox.SelectedIndexChanged -= new EventHandler(ChaptersListBox_SelectedIndexChanged);
            if (m_client != null)
            {
                if (m_client.Book != null)
                {
                    ChaptersListBox.BeginUpdate();
                    ChaptersListBox.Items.Clear();
                    foreach (Chapter chapter in m_client.Book.Chapters)
                    {
                        ChaptersListBox.Items.Add(chapter.Number + " - " + chapter.Name);
                    }
                }
            }
            DisplayChapterRevelationInfo();
        }
        finally
        {
            ChaptersListBox.EndUpdate();
            ChaptersListBox.SelectedIndexChanged += new EventHandler(ChaptersListBox_SelectedIndexChanged);
        }
    }
    private void ChapterComboBox_KeyDown(object sender, KeyEventArgs e)
    {
        bool SeparatorKeys = (
            ((e.KeyCode == Keys.Subtract) && (e.Modifiers != Keys.Shift))           // HYPHEN
            || ((e.KeyCode == Keys.OemMinus) && (e.Modifiers != Keys.Shift))        // HYPHEN
            || ((e.KeyCode == Keys.Oemcomma) && (e.Modifiers != Keys.Shift))        // COMMA
            || ((e.KeyCode == Keys.OemSemicolon) && (e.Modifiers == Keys.Shift))    // COLON
            );

        bool NumericKeys = (
            ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9))
            && e.Modifiers != Keys.Shift);

        bool EditKeys = (
            (e.KeyCode == Keys.A && e.Modifiers == Keys.Control) ||
            (e.KeyCode == Keys.Z && e.Modifiers == Keys.Control) ||
            (e.KeyCode == Keys.X && e.Modifiers == Keys.Control) ||
            (e.KeyCode == Keys.C && e.Modifiers == Keys.Control) ||
            (e.KeyCode == Keys.V && e.Modifiers == Keys.Control) ||
            e.KeyCode == Keys.Delete ||
            e.KeyCode == Keys.Back);

        bool NavigationKeys = (
            e.KeyCode == Keys.Up ||
            e.KeyCode == Keys.Right ||
            e.KeyCode == Keys.Down ||
            e.KeyCode == Keys.Left ||
            e.KeyCode == Keys.Home ||
            e.KeyCode == Keys.End);

        bool ExecuteKeys = (e.KeyCode == Keys.Enter);

        if (ExecuteKeys)
        {
            if (m_client != null)
            {
                try
                {
                    string text = ChapterComboBox.Text;
                    if (!String.IsNullOrEmpty(text))
                    {
                        // 1, 3-4, 5:55, 3-4:19, 6:19-23, 24:35-27:62
                        SelectionScope scope = SelectionScope.Verse;
                        List<int> indexes = new List<int>();

                        foreach (string part in text.Split(','))
                        {
                            string[] range_parts = part.Split('-');
                            if (range_parts.Length == 1) // 1 | 5:55
                            {
                                string[] sub_range_parts = part.Split(':');
                                if (sub_range_parts.Length == 1) // 1
                                {
                                    int chapter_number;
                                    if (int.TryParse(sub_range_parts[0], out chapter_number))
                                    {
                                        Chapter chapter = null;
                                        foreach (Chapter book_chapter in m_client.Book.Chapters)
                                        {
                                            if (book_chapter.Number == chapter_number)
                                            {
                                                chapter = book_chapter;
                                            }
                                        }
                                        if (chapter != null)
                                        {
                                            foreach (Verse verse in chapter.Verses)
                                            {
                                                indexes.Add(verse.Number - 1);
                                            }
                                        }
                                    }
                                }
                                else if (sub_range_parts.Length == 2) // 5:55
                                {
                                    int chapter_number;
                                    if (int.TryParse(sub_range_parts[0], out chapter_number)) // 5:55
                                    {
                                        int verse_number_in_chapter;
                                        if (int.TryParse(sub_range_parts[1], out verse_number_in_chapter))
                                        {
                                            Chapter chapter = null;
                                            foreach (Chapter book_chapter in m_client.Book.Chapters)
                                            {
                                                if (book_chapter.Number == chapter_number)
                                                {
                                                    chapter = book_chapter;
                                                }
                                            }
                                            if (chapter != null)
                                            {
                                                int from_verse_index = chapter.Verses[verse_number_in_chapter - 1].Number - 1;
                                                indexes.Add(from_verse_index);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (range_parts.Length == 2) // 3-4, 3-4:19, 6:19-23, 24:35-27:62
                            {
                                int from_chapter_number;
                                int to_chapter_number;
                                if (int.TryParse(range_parts[0], out from_chapter_number)) // 3-4
                                {
                                    if (int.TryParse(range_parts[1], out to_chapter_number))
                                    {
                                        if (from_chapter_number <= to_chapter_number)
                                        {
                                            for (int number = from_chapter_number; number <= to_chapter_number; number++)
                                            {
                                                Chapter chapter = null;
                                                foreach (Chapter book_chapter in m_client.Book.Chapters)
                                                {
                                                    if (book_chapter.Number == number)
                                                    {
                                                        chapter = book_chapter;
                                                    }
                                                }
                                                if (chapter != null)
                                                {
                                                    foreach (Verse verse in chapter.Verses)
                                                    {
                                                        indexes.Add(verse.Number - 1);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else // 3-4:19
                                    {
                                        // range_parts[0] == 3
                                        // range_parts[1] == 4:19
                                        string[] to_range_parts = range_parts[1].Split(':'); // 4:19
                                        if (to_range_parts.Length == 2)
                                        {
                                            int from_verse_number_in_chapter = 1; // not specified so start from beginning of chapter

                                            if (int.TryParse(to_range_parts[0], out to_chapter_number))  // 4
                                            {
                                                int to_verse_number_in_chapter;
                                                if (int.TryParse(to_range_parts[1], out to_verse_number_in_chapter)) // 19
                                                {
                                                    Chapter from_chapter = null;
                                                    foreach (Chapter book_chapter in m_client.Book.Chapters)
                                                    {
                                                        if (book_chapter.Number == from_chapter_number)
                                                        {
                                                            from_chapter = book_chapter;
                                                        }
                                                    }
                                                    if (from_chapter != null)
                                                    {
                                                        int from_verse_index = from_chapter.Verses[from_verse_number_in_chapter - 1].Number - 1;

                                                        Chapter to_chapter = null;
                                                        foreach (Chapter book_chapter in m_client.Book.Chapters)
                                                        {
                                                            if (book_chapter.Number == to_chapter_number)
                                                            {
                                                                to_chapter = book_chapter;
                                                            }
                                                        }
                                                        if (to_chapter != null)
                                                        {
                                                            int to_verse_index = to_chapter.Verses[to_verse_number_in_chapter - 1].Number - 1;
                                                            for (int i = from_verse_index; i <= to_verse_index; i++)
                                                            {
                                                                indexes.Add(i);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else // "range_parts[0]" contains a colon ':'  // "6:19"-23, "24:35"-27:62
                                {
                                    //int from_chapter_number;
                                    //int to_chapter_number;
                                    string[] from_parts = range_parts[0].Split(':');
                                    if (from_parts.Length == 2)
                                    {
                                        int from_verse_number_in_chapter;
                                        if (int.TryParse(from_parts[0], out from_chapter_number))
                                        {
                                            if (int.TryParse(from_parts[1], out from_verse_number_in_chapter))
                                            {
                                                string[] to_parts = range_parts[1].Split(':'); // "range_parts[1]" may or may not contain a colon ':'  // 6:19-"23", 24:35-"27:62"
                                                if (to_parts.Length == 1) // 6:19-"23"
                                                {
                                                    int to_verse_number_in_chapter;
                                                    if (int.TryParse(to_parts[0], out to_verse_number_in_chapter))
                                                    {
                                                        if (from_verse_number_in_chapter <= to_verse_number_in_chapter)  // XX:19-23
                                                        {
                                                            Chapter from_chapter = null;
                                                            foreach (Chapter book_chapter in m_client.Book.Chapters)
                                                            {
                                                                if (book_chapter.Number == from_chapter_number)
                                                                {
                                                                    from_chapter = book_chapter;
                                                                }
                                                            }
                                                            if (from_chapter != null)
                                                            {
                                                                int from_verse_index = from_chapter.Verses[from_verse_number_in_chapter - 1].Number - 1;
                                                                int to_verse_index = from_chapter.Verses[to_verse_number_in_chapter - 1].Number - 1;
                                                                for (int i = from_verse_index; i <= to_verse_index; i++)
                                                                {
                                                                    indexes.Add(i);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (to_parts.Length == 2) // 24:35-"27:62"
                                                {
                                                    int to_verse_number_in_chapter;
                                                    if (int.TryParse(to_parts[0], out to_chapter_number))
                                                    {
                                                        if (int.TryParse(to_parts[1], out to_verse_number_in_chapter))
                                                        {
                                                            if (from_chapter_number <= to_chapter_number)  // 24:XX-27:XX // only worry about chapters
                                                            {
                                                                Chapter from_chapter = null;
                                                                foreach (Chapter book_chapter in m_client.Book.Chapters)
                                                                {
                                                                    if (book_chapter.Number == from_chapter_number)
                                                                    {
                                                                        from_chapter = book_chapter;
                                                                    }
                                                                }
                                                                if (from_chapter != null)
                                                                {
                                                                    int from_verse_index = from_chapter.Verses[from_verse_number_in_chapter - 1].Number - 1;
                                                                    Chapter to_chapter = null;
                                                                    foreach (Chapter book_chapter in m_client.Book.Chapters)
                                                                    {
                                                                        if (book_chapter.Number == to_chapter_number)
                                                                        {
                                                                            to_chapter = book_chapter;
                                                                        }
                                                                    }
                                                                    if (to_chapter != null)
                                                                    {
                                                                        int to_verse_index = to_chapter.Verses[to_verse_number_in_chapter - 1].Number - 1;
                                                                        for (int i = from_verse_index; i <= to_verse_index; i++)
                                                                        {
                                                                            indexes.Add(i);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        m_client.Selection = new Selection(m_client.Book, scope, indexes);

                        bool add_to_history = ChapterComboBox.Focused;
                        DisplaySelection(add_to_history);
                    }
                }
                catch
                {
                    // log exception
                }
            }
        }

        // reject all other keys
        if (!(SeparatorKeys || NumericKeys || EditKeys || NavigationKeys))
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }
    private void ChapterComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            List<Chapter> chapters = m_client.Book.Chapters;
            int index = ChapterComboBox.SelectedIndex;
            if ((index >= 0) && (index < chapters.Count))
            {
                int chapter_index = chapters[index].Number - 1;

                if (
                     ChapterComboBox.Focused ||
                     ChapterVerseNumericUpDown.Focused ||
                     ChapterWordNumericUpDown.Focused ||
                     ChapterLetterNumericUpDown.Focused ||
                     PageNumericUpDown.Focused ||
                     StationNumericUpDown.Focused ||
                     PartNumericUpDown.Focused ||
                     GroupNumericUpDown.Focused ||
                     QuarterNumericUpDown.Focused ||
                     BowingNumericUpDown.Focused ||
                     VerseNumericUpDown.Focused ||
                     WordNumericUpDown.Focused ||
                     LetterNumericUpDown.Focused
                 )
                {
                    m_client.Selection = new Selection(m_client.Book, SelectionScope.Chapter, new List<int>() { chapter_index });
                }
                else if ((sender == PreviousBookmarkButton) || (sender == NextBookmarkButton))
                {
                }
                else if ((sender == BrowseHistoryBackwardButton) || (sender == BrowseHistoryForwardButton))
                {
                }
                else
                {
                }

                DisplaySelection(false);
            }
        }
    }
    private void UpdateMinMaxChapterVerseWordLetter(int chapter_index)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if (m_client.Book.Chapters != null)
                {
                    if ((chapter_index >= 0) && (chapter_index < m_client.Book.Chapters.Count))
                    {
                        Chapter chapter = m_client.Book.Chapters[chapter_index];
                        if (chapter != null)
                        {
                            try
                            {
                                ChapterVerseNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                                ChapterWordNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                                ChapterLetterNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);

                                ChapterVerseNumericUpDown.Minimum = 1;
                                ChapterVerseNumericUpDown.Maximum = chapter.Verses.Count;

                                ChapterWordNumericUpDown.Minimum = 1;
                                ChapterWordNumericUpDown.Maximum = chapter.WordCount;

                                ChapterLetterNumericUpDown.Minimum = 1;
                                ChapterLetterNumericUpDown.Maximum = chapter.LetterCount;
                            }
                            finally
                            {
                                ChapterVerseNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                                ChapterWordNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                                ChapterLetterNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                            }
                        }
                    }
                }
            }
        }
    }
    private void DisplayChapterRevelationInfo()
    {
        if (ChaptersListBox.SelectedIndices.Count > 0)
        {
            int index = ChaptersListBox.SelectedIndices[0];
            Chapter chapter = m_client.Book.Chapters[index];
            if (chapter != null)
            {
                // EITHER THIS
                string arabic_revelation_place = null;
                switch (chapter.RevelationPlace)
                {
                    case RevelationPlace.Both:
                        arabic_revelation_place = "كلاهما";
                        break;
                    case RevelationPlace.Makkah:
                        arabic_revelation_place = "مكّية";
                        break;
                    case RevelationPlace.Medina:
                        arabic_revelation_place = "مدنيّة";
                        break;
                    case RevelationPlace.None:
                        arabic_revelation_place = "مجهولة";
                        break;
                }
                ChapterGroupBox.Text = chapter.RevelationOrder.ToString() + " - " + arabic_revelation_place + " ";
                //ChapterGroupBox.Text = chapter.RevelationOrder.ToString().WithNth() + " - " + arabic_revelation_place + " ";

                //OR THIS LINE ONLY
                //ChapterGroupBox.Text = chapter.RevelationOrder.ToString() + " - " + chapter.RevelationPlace;
            }
        }
    }
    private void CopySelectionToChaptersListBoxIndexes()
    {
        if (m_client != null)
        {
            if (m_client.Selection != null)
            {
                List<Chapter> selected_chapters = m_client.Selection.Chapters;

                // add the chapters to the ChaptersListBox
                try
                {
                    ChaptersListBox.SelectedIndexChanged -= new EventHandler(ChaptersListBox_SelectedIndexChanged);
                    ChaptersListBox.SelectedIndices.Clear();
                    if (selected_chapters != null)
                    {
                        foreach (Chapter selected_chapter in m_client.Selection.Chapters)
                        {
                            int chapter_index = -1;
                            foreach (Chapter chapter in m_client.Book.Chapters)
                            {
                                chapter_index++;
                                if (selected_chapter == chapter)
                                {
                                    ChaptersListBox.SelectedIndices.Add(chapter_index);
                                    break;
                                }
                            }
                        }

                        DisplayChapterRevelationInfo();
                    }
                }
                finally
                {
                    ChaptersListBox.SelectedIndexChanged += new EventHandler(ChaptersListBox_SelectedIndexChanged);
                }
            }
        }
    }
    private void CopyChaptersListBoxIndexesToSelection()
    {
        if (ChaptersListBox.SelectedIndices.Count > 0)
        {
            SelectionScope scope = SelectionScope.Chapter;
            List<int> indexes = new List<int>();
            for (int i = 0; i < ChaptersListBox.SelectedIndices.Count; i++)
            {
                int index = ChaptersListBox.SelectedIndices[i];
                Chapter chapter = m_client.Book.Chapters[index];
                if (chapter != null)
                {
                    indexes.Add(chapter.Number - 1);
                }
            }
            m_client.Selection = new Selection(m_client.Book, scope, indexes);
        }
    }
    private void ChaptersListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender == ChaptersListBox)
        {
            if (m_client != null)
            {
                CopyChaptersListBoxIndexesToSelection();
                DisplayChapterRevelationInfo();
                DisplaySelection(false);
            }
        }
    }
    private int m_previous_selected_index = -1;
    private void ChaptersListBox_MouseMove(object sender, MouseEventArgs e)
    {
        int selected_index = ChaptersListBox.IndexFromPoint(e.Location);
        if (selected_index != m_previous_selected_index)
        {
            m_previous_selected_index = selected_index;
            Chapter chapter = GetChapter(selected_index);
            if (chapter != null)
            {
                ToolTip.SetToolTip(ChaptersListBox, chapter.Number.ToString() + " - " + chapter.TransliteratedName + "\r\n" +
                                                    chapter.EnglishName + "\r\n" +
                                                    "Revelation Order \t " + chapter.RevelationOrder.ToString() + "\r\n" +
                                                    "Revelation Place \t " + chapter.RevelationPlace.ToString() + "\r\n" +
                                                    "Verses  \t\t " + chapter.Verses.Count.ToString() + "\r\n" +
                                                    "Words   \t\t " + chapter.WordCount.ToString() + "\r\n" +
                                                    "Letters \t\t " + chapter.LetterCount.ToString() + "\r\n" +
                                                    "Unique Letters \t " + chapter.UniqueLetters.Count.ToString()
                                                    );
            }
        }
    }
    private Chapter GetChapter(int selected_index)
    {
        if (((selected_index >= 0)) && ((selected_index < ChaptersListBox.Items.Count)))
        {
            string number_name_str = ChaptersListBox.Items[selected_index].ToString();
            string number_str = number_name_str.Substring(0, number_name_str.IndexOf(" - "));
            try
            {
                int number = int.Parse(number_str);
                if (((number > 0)) && ((number <= Chapter.MAX_NUMBER)))
                {
                    foreach (Chapter chapter in m_client.Book.Chapters)
                    {
                        if (chapter.Number == number)
                        {
                            return chapter;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        return null;
    }
    private void PinTheKeyCheckBox_CheckStateChanged(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (PinTheKeyCheckBox.CheckState == CheckState.Unchecked)
            {
                Chapter.PinTheKey = false;
            }
            else if (PinTheKeyCheckBox.CheckState == CheckState.Indeterminate)
            {
                Chapter.PinTheKey = null;
            }
            else if (PinTheKeyCheckBox.CheckState == CheckState.Checked)
            {
                Chapter.PinTheKey = true;
            }

            SortChapters();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void ChapterSortComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (m_client.Book != null)
                {
                    switch (ChapterSortComboBox.SelectedIndex)
                    {
                        case 0:
                            Chapter.SortMethod = ChapterSortMethod.ByCompilation;
                            ToolTip.SetToolTip(ChapterSortComboBox, "حسب الورود في الكتاب");
                            break;
                        case 1:
                            Chapter.SortMethod = ChapterSortMethod.ByRevelation;
                            ToolTip.SetToolTip(ChapterSortComboBox, "حسب نزول السور");
                            break;
                        case 2:
                            Chapter.SortMethod = ChapterSortMethod.ByVerses;
                            ToolTip.SetToolTip(ChapterSortComboBox, "حسب عدد ءايات السور");
                            break;
                        case 3:
                            Chapter.SortMethod = ChapterSortMethod.ByWords;
                            ToolTip.SetToolTip(ChapterSortComboBox, "حسب عدد كلمات السور");
                            break;
                        case 4:
                            Chapter.SortMethod = ChapterSortMethod.ByLetters;
                            ToolTip.SetToolTip(ChapterSortComboBox, "حسب عدد حروف السور");
                            break;
                        case 5:
                            Chapter.SortMethod = ChapterSortMethod.ByValue;
                            ToolTip.SetToolTip(ChapterSortComboBox, "حسب قيم السور");
                            break;
                    }

                    SortChapters();
                }
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void ChapterSortLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (m_client.Book != null)
                {
                    if (Chapter.SortOrder == ChapterSortOrder.Ascending)
                    {
                        Chapter.SortOrder = ChapterSortOrder.Descending;
                        if (File.Exists("Images/arrow_down.png"))
                        {
                            ChapterSortLabel.Image = new Bitmap("Images/arrow_down.png");
                            ToolTip.SetToolTip(ChapterSortLabel, "ترتيب تنازلي Descending");
                        }
                    }
                    else
                    {
                        Chapter.SortOrder = ChapterSortOrder.Ascending;
                        if (File.Exists("Images/arrow_up.png"))
                        {
                            ChapterSortLabel.Image = new Bitmap("Images/arrow_up.png");
                            ToolTip.SetToolTip(ChapterSortLabel, "ترتيب نصاعدي Ascending");
                        }
                    }

                    SortChapters();
                }
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void SortChapters()
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if (m_client.Book.Chapters != null)
                {
                    // sort and re-populate lists
                    m_client.Book.Chapters.Sort();
                    PopulateChaptersListBox();

                    CopySelectionToChaptersListBoxIndexes();
                    CopyChaptersListBoxIndexesToSelection();

                    DisplaySelection(false);
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 10. Display Selection
    ///////////////////////////////////////////////////////////////////////////////
    private bool m_is_selection_mode = false;
    private int m_word_number_in_verse = -1;
    private int m_letter_number_in_verse = -1;
    private int m_word_number_in_chapter = -1;
    private int m_letter_number_in_chapter = -1;
    private void NumericUpDown_Enter(object sender, EventArgs e)
    {
        SearchGroupBox_Leave(null, null);
        this.AcceptButton = null;

        // Ctrl+Click factorizes number
        if (ModifierKeys == Keys.Control)
        {
            long value = 0L;
            if (sender == ChapterComboBox)
            {
                if (ChapterComboBox.SelectedIndex != -1)
                {
                    string[] parts = ChapterComboBox.Text.Split('-');
                    if (parts.Length > 0)
                    {
                        value = long.Parse(parts[0]);
                    }
                }
            }
            else if (sender is NumericUpDown)
            {
                try
                {
                    value = (long)(sender as NumericUpDown).Value;
                }
                catch
                {
                    value = -1L; // error
                }
            }
            else if (sender is TextBox)
            {
                try
                {
                    value = long.Parse((sender as TextBox).Text);
                }
                catch
                {
                    value = -1L; // error
                }
            }
            else
            {
                value = -1L; // error
            }

            if (sender == FindByFrequencySumNumericUpDown)
            {
                FactorizeValue(value, "FreqSum");
            }
            else
            {
                FactorizeValue(value, "Position");
            }
        }
    }
    private void NumericUpDown_Leave(object sender, EventArgs e)
    {
        this.AcceptButton = null;
    }
    private void NumericUpDown_ValueChanged(object sender, EventArgs e)
    {
        Control control = sender as NumericUpDown;
        if (control != null)
        {
            if (control.Focused)
            {
                DisplayNumericSelection(control);
            }
        }
    }
    private void DisplayNumericSelection(Control control)
    {
        if (control is NumericUpDown)
        {
            if (control.Focused)
            {
                try
                {
                    ChapterVerseNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    ChapterWordNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    ChapterLetterNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    PageNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    StationNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    PartNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    GroupNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    QuarterNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    BowingNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    VerseNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    WordNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                    LetterNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);

                    int number = (int)((control as NumericUpDown).Value);

                    // backup number before as it will be overwritten with verse.Number
                    // if control is WordNumericUpDown OR LetterNumericUpDown or
                    // if control is ChapterWordNumericUpDown OR ChapterLetterNumericUpDown 
                    int word_number = 0;
                    int letter_number = 0;
                    if ((control == WordNumericUpDown) || (control == ChapterLetterNumericUpDown))
                    {
                        word_number = number;
                    }
                    else if ((control == LetterNumericUpDown) || (control == ChapterLetterNumericUpDown))
                    {
                        letter_number = number;
                    }

                    if (m_client != null)
                    {
                        SelectionScope scope = SelectionScope.Book;

                        if (control == ChapterVerseNumericUpDown)
                        {
                            if (m_client.Book.Verses != null)
                            {
                                scope = SelectionScope.Verse;

                                if (m_client.Book.Chapters != null)
                                {
                                    int verse_number_in_chapter = (int)ChapterVerseNumericUpDown.Value;
                                    Chapter chapter = GetChapter(ChaptersListBox.SelectedIndex);
                                    if (chapter != null)
                                    {
                                        if (chapter.Verses != null)
                                        {
                                            if (chapter.Verses != null)
                                            {
                                                if (chapter.Verses.Count > verse_number_in_chapter - 1)
                                                {
                                                    Verse verse = chapter.Verses[verse_number_in_chapter - 1];
                                                    if (verse != null)
                                                    {
                                                        number = verse.Number;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if ((control == ChapterWordNumericUpDown) || (control == ChapterLetterNumericUpDown))
                        {
                            if (m_client.Book.Verses != null)
                            {
                                scope = SelectionScope.Verse;

                                int chapter_index = ChapterComboBox.SelectedIndex;
                                if (m_client.Book.Chapters != null)
                                {
                                    if ((chapter_index >= 0) && (chapter_index < m_client.Book.Chapters.Count))
                                    {
                                        Chapter chapter = m_client.Book.Chapters[chapter_index];
                                        if (chapter != null)
                                        {
                                            if (chapter.Verses != null)
                                            {
                                                Verse verse = null;
                                                if (control == ChapterWordNumericUpDown)
                                                {
                                                    word_number = number + chapter.Verses[0].Words[0].Number - 1;
                                                    verse = m_client.Book.GetVerseByWordNumber(word_number);
                                                }
                                                else if (control == ChapterLetterNumericUpDown)
                                                {
                                                    letter_number = number + chapter.Verses[0].Words[0].Letters[0].Number - 1;
                                                    verse = m_client.Book.GetVerseByLetterNumber(letter_number);
                                                }

                                                if (verse != null)
                                                {
                                                    number = verse.Number;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (control == PageNumericUpDown)
                        {
                            if (m_client.Book.Pages != null)
                            {
                                scope = SelectionScope.Page;
                            }
                        }
                        else if (control == StationNumericUpDown)
                        {
                            if (m_client.Book.Stations != null)
                            {
                                scope = SelectionScope.Station;
                            }
                        }
                        else if (control == PartNumericUpDown)
                        {
                            if (m_client.Book.Parts != null)
                            {
                                scope = SelectionScope.Part;
                            }
                        }
                        else if (control == GroupNumericUpDown)
                        {
                            if (m_client.Book.Groups != null)
                            {
                                scope = SelectionScope.Group;
                            }
                        }
                        else if (control == QuarterNumericUpDown)
                        {
                            if (m_client.Book.Quarters != null)
                            {
                                scope = SelectionScope.Quarter;
                            }
                        }
                        else if (control == BowingNumericUpDown)
                        {
                            if (m_client.Book.Bowings != null)
                            {
                                scope = SelectionScope.Bowing;
                            }
                        }
                        else if (control == VerseNumericUpDown)
                        {
                            if (m_client.Book.Verses != null)
                            {
                                scope = SelectionScope.Verse;
                            }
                        }
                        else if (control == WordNumericUpDown)
                        {
                            Verse verse = m_client.Book.GetVerseByWordNumber(word_number);
                            if (verse != null)
                            {
                                scope = SelectionScope.Verse;
                                number = verse.Number;
                            }
                        }
                        else if (control == LetterNumericUpDown)
                        {
                            Verse verse = m_client.Book.GetVerseByLetterNumber(letter_number);
                            if (verse != null)
                            {
                                scope = SelectionScope.Verse;
                                number = verse.Number;
                            }
                        }
                        else
                        {
                            // do nothing
                        }

                        // if selection has changed
                        if (m_client.Selection != null)
                        {
                            if (
                                (m_client.Selection.Scope != scope)
                                ||
                                ((m_client.Selection.Indexes.Count > 0) && (m_client.Selection.Indexes[0] != (number - 1)))
                               )
                            {
                                List<int> indexes = new List<int>() { number - 1 };
                                m_client.Selection = new Selection(m_client.Book, scope, indexes);
                                DisplaySelection(true);
                            }
                        }

                        if ((control == WordNumericUpDown) || (control == ChapterWordNumericUpDown))
                        {
                            SelectWord(word_number);
                        }
                        else if ((control == LetterNumericUpDown) || (control == ChapterLetterNumericUpDown))
                        {
                            SelectLetter(letter_number);
                        }
                        else
                        {
                            // unknown
                        }
                    }
                }
                finally
                {
                    ChapterVerseNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    ChapterWordNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    ChapterLetterNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    PageNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    StationNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    PartNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    GroupNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    QuarterNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    BowingNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    VerseNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    WordNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                    LetterNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                }
            }
        }
    }
    private void DisplaySelection(bool add_to_history)
    {
        try
        {
            if ((m_mp3player.Playing) || (m_mp3player.Paused))
            {
                PlayerStopLabel_Click(null, null);
            }

            SwitchToMainTextBox();

            MainTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            MainTextBox.BeginUpdate();

            BookmarkTextBox.Enabled = true;

            if (m_client != null)
            {
                m_is_selection_mode = true;

                AutoCompleteHeaderLabel.Visible = false;
                AutoCompleteListBox.Visible = false;
                AutoCompleteListBox.SendToBack();

                this.Text = Application.ProductName + " | " + GetSummarizedFindScope();
                UpdateFindScope();

                UpdateHeaderLabel();

                DisplaySelectionText();

                CalculateCurrentValue();

                DisplaySelectionPositions();

                if (m_client.Selection != null)
                {
                    DisplayVersesWordsLetters(m_client.Selection.Verses);
                }

                CalculateLetterStatistics();
                DisplayLetterStatistics();

                CalculatePhraseLetterStatistics();
                DisplayPhraseLetterStatistics();

                MainTextBox.ClearHighlight();
                MainTextBox.AlignToStart();

                m_current_selection_verse_index = 0;
                PrepareVerseToPlay();

                if (m_client.Selection != null)
                {
                    List<Verse> verses = m_client.Selection.Verses;
                    if (verses != null)
                    {
                        DisplayTranslations(verses);
                        DisplayTafseer(verses);

                        if (verses.Count > 0)
                        {
                            if (add_to_history)
                            {
                                AddSelectionHistoryItem();
                            }

                            m_similarity_current_verse = verses[0];
                        }
                    }

                    // display selection's note (if any)
                    DisplayNote(m_client.GetBookmark(m_client.Selection));
                }

                if (m_client.NumerologySystem.TextMode.Contains("Images"))
                {
                    PictureBoxPanel.BringToFront();
                    PictureBoxPanel.Visible = true;
                    DisplayCurrentPage();
                }
                else if (PictureBoxEx.Visible)
                {
                    RedrawCurrentGraph();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            MainTextBox.EndUpdate();
            MainTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void DisplaySelectionText()
    {
        if (m_client != null)
        {
            if (m_client.Selection != null)
            {
                List<Verse> verses = m_client.Selection.Verses;
                StringBuilder str = new StringBuilder();
                if (verses != null)
                {
                    if (verses.Count > 0)
                    {
                        foreach (Verse verse in verses)
                        {
                            str.Append(verse.Text + verse.Endmark);
                        }
                        if (str.Length > 1)
                        {
                            str.Remove(str.Length - 1, 1); // last space in " {###} "   OR  \n
                        }
                    }
                }
                m_current_text = str.ToString();
                MainTextBox.Text = m_current_text;

                ColorizeGoldenRatios();
            }
        }
    }
    private void RefreshLanguageType(string text)
    {
        if (text.IsArabic())
        {
            SetLanguageType(LanguageType.Arabic);
        }
        else
        {
            SetLanguageType(LanguageType.Translation);
        }
        UpdateFindByTextControls();
    }
    private void DisplaySelectionPositions()
    {
        if (m_client != null)
        {
            if (m_client.Selection != null)
            {
                List<Verse> verses = m_client.Selection.Verses;
                if (verses != null)
                {
                    if (verses.Count > 0)
                    {
                        Verse verse = verses[0];
                        if (verse != null)
                        {
                            // show postion of verse in the Quran visually
                            ProgressBar.Minimum = Verse.MIN_NUMBER;
                            ProgressBar.Maximum = Verse.MAX_NUMBER;
                            ProgressBar.Value = verse.Number;
                            ProgressBar.Refresh();

                            if (verse.Chapter != null)
                            {
                                UpdateMinMaxChapterVerseWordLetter(verse.Chapter.Number - 1);
                            }

                            if (ChapterComboBox.Items.Count > 0)
                            {
                                // without this guard, we cannot select more than 1 chapter in ChaptersListBox and
                                // we cannot move backward/forward inside the ChaptersListBox using Backspace
                                if (!ChaptersListBox.Focused)
                                {
                                    CopySelectionToChaptersListBoxIndexes();
                                }
                            }
                            UpdateVersePositions(verse);

                            Bookmark bookmark = m_client.GotoBookmark(m_client.Selection.Scope, m_client.Selection.Indexes);
                            if (bookmark != null)
                            {
                                BookmarkTextBox.ForeColor = m_note_view_color;
                                BookmarkTextBox.Text = bookmark.Note;
                                string hint = "Creation Time" + "\t" + bookmark.CreatedTime + "\r\n"
                                            + "Last Modified" + "\t" + bookmark.LastModifiedTime;
                                ToolTip.SetToolTip(BookmarkTextBox, hint);
                                UpdateBookmarkHistoryButtons();
                            }
                            else
                            {
                                DisplayNoteWritingInstruction();
                            }
                        }
                    }
                }
            }
        }
    }
    private void DisplayCurrentPositions()
    {
        if (m_active_textbox.Lines.Length > 0)
        {
            Verse verse = GetCurrentVerse();
            if (verse != null)
            {
                // show postion of verse in the Quran visually
                ProgressBar.Minimum = Verse.MIN_NUMBER;
                ProgressBar.Maximum = Verse.MAX_NUMBER;
                ProgressBar.Value = verse.Number;
                ProgressBar.Refresh();

                if (verse.Chapter != null)
                {
                    UpdateMinMaxChapterVerseWordLetter(verse.Chapter.Number - 1);
                }

                if (ChapterComboBox.Items.Count > 0)
                {
                    // without this guard, we cannot select more than 1 chapter in ChaptersListBox and
                    // we cannot move backward/forward inside the ChaptersListBox using Backspace
                    if (!ChaptersListBox.Focused)
                    {
                        CopySelectionToChaptersListBoxIndexes();
                    }
                }
                UpdateVersePositions(verse);
            }
        }
    }
    private void UpdateVersePositions(Verse verse)
    {
        if (verse != null)
        {
            try
            {
                ChapterComboBox.SelectedIndexChanged -= new EventHandler(ChapterComboBox_SelectedIndexChanged);
                ChapterVerseNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                ChapterWordNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                ChapterLetterNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                PageNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                StationNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                PartNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                GroupNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                QuarterNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                BowingNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                VerseNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                WordNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);
                LetterNumericUpDown.ValueChanged -= new EventHandler(NumericUpDown_ValueChanged);

                if (verse.Chapter != null)
                {
                    ChapterComboBox.SelectedIndex = verse.Chapter.Number - 1;
                }

                if ((verse.NumberInChapter >= Verse.MIN_NUMBER) && (verse.NumberInChapter <= Verse.MAX_NUMBER))
                {
                    if (verse.Chapter != null)
                    {
                        ChapterVerseNumericUpDown.Value = (verse.NumberInChapter > ChapterVerseNumericUpDown.Maximum) ? ChapterVerseNumericUpDown.Maximum : verse.NumberInChapter;
                    }
                }

                if (verse.Page != null)
                {
                    PageNumericUpDown.Value = verse.Page.Number;
                }
                if (verse.Station != null)
                {
                    StationNumericUpDown.Value = verse.Station.Number;
                }
                if (verse.Part != null)
                {
                    PartNumericUpDown.Value = verse.Part.Number;
                }
                if (verse.Group != null)
                {
                    GroupNumericUpDown.Value = verse.Group.Number;
                }
                if (verse.Quarter != null)
                {
                    QuarterNumericUpDown.Value = verse.Quarter.Number;
                }
                if (verse.Bowing != null)
                {
                    BowingNumericUpDown.Value = verse.Bowing.Number;
                }
                VerseNumericUpDown.Value = verse.Number;

                int char_index = m_active_textbox.SelectionStart;
                int line_index = m_active_textbox.GetLineFromCharIndex(char_index);

                Word word = GetWordAtChar(char_index);
                int word_index_in_verse = word.NumberInVerse - 1;
                Letter letter = GetLetterAtChar(char_index);
                if (letter == null) letter = GetLetterAtChar(char_index - 1); // (Ctrl_End)
                if ((word != null) && (letter != null))
                {
                    int letter_index_in_verse = letter.NumberInVerse - 1;
                    int word_number = verse.Words[0].Number + word_index_in_verse;
                    if (word_number > WordNumericUpDown.Maximum)
                    {
                        WordNumericUpDown.Value = WordNumericUpDown.Maximum;
                    }
                    else if (word_number < WordNumericUpDown.Minimum)
                    {
                        WordNumericUpDown.Value = WordNumericUpDown.Minimum;
                    }
                    else
                    {
                        WordNumericUpDown.Value = word_number;
                    }

                    int letter_number = verse.Words[0].Letters[0].Number + letter_index_in_verse;
                    if (letter_number > LetterNumericUpDown.Maximum)
                    {
                        LetterNumericUpDown.Value = LetterNumericUpDown.Maximum;
                    }
                    else if (letter_number < LetterNumericUpDown.Minimum)
                    {
                        LetterNumericUpDown.Value = LetterNumericUpDown.Minimum;
                    }
                    else
                    {
                        LetterNumericUpDown.Value = letter_number;
                    }

                    m_word_number_in_verse = word_index_in_verse + 1;
                    m_letter_number_in_verse = letter_index_in_verse + 1;
                    int word_count = 0;
                    int letter_count = 0;
                    if (verse.Chapter != null)
                    {
                        foreach (Verse chapter_verse in verse.Chapter.Verses)
                        {
                            if (chapter_verse.NumberInChapter < verse.NumberInChapter)
                            {
                                word_count += chapter_verse.Words.Count;
                                letter_count += chapter_verse.LetterCount;
                            }
                        }
                    }
                    m_word_number_in_chapter = word_count + m_word_number_in_verse;
                    m_letter_number_in_chapter = letter_count + m_letter_number_in_verse;

                    if (m_word_number_in_chapter > ChapterWordNumericUpDown.Maximum)
                    {
                        ChapterWordNumericUpDown.Value = ChapterWordNumericUpDown.Maximum;
                    }
                    else if (m_word_number_in_chapter < ChapterWordNumericUpDown.Minimum)
                    {
                        ChapterWordNumericUpDown.Value = ChapterWordNumericUpDown.Minimum;
                    }
                    else
                    {
                        ChapterWordNumericUpDown.Value = m_word_number_in_chapter;
                    }

                    if (m_letter_number_in_chapter > ChapterLetterNumericUpDown.Maximum)
                    {
                        ChapterLetterNumericUpDown.Value = ChapterLetterNumericUpDown.Maximum;
                    }
                    else if (m_letter_number_in_chapter < ChapterLetterNumericUpDown.Minimum)
                    {
                        ChapterLetterNumericUpDown.Value = ChapterLetterNumericUpDown.Minimum;
                    }
                    else
                    {
                        ChapterLetterNumericUpDown.Value = m_letter_number_in_chapter;
                    }

                    ColorizePositionNumbers();
                    ColorizePositionControls();

                    // update player buttons
                    List<Verse> verses = null;
                    if (m_found_verses_displayed)
                    {
                        verses = m_client.FoundVerses;
                    }
                    else
                    {
                        if (m_client.Selection != null)
                        {
                            verses = m_client.Selection.Verses;
                        }
                    }
                    if (verses != null)
                    {
                        if (verses.Count > 0)
                        {
                            PlayerPreviousLabel.Enabled = (verse.Number != verses[0].Number);
                            PlayerNextLabel.Enabled = (verse.Number != verses[verses.Count - 1].Number);
                        }
                        else
                        {
                            PlayerPreviousLabel.Enabled = false;
                            PlayerNextLabel.Enabled = false;
                        }
                    }
                }
            }
            catch
            {
                // ignore poosible error due to non-Arabic search result
                // showing verses with more words than the words in the Arabic verse
                // and throwing exception when assigned to WordNumericUpDown.Value or LetterNumericUpDown.Value
            }
            finally
            {
                ChapterComboBox.SelectedIndexChanged += new EventHandler(ChapterComboBox_SelectedIndexChanged);
                ChapterVerseNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                ChapterWordNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                ChapterLetterNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                PageNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                StationNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                PartNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                GroupNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                QuarterNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                BowingNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                VerseNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                WordNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
                LetterNumericUpDown.ValueChanged += new EventHandler(NumericUpDown_ValueChanged);
            }
        }
    }
    private void ColorizePositionNumbers()
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if ((ChapterComboBox.SelectedIndex >= 0) && (ChapterComboBox.SelectedIndex < m_client.Book.Chapters.Count))
                {
                    int chapter_number = m_client.Book.Chapters[ChapterComboBox.SelectedIndex].Number;
                    ChapterComboBox.ForeColor = GetNumberTypeColor(chapter_number);
                }
            }
        }

        ChapterVerseNumericUpDown.ForeColor = GetNumberTypeColor((int)ChapterVerseNumericUpDown.Value);
        ChapterWordNumericUpDown.ForeColor = GetNumberTypeColor((int)ChapterWordNumericUpDown.Value);
        ChapterLetterNumericUpDown.ForeColor = GetNumberTypeColor((int)ChapterLetterNumericUpDown.Value);
        PageNumericUpDown.ForeColor = GetNumberTypeColor((int)PageNumericUpDown.Value);
        StationNumericUpDown.ForeColor = GetNumberTypeColor((int)StationNumericUpDown.Value);
        PartNumericUpDown.ForeColor = GetNumberTypeColor((int)PartNumericUpDown.Value);
        GroupNumericUpDown.ForeColor = GetNumberTypeColor((int)GroupNumericUpDown.Value);
        QuarterNumericUpDown.ForeColor = GetNumberTypeColor((int)QuarterNumericUpDown.Value);
        BowingNumericUpDown.ForeColor = GetNumberTypeColor((int)BowingNumericUpDown.Value);
        VerseNumericUpDown.ForeColor = GetNumberTypeColor((int)VerseNumericUpDown.Value);
        WordNumericUpDown.ForeColor = GetNumberTypeColor((int)WordNumericUpDown.Value);
        LetterNumericUpDown.ForeColor = GetNumberTypeColor((int)LetterNumericUpDown.Value);

        ChapterComboBox.Refresh();
        ChapterVerseNumericUpDown.Refresh();
        ChapterWordNumericUpDown.Refresh();
        ChapterLetterNumericUpDown.Refresh();
        PageNumericUpDown.Refresh();
        StationNumericUpDown.Refresh();
        PartNumericUpDown.Refresh();
        GroupNumericUpDown.Refresh();
        QuarterNumericUpDown.Refresh();
        BowingNumericUpDown.Refresh();
        VerseNumericUpDown.Refresh();
        WordNumericUpDown.Refresh();
        LetterNumericUpDown.Refresh();
    }
    private void ColorizePositionControls()
    {
        if (m_client != null)
        {
            // Clear BackColors
            ChapterComboBox.BackColor = SystemColors.Window;
            ChapterVerseNumericUpDown.BackColor = SystemColors.Window;
            ChapterWordNumericUpDown.BackColor = SystemColors.Window;
            ChapterLetterNumericUpDown.BackColor = SystemColors.Window;
            PageNumericUpDown.BackColor = SystemColors.Window;
            StationNumericUpDown.BackColor = SystemColors.Window;
            PartNumericUpDown.BackColor = SystemColors.Window;
            GroupNumericUpDown.BackColor = SystemColors.Window;
            QuarterNumericUpDown.BackColor = SystemColors.Window;
            BowingNumericUpDown.BackColor = SystemColors.Window;
            VerseNumericUpDown.BackColor = SystemColors.Window;
            WordNumericUpDown.BackColor = SystemColors.Window;
            LetterNumericUpDown.BackColor = SystemColors.Window;

            if (m_client.Selection != null)
            {
                switch (m_client.Selection.Scope)
                {
                    case SelectionScope.Book:
                        {
                        }
                        break;
                    case SelectionScope.Chapter:
                        {
                            ChapterComboBox.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Page:
                        {
                            PageNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Station:
                        {
                            StationNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Part:
                        {
                            PartNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Group:
                        {
                            GroupNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Quarter:
                        {
                            QuarterNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Bowing:
                        {
                            BowingNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Verse:
                        {
                            ChapterVerseNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                            VerseNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Word:
                        {
                            WordNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                            ChapterWordNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    case SelectionScope.Letter:
                        {
                            LetterNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                            ChapterLetterNumericUpDown.BackColor = Color.FromArgb(255, 255, 192);
                        }
                        break;
                    default: // Unknown
                        {
                            MessageBox.Show("Unknown selection scope.", Application.ProductName);
                        }
                        break;
                }
            }
        }
    }
    private void SelectWord(int word_number)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                Verse verse = m_client.Book.GetVerseByWordNumber(word_number);
                if (verse != null)
                {
                    word_number -= verse.Words[0].Number;
                    m_active_textbox.Select(verse.Words[word_number].Position, verse.Words[word_number].Text.Length);
                }
            }
        }
    }
    private void SelectLetter(int letter_number)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                Word word = m_client.Book.GetWordByLetterNumber(letter_number);
                if (word != null)
                {
                    letter_number -= word.Letters[0].Number;
                    int letter_position = word.Position + letter_number;
                    int letter_length = 1;
                    m_active_textbox.Select(letter_position, letter_length);
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 11. Bookmarks
    ///////////////////////////////////////////////////////////////////////////////
    private string m_note_writing_instruction = "write a note";
    private Color m_note_writing_instruction_color = Color.Gray;
    private Color m_note_edit_color = Color.Black;
    private Color m_note_view_color = Color.Blue;
    private void BookmarkTextBox_Enter(object sender, EventArgs e)
    {
        SearchGroupBox_Leave(null, null);

        BookmarkTextBox.ForeColor = m_note_edit_color;
        if (!String.IsNullOrEmpty(BookmarkTextBox.Text))
        {
            if (BookmarkTextBox.Text.StartsWith(m_note_writing_instruction))
            {
                BookmarkTextBox.Text = null;
            }
        }
    }
    private void BookmarkTextBox_Leave(object sender, EventArgs e)
    {
        AddBookmarkButton_Click(null, null);
        this.AcceptButton = null;
    }
    private void BookmarkTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            if (!String.IsNullOrEmpty(BookmarkTextBox.Text))
            {
                if (BookmarkTextBox.Text.Length > 0)
                {
                    AddBookmarkButton_Click(null, null);
                }
                else
                {
                    DeleteBookmarkLabel_Click(null, null);
                }
            }
        }
        else
        {
            BookmarkTextBox.ForeColor = m_note_edit_color;
        }
        UpdateBookmarkHistoryButtons();
    }
    private void DisplayNoteWritingInstruction()
    {
        DeleteBookmarkLabel.Enabled = false;
        ClearBookmarksLabel.Enabled = false;

        BookmarkTextBox.ForeColor = m_note_writing_instruction_color;
        if (BookmarkTextBox.Focused)
        {
            BookmarkTextBox.Text = null;
        }
        else
        {
            if (m_client != null)
            {
                if (m_client.Book != null)
                {
                    if (m_client.Selection != null)
                    {
                        if (m_client.Selection.Scope == SelectionScope.Book)
                        {
                            BookmarkTextBox.Text = m_note_writing_instruction + " for "
                                + m_client.Selection.Scope.ToString();
                        }
                        else if ((m_client.Selection.Scope == SelectionScope.Verse) || (m_client.Selection.Scope == SelectionScope.Word) || (m_client.Selection.Scope == SelectionScope.Letter))
                        {
                            BookmarkTextBox.Text = m_note_writing_instruction + " for Chapter "
                                + (ChapterComboBox.SelectedIndex + 1).ToString() + " Verse "
                                + (ChapterVerseNumericUpDown.Value).ToString();
                        }
                        else
                        {
                            StringBuilder str = new StringBuilder();
                            if (m_client.Selection.Indexes.Count > 0)
                            {
                                foreach (int index in m_client.Selection.Indexes)
                                {
                                    str.Append((index + 1).ToString() + "+");
                                }
                                if (str.Length > 1)
                                {
                                    str.Remove(str.Length - 1, 1);
                                }
                            }

                            BookmarkTextBox.Text = m_note_writing_instruction + " for "
                                         + m_client.Selection.Scope.ToString() + " "
                                         + str.ToString();

                            ToolTip.SetToolTip(BookmarkTextBox, null);
                        }
                    }
                }
            }
        }
        UpdateBookmarkHistoryButtons();
    }
    private void DisplayNote(Bookmark bookmark)
    {
        if (m_client != null)
        {
            if (bookmark != null)
            {
                if (bookmark.Selection != null)
                {
                    BookmarkTextBox.Text = bookmark.Note;
                    BookmarkTextBox.ForeColor = m_note_view_color;

                    string hint = "Creation Time" + "\t" + bookmark.CreatedTime + "\r\n"
                         + "Last Modified" + "\t" + bookmark.LastModifiedTime;
                    ToolTip.SetToolTip(BookmarkTextBox, hint);
                }
            }
            else
            {
                DisplayNoteWritingInstruction();
            }
        }
    }
    private void DisplayBookmark(Bookmark bookmark)
    {
        if (bookmark != null)
        {
            if (bookmark.Selection != null)
            {
                if (m_client != null)
                {
                    m_client.Selection = new Selection(m_client.Book, bookmark.Selection.Scope, bookmark.Selection.Indexes);

                    DisplaySelection(false);

                    BookmarkTextBox.Text = bookmark.Note;
                    BookmarkTextBox.ForeColor = m_note_view_color;
                    string hint = "Creation Time" + "\t" + bookmark.CreatedTime + "\r\n"
                         + "Last Modified" + "\t" + bookmark.LastModifiedTime;
                    ToolTip.SetToolTip(BookmarkTextBox, hint);
                    MainTextBox.Focus();

                    UpdateBookmarkHistoryButtons();
                }
            }
        }
    }
    private void AddBookmarkButton_Click(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if (!String.IsNullOrEmpty(BookmarkTextBox.Text))
                {
                    if (BookmarkTextBox.Text.StartsWith(m_note_writing_instruction))
                    {
                        // ignore it
                    }
                    else if (BookmarkTextBox.Text.Length == 0)
                    {
                        DeleteBookmarkLabel_Click(null, null);
                    }
                    else //if (!BookmarkTextBox.Text.StartsWith(m_note_writing_instruction))
                    {
                        if (m_client.Selection != null)
                        {
                            Selection selection = new Selection(m_client.Book, m_client.Selection.Scope, m_client.Selection.Indexes);
                            Bookmark bookmark = m_client.CreateBookmark(selection, BookmarkTextBox.Text);

                            BookmarkTextBox.ForeColor = m_note_view_color;
                            UpdateBookmarkHistoryButtons();
                        }
                    }
                }
            }
        }
    }
    private void PreviousBookmarkButton_Click(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                Bookmark bookmark = m_client.GotoPreviousBookmark();
                if (bookmark != null)
                {
                    DisplayBookmark(bookmark);
                }
            }
        }
    }
    private void NextBookmarkButton_Click(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                Bookmark bookmark = m_client.GotoNextBookmark();
                if (bookmark != null)
                {
                    DisplayBookmark(bookmark);
                }
            }
        }
    }
    private void BookmarkCounterLabel_Click(object sender, EventArgs e)
    {
        if (m_client.Bookmarks.Count > 0)
        {
            DisplayBookmark(m_client.CurrentBookmark);

            // call again so the chapter is selected in ChapterListBox
            DisplayBookmark(m_client.CurrentBookmark);
        }
    }
    private void DeleteBookmarkLabel_Click(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                // remove existing bookmark (if any)
                m_client.DeleteCurrentBookmark();

                Bookmark bookmark = m_client.CurrentBookmark;
                if (bookmark != null)
                {
                    DisplayBookmark(bookmark);
                }
                else
                {
                    DisplaySelection(false);
                }
            }
        }
    }
    private void ClearBookmarksLabel_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show(
            "Delete all bookmarks and notes?",
            Application.ProductName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes)
        {
            if (m_client != null)
            {
                if (m_client.Book != null)
                {
                    m_client.ClearBookmarks();
                    DisplaySelection(false);
                }
            }
        }
    }
    private void UpdateBookmarkHistoryButtons()
    {
        if (m_client != null)
        {
            if (m_client.Bookmarks != null)
            {
                {
                    PreviousBookmarkButton.Enabled = (m_client.Bookmarks.Count > 0) && (m_client.CurrentBookmarkIndex > 0);
                    NextBookmarkButton.Enabled = (m_client.Bookmarks.Count > 0) && (m_client.CurrentBookmarkIndex < m_client.Bookmarks.Count - 1);
                    BookmarkCounterLabel.Text = (m_client.CurrentBookmarkIndex + 1).ToString() + " / " + m_client.Bookmarks.Count.ToString();
                    DeleteBookmarkLabel.Enabled = (!BookmarkTextBox.Text.StartsWith(m_note_writing_instruction)) && (!m_found_verses_displayed) && (m_client.Bookmarks.Count > 0);
                    ClearBookmarksLabel.Enabled = (!BookmarkTextBox.Text.StartsWith(m_note_writing_instruction)) && (!m_found_verses_displayed) && (m_client.Bookmarks.Count > 0);
                    ClearBookmarksLabel.BackColor = (!BookmarkTextBox.Text.StartsWith(m_note_writing_instruction)) && (!m_found_verses_displayed) && (m_client.Bookmarks.Count > 0) ? Color.LightCoral : SystemColors.ControlLight;
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 12. Audio Player
    ///////////////////////////////////////////////////////////////////////////////
    private MP3Player m_mp3player = new MP3Player();
    private int m_audio_volume = DEFAULT_AUDIO_VOLUME;
    private string m_downloaded_audio_filename = null;
    private bool m_player_looping = false;
    private bool m_player_looping_all = false;
    private bool m_first_play = true;
    private bool m_in_silent_mode = false;
    private float m_silence_between_verses = DEFAULT_SILENCE_BETWEEN_VERSES; // in multiples of verses
    private int m_silence_time_between_verses = 0;
    private void PlayPreviousVerseOrLoop()
    {
        try
        {
            Verse verse = null;
            // if looping then replay
            if (m_player_looping)
            {
                verse = GetCurrentVerse();
            }
            else // move to previous verse
            {
                verse = SetPreviousVerse();
            }

            if (verse != null)
            {
                HighlightVerse(verse);
                PlayVerse(verse);
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayCurrentVerse()
    {
        try
        {
            Verse verse = GetCurrentVerse();
            if (verse != null)
            {
                HighlightVerse(verse);
                PlayVerse(verse);
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayNextVerseOrLoop()
    {
        try
        {
            Verse verse = null;
            // if looping then replay
            if (m_player_looping)
            {
                verse = GetCurrentVerse();
            }
            else // move to next verse
            {
                verse = SetNextVerse();
            }

            if (verse != null)
            {
                HighlightVerse(verse);
                PlayVerse(verse);
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayVerse(Verse verse)
    {
        if (verse != null)
        {
            DoPlayVerse(verse);
        }
        else
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void DoPlayVerse(Verse verse)
    {
        try
        {
            if (m_client != null)
            {
                if (verse != null)
                {
                    if (m_mp3player != null)
                    {
                        if (m_mp3player.Closed)
                        {
                            PlayerOpenAudioFile(verse);
                        }

                        if (m_mp3player.Opened)
                        {
                            if (m_mp3player.MuteAll)
                            {
                                m_mp3player.VolumeAll = 0;
                            }
                            else
                            {
                                m_mp3player.VolumeAll = m_audio_volume;
                            }

                            m_mp3player.Play();
                            PlayerTimer.Enabled = true;
                            PlayerStopLabel.Enabled = true;
                            PlayerStopLabel.Refresh();
                        }
                    }

                    // simulate mouse click to continue playing next verse and not restart from 1
                    m_active_textbox.Focus();
                    m_is_selection_mode = false;
                }
                else // invalid verse
                {
                    MessageBox.Show("No verse available.", Application.ProductName);

                    // reset player buttons
                    PlayerStopLabel_Click(null, null);
                }
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerPlayAudhuBillah()
    {
        try
        {
            if (m_mp3player != null)
            {
                m_mp3player.Open(Globals.AUDIO_FOLDER + "/" + "audhubillah.mp3");
                if (m_mp3player.Opened)
                {
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_pause.png"))
                    {
                        PlayerPlayLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_pause.png");
                    }
                    PlayerPlayLabel.Refresh();

                    m_mp3player.VolumeAll = m_audio_volume;
                    m_mp3player.Play();
                }
                else
                {
                    PlayerStopLabel_Click(null, null);
                    AskUserToDownloadAudioFilesManually();
                }
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerPlayBismAllah()
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if (m_mp3player != null)
                {
                    Chapter al_fatiha = null;
                    foreach (Chapter chapter in m_client.Book.Chapters)
                    {
                        if (chapter.Number == 1)
                        {
                            al_fatiha = chapter;
                            break;
                        }
                    }

                    if (al_fatiha != null)
                    {
                        if (al_fatiha.Verses.Count > 0)
                        {
                            try
                            {
                                // download file if not on disk
                                DownloadVerseAudioFile(al_fatiha.Verses[0]);

                                // open only, don't play
                                m_mp3player.Open(m_downloaded_audio_filename);

                                if (m_mp3player.Opened)
                                {
                                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_pause.png"))
                                    {
                                        PlayerPlayLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_pause.png");
                                    }
                                    PlayerPlayLabel.Refresh();

                                    m_mp3player.VolumeAll = m_audio_volume;
                                    m_mp3player.Play();
                                }
                            }
                            catch
                            {
                                PlayerStopLabel_Click(null, null);
                                AskUserToDownloadAudioFilesManually();
                            }
                        }
                    }
                }
            }
        }
    }
    private void PlayerOpenAudioFile(Verse verse)
    {
        try
        {
            if (verse != null)
            {
                if (m_mp3player != null)
                {
                    // on first play
                    if (m_first_play)
                    {
                        PlayerPlayAudhuBillah();
                        WaitForPlayToFinish();

                        // don't play bismAllah for not first verses NOR first verse of chapter 9 
                        if (verse.Chapter != null)
                        {
                            if ((verse.Chapter.Number != 9) && (verse.NumberInChapter != 1))
                            {
                                PlayerPlayBismAllah();
                                WaitForPlayToFinish();
                            }
                        }

                        m_first_play = false;
                    }

                    // on all plays
                    // play BismAllah for every verse 1, except chapter 9
                    if (verse.NumberInChapter == 1)
                    {
                        if (verse.Chapter != null)
                        {
                            if ((verse.Chapter.Number != 1) && (verse.Chapter.Number != 9))
                            {
                                PlayerPlayBismAllah();
                                WaitForPlayToFinish();
                            }
                        }
                    }

                    try
                    {
                        // download file if not on disk
                        DownloadVerseAudioFile(verse);

                        // open only, don't play
                        m_mp3player.Open(m_downloaded_audio_filename);
                    }
                    catch
                    {
                        PlayerStopLabel_Click(null, null);
                        AskUserToDownloadAudioFilesManually();
                    }
                }
            }
            else // invalid verse
            {
                MessageBox.Show("No verse available.", Application.ProductName);

                // reset player buttons
                PlayerStopLabel_Click(null, null);
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerPreviousLabel_Click(object sender, EventArgs e)
    {
        try
        {
            bool player_is_opened = false;
            if (m_mp3player != null)
            {
                if (m_mp3player.Playing)
                {
                    player_is_opened = m_mp3player.Opened;
                    PlayerStopLabel_Click(null, null);

                    PlayPreviousVerseOrLoop();
                }
                else
                {
                    PlayCurrentVerse();
                }
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerNextLabel_Click(object sender, EventArgs e)
    {
        try
        {
            bool player_is_opened = false;
            if (m_mp3player != null)
            {
                if (m_mp3player.Playing)
                {
                    player_is_opened = m_mp3player.Opened;
                    PlayerStopLabel_Click(null, null);

                    PlayNextVerseOrLoop();
                }
                else
                {
                    PlayCurrentVerse();
                }
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerPlayLabel_Click(object sender, EventArgs e)
    {
        try
        {
            if (m_mp3player != null)
            {
                if ((m_mp3player.Closed) || (m_mp3player.Stopped) || (m_mp3player.Paused))
                {
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_pause.png"))
                    {
                        PlayerPlayLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_pause.png");
                    }
                    ToolTip.SetToolTip(PlayerPlayLabel, "Pause");
                    PlayerPlayLabel.Refresh();

                    PlayCurrentVerse();
                }
                else if (m_mp3player.Playing)
                {
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_play.png"))
                    {
                        PlayerPlayLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_play.png");
                    }
                    ToolTip.SetToolTip(PlayerPlayLabel, "Play");
                    PlayerPlayLabel.Refresh();

                    m_mp3player.Pause();
                    PlayerStopLabel.Enabled = true;
                    PlayerStopLabel.Refresh();
                }
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerStopLabel_Click(object sender, EventArgs e)
    {
        try
        {
            if (m_mp3player != null)
            {
                if (m_mp3player.Opened)
                {
                    m_mp3player.Stop();
                    m_mp3player.Close();
                }
                PlayerTimer.Enabled = false;
                PlayerStopLabel.Enabled = false;
                PlayerStopLabel.Refresh();
                if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_play.png"))
                {
                    PlayerPlayLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_play.png");
                }
                PlayerPlayLabel.Refresh();
            }
        }
        catch
        {
            // silence error
        }
    }
    private void PlayerRepeatLabel_Click(object sender, EventArgs e)
    {
        try
        {
            if (m_mp3player != null)
            {
                //m_player.Looping = !m_player.Looping;
                //if (m_player.Looping)
                m_player_looping = !m_player_looping; // manual looping to allow different reciters to read the same verse
                if (m_player_looping)
                {
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_repeat_on.png"))
                    {
                        PlayerRepeatLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_repeat_on.png");
                    }
                }
                else
                {
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_repeat.png"))
                    {
                        PlayerRepeatLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_repeat.png");
                    }
                }
                PlayerRepeatLabel.Refresh();
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerRepeatSelectionLabel_Click(object sender, EventArgs e)
    {
        try
        {
            if (m_mp3player != null)
            {
                m_player_looping_all = !m_player_looping_all;
                if (m_player_looping_all)
                {
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_repeat_all_on.png"))
                    {
                        PlayerRepeatSelectionLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_repeat_all_on.png");
                    }
                }
                else
                {
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_repeat_all.png"))
                    {
                        PlayerRepeatSelectionLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_repeat_all.png");
                    }
                }
                PlayerRepeatSelectionLabel.Refresh();
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerMuteLabel_Click(object sender, EventArgs e)
    {
        try
        {
            if (m_mp3player != null)
            {
                m_mp3player.MuteAll = !m_mp3player.MuteAll;
                if (m_mp3player.MuteAll)
                {
                    m_mp3player.VolumeAll = 0;
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_muted.png"))
                    {
                        PlayerMuteLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_muted.png");
                    }
                }
                else
                {
                    m_mp3player.VolumeAll = m_audio_volume;
                    if (File.Exists(Globals.IMAGES_FOLDER + "/" + "player_vol_hi.png"))
                    {
                        PlayerMuteLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "player_vol_hi.png");
                    }
                }
                PlayerMuteLabel.Refresh();
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerVolumeTrackBar_Scroll(object sender, EventArgs e)
    {
        try
        {
            if (m_mp3player != null)
            {
                m_audio_volume = PlayerVolumeTrackBar.Value * (1000 / PlayerVolumeTrackBar.Maximum);
                m_mp3player.VolumeAll = m_audio_volume;
                ToolTip.SetToolTip(PlayerVolumeTrackBar, "Volume " + (m_audio_volume / (1000 / PlayerVolumeTrackBar.Maximum)).ToString() + "%");
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerSilenceTrackBar_Scroll(object sender, EventArgs e)
    {
        try
        {
            if (m_mp3player != null)
            {
                m_silence_between_verses = (float)PlayerSilenceTrackBar.Value / (PlayerSilenceTrackBar.Maximum / 2);
                ToolTip.SetToolTip(PlayerSilenceTrackBar, "Silence " + m_silence_between_verses.ToString("0.0") + " verses");
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PlayerTimer_Tick(object sender, EventArgs e)
    {
        try
        {
            if (m_client != null)
            {
                Verse verse = GetCurrentVerse();
                if (verse != null)
                {
                    // check if this is the last verse
                    bool m_is_last_verse = false;
                    if (m_found_verses_displayed)
                    {
                        if (m_client.FoundVerses != null)
                        {
                            if (m_client.FoundVerses.Count > 0)
                            {
                                if (verse == m_client.FoundVerses[m_client.FoundVerses.Count - 1])
                                {
                                    m_is_last_verse = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (m_client.Selection != null)
                        {
                            if (m_client.Selection.Verses != null)
                            {
                                if (m_client.Selection.Verses.Count > 0)
                                {
                                    if (verse == m_client.Selection.Verses[m_client.Selection.Verses.Count - 1])
                                    {
                                        m_is_last_verse = true;
                                    }
                                }
                            }
                        }

                    }

                    if (m_mp3player != null)
                    {
                        // if silent_mode or verse play is finished, then play next verse
                        if (
                                m_in_silent_mode
                                ||
                                ((m_mp3player.Length - m_mp3player.Position) < ((ulong)(PlayerTimer.Interval / 2)))
                           )
                        {
                            // get verse time length before stop
                            int verse_time_length = (int)m_mp3player.Length;

                            // stop verse play
                            if (m_mp3player.Opened)
                            {
                                m_mp3player.Stop();
                                m_mp3player.Close();
                            }

                            // re-calculate required silence time
                            m_silence_time_between_verses = (int)(verse_time_length * m_silence_between_verses);

                            // if silence still required
                            if (m_silence_time_between_verses > 0)
                            {
                                m_in_silent_mode = true;
                                PlayerTimer.Interval = m_silence_time_between_verses;
                                return; // and call us back after Interval
                            }

                            m_in_silent_mode = false;
                            PlayerTimer.Interval = 100;

                            if (
                                 (!m_is_last_verse) ||
                                 ((m_is_last_verse) && (m_player_looping_all)))
                            {
                                PlayNextVerseOrLoop();
                            }
                            else
                            {
                                PlayerStopLabel_Click(null, null);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    private void PrepareVerseToPlay()
    {
        try
        {
            if (m_mp3player != null)
            {
                if (m_mp3player.Opened)
                {
                    if (m_mp3player.Playing)
                    {
                        WaitForPlayToFinish();

                        // prepare to play this verse, so step back for PlayNextVerse
                        //CurrentVerseIndex--; // this will not allow us to set variable to -1 if first verse was clicked
                        // set variable directly so we can set -1 when first verse is clicked
                        if (m_found_verses_displayed)
                        {
                            m_current_found_verse_index--;
                        }
                        else
                        {
                            m_current_selection_verse_index--;
                        }
                    }
                }
            }
        }
        catch
        {
            PlayerStopLabel_Click(null, null);
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 13. Audio Downloader
    ///////////////////////////////////////////////////////////////////////////////
    private List<string> m_downloaded_reciter_folders = null;
    private void DownloadFile(string url, string path)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            Downloader.Download(url, Application.StartupPath + "/" + path, 10000);
            //using (WebClient web_client = new WebClient())
            //{
            //    web_client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadDataCompleted);
            //    web_client.DownloadDataAsync(new Uri(url));
            //}
        }
        catch
        {
            // silent error
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    //private void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
    //{
    //    // WARNING: runs on different thread to UI thread
    //    byte[] raw = e.Result;
    //}
    private void AskUserToDownloadAudioFilesManually()
    {
        if (MessageBox.Show("Cannot auto-download audio file for this verse.\r\n\r\n"
                          + "You need to download all audio files and unzip them to folder:\r\n"
                          + Application.ProductName + "\\" + Globals.AUDIO_FOLDER + "\\" + m_reciter + "\\" + "\r\n\r\nDo you want to download them now?",
                          Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            Control control = new Control();
            foreach (string key in m_client.Book.RecitationInfos.Keys)
            {
                if (m_client.Book.RecitationInfos[key].Folder == m_reciter)
                {
                    control.Tag = RecitationInfo.DEFAULT_URL_PREFIX + m_client.Book.RecitationInfos[key].Url;
                    LinkLabel_Click(control, null);
                    break;
                }
            }
        }
    }
    private void DownloadVerseAudioFile(Verse verse)
    {
        // mirror remote_folder locally
        string audio_folder = Globals.AUDIO_FOLDER + "/" + m_reciter;
        if (!Directory.Exists(audio_folder))
        {
            Directory.CreateDirectory(audio_folder);
        }

        // generate audio_filename from verse address
        string audio_filename = null;
        string full_audio_folder = null;
        if (verse == null)
        {
            audio_filename = "001000" + "." + RecitationInfo.FileType; // audhubillah
            full_audio_folder = audio_folder + "/" + "001";
        }
        else
        {
            if (verse.Chapter != null)
            {
                audio_filename = verse.Chapter.Number.ToString("000") + verse.NumberInChapter.ToString("000") + "." + RecitationInfo.FileType;
                full_audio_folder = audio_folder + "/" + verse.Chapter.Number.ToString("000");
            }
        }

        // fill up local_audio_filename to return to caller
        m_downloaded_audio_filename = full_audio_folder + "/" + audio_filename;
        string outer_downloaded_audio_filename = audio_folder + "/" + audio_filename;
        if (File.Exists(m_downloaded_audio_filename))
        {
            // no need to download
        }
        else if (File.Exists(outer_downloaded_audio_filename))
        {
            if (!Directory.Exists(full_audio_folder))
            {
                Directory.CreateDirectory(full_audio_folder);
            }

            if (Directory.Exists(full_audio_folder))
            {
                File.Move(outer_downloaded_audio_filename, m_downloaded_audio_filename);
            }
        }
        else
        {
            // try to download audio file

            string recitation_url = null;
            foreach (string key in m_client.Book.RecitationInfos.Keys)
            {
                if (m_client.Book.RecitationInfos[key].Folder == m_reciter)
                {
                    recitation_url = RecitationInfo.UrlPrefix + m_client.Book.RecitationInfos[key].Url + "/" + audio_filename;
                    break;
                }
            }

            DownloadFile(recitation_url, m_downloaded_audio_filename);
        }
    }
    private string GetVerseAudioFilename(int verse_index)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if ((verse_index >= 0) && (verse_index < m_client.Book.Verses.Count))
                {
                    Verse verse = m_client.Book.Verses[verse_index];
                    if (verse != null)
                    {
                        if (verse.Chapter != null)
                        {
                            return (verse.Chapter.Number.ToString("000") + verse.NumberInChapter.ToString("000") + "." + RecitationInfo.FileType);
                        }
                    }
                }
            }
        }
        return "000000.mp3";
    }
    private string GetVerseAudioFullFilename(int verse_index)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if ((verse_index >= 0) && (verse_index < m_client.Book.Verses.Count))
                {
                    Verse verse = m_client.Book.Verses[verse_index];
                    if (verse != null)
                    {
                        if (verse.Chapter != null)
                        {
                            return (verse.Chapter.Number.ToString("000") + "/" + verse.Chapter.Number.ToString("000") + verse.NumberInChapter.ToString("000") + "." + RecitationInfo.FileType);
                        }
                    }
                }
            }
        }
        return "000/000000.mp3";
    }
    private void WaitForPlayToFinish()
    {
        if (m_mp3player != null)
        {
            while ((m_mp3player.Length - m_mp3player.Position) > (ulong)PlayerTimer.Interval)
            {
                Thread.Sleep(100);
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 14. Audio Recitations
    ///////////////////////////////////////////////////////////////////////////////
    private string m_reciter = Client.DEFAULT_RECITATION;
    private void PopulateRecitationsCheckedListBox()
    {
        try
        {
            // to disable item in a list, just ignore user check using this trick
            RecitationsCheckedListBox.ItemCheck += new ItemCheckEventHandler(RecitationsCheckedListBox_ItemCheck);

            RecitationsCheckedListBox.SelectedIndexChanged -= new EventHandler(RecitationsCheckedListBox_SelectedIndexChanged);
            RecitationsCheckedListBox.BeginUpdate();
            RecitationsCheckedListBox.Items.Clear();
            foreach (string key in m_client.Book.RecitationInfos.Keys)
            {
                string reciter = m_client.Book.RecitationInfos[key].Reciter;
                RecitationsCheckedListBox.Items.Add(reciter);
            }
        }
        finally
        {
            RecitationsCheckedListBox.EndUpdate();
            RecitationsCheckedListBox.SelectedIndexChanged += new EventHandler(RecitationsCheckedListBox_SelectedIndexChanged);
        }
    }
    private void RecitationsCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        if (e.CurrentValue == CheckState.Indeterminate)
        {
            e.NewValue = e.CurrentValue;
        }
    }
    private void PopulateReciterComboBox()
    {
        try
        {
            ReciterComboBox.BeginUpdate();
            ReciterComboBox.SelectedIndexChanged -= new EventHandler(ReciterComboBox_SelectedIndexChanged);
            ReciterComboBox.Items.Clear();
            foreach (string key in m_client.Book.RecitationInfos.Keys)
            {
                string reciter = m_client.Book.RecitationInfos[key].Reciter;
                ReciterComboBox.Items.Add(reciter);
            }
            if (ReciterComboBox.Items.Count > 3)
            {
                ReciterComboBox.SelectedIndex = 3;
            }
        }
        finally
        {
            ReciterComboBox.EndUpdate();
            ReciterComboBox.SelectedIndexChanged += new EventHandler(ReciterComboBox_SelectedIndexChanged);
        }
    }
    private void UpdateRecitationsCheckedListBox()
    {
        try
        {
            /////////////////////////////////////////////////////////////////////////////
            // foreach reciter -> foreach verse, if audio file exist and valid then check
            /////////////////////////////////////////////////////////////////////////////

            if (m_downloaded_reciter_folders == null)
            {
                m_downloaded_reciter_folders = new List<string>();
            }
            m_downloaded_reciter_folders.Clear();

            foreach (string reciter_folder in m_client.Book.RecitationInfos.Keys)
            {
                bool fully_downloaded = true;
                for (int i = 0; i < Verse.MAX_NUMBER; i++)
                {
                    string download_folder = Globals.AUDIO_FOLDER + "/" + reciter_folder;
                    string filename = GetVerseAudioFilename(i); // e.g. i=8 ==> 002001.mp3
                    string full_filename = GetVerseAudioFullFilename(i); // e.g. i=8 ==> 002/002001.mp3
                    string full_path = download_folder + "/" + full_filename;
                    if (File.Exists(full_path)) // file exist
                    {
                        long filesize = (new FileInfo(full_path)).Length;
                        if (filesize < 1024) // invalid file
                        {
                            fully_downloaded = false;
                            break;
                        }
                    }
                    else // file not found
                    {
                        fully_downloaded = false;
                        break;
                    }
                }

                int index = 0;
                string reciter = m_client.Book.RecitationInfos[reciter_folder].Reciter;
                for (int i = 0; i < RecitationsCheckedListBox.Items.Count; i++)
                {
                    if (RecitationsCheckedListBox.Items[i].ToString() == reciter)
                    {
                        index = i;
                    }
                }

                if (fully_downloaded)
                {
                    RecitationsCheckedListBox.SetItemCheckState(index, CheckState.Indeterminate);
                    m_downloaded_reciter_folders.Add(reciter_folder);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
    }
    private void RecitationsCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
    }
    private void RecitationsCheckedListBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (RecitationsCheckedListBox.SelectedItem != null)
        {
            string reciter = RecitationsCheckedListBox.SelectedItem.ToString();

            string reciter_folder = null;
            foreach (string key in m_client.Book.RecitationInfos.Keys)
            {
                if (reciter == m_client.Book.RecitationInfos[key].Reciter)
                {
                    reciter_folder = key;
                    break;
                }
            }

            if (m_downloaded_reciter_folders.Contains(reciter_folder))
            {
                RecitationsCheckedListBox.SetItemCheckState(RecitationsCheckedListBox.SelectedIndex, CheckState.Indeterminate);
            }
        }
    }
    private void ReciterComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ReciterComboBox.SelectedItem != null)
        {
            string reciter = ReciterComboBox.SelectedItem.ToString();
            RecitationGroupBox.Text = reciter;

            // update m_recitation_folder
            foreach (string key in m_client.Book.RecitationInfos.Keys)
            {
                if (m_client.Book.RecitationInfos[key].Reciter == reciter)
                {
                    m_reciter = m_client.Book.RecitationInfos[key].Folder;
                    break;
                }
            }
        }
    }
    private void RecitationsApplySettingsLabel_Click(object sender, EventArgs e)
    {
        if (!RecitationsDownloadGroupBox.Visible)
        {
            UpdateRecitationsCheckedListBox();

            RecitationsDownloadGroupBox.Visible = true;
            RecitationsCancelSettingsLabel.Visible = true;
            RecitationsDownloadGroupBox.BringToFront();

            if (File.Exists(Globals.IMAGES_FOLDER + "/" + "apply.png"))
            {
                RecitationsApplySettingsLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "apply.png");
            }
            ToolTip.SetToolTip(RecitationsApplySettingsLabel, "Download complete Quran recitations");
        }
        else
        {
            RecitationsDownloadGroupBox.Visible = false;
            RecitationsCancelSettingsLabel.Visible = false;

            try
            {
                if (File.Exists(Globals.IMAGES_FOLDER + "/" + "settings.png"))
                {
                    RecitationsApplySettingsLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "settings.png");
                }
                ToolTip.SetToolTip(RecitationsApplySettingsLabel, "Add/Remove recitations");

                List<string> keys_to_download = new List<string>();
                foreach (int cheched_index in RecitationsCheckedListBox.CheckedIndices)
                {
                    if (RecitationsCheckedListBox.GetItemCheckState(cheched_index) != CheckState.Indeterminate)
                    {
                        foreach (string key in m_client.Book.RecitationInfos.Keys)
                        {
                            string reciter = RecitationsCheckedListBox.Items[cheched_index].ToString();
                            if (m_client.Book.RecitationInfos[key].Reciter == reciter)
                            {
                                keys_to_download.Add(key);
                                break;
                            }
                        }
                    }
                }

                foreach (string reciter_folder in m_client.Book.RecitationInfos.Keys)
                {
                    if (keys_to_download.Contains(reciter_folder))
                    {
                        ProgressBar.Minimum = Verse.MIN_NUMBER;
                        ProgressBar.Maximum = Verse.MAX_NUMBER;
                        ProgressBar.Value = 1;
                        ProgressBar.Refresh();

                        for (int i = 0; i < Verse.MAX_NUMBER; i++)
                        {
                            string download_folder = Globals.AUDIO_FOLDER + "/" + reciter_folder;
                            string filename = GetVerseAudioFilename(i); // e.g. i=8 ==> 002001.mp3
                            string full_filename = GetVerseAudioFullFilename(i); // e.g. i=8 ==> 002/002001.mp3
                            string full_path = download_folder + "/" + full_filename;
                            if (File.Exists(full_path)) // file exist
                            {
                                long filesize = (new FileInfo(full_path)).Length;
                                if (filesize < 1024) // if < 1kb invalid file then re-download
                                {
                                    DownloadFile(RecitationInfo.UrlPrefix + m_client.Book.RecitationInfos[reciter_folder].Url + "/" + filename, full_path);
                                }
                            }
                            else // file not found so download it
                            {
                                DownloadFile(RecitationInfo.UrlPrefix + m_client.Book.RecitationInfos[reciter_folder].Url + "/" + filename, full_path);
                            }

                            ProgressBar.Value = i + 1;
                            ProgressBar.Refresh();

                            Application.DoEvents();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName);
            }
            finally
            {
                if (m_client != null)
                {
                    if (m_client.Selection != null)
                    {
                        List<Verse> verses = m_client.Selection.Verses;
                        if (verses.Count > 0)
                        {
                            ProgressBar.Minimum = Verse.MIN_NUMBER;
                            ProgressBar.Maximum = Verse.MAX_NUMBER;
                            ProgressBar.Value = verses[0].Number;
                            ProgressBar.Refresh();
                        }
                    }
                }
            }
        }
        RecitationsApplySettingsLabel.Refresh();
    }
    private void RecitationsCancelSettingsLabel_Click(object sender, EventArgs e)
    {
        RecitationsDownloadGroupBox.Visible = false;
        RecitationsDownloadGroupBox.Refresh();
        RecitationsCancelSettingsLabel.Visible = RecitationsDownloadGroupBox.Visible;
        RecitationsCancelSettingsLabel.Refresh();
        if (File.Exists(Globals.IMAGES_FOLDER + "/" + "settings.png"))
        {
            RecitationsApplySettingsLabel.Image = new Bitmap(Globals.IMAGES_FOLDER + "/" + "settings.png");
        }
        ToolTip.SetToolTip(RecitationsApplySettingsLabel, "Setup recitations");
        PopulateRecitationsCheckedListBox();
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 15. Search Setup
    ///////////////////////////////////////////////////////////////////////////////
    private FindType m_find_type = FindType.Text; // named with private to indicate must set via Property, not directly by field
    private FindType FindType
    {
        get { return m_find_type; }
        set
        {
            m_find_type = value;
            if (m_find_type == FindType.Text)
            {
                AutoCompleteHeaderLabel.Visible = true;
                AutoCompleteListBox.Visible = true;
                AutoCompleteHeaderLabel.BringToFront();
                AutoCompleteListBox.BringToFront();
                UpdateFindByTextControls();
            }
            else // Similarity, Numbers, FrequencySum, Revelation, Prostration
            {
                AutoCompleteHeaderLabel.Visible = false;
                AutoCompleteListBox.Visible = false;
            }
        }
    }
    private LanguageType m_language_type = LanguageType.Arabic;
    private TextSearchType m_text_search_type = TextSearchType.Exact;
    private void SearchGroupBox_Enter(object sender, EventArgs e)
    {
        if (FindType == FindType.Text)
        {
            AutoCompleteHeaderLabel.Visible = true;
            AutoCompleteListBox.Visible = true;
            AutoCompleteHeaderLabel.BringToFront();
            AutoCompleteListBox.BringToFront();
        }
        else
        {
            AutoCompleteHeaderLabel.Visible = false;
            AutoCompleteListBox.Visible = false;
        }
    }
    private void SearchGroupBox_Leave(object sender, EventArgs e)
    {
        if ((!AutoCompleteListBox.Focused) && (!FindByTextAtWordStartCheckBox.Focused))
        {
            AutoCompleteHeaderLabel.Visible = false;
            AutoCompleteListBox.Visible = false;
        }
    }
    private void PrepareNewSearch()
    {
        // to prevent controls from disappearing in heavy calculations
        this.Refresh();

        if (m_client != null)
        {
            m_client.FoundPhrases = null;
            m_client.FoundWords = null;
            m_client.FoundWordRanges = null;
            m_client.FoundVerses = null;
            m_client.FoundVerseRanges = null;
            m_client.FoundChapters = null;
            m_client.FoundChapterRanges = null;
            m_find_matches.Clear();
            m_find_match_index = -1;
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 16. Search AutoComplete
    ///////////////////////////////////////////////////////////////////////////////
    private bool m_auto_complete_list_double_click = false;
    private Dictionary<string, int> m_auto_complete_words = null;
    private void AutoCompleteListBox_Enter(object sender, EventArgs e)
    {
        this.AcceptButton = null;
    }
    private void AutoCompleteListBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            AutoCompleteListBox_DoubleClick(sender, e);
        }
        else if (e.KeyCode == Keys.Space)
        {
            FindByTextTextBox.Text += " ";
            FindByTextTextBox.Focus();
        }
        else if ((e.KeyCode == Keys.Left) || (e.KeyCode == Keys.Right))
        {
            FindByTextTextBox.Focus();
        }
        FindByTextTextBox.SelectionStart = FindByTextTextBox.Text.Length;
    }
    private void AutoCompleteListBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            // set cursor at mouse RIGHT-click location so we know which word to Find
            if (AutoCompleteListBox.SelectedIndices.Count == 1)
            {
                AutoCompleteListBox.SelectedIndex = -1;
            }
            AutoCompleteListBox.SelectedIndex = AutoCompleteListBox.IndexFromPoint(e.X, e.Y);
        }
    }
    private void AutoCompleteListBox_Click(object sender, EventArgs e)
    {
        // do nothing
    }
    private void AutoCompleteListBox_DoubleClick(object sender, EventArgs e)
    {
        m_auto_complete_list_double_click = true;
        if (AutoCompleteListBox.Items.Count > 0)
        {
            AddNextWordToFindText();
        }
        else
        {
            FindByTextButton_Click(null, null);
        }
        m_auto_complete_list_double_click = false;
    }
    private void AutoCompleteListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (m_auto_complete_words != null)
        {
            int count = 0;
            int total = 0;
            if (AutoCompleteListBox.SelectedIndices.Count > 1)
            {
                // update total(unique) counts
                foreach (object item in AutoCompleteListBox.SelectedItems)
                {
                    string[] parts = item.ToString().Split();
                    foreach (string key in m_auto_complete_words.Keys)
                    {
                        if (key == parts[parts.Length - 1])
                        {
                            string entry = String.Format("{0,-3} {1,10}", m_auto_complete_words[key], key);
                            total += m_auto_complete_words[key];
                            count++;

                            break;
                        }
                    }
                }
            }
            else
            {
                // restore total(unique) counts
                foreach (string key in m_auto_complete_words.Keys)
                {
                    string entry = String.Format("{0,-3} {1,10}", m_auto_complete_words[key], key);
                    total += m_auto_complete_words[key];
                    count++;
                }
            }
            AutoCompleteHeaderLabel.Text = total.ToString() + " (" + count.ToString() + ")";
            AutoCompleteHeaderLabel.Refresh();
        }
    }
    private void AddNextWordToFindText()
    {
        if (AutoCompleteListBox.SelectedItem != null)
        {
            string word_to_add = AutoCompleteListBox.SelectedItem.ToString();
            int pos = word_to_add.LastIndexOf(' ');
            if (pos > -1)
            {
                word_to_add = word_to_add.Substring(pos + 1);
            }

            string text = FindByTextTextBox.Text;
            int index = text.LastIndexOf(' ');
            if (index != -1)
            {
                // if no multi-root search, then prevent losing previous word in TextBox
                //if (m_text_search_type == FindByTextSearchType.Root)
                //{
                //    FindByTextTextBox.Text = word_to_add;
                //}
                //else
                {
                    if (text.Length > index + 1)
                    {
                        if ((text[index + 1] == '+') || (text[index + 1] == '-'))
                        {
                            index++;
                        }
                    }

                    text = text.Substring(0, index + 1);
                    text += word_to_add;
                    FindByTextTextBox.Text = text + " ";
                }
            }
            else
            {
                FindByTextTextBox.Text = word_to_add + " ";
            }
            FindByTextTextBox.Refresh();
            FindByTextTextBox.SelectionStart = FindByTextTextBox.Text.Length;
        }
    }
    private void ClearAutoCompleteListBox()
    {
        AutoCompleteListBox.Items.Clear();
    }
    private void PopulateAutoCompleteListBoxWithNextWords()
    {
        AutoCompleteListBox.SelectedIndexChanged -= new EventHandler(AutoCompleteListBox_SelectedIndexChanged);

        try
        {
            if (m_client != null)
            {
                //SearchGroupBox.Text = " Search by Exact words      ";
                //SearchGroupBox.Refresh();
                AutoCompleteHeaderLabel.Text = "000 (00)";
                ToolTip.SetToolTip(AutoCompleteHeaderLabel, "total (unique)");
                AutoCompleteHeaderLabel.Refresh();

                AutoCompleteListBox.BeginUpdate();
                AutoCompleteListBox.Items.Clear();

                string text = FindByTextTextBox.Text;
                if (!String.IsNullOrEmpty(text))
                {
                    if (text.EndsWith(" "))
                    {
                        m_auto_complete_words = m_client.GetNextWords(text, m_at_word_start, m_with_diacritics);
                    }
                    else
                    {
                        m_auto_complete_words = m_client.GetCurrentWords(text, m_at_word_start, m_with_diacritics);
                    }

                    if (m_auto_complete_words != null)
                    {
                        int count = 0;
                        int total = 0;
                        foreach (string key in m_auto_complete_words.Keys)
                        {
                            //string value_str = found_words[key].ToString().PadRight(3, ' ');
                            //string key_str = key.PadLeft(10, ' ');
                            //string entry = String.Format("{0} {1}", value_str, key_str);
                            string entry = String.Format("{0,-3} {1,10}", m_auto_complete_words[key], key);
                            AutoCompleteListBox.Items.Add(entry);
                            total += m_auto_complete_words[key];
                            count++;
                        }

                        if (AutoCompleteListBox.Items.Count > 0)
                        {
                            AutoCompleteListBox.SelectedIndex = 0;
                        }
                        else // no match [either current text_mode doesn't have a match or it was last word in verse]
                        {
                            if (m_language_type == LanguageType.Arabic)
                            {
                                // m_auto_complete_list_double_click == false if input was via keyboard
                                // m_auto_complete_list_double_click == true  if input was via double click
                                // if no more word when double click, then it means it was the last word in the verse
                                // else the user has entered non-matching text

                                // if last word in verse, remove the extra space after it
                                if ((m_auto_complete_list_double_click) && (AutoCompleteListBox.Items.Count == 0) && (FindByTextTextBox.Text.EndsWith(" ")))
                                {
                                    FindByTextTextBox.TextChanged -= new EventHandler(FindByTextTextBox_TextChanged);
                                    try
                                    {
                                        FindByTextTextBox.Text = FindByTextTextBox.Text.Remove(FindByTextTextBox.Text.Length - 1);
                                    }
                                    finally
                                    {
                                        FindByTextTextBox.TextChanged += new EventHandler(FindByTextTextBox_TextChanged);
                                    }
                                }
                            }
                            else
                            {
                                // allow no-matching text entry e.g. in English
                            }
                        }

                        AutoCompleteHeaderLabel.Text = total.ToString() + " (" + count.ToString() + ")";
                        AutoCompleteHeaderLabel.Refresh();
                    }
                }
            }
        }
        finally
        {
            AutoCompleteListBox.EndUpdate();
            AutoCompleteListBox.SelectedIndexChanged += new EventHandler(AutoCompleteListBox_SelectedIndexChanged);
        }
    }
    private void PopulateAutoCompleteListBoxWithCurrentWords()
    {
        AutoCompleteListBox.SelectedIndexChanged -= new EventHandler(AutoCompleteListBox_SelectedIndexChanged);

        try
        {
            //SearchGroupBox.Text = " Search by Proximity        ";
            //SearchGroupBox.Refresh();
            AutoCompleteHeaderLabel.Text = "000 (00)";
            ToolTip.SetToolTip(AutoCompleteHeaderLabel, "total (unique)");
            AutoCompleteHeaderLabel.Refresh();

            AutoCompleteListBox.BeginUpdate();
            AutoCompleteListBox.Items.Clear();

            string text = FindByTextTextBox.Text;
            if (!String.IsNullOrEmpty(text))
            {
                string[] text_parts = text.Split();
                text = text_parts[text_parts.Length - 1];
                if (!String.IsNullOrEmpty(text))
                {
                    m_auto_complete_words = m_client.GetCurrentWords(text, m_at_word_start, m_with_diacritics);
                    if (m_auto_complete_words != null)
                    {
                        int count = 0;
                        int total = 0;
                        foreach (string key in m_auto_complete_words.Keys)
                        {
                            //string value_str = found_words[key].ToString().PadRight(3, ' ');
                            //string key_str = key.PadLeft(10, ' ');
                            //string entry = String.Format("{0} {1}", value_str, key_str);
                            string entry = String.Format("{0,-3} {1,10}", m_auto_complete_words[key], key);
                            AutoCompleteListBox.Items.Add(entry);
                            total += m_auto_complete_words[key];
                            count++;
                        }


                        if (AutoCompleteListBox.Items.Count > 0)
                        {
                            AutoCompleteListBox.SelectedIndex = 0;
                        }
                        else
                        {
                            // if not a valid word, keep word as is
                        }

                        AutoCompleteHeaderLabel.Text = total.ToString() + " (" + count.ToString() + ")";
                        AutoCompleteHeaderLabel.Refresh();
                    }
                }
            }
        }
        finally
        {
            AutoCompleteListBox.EndUpdate();
            AutoCompleteListBox.SelectedIndexChanged += new EventHandler(AutoCompleteListBox_SelectedIndexChanged);
        }
    }
    private void PopulateAutoCompleteListBoxWithRoots()
    {
        AutoCompleteListBox.SelectedIndexChanged -= new EventHandler(AutoCompleteListBox_SelectedIndexChanged);

        try
        {
            //SearchGroupBox.Text = " Search by Roots            ";
            //SearchGroupBox.Refresh();
            AutoCompleteHeaderLabel.Text = "0 Roots";
            ToolTip.SetToolTip(AutoCompleteHeaderLabel, "total roots");
            AutoCompleteHeaderLabel.Refresh();

            AutoCompleteListBox.BeginUpdate();
            AutoCompleteListBox.Items.Clear();

            string text = FindByTextTextBox.Text;

            // to support multi root search take the last word a user is currently writing
            string[] text_parts = text.Split();
            if (text_parts.Length > 0)
            {
                text = text_parts[text_parts.Length - 1];
            }

            List<string> found_roots = null;
            if (text.Length == 0)
            {
                found_roots = m_client.Book.GetRoots();
            }
            else if (!String.IsNullOrEmpty(text))
            {
                if (m_at_word_start)
                {
                    found_roots = m_client.Book.GetRootsStartingWith(text, m_with_diacritics);
                }
                else
                {
                    found_roots = m_client.Book.GetRootsContaining(text, m_with_diacritics);
                }
            }

            if (found_roots != null)
            {
                int count = 0;
                foreach (string root in found_roots)
                {
                    string entry = root.PadLeft(14, ' ');
                    //string entry = String.Format("{0,14}", root);
                    AutoCompleteListBox.Items.Add(entry);
                    count++;
                }

                if (AutoCompleteListBox.Items.Count > 0)
                {
                    AutoCompleteListBox.SelectedIndex = 0;
                }
                else
                {
                    // if not a valid root, put word as is so we can find same rooted words
                    AutoCompleteListBox.Items.Add(text);
                }
                AutoCompleteHeaderLabel.Text = count.ToString() + " Roots";
                AutoCompleteHeaderLabel.Refresh();
            }
        }
        finally
        {
            AutoCompleteListBox.EndUpdate();
            AutoCompleteListBox.SelectedIndexChanged += new EventHandler(AutoCompleteListBox_SelectedIndexChanged);
        }
    }
    private void FindSelectedWordsMenuItem_Click(object sender, EventArgs e)
    {
        StringBuilder str = new StringBuilder();
        char[] separators = { ' ' };
        if (AutoCompleteListBox.SelectedItems.Count > 0)
        {
            foreach (object item in AutoCompleteListBox.SelectedItems)
            {
                string[] parts = item.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    str.Append(parts[1] + " ");
                }
            }
            if (str.Length > 1)
            {
                str.Remove(str.Length - 1, 1);
            }
            string text = str.ToString();
            string translation = "";
            TextLocation text_location = TextLocation.AnyWord;
            bool case_sensitive = false;
            TextWordness wordness = TextWordness.WholeWord;
            int multiplicity = -1;
            FindByText(text, m_language_type, translation, text_location, case_sensitive, wordness, multiplicity, m_at_word_start, m_with_diacritics);
        }
    }
    private void SaveWordFrequenciesMenuItem_Click(object sender, EventArgs e)
    {
        string text = FindByTextTextBox.Text;

        if (AutoCompleteHeaderLabel.Text.Length >= 5) // minimum is "0 (0)"
        {
            string[] header_parts = AutoCompleteHeaderLabel.Text.Split();
            if (header_parts.Length == 2)
            {
                string total_str = header_parts[0];
                string count_str = header_parts[1].Substring(1, header_parts[1].Length - 2);

                string filename = Globals.STATISTICS_FOLDER + "/" + text + ".txt";
                try
                {
                    using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                    {
                        StringBuilder str = new StringBuilder();
                        str.AppendLine("-----------------");
                        str.AppendLine("Word" + "\t" + "Frequency");
                        str.AppendLine("-----------------");

                        char[] separators = { ' ' };
                        if (AutoCompleteListBox.SelectedItems.Count > 1)
                        {
                            foreach (object item in AutoCompleteListBox.SelectedItems)
                            {
                                string[] parts = item.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length == 2)
                                {
                                    str.AppendLine(parts[1] + "\t" + parts[0]);
                                }
                            }
                        }
                        else
                        {
                            foreach (object item in AutoCompleteListBox.Items)
                            {
                                string[] parts = item.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length == 2)
                                {
                                    str.AppendLine(parts[1] + "\t" + parts[0]);
                                }
                            }
                        }
                        str.AppendLine("-----------------");
                        str.AppendLine("Count = " + count_str);
                        str.AppendLine("Total = " + total_str);

                        writer.Write(str.ToString());
                    }

                    // show file content after save
                    if (File.Exists(filename))
                    {
                        System.Diagnostics.Process.Start("Notepad.exe", filename);
                    }
                }
                catch
                {
                    // silence IO error in case running from read-only media (CD/DVD)
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 17. Search By Text/Root/Promimity
    ///////////////////////////////////////////////////////////////////////////////
    private bool m_at_word_start = false;
    private bool m_with_diacritics = false;
    private void SetLanguageType(LanguageType language_type)
    {
        if (language_type == LanguageType.Arabic)
        {
            m_language_type = language_type;
        }
        else if (language_type == LanguageType.Translation)
        {
            if (m_text_search_type == TextSearchType.Root)
            {
                m_language_type = LanguageType.Arabic;
            }
            else
            {
                m_language_type = language_type;
            }
        }
    }
    private void FindByTextExactSearchTypeLabel_Click(object sender, EventArgs e)
    {
        m_text_search_type = TextSearchType.Exact;
        PopulateAutoCompleteListBoxWithNextWords();
        FindByTextAnywhereRadioButton.Checked = true;

        UpdateFindByTextControls();
        FindByTextControls_Enter(null, null);
    }
    private void FindByTextProximitySearchTypeLabel_Click(object sender, EventArgs e)
    {
        m_text_search_type = TextSearchType.Proximity;
        PopulateAutoCompleteListBoxWithCurrentWords();
        FindByTextAllWordsRadioButton.Checked = true;

        UpdateFindByTextControls();
        FindByTextControls_Enter(null, null);
    }
    private void FindByTextRootSearchTypeLabel_Click(object sender, EventArgs e)
    {
        m_text_search_type = TextSearchType.Root;
        PopulateAutoCompleteListBoxWithRoots();

        UpdateFindByTextControls();
        FindByTextControls_Enter(null, null);
    }
    private void FindByTextRadioButton_CheckedChanged(object sender, EventArgs e)
    {
        UpdateFindByTextControls();
    }
    private void FindByTextWithDiacriticsCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        m_with_diacritics = FindByTextWithDiacriticsCheckBox.Checked;
        FindByTextTextBox_TextChanged(null, null);
    }
    private void FindByTextAtWordStartCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        m_at_word_start = FindByTextAtWordStartCheckBox.Checked;
        FindByTextTextBox_TextChanged(null, null);
    }
    private void FindByTextWordnessCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        UpdateFindByTextControls();
    }
    private void FindByTextCaseSensitiveCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        UpdateFindByTextControls();
    }
    private void FindByTextMultiplicityCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        FindByTextMultiplicityNumericUpDown.Enabled = FindByTextMultiplicityCheckBox.Checked;
    }
    private void FindByTextMultiplicityNumericUpDown_ValueChanged(object sender, EventArgs e)
    {
        // do nothing
    }
    private void FindByTextMultiplicityNumericUpDown_KeyDown(object sender, KeyEventArgs e)
    {
        // do nothing
    }
    private void FindByTextControls_Enter(object sender, EventArgs e)
    {
        this.AcceptButton = FindByTextButton;
        FindType = FindType.Text;

        FindByTextButton.Enabled = true;
        FindBySimilarityButton.Enabled = false;
        FindByNumbersButton.Enabled = false;
        FindByFrequencySumButton.Enabled = false;

        ResetFindBySimilarityResultTypeLabels();
        ResetFindByNumbersResultTypeLabels();
        ResetFindByFrequencyResultTypeLabels();
    }
    private void FindByTextPanel_Leave(object sender, EventArgs e)
    {
        SearchGroupBox_Leave(null, null);
    }
    private void FindByTextTextBox_TextChanged(object sender, EventArgs e)
    {
        UpdateFindByTextControls();

        if (m_text_search_type == TextSearchType.Exact)
        {
            PopulateAutoCompleteListBoxWithNextWords();
        }
        else if (m_text_search_type == TextSearchType.Root)
        {
            PopulateAutoCompleteListBoxWithRoots();
        }
        else if (m_text_search_type == TextSearchType.Proximity)
        {
            PopulateAutoCompleteListBoxWithCurrentWords();
        }
        else
        {
            //
        }

        RefreshLanguageType(FindByTextTextBox.Text);
    }
    private void FindByTextTextBox_KeyPress(object sender, KeyPressEventArgs e)
    {
        FixMicrosoft(sender, e);

        if (e.KeyChar == ' ')
        {
            // if no multi-root search, then prevent space entry
            //if (m_text_search_type == FindByTextSearchType.Root)
            //{
            //    e.Handled = true; // prevent space in Root search
            //}

            // prevent double spaces
            if (FindByTextTextBox.SelectionStart > 0)
            {
                if (FindByTextTextBox.Text[FindByTextTextBox.SelectionStart - 1] == ' ')
                {
                    e.Handled = true;
                }
            }
        }
    }
    private void FindByTextTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (ModifierKeys == Keys.Control)
        {
            if (e.KeyCode == Keys.A)
            {
                if (sender is TextBoxBase)
                {
                    (sender as TextBoxBase).SelectAll();
                }
            }
        }
        else if ((e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down))
        {
            AutoCompleteListBox.Focus();
        }
    }
    private void FindByTextButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (ValueTextBox.Focused)
            {
                CalculateExpression();
            }
            else
            {
                PrepareNewSearch();
                if (m_text_search_type == TextSearchType.Exact)
                {
                    //FindByExactProximityRoot();
                    FindByText();
                }
                else if (m_text_search_type == TextSearchType.Proximity)
                {
                    //FindByProximityExactRoot();
                    FindByProximity();
                }
                else if (m_text_search_type == TextSearchType.Root)
                {
                    //FindByRootExactProximity();
                    FindByRoot();
                }
                else
                {
                    // do nothing
                }
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void FindByText()
    {
        string text = FindByTextTextBox.Text;
        if (text.Length > 0)
        {
            FindByText(text);
        }
    }
    private void FindByProximity()
    {
        FindByText();
    }
    private void FindByRoot()
    {
        if (FindByTextTextBox.Text.Length > 0)
        {
            string root = FindByTextTextBox.Text.Trim();
            int multiplicity = FindByTextMultiplicityNumericUpDown.Enabled ? (int)FindByTextMultiplicityNumericUpDown.Value : -1;
            FindByRoot(root, multiplicity, m_with_diacritics);
        }
    }
    private void FindByText(string text)
    {
        if (!String.IsNullOrEmpty(text))
        {
            string translation = Client.DEFAULT_NEW_TRANSLATION;
            if (TranslatorComboBox.SelectedItem != null)
            {
                translation = m_client.GetTranslationKey(TranslatorComboBox.SelectedItem.ToString());
            }

            TextLocation text_location = TextLocation.Anywhere;
            if (FindByTextAnywhereRadioButton.Checked)
            {
                text_location = TextLocation.Anywhere;
            }
            else if (FindByTextAtStartRadioButton.Checked)
            {
                text_location = TextLocation.AtStart;
            }
            else if (FindByTextAtMiddleRadioButton.Checked)
            {
                text_location = TextLocation.AtMiddle;
            }
            else if (FindByTextAtEndRadioButton.Checked)
            {
                text_location = TextLocation.AtEnd;
            }
            else if (FindByTextAllWordsRadioButton.Checked)
            {
                text_location = TextLocation.AllWords;
            }
            else if (FindByTextAnyWordRadioButton.Checked)
            {
                text_location = TextLocation.AnyWord;
            }
            else // default
            {
                text_location = TextLocation.Anywhere;
            }

            bool case_sensitive = FindByTextCaseSensitiveCheckBox.Checked;

            TextWordness wordness = TextWordness.Any;
            switch (FindByTextWordnessCheckBox.CheckState)
            {
                case CheckState.Checked:
                    wordness = TextWordness.WholeWord;
                    break;
                case CheckState.Indeterminate:
                    wordness = TextWordness.PartOfWord;
                    break;
                case CheckState.Unchecked:
                    wordness = TextWordness.Any;
                    break;
                default:
                    wordness = TextWordness.Any;
                    break;
            }
            int multiplicity = FindByTextMultiplicityNumericUpDown.Enabled ? (int)FindByTextMultiplicityNumericUpDown.Value : -1;
            FindByText(text, m_language_type, translation, text_location, case_sensitive, wordness, multiplicity, m_at_word_start, m_with_diacritics);
        }
    }
    private void FindByText(string text, LanguageType language_type, string translation, TextLocation text_location, bool case_sensitive, TextWordness wordness, int multiplicity, bool at_word_start, bool with_diacritics)
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            if (!String.IsNullOrEmpty(text))
            {
                m_client.FindPhrases(text, language_type, translation, text_location, case_sensitive, wordness, multiplicity, at_word_start, m_with_diacritics);
                if (m_client.FoundPhrases != null)
                {
                    int phrase_count = m_client.FoundPhrases.Count;
                    if (m_client.FoundVerses != null)
                    {
                        int verse_count = m_client.FoundVerses.Count;
                        if (multiplicity == 0)
                        {
                            m_find_result_header = verse_count + ((verse_count == 1) ? " verse" : " verses") + " without " + text + " " + text_location.ToString() + " in " + m_client.FindScope.ToString();
                        }
                        else
                        {
                            m_find_result_header = phrase_count + " matches in " + verse_count + ((verse_count == 1) ? " verse" : " verses") + " with " + text + " " + text_location.ToString() + " in " + m_client.FindScope.ToString();
                        }
                        DisplayFoundVerses(true);
                    }
                }
            }
        }
    }
    private void FindByRoot(string root, int multiplicity, bool with_diacritics)
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            if (root.Length > 0)
            {
                m_client.FindPhrases(root, multiplicity, with_diacritics);
                if (m_client.FoundPhrases != null)
                {
                    if (m_client.FoundVerses != null)
                    {
                        int verse_count = m_client.FoundVerses.Count;
                        int phrase_count = m_client.FoundPhrases.Count;
                        m_find_result_header = ((m_client.FoundPhrases != null) ? phrase_count + " matches in " : "") + verse_count + ((verse_count == 1) ? " verse" : " verses") + ((multiplicity == 0) ? " without " : " with root ") + root + " in " + m_client.FindScope.ToString();
                        DisplayFoundVerses(true);
                    }
                }
            }
        }
    }
    private void FindByTextKeyboardLabel_Click(object sender, EventArgs e)
    {
        Control control = (sender as Control);
        if (control != null)
        {
            control.BackColor = Color.LightSteelBlue;
            control.Refresh();

            // prevent double spaces
            if (control == FindByTextSpaceLabel)
            {
                if (FindByTextTextBox.SelectionStart > 0)
                {
                    if (FindByTextTextBox.Text[FindByTextTextBox.SelectionStart - 1] == ' ')
                    {
                        return;
                    }
                }
            }

            string letter = control.Text[0].ToString();
            int pos = FindByTextTextBox.SelectionStart;
            int len = FindByTextTextBox.SelectionLength;
            if (pos >= 0)
            {
                if (len > 0)
                {
                    FindByTextTextBox.Text = FindByTextTextBox.Text.Remove(pos, len);
                }
                else
                {
                    // do nothing
                }
                FindByTextTextBox.Text = FindByTextTextBox.Text.Insert(pos, letter);
                FindByTextTextBox.SelectionStart = pos + 1;
                FindByTextTextBox.Refresh();
            }

            Thread.Sleep(100);
            control.BackColor = Color.LightGray;
            control.Refresh();

            FindByTextKeyboardLabel_MouseEnter(sender, e);
            FindByTextControls_Enter(null, null);
        }
    }
    private void FindByTextBackspaceLabel_Click(object sender, EventArgs e)
    {
        Control control = (sender as Control);
        if (control != null)
        {
            control.BackColor = Color.LightSteelBlue;
            control.Refresh();

            int pos = FindByTextTextBox.SelectionStart;
            int len = FindByTextTextBox.SelectionLength;
            if ((len == 0) && (pos > 0))        // delete character prior to cursor
            {
                FindByTextTextBox.Text = FindByTextTextBox.Text.Remove(pos - 1, 1);
                FindByTextTextBox.SelectionStart = pos - 1;
            }
            else if ((len > 0) && (pos >= 0))   // delete current highlighted characters
            {
                FindByTextTextBox.Text = FindByTextTextBox.Text.Remove(pos, len);
                FindByTextTextBox.SelectionStart = pos;
            }
            else                  // nothing to delete
            {
            }
            FindByTextTextBox.Refresh();

            Thread.Sleep(100);
            control.BackColor = Color.LightGray;
            control.Refresh();

            FindByTextKeyboardLabel_MouseEnter(sender, e);
            FindByTextControls_Enter(null, null);
        }
    }
    private void FindByTextKeyboardLabel_MouseEnter(object sender, EventArgs e)
    {
        Control control = (sender as Control);
        if (control != null)
        {
            if (control == FindByTextBackspaceLabel)
            {
                control.BackColor = Color.DarkGray;
            }
            else
            {
                control.BackColor = Color.White;
            }
            control.Refresh();
        }
    }
    private void FindByTextKeyboardLabel_MouseLeave(object sender, EventArgs e)
    {
        Control control = (sender as Control);
        if (control != null)
        {
            control.BackColor = Color.LightGray;
            control.Refresh();
        }
    }
    private void FindByTextKeyboardModifierLabel_MouseLeave(object sender, EventArgs e)
    {
        Control control = (sender as Control);
        if (control != null)
        {
            control.BackColor = Color.Silver;
            control.Refresh();
        }
    }
    private void FindByTextOrLabel_MouseHover(object sender, EventArgs e)
    {
        Control control = (sender as Control);
        if (control != null)
        {
            string letter_sound;
            if (control.ForeColor == Color.Red)
            {
                letter_sound = "مدّ";
            }
            else if (control.ForeColor == Color.DimGray)
            {
                letter_sound = "إيصال";
            }
            else if (control.ForeColor == Color.LimeGreen)
            {
                letter_sound = "إقلاب";
            }
            else if (control.ForeColor == Color.Blue)
            {
                letter_sound = "إدغام بغنة";
            }
            else if (control.ForeColor == Color.RoyalBlue)
            {
                //letter_sound = "إدغام بلا غنة";
                letter_sound = "إدغام";
            }
            else if (control.ForeColor == Color.DarkViolet)
            {
                letter_sound = "إظهار";
            }
            else if (control.ForeColor == Color.Black)
            {
                //letter_sound = "إخفاء";
                letter_sound = "إخفاء بغنة";
            }
            else
            {
                letter_sound = "";
            }

            int start = "FindByText".Length;
            int length = control.Name.Length - start - "Label".Length;
            ToolTip.SetToolTip(control, control.Name.Substring(start, length) + " " + letter_sound);
        }
    }
    private void UpdateKeyboard(string text_mode)
    {
        FindByTextHamzaLabel.Visible = false;
        FindByTextTaaMarbootaLabel.Visible = false;
        FindByTextElfMaqsuraLabel.Visible = false;
        FindByTextElfWaslLabel.Visible = false;
        FindByTextHamzaAboveElfLabel.Visible = false;
        FindByTextHamzaBelowElfLabel.Visible = false;
        FindByTextElfMedLabel.Visible = false;
        FindByTextHamzaAboveWawLabel.Visible = false;
        FindByTextHamzaAboveYaaLabel.Visible = false;
        FindByTextWithDiacriticsCheckBox.Visible = false;

        if (text_mode.Contains("Simplified28"))
        {
            // do nothing
        }
        else if (text_mode.Contains("Simplified29"))
        {
            FindByTextHamzaLabel.Visible = true;
        }
        else if (text_mode.Contains("Simplified30"))
        {
            FindByTextTaaMarbootaLabel.Visible = true;
            FindByTextElfMaqsuraLabel.Visible = true;
        }
        else if (text_mode.Contains("Simplified31"))
        {
            FindByTextHamzaLabel.Visible = true;
            FindByTextTaaMarbootaLabel.Visible = true;
            FindByTextElfMaqsuraLabel.Visible = true;
        }
        else if (text_mode.Contains("Simplified37"))
        {
            FindByTextHamzaLabel.Visible = true;

            FindByTextTaaMarbootaLabel.Visible = true;
            FindByTextElfMaqsuraLabel.Visible = true;

            FindByTextElfWaslLabel.Visible = true;
            FindByTextHamzaAboveElfLabel.Visible = true;
            FindByTextHamzaBelowElfLabel.Visible = true;
            FindByTextElfMedLabel.Visible = true;
            FindByTextHamzaAboveWawLabel.Visible = true;
            FindByTextHamzaAboveYaaLabel.Visible = true;
        }
        else if (text_mode.Contains("Original"))
        {
            FindByTextHamzaLabel.Visible = true;

            FindByTextTaaMarbootaLabel.Visible = true;
            FindByTextElfMaqsuraLabel.Visible = true;

            FindByTextElfWaslLabel.Visible = true;
            FindByTextHamzaAboveElfLabel.Visible = true;
            FindByTextHamzaBelowElfLabel.Visible = true;
            FindByTextElfMedLabel.Visible = true;
            FindByTextHamzaAboveWawLabel.Visible = true;
            FindByTextHamzaAboveYaaLabel.Visible = true;

            FindByTextWithDiacriticsCheckBox.Visible = true;
        }
        else if (text_mode.Contains("Images"))
        {
            FindByTextHamzaLabel.Visible = true;
        }
        else
        {
            // do nothing
        }
    }
    private void UpdateFindByTextControls()
    {
        FindByTextExactSearchTypeLabel.BackColor = (m_text_search_type == TextSearchType.Exact) ? Color.SteelBlue : Color.DarkGray;
        FindByTextProximitySearchTypeLabel.BackColor = (m_text_search_type == TextSearchType.Proximity) ? Color.SteelBlue : Color.DarkGray;
        FindByTextRootSearchTypeLabel.BackColor = (m_text_search_type == TextSearchType.Root) ? Color.SteelBlue : Color.DarkGray;
        FindByTextExactSearchTypeLabel.BorderStyle = (m_text_search_type == TextSearchType.Exact) ? BorderStyle.Fixed3D : BorderStyle.None;
        FindByTextProximitySearchTypeLabel.BorderStyle = (m_text_search_type == TextSearchType.Proximity) ? BorderStyle.Fixed3D : BorderStyle.None;
        FindByTextRootSearchTypeLabel.BorderStyle = (m_text_search_type == TextSearchType.Root) ? BorderStyle.Fixed3D : BorderStyle.None;

        FindByTextAtStartRadioButton.Enabled = (m_text_search_type == TextSearchType.Exact);
        FindByTextAtMiddleRadioButton.Enabled = (m_text_search_type == TextSearchType.Exact);
        FindByTextAtEndRadioButton.Enabled = (m_text_search_type == TextSearchType.Exact);
        FindByTextAnywhereRadioButton.Enabled = (m_text_search_type == TextSearchType.Exact);

        FindByTextAllWordsRadioButton.Enabled = (m_text_search_type == TextSearchType.Proximity);
        FindByTextAnyWordRadioButton.Enabled = (m_text_search_type == TextSearchType.Proximity)
                                                && (!FindByTextTextBox.Text.Contains("-"))
                                                && (!FindByTextTextBox.Text.Contains("+"));
        FindByTextPlusLabel.Visible = ((m_text_search_type == TextSearchType.Proximity) || (m_text_search_type == TextSearchType.Root));
        FindByTextMinusLabel.Visible = ((m_text_search_type == TextSearchType.Proximity) || (m_text_search_type == TextSearchType.Root));

        FindByTextWordnessCheckBox.Enabled = (m_text_search_type == TextSearchType.Exact);

        FindByTextAtWordStartCheckBox.Enabled = (m_text_search_type != TextSearchType.Proximity);
        FindByTextMultiplicityCheckBox.Enabled = (m_text_search_type != TextSearchType.Proximity);
        FindByTextMultiplicityNumericUpDown.Enabled = (FindByTextMultiplicityCheckBox.Enabled) && (FindByTextMultiplicityCheckBox.Checked);

        FindByTextCaseSensitiveCheckBox.Enabled = (m_language_type == LanguageType.Translation);
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 18. Search By Similarity
    ///////////////////////////////////////////////////////////////////////////////
    private Verse m_similarity_current_verse = null;
    private SimilaritySource m_similarity_source = SimilaritySource.Verse;
    private void FindBySimilarityVerseSourceLabel_Click(object sender, EventArgs e)
    {
        m_similarity_source = SimilaritySource.Verse;
        FindBySimilarityVerseSourceLabel.BackColor = Color.SteelBlue;
        FindBySimilarityBookSourceLabel.BackColor = Color.DarkGray;
        FindBySimilarityVerseSourceLabel.BorderStyle = BorderStyle.Fixed3D;
        FindBySimilarityBookSourceLabel.BorderStyle = BorderStyle.None;

        FindBySimilarityPercentageTrackBar.Value = 73;

        FindBySimilarityControls_Enter(null, null);
    }
    private void FindBySimilarityBookSourceLabel_Click(object sender, EventArgs e)
    {
        m_similarity_source = SimilaritySource.Book;
        FindBySimilarityVerseSourceLabel.BackColor = Color.DarkGray;
        FindBySimilarityBookSourceLabel.BackColor = Color.SteelBlue;
        FindBySimilarityVerseSourceLabel.BorderStyle = BorderStyle.None;
        FindBySimilarityBookSourceLabel.BorderStyle = BorderStyle.Fixed3D;

        FindBySimilarityPercentageTrackBar.Value = 100;

        FindBySimilarityControls_Enter(null, null);
    }
    private void FindBySimilarityRadioButton_CheckedChanged(object sender, EventArgs e)
    {
        //if (m_similarity_source == FindBySimilaritySource.CurrentVerse)
        //{
        //    FindBySimilarityButton_Click(null, null);
        //}
    }
    private void FindBySimilarityPercentageTrackBar_ValueChanged(object sender, EventArgs e)
    {
        FindBySimilarityPercentageLabel.Text = FindBySimilarityPercentageTrackBar.Value.ToString() + "%";
        //if (m_similarity_source == FindBySimilaritySource.CurrentVerse)
        //{
        //    FindBySimilarityButton_Click(null, null);
        //}
    }
    private void FindBySimilarityControls_Enter(object sender, EventArgs e)
    {
        this.AcceptButton = FindBySimilarityButton;
        FindType = FindType.Similarity;

        ResetFindBySimilarityResultTypeLabels();
        ResetFindByNumbersResultTypeLabels();
        ResetFindByFrequencyResultTypeLabels();

        switch (m_similarity_source)
        {
            case SimilaritySource.Verse:
                {
                    FindBySimilarityVerseSourceLabel.BackColor = Color.SteelBlue;
                    FindBySimilarityVerseSourceLabel.BorderStyle = BorderStyle.Fixed3D;
                }
                break;
            case SimilaritySource.Book:
                {
                    FindBySimilarityBookSourceLabel.BackColor = Color.SteelBlue;
                    FindBySimilarityBookSourceLabel.BorderStyle = BorderStyle.Fixed3D;
                }
                break;
        }

        FindByTextButton.Enabled = false;
        FindBySimilarityButton.Enabled = true;
        FindByNumbersButton.Enabled = false;
        FindByFrequencySumButton.Enabled = false;
    }
    private void ResetFindBySimilarityResultTypeLabels()
    {
        FindBySimilarityVerseSourceLabel.BackColor = Color.DarkGray;
        FindBySimilarityVerseSourceLabel.BorderStyle = BorderStyle.None;
        FindBySimilarityBookSourceLabel.BackColor = Color.DarkGray;
        FindBySimilarityBookSourceLabel.BorderStyle = BorderStyle.None;
    }
    private void FindBySimilarityButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            FindBySimilarity();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void FindBySimilarity()
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            SimilarityMethod find_by_similarity_method = SimilarityMethod.SimilarText;
            if (FindBySimilaritySimilarTextRadioButton.Checked)
            {
                find_by_similarity_method = SimilarityMethod.SimilarText;
            }
            else if (FindBySimilaritySimilarWordsRadioButton.Checked)
            {
                find_by_similarity_method = SimilarityMethod.SimilarWords;
            }
            else if (FindBySimilaritySimilarFirstHalfRadioButton.Checked)
            {
                find_by_similarity_method = SimilarityMethod.SimilarFirstHalf;
            }
            else if (FindBySimilaritySimilarLastHalfRadioButton.Checked)
            {
                find_by_similarity_method = SimilarityMethod.SimilarLastHalf;
            }
            else if (FindBySimilaritySimilarFirstWordRadioButton.Checked)
            {
                find_by_similarity_method = SimilarityMethod.SimilarFirstWord;
            }
            else if (FindBySimilaritySimilarLastWordRadioButton.Checked)
            {
                find_by_similarity_method = SimilarityMethod.SimilarLastWord;
            }
            else
            {
                //
            }

            double similarity_percentage = (double)FindBySimilarityPercentageTrackBar.Value / 100.0D;

            string similarity_source = null;
            if (m_similarity_source == SimilaritySource.Verse)
            {
                if (m_similarity_current_verse == null)
                {
                    m_similarity_current_verse = GetCurrentVerse();
                }
                if (m_similarity_current_verse != null)
                {
                    if (m_similarity_current_verse.Chapter != null)
                    {
                        m_client.FindVerses(m_similarity_current_verse, find_by_similarity_method, similarity_percentage);
                        similarity_source = " to verse " + m_similarity_current_verse.Chapter.Name + " " + m_similarity_current_verse.NumberInChapter + " ";
                    }
                }
            }
            else if (m_similarity_source == SimilaritySource.Book)
            {
                m_client.FindVerseRanges(find_by_similarity_method, similarity_percentage);
                similarity_source = null;
            }
            else
            {
                //
            }

            if (m_client.FoundVerses != null)
            {
                int verse_count = m_client.FoundVerses.Count;
                m_find_result_header = verse_count + ((verse_count == 1) ? " verse" : " verses") + " with " + find_by_similarity_method.ToString() + similarity_source + " in " + m_client.FindScope.ToString();

                DisplayFoundVerses(true);
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 19. Search By Numbers
    ///////////////////////////////////////////////////////////////////////////////
    private NumberSearchType m_numbers_result_type = NumberSearchType.Verses;
    private void FindByNumbersLabel_Click(object sender, EventArgs e)
    {
        FindByNumbersControls_Enter(null, null);
    }
    private void FindByNumbersResultTypeWordsLabel_Click(object sender, EventArgs e)
    {
        m_numbers_result_type = NumberSearchType.Words;
        //                           num   Cs     Vs     Ws    Ls    uLs   value dsum  droot
        RefreshFindByNumbersControls(true, false, false, true, true, true, true, true, true);

        FindByNumbersControls_Enter(null, null);
        FindByNumbersNumberNumericUpDown.Value = 0;
        FindByNumbersChaptersNumericUpDown.Value = 0;
        FindByNumbersVersesNumericUpDown.Value = 0;
        FindByNumbersWordsNumericUpDown.Value = 1;
        FindByNumbersLettersNumericUpDown.Focus();
    }
    private void FindByNumbersResultTypeSentencesLabel_Click(object sender, EventArgs e)
    {
        m_numbers_result_type = NumberSearchType.Sentences;
        //                           num   Cs     Vs     Ws    Ls    uLs   value dsum  droot
        RefreshFindByNumbersControls(true, false, false, true, true, true, true, true, true);

        FindByNumbersControls_Enter(null, null);
        FindByNumbersNumberNumericUpDown.Value = 0;
        FindByNumbersChaptersNumericUpDown.Value = 0;
        FindByNumbersVersesNumericUpDown.Value = 0;
        FindByNumbersWordsNumericUpDown.Value = 0; // must be 0 for any sentence length
        FindByNumbersWordsNumericUpDown.Focus();
    }
    private void FindByNumbersResultTypeVersesLabel_Click(object sender, EventArgs e)
    {
        m_numbers_result_type = NumberSearchType.Verses;
        //                           num   Cs     Vs    Ws    Ls    uLs   value dsum  droot
        RefreshFindByNumbersControls(true, false, true, true, true, true, true, true, true);

        FindByNumbersControls_Enter(null, null);
        FindByNumbersNumberNumericUpDown.Value = 0;
        FindByNumbersChaptersNumericUpDown.Value = 0;
        FindByNumbersVersesNumericUpDown.Value = 1;
        FindByNumbersWordsNumericUpDown.Focus();
    }
    private void FindByNumbersResultTypeChaptersLabel_Click(object sender, EventArgs e)
    {
        m_numbers_result_type = NumberSearchType.Chapters;
        //                           num   Cs    Vs    Ws    Ls    uLs   value dsum  droot
        RefreshFindByNumbersControls(true, true, true, true, true, true, true, true, true);

        FindByNumbersControls_Enter(null, null);
        FindByNumbersNumberNumericUpDown.Value = 0;
        FindByNumbersChaptersNumericUpDown.Value = 1;
        FindByNumbersVersesNumericUpDown.Focus();
    }
    private void RefreshFindByNumbersControls(
                                                bool enable_number,
                                                bool enable_chapters,
                                                bool enable_verses,
                                                bool enable_words,
                                                bool enable_letters,
                                                bool enable_unique_letters,
                                                bool enable_value,
                                                bool enable_value_digit_sum,
                                                bool enable_value_digital_root
                                            )
    {
        FindByNumbersNumberLabel.Enabled = enable_number;
        FindByNumbersNumberComparisonOperatorLabel.Enabled = enable_number && (FindByNumbersNumberNumberTypeLabel.Text.Length == 0);
        FindByNumbersNumberNumericUpDown.Enabled = enable_number && (FindByNumbersNumberNumberTypeLabel.Text.Length == 0);
        FindByNumbersNumberNumberTypeLabel.Enabled = enable_number;
        if (enable_number == false)
        {
            FindByNumbersNumberComparisonOperatorLabel.Text = "=";
            FindByNumbersNumberNumericUpDown.Value = 0;
        }

        FindByNumbersChaptersLabel.Enabled = enable_chapters;
        FindByNumbersChaptersComparisonOperatorLabel.Enabled = enable_chapters && (FindByNumbersChaptersNumberTypeLabel.Text.Length == 0);
        FindByNumbersChaptersNumericUpDown.Enabled = enable_chapters && (FindByNumbersChaptersNumberTypeLabel.Text.Length == 0);
        FindByNumbersChaptersNumberTypeLabel.Enabled = enable_chapters;
        if (enable_chapters == false)
        {
            FindByNumbersChaptersComparisonOperatorLabel.Text = "=";
            FindByNumbersChaptersNumericUpDown.Value = 0;
        }

        FindByNumbersVersesLabel.Enabled = enable_verses;
        FindByNumbersVersesComparisonOperatorLabel.Enabled = enable_verses && (FindByNumbersVersesNumberTypeLabel.Text.Length == 0);
        FindByNumbersVersesNumericUpDown.Enabled = enable_verses && (FindByNumbersVersesNumberTypeLabel.Text.Length == 0);
        FindByNumbersVersesNumberTypeLabel.Enabled = enable_verses;
        if (enable_verses == false)
        {
            FindByNumbersVersesComparisonOperatorLabel.Text = "=";
            FindByNumbersVersesNumericUpDown.Value = 0;
        }

        FindByNumbersWordsLabel.Enabled = enable_words;
        FindByNumbersWordsComparisonOperatorLabel.Enabled = enable_words && (FindByNumbersWordsNumberTypeLabel.Text.Length == 0);
        FindByNumbersWordsNumericUpDown.Enabled = enable_words && (FindByNumbersWordsNumberTypeLabel.Text.Length == 0);
        FindByNumbersWordsNumberTypeLabel.Enabled = enable_words;
        if (enable_words == false)
        {
            FindByNumbersWordsComparisonOperatorLabel.Text = "=";
            FindByNumbersWordsNumericUpDown.Value = 0;
        }

        FindByNumbersLettersLabel.Enabled = enable_letters;
        FindByNumbersLettersComparisonOperatorLabel.Enabled = enable_letters && (FindByNumbersLettersNumberTypeLabel.Text.Length == 0);
        FindByNumbersLettersNumericUpDown.Enabled = enable_letters && (FindByNumbersLettersNumberTypeLabel.Text.Length == 0);
        FindByNumbersLettersNumberTypeLabel.Enabled = enable_letters;
        if (enable_letters == false)
        {
            FindByNumbersLettersComparisonOperatorLabel.Text = "=";
            FindByNumbersLettersNumericUpDown.Value = 0;
        }

        FindByNumbersUniqueLettersLabel.Enabled = enable_unique_letters;
        FindByNumbersUniqueLettersComparisonOperatorLabel.Enabled = enable_unique_letters && (FindByNumbersUniqueLettersNumberTypeLabel.Text.Length == 0);
        FindByNumbersUniqueLettersNumericUpDown.Enabled = enable_unique_letters && (FindByNumbersUniqueLettersNumberTypeLabel.Text.Length == 0);
        FindByNumbersUniqueLettersNumberTypeLabel.Enabled = enable_unique_letters;
        if (enable_unique_letters == false)
        {
            FindByNumbersUniqueLettersComparisonOperatorLabel.Text = "=";
            FindByNumbersUniqueLettersNumericUpDown.Value = 0;
        }

        FindByNumbersValueLabel.Enabled = enable_value;
        FindByNumbersValueComparisonOperatorLabel.Enabled = enable_value && (FindByNumbersValueNumberTypeLabel.Text.Length == 0);
        FindByNumbersValueNumericUpDown.Enabled = enable_value && (FindByNumbersValueNumberTypeLabel.Text.Length == 0);
        FindByNumbersValueNumberTypeLabel.Enabled = enable_value;
        if (enable_value == false)
        {
            FindByNumbersValueComparisonOperatorLabel.Text = "=";
            FindByNumbersValueNumericUpDown.Value = 0;
        }

        FindByNumbersValueDigitSumLabel.Enabled = enable_value_digit_sum;
        FindByNumbersValueDigitSumComparisonOperatorLabel.Enabled = enable_value_digit_sum && (FindByNumbersValueDigitSumNumberTypeLabel.Text.Length == 0);
        FindByNumbersValueDigitSumNumericUpDown.Enabled = enable_value_digit_sum && (FindByNumbersValueDigitSumNumberTypeLabel.Text.Length == 0);
        FindByNumbersValueDigitSumNumberTypeLabel.Enabled = enable_value_digit_sum;
        if (enable_value_digit_sum == false)
        {
            FindByNumbersValueDigitSumComparisonOperatorLabel.Text = "=";
            FindByNumbersValueDigitSumNumericUpDown.Value = 0;
        }

        FindByNumbersValueDigitalRootLabel.Enabled = enable_value_digital_root;
        FindByNumbersValueDigitalRootComparisonOperatorLabel.Enabled = enable_value_digital_root && (FindByNumbersValueDigitalRootNumberTypeLabel.Text.Length == 0);
        FindByNumbersValueDigitalRootNumericUpDown.Enabled = enable_value_digital_root && (FindByNumbersValueDigitalRootNumberTypeLabel.Text.Length == 0);
        FindByNumbersValueDigitalRootNumberTypeLabel.Enabled = enable_value_digital_root;
        if (enable_value_digital_root == false)
        {
            FindByNumbersValueDigitalRootComparisonOperatorLabel.Text = "=";
            FindByNumbersValueDigitalRootNumericUpDown.Value = 0;
        }
    }
    private void ResetFindByNumbersControls()
    {
        FindByNumbersNumberComparisonOperatorLabel.Text = "=";
        FindByNumbersNumberNumericUpDown.Value = 0;
        FindByNumbersNumberNumberTypeLabel.Text = null;

        FindByNumbersChaptersComparisonOperatorLabel.Text = "=";
        FindByNumbersChaptersNumericUpDown.Value = 0;
        FindByNumbersChaptersNumberTypeLabel.Text = null;

        FindByNumbersVersesComparisonOperatorLabel.Text = "=";
        FindByNumbersVersesNumericUpDown.Value = 0;
        FindByNumbersVersesNumberTypeLabel.Text = null;

        FindByNumbersWordsComparisonOperatorLabel.Text = "=";
        FindByNumbersWordsNumericUpDown.Value = 0;
        FindByNumbersWordsNumberTypeLabel.Text = null;

        FindByNumbersLettersComparisonOperatorLabel.Text = "=";
        FindByNumbersLettersNumericUpDown.Value = 0;
        FindByNumbersLettersNumberTypeLabel.Text = null;

        FindByNumbersUniqueLettersComparisonOperatorLabel.Text = "=";
        FindByNumbersUniqueLettersNumericUpDown.Value = 0;
        FindByNumbersUniqueLettersNumberTypeLabel.Text = null;

        FindByNumbersValueComparisonOperatorLabel.Text = "=";
        FindByNumbersValueNumericUpDown.Value = 0;
        FindByNumbersValueNumberTypeLabel.Text = null;

        FindByNumbersValueDigitSumComparisonOperatorLabel.Text = "=";
        FindByNumbersValueDigitSumNumericUpDown.Value = 0;
        FindByNumbersValueDigitSumNumberTypeLabel.Text = null;

        FindByNumbersValueDigitalRootComparisonOperatorLabel.Text = "=";
        FindByNumbersValueDigitalRootNumericUpDown.Value = 0;
        FindByNumbersValueDigitalRootNumberTypeLabel.Text = null;
    }
    private void ResetFindByNumbersNumberTypeControl(Control control)
    {
        (control as Control).Text = null;
        control.ForeColor = Color.Black;
        ToolTip.SetToolTip(control, "");
    }
    private void ResetFindByNumbersResultTypeLabels()
    {
        FindByNumbersResultTypeWordsLabel.BackColor = Color.DarkGray;
        FindByNumbersResultTypeWordsLabel.BorderStyle = BorderStyle.None;

        FindByNumbersResultTypeSentencesLabel.BackColor = Color.DarkGray;
        FindByNumbersResultTypeSentencesLabel.BorderStyle = BorderStyle.None;

        FindByNumbersResultTypeVersesLabel.BackColor = Color.DarkGray;
        FindByNumbersResultTypeVersesLabel.BorderStyle = BorderStyle.None;

        FindByNumbersResultTypeChaptersLabel.BackColor = Color.DarkGray;
        FindByNumbersResultTypeChaptersLabel.BorderStyle = BorderStyle.None;

        UpdateFindByNumbersResultType();
    }
    private void ResetFindByNumbersComparisonOperatorLabels()
    {
        FindByNumbersNumberComparisonOperatorLabel.Text = "=";
        FindByNumbersChaptersComparisonOperatorLabel.Text = "=";
        FindByNumbersVersesComparisonOperatorLabel.Text = "=";
        FindByNumbersWordsComparisonOperatorLabel.Text = "=";
        FindByNumbersLettersComparisonOperatorLabel.Text = "=";
        FindByNumbersUniqueLettersComparisonOperatorLabel.Text = "=";
        FindByNumbersValueComparisonOperatorLabel.Text = "=";
        FindByNumbersValueDigitSumComparisonOperatorLabel.Text = "=";
        FindByNumbersValueDigitalRootComparisonOperatorLabel.Text = "=";

        FindByNumbersNumberComparisonOperatorLabel.Enabled = false;
        FindByNumbersChaptersComparisonOperatorLabel.Enabled = false;
        FindByNumbersVersesComparisonOperatorLabel.Enabled = false;
        FindByNumbersWordsComparisonOperatorLabel.Enabled = false;
        FindByNumbersLettersComparisonOperatorLabel.Enabled = false;
        FindByNumbersUniqueLettersComparisonOperatorLabel.Enabled = false;
        FindByNumbersValueComparisonOperatorLabel.Enabled = false;
        FindByNumbersValueDigitSumComparisonOperatorLabel.Enabled = false;
        FindByNumbersValueDigitalRootComparisonOperatorLabel.Enabled = false;
    }
    private void UpdateFindByNumbersResultType()
    {
        switch (m_numbers_result_type)
        {
            case NumberSearchType.Words:
                {
                    if ((FindByNumbersWordsNumericUpDown.Value > 1) || (FindByNumbersWordsNumberTypeLabel.Text.Length > 0))
                    {
                        m_numbers_result_type = NumberSearchType.WordRanges;
                    }
                }
                break;
            case NumberSearchType.WordRanges:
                {
                    if ((FindByNumbersWordsNumericUpDown.Value <= 1) && (FindByNumbersWordsNumberTypeLabel.Text.Length == 0))
                    {
                        m_numbers_result_type = NumberSearchType.Words;
                    }
                }
                break;
            case NumberSearchType.Sentences:
                {
                    m_numbers_result_type = NumberSearchType.Sentences;
                }
                break;
            case NumberSearchType.Verses:
                {
                    if ((FindByNumbersVersesNumericUpDown.Value > 1) || (FindByNumbersVersesNumberTypeLabel.Text.Length > 0))
                    {
                        m_numbers_result_type = NumberSearchType.VerseRanges;
                    }
                }
                break;
            case NumberSearchType.VerseRanges:
                {
                    if ((FindByNumbersVersesNumericUpDown.Value <= 1) && (FindByNumbersVersesNumberTypeLabel.Text.Length == 0))
                    {
                        m_numbers_result_type = NumberSearchType.Verses;
                    }
                }
                break;
            case NumberSearchType.Chapters:
                {
                    if ((FindByNumbersChaptersNumericUpDown.Value > 1) || (FindByNumbersChaptersNumberTypeLabel.Text.Length > 0))
                    {
                        m_numbers_result_type = NumberSearchType.ChapterRanges;
                    }
                }
                break;
            case NumberSearchType.ChapterRanges:
                {
                    if ((FindByNumbersChaptersNumericUpDown.Value <= 1) && (FindByNumbersChaptersNumberTypeLabel.Text.Length == 0))
                    {
                        m_numbers_result_type = NumberSearchType.Chapters;
                    }
                }
                break;
            default:
                break;
        }
        // DEBUG only
        //FindByNumbersLabel.Text = m_numbers_result_type.ToString();

        switch (m_numbers_result_type)
        {
            case NumberSearchType.Words:
                {
                    FindByNumbersResultTypeWordsLabel.Text = "W";
                    ToolTip.SetToolTip(FindByNumbersResultTypeWordsLabel, "find words");
                    FindByNumbersNumberLabel.Text = "number";
                    ToolTip.SetToolTip(FindByNumbersNumberLabel, "word number in verse");
                }
                break;
            case NumberSearchType.WordRanges:
                {
                    FindByNumbersResultTypeWordsLabel.Text = "Ws";
                    ToolTip.SetToolTip(FindByNumbersResultTypeWordsLabel, "find word ranges");
                    FindByNumbersNumberLabel.Text = "sum";
                    ToolTip.SetToolTip(FindByNumbersNumberLabel, "sum of word numbers");
                }
                break;
            case NumberSearchType.Sentences:
                {
                    FindByNumbersResultTypeSentencesLabel.Text = "S";
                    ToolTip.SetToolTip(FindByNumbersResultTypeSentencesLabel, "find sentences across verses");
                }
                break;
            case NumberSearchType.Verses:
                {
                    FindByNumbersResultTypeVersesLabel.Text = "V";
                    ToolTip.SetToolTip(FindByNumbersResultTypeVersesLabel, "find verses");
                    FindByNumbersNumberLabel.Text = "number";
                    ToolTip.SetToolTip(FindByNumbersNumberLabel, "verse number");
                }
                break;
            case NumberSearchType.VerseRanges:
                {
                    FindByNumbersResultTypeVersesLabel.Text = "Vs";
                    ToolTip.SetToolTip(FindByNumbersResultTypeVersesLabel, "find verse ranges");
                    FindByNumbersNumberLabel.Text = "sum";
                    ToolTip.SetToolTip(FindByNumbersNumberLabel, "sum of verse numbers");
                }
                break;
            case NumberSearchType.Chapters:
                {
                    FindByNumbersResultTypeChaptersLabel.Text = "C";
                    ToolTip.SetToolTip(FindByNumbersResultTypeChaptersLabel, "find chapters");
                    FindByNumbersNumberLabel.Text = "number";
                    ToolTip.SetToolTip(FindByNumbersNumberLabel, "chapter number");
                }
                break;
            case NumberSearchType.ChapterRanges:
                {
                    FindByNumbersResultTypeChaptersLabel.Text = "Cs";
                    ToolTip.SetToolTip(FindByNumbersResultTypeChaptersLabel, "find chapter ranges");
                    FindByNumbersNumberLabel.Text = "sum";
                    ToolTip.SetToolTip(FindByNumbersNumberLabel, "sum of chapter numbers");
                }
                break;
            default:
                break;
        }

        switch (m_numbers_result_type)
        {
            case NumberSearchType.Words:
            case NumberSearchType.WordRanges:
                {
                    FindByNumbersWordsComparisonOperatorLabel.Text = "=";
                    FindByNumbersWordsComparisonOperatorLabel.Enabled = false;
                    FindByNumbersWordsNumberTypeLabel.Text = "";
                    FindByNumbersWordsNumberTypeLabel.Enabled = false;
                }
                break;
            case NumberSearchType.Sentences:
                {
                    FindByNumbersNumberLabel.Enabled = false;
                    FindByNumbersNumberComparisonOperatorLabel.Enabled = false;
                    FindByNumbersNumberNumericUpDown.Enabled = false;
                    FindByNumbersNumberNumberTypeLabel.Enabled = false;
                    FindByNumbersNumberComparisonOperatorLabel.Text = "=";
                    FindByNumbersNumberNumericUpDown.Value = 0;
                }
                break;
            case NumberSearchType.Verses:
            case NumberSearchType.VerseRanges:
                {
                    FindByNumbersVersesComparisonOperatorLabel.Text = "=";
                    FindByNumbersVersesComparisonOperatorLabel.Enabled = false;
                    FindByNumbersVersesNumberTypeLabel.Text = "";
                    FindByNumbersVersesNumberTypeLabel.Enabled = false;
                }
                break;
            case NumberSearchType.Chapters:
            case NumberSearchType.ChapterRanges:
                {
                    FindByNumbersChaptersComparisonOperatorLabel.Text = "=";
                    FindByNumbersChaptersComparisonOperatorLabel.Enabled = false;
                    FindByNumbersChaptersNumberTypeLabel.Text = "";
                    FindByNumbersChaptersNumberTypeLabel.Enabled = false;
                }
                break;
            default:
                break;
        }
    }
    private void FindByNumbersControl_EnabledChanged(object sender, EventArgs e)
    {
        Control control = sender as Control;
        if (control != null)
        {
            control.BackColor = (control.Enabled) ? SystemColors.Window : SystemColors.Control;
        }
    }
    private void FindByNumbersComparisonOperatorLabel_Click(object sender, EventArgs e)
    {
        Control control = sender as Control;
        if (control != null)
        {
            if (ModifierKeys == Keys.Shift)
            {
                if (control.Text == "=")
                {
                    control.Text = "≥";
                    ToolTip.SetToolTip(control, "greater than or equal");
                }
                else if (control.Text == "≠")
                {
                    control.Text = "=";
                    ToolTip.SetToolTip(control, "equal");
                }
                else if (control.Text == "<")
                {
                    control.Text = "≠";
                    ToolTip.SetToolTip(control, "not equal");
                }
                else if (control.Text == "≤")
                {
                    control.Text = "<";
                    ToolTip.SetToolTip(control, "less than");
                }
                else if (control.Text == ">")
                {
                    control.Text = "≤";
                    ToolTip.SetToolTip(control, "less than or equal");
                }
                else if (control.Text == "≥")
                {
                    control.Text = ">";
                    ToolTip.SetToolTip(control, "greater than");
                }
                else
                {
                    // do nothing
                }
            }
            else
            {
                if (control.Text == "=")
                {
                    control.Text = "≠";
                    ToolTip.SetToolTip(control, "not equal");
                }
                else if (control.Text == "≠")
                {
                    control.Text = "<";
                    ToolTip.SetToolTip(control, "less than");
                }
                else if (control.Text == "<")
                {
                    control.Text = "≤";
                    ToolTip.SetToolTip(control, "less than or equal");
                }
                else if (control.Text == "≤")
                {
                    control.Text = ">";
                    ToolTip.SetToolTip(control, "greater than");
                }
                else if (control.Text == ">")
                {
                    control.Text = "≥";
                    ToolTip.SetToolTip(control, "greater than or equal");
                }
                else if (control.Text == "≥")
                {
                    control.Text = "=";
                    ToolTip.SetToolTip(control, "equal");
                }
                else
                {
                    // do nothing
                }
            }
        }
        FindByNumbersControls_Enter(null, null);
    }
    private void FindByNumbersNumberTypeLabel_Click(object sender, EventArgs e)
    {
        Control control = sender as Control;
        if (control != null)
        {
            if (ModifierKeys != Keys.Shift)
            {
                if (control.Text == "")
                {
                    control.Text = "P";
                    control.ForeColor = GetNumberTypeColor(19);
                    ToolTip.SetToolTip(control, "prime = divisible by itself and 1 only");
                }
                else if (control.Text == "P")
                {
                    control.Text = "AP";
                    control.ForeColor = GetNumberTypeColor(47);
                    ToolTip.SetToolTip(control, "additive prime = prime with prime digit sum");
                }
                else if (control.Text == "AP")
                {
                    control.Text = "PP";
                    control.ForeColor = GetNumberTypeColor(313);
                    ToolTip.SetToolTip(control, "pure prime = additive prime with prime digits");
                }
                else if (control.Text == "PP")
                {
                    control.Text = "C";
                    control.ForeColor = GetNumberTypeColor(14);
                    ToolTip.SetToolTip(control, "composite = divisible by prime(s) below it");
                }
                else if (control.Text == "C")
                {
                    control.Text = "AC";
                    control.ForeColor = GetNumberTypeColor(114);
                    ToolTip.SetToolTip(control, "additive composite = composite with composite digit sum");
                }
                else if (control.Text == "AC")
                {
                    control.Text = "PC";
                    control.ForeColor = GetNumberTypeColor(9);
                    ToolTip.SetToolTip(control, "pure composite = additive composite with composite digits");
                }
                else if (control.Text == "PC")
                {
                    control.Text = "O";
                    control.ForeColor = Color.Olive;
                    ToolTip.SetToolTip(control, "odd = indivisible by 2");
                }
                else if (control.Text == "O")
                {
                    control.Text = "E";
                    control.ForeColor = Color.Olive;
                    ToolTip.SetToolTip(control, "even = divisible by 2");
                }
                else if (control.Text == "E")
                {
                    control.Text = null;
                    control.ForeColor = control.BackColor;
                    ToolTip.SetToolTip(control, "");
                }
            }
            else
            {
                if (control.Text == "")
                {
                    control.Text = "E";
                    control.ForeColor = Color.Olive;
                    ToolTip.SetToolTip(control, "even = divisible by 2");
                }
                else if (control.Text == "E")
                {
                    control.Text = "O";
                    control.ForeColor = Color.Olive;
                    ToolTip.SetToolTip(control, "odd = indivisible by 2");
                }
                else if (control.Text == "O")
                {
                    control.Text = "PC";
                    control.ForeColor = GetNumberTypeColor(9);
                    ToolTip.SetToolTip(control, "pure composite = additive composite with composite digits");
                }
                else if (control.Text == "PC")
                {
                    control.Text = "AC";
                    control.ForeColor = GetNumberTypeColor(114);
                    ToolTip.SetToolTip(control, "additive composite = composite with composite digit sum");
                }
                else if (control.Text == "AC")
                {
                    control.Text = "C";
                    control.ForeColor = GetNumberTypeColor(14);
                    ToolTip.SetToolTip(control, "composite = divisible by prime(s) below it");
                }
                else if (control.Text == "C")
                {
                    control.Text = "PP";
                    control.ForeColor = GetNumberTypeColor(313);
                    ToolTip.SetToolTip(control, "pure prime = additive prime with prime digits");
                }
                else if (control.Text == "PP")
                {
                    control.Text = "AP";
                    control.ForeColor = GetNumberTypeColor(47);
                    ToolTip.SetToolTip(control, "additive prime = prime with prime digit sum");
                }
                else if (control.Text == "AP")
                {
                    control.Text = "P";
                    control.ForeColor = GetNumberTypeColor(19);
                    ToolTip.SetToolTip(control, "prime = divisible by itself and 1 only");
                }
                else if (control.Text == "P")
                {
                    control.Text = null;
                    control.ForeColor = control.BackColor;
                    ToolTip.SetToolTip(control, "");
                }
            }

            if (control == FindByNumbersNumberNumberTypeLabel)
            {
                FindByNumbersNumberComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersNumberNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    FindByNumbersNumberComparisonOperatorLabel.Text = "=";
                    FindByNumbersNumberNumericUpDown.Value = 0;
                }
                else
                {
                    FindByNumbersNumberNumericUpDown.Focus();
                }
            }
            else if (control == FindByNumbersChaptersNumberTypeLabel)
            {
                FindByNumbersChaptersComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersChaptersNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    //if (m_numbers_result_type == FindByNumbersResultType.Chapters)
                    //{
                    //    m_numbers_result_type = FindByNumbersResultType.ChapterRanges;
                    //    FindByNumbersResultTypeChaptersLabel.Text = "Chapters";
                    //}
                    FindByNumbersChaptersComparisonOperatorLabel.Text = "=";
                    FindByNumbersChaptersNumericUpDown.Value = 0;
                }
                else
                {
                    //if (m_numbers_result_type == FindByNumbersResultType.ChapterRanges)
                    //{
                    //    m_numbers_result_type = FindByNumbersResultType.Chapters;
                    //    FindByNumbersResultTypeChaptersLabel.Text = "CHAPTER";
                    //}
                    FindByNumbersChaptersNumericUpDown.Focus();
                }
            }
            else if (control == FindByNumbersVersesNumberTypeLabel)
            {
                FindByNumbersVersesComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersVersesNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    //if (m_numbers_result_type == FindByNumbersResultType.Verses)
                    //{
                    //    m_numbers_result_type = FindByNumbersResultType.VerseRanges;
                    //    FindByNumbersResultTypeVersesLabel.Text = "Verses";
                    //}
                    FindByNumbersVersesComparisonOperatorLabel.Text = "=";
                    FindByNumbersVersesNumericUpDown.Value = 0;
                }
                else
                {
                    //if (m_numbers_result_type == FindByNumbersResultType.VerseRanges)
                    //{
                    //    m_numbers_result_type = FindByNumbersResultType.Verses;
                    //    FindByNumbersResultTypeVersesLabel.Text = "VERSES";
                    //}
                    FindByNumbersVersesNumericUpDown.Focus();
                }
            }
            else if (control == FindByNumbersWordsNumberTypeLabel)
            {
                FindByNumbersWordsComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersWordsNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    //if (m_numbers_result_type == FindByNumbersResultType.Words)
                    //{
                    //    m_numbers_result_type = FindByNumbersResultType.WordRanges;
                    //    FindByNumbersResultTypeWordsLabel.Text = "Words";
                    //}
                    FindByNumbersWordsComparisonOperatorLabel.Text = "=";
                    FindByNumbersWordsNumericUpDown.Value = 0;
                }
                else
                {
                    //if (m_numbers_result_type == FindByNumbersResultType.WordRanges)
                    //{
                    //    m_numbers_result_type = FindByNumbersResultType.Words;
                    //    FindByNumbersResultTypeWordsLabel.Text = "WORD";
                    //}
                    FindByNumbersWordsNumericUpDown.Focus();
                }
            }
            else if (control == FindByNumbersLettersNumberTypeLabel)
            {
                FindByNumbersLettersComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersLettersNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    FindByNumbersLettersComparisonOperatorLabel.Text = "=";
                    FindByNumbersLettersNumericUpDown.Value = 0;
                }
                else
                {
                    FindByNumbersLettersNumericUpDown.Focus();
                }
            }
            else if (control == FindByNumbersUniqueLettersNumberTypeLabel)
            {
                FindByNumbersUniqueLettersComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersUniqueLettersNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    FindByNumbersUniqueLettersComparisonOperatorLabel.Text = "=";
                    FindByNumbersUniqueLettersNumericUpDown.Value = 0;
                }
                else
                {
                    FindByNumbersUniqueLettersNumericUpDown.Focus();
                }
            }
            else if (control == FindByNumbersValueNumberTypeLabel)
            {
                FindByNumbersValueComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersValueNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    FindByNumbersValueComparisonOperatorLabel.Text = "=";
                    FindByNumbersValueNumericUpDown.Value = 0;
                }
                else
                {
                    FindByNumbersValueNumericUpDown.Focus();
                }
            }
            else if (control == FindByNumbersValueDigitSumNumberTypeLabel)
            {
                FindByNumbersValueDigitSumComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersValueDigitSumNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    FindByNumbersValueDigitSumComparisonOperatorLabel.Text = "=";
                    FindByNumbersValueDigitSumNumericUpDown.Value = 0;
                }
                else
                {
                    FindByNumbersValueDigitSumNumericUpDown.Focus();
                }
            }
            else if (control == FindByNumbersValueDigitalRootNumberTypeLabel)
            {
                FindByNumbersValueDigitalRootComparisonOperatorLabel.Enabled = (control.Text.Length == 0);
                FindByNumbersValueDigitalRootNumericUpDown.Enabled = (control.Text == "");
                if (control.Text.Length > 0)
                {
                    FindByNumbersValueDigitalRootComparisonOperatorLabel.Text = "=";
                    FindByNumbersValueDigitalRootNumericUpDown.Value = 0;
                }
                else
                {
                    FindByNumbersValueDigitalRootNumericUpDown.Focus();
                }
            }
            else
            {
                // do nothing
            }

            FindByNumbersControls_Enter(null, null);
        }
    }
    private void FindByNumbersControls_Enter(object sender, EventArgs e)
    {
        this.AcceptButton = FindByNumbersButton;
        FindType = FindType.Numbers;

        ResetFindBySimilarityResultTypeLabels();
        ResetFindByNumbersResultTypeLabels();
        ResetFindByFrequencyResultTypeLabels();

        switch (m_numbers_result_type)
        {
            case NumberSearchType.Words:
            case NumberSearchType.WordRanges:
                {
                    FindByNumbersResultTypeWordsLabel.BackColor = Color.SteelBlue;
                    FindByNumbersResultTypeWordsLabel.BorderStyle = BorderStyle.Fixed3D;
                }
                break;
            case NumberSearchType.Sentences:
                {
                    FindByNumbersResultTypeSentencesLabel.BackColor = Color.SteelBlue;
                    FindByNumbersResultTypeSentencesLabel.BorderStyle = BorderStyle.Fixed3D;
                }
                break;
            case NumberSearchType.Verses:
            case NumberSearchType.VerseRanges:
                {
                    FindByNumbersResultTypeVersesLabel.BackColor = Color.SteelBlue;
                    FindByNumbersResultTypeVersesLabel.BorderStyle = BorderStyle.Fixed3D;
                }
                break;
            case NumberSearchType.Chapters:
            case NumberSearchType.ChapterRanges:
                {
                    FindByNumbersResultTypeChaptersLabel.BackColor = Color.SteelBlue;
                    FindByNumbersResultTypeChaptersLabel.BorderStyle = BorderStyle.Fixed3D;
                }
                break;
            default:
                break;
        }

        FindByTextButton.Enabled = false;
        FindBySimilarityButton.Enabled = false;
        FindByNumbersButton.Enabled = true;
        FindByFrequencySumButton.Enabled = false;
    }
    private void FindByNumbersNumericUpDown_Leave(object sender, EventArgs e)
    {
        NumericUpDown control = sender as NumericUpDown;
        if (control != null)
        {
            if (String.IsNullOrEmpty(control.Text))
            {
                control.Value = 0;
                control.Refresh();
            }
        }
    }
    private void FindByNumbersNumericUpDown_ValueChanged(object sender, EventArgs e)
    {
        //NumericUpDown control = sender as NumericUpDown;
        //if (control != null)
        //{
        //    if (control.Value == 0)
        //    {
        //        this.ToolTip.SetToolTip(control, "any number");
        //    }
        //    else
        //    {
        //        this.ToolTip.SetToolTip(control, "");
        //    }
        //}

        // don't do auto-find as user may not have setting all parameters yet
        // some operations take too long and would frustrate user
        //FindByNumbers();

        UpdateFindByNumbersResultType();
    }
    private void FindByNumbersButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            FindByNumbers();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void FindByNumbers()
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            // 1. number types
            string number_symbol = FindByNumbersNumberNumberTypeLabel.Enabled ? FindByNumbersNumberNumberTypeLabel.Text : "";
            NumberType number_number_type =
                (number_symbol == "PP") ? NumberType.PurePrime :
                (number_symbol == "AP") ? NumberType.AdditivePrime :
                (number_symbol == "P") ? NumberType.Prime :
                (number_symbol == "PC") ? NumberType.PureComposite :
                (number_symbol == "AC") ? NumberType.AdditiveComposite :
                (number_symbol == "C") ? NumberType.Composite :
                (number_symbol == "O") ? NumberType.Odd :
                (number_symbol == "E") ? NumberType.Even :
                (number_symbol == "*") ? NumberType.Any :
                                         NumberType.None;
            string chapter_count_symbol = FindByNumbersChaptersNumberTypeLabel.Enabled ? FindByNumbersChaptersNumberTypeLabel.Text : "";
            NumberType chapter_count_number_type =
                (chapter_count_symbol == "PP") ? NumberType.PurePrime :
                (chapter_count_symbol == "AP") ? NumberType.AdditivePrime :
                (chapter_count_symbol == "P") ? NumberType.Prime :
                (chapter_count_symbol == "PC") ? NumberType.PureComposite :
                (chapter_count_symbol == "AC") ? NumberType.AdditiveComposite :
                (chapter_count_symbol == "C") ? NumberType.Composite :
                (chapter_count_symbol == "O") ? NumberType.Odd :
                (chapter_count_symbol == "E") ? NumberType.Even :
                (chapter_count_symbol == "*") ? NumberType.Any :
                                                NumberType.None;
            string verse_count_symbol = FindByNumbersVersesNumberTypeLabel.Enabled ? FindByNumbersVersesNumberTypeLabel.Text : "";
            NumberType verse_count_number_type =
                (verse_count_symbol == "PP") ? NumberType.PurePrime :
                (verse_count_symbol == "AP") ? NumberType.AdditivePrime :
                (verse_count_symbol == "P") ? NumberType.Prime :
                (verse_count_symbol == "PC") ? NumberType.PureComposite :
                (verse_count_symbol == "AC") ? NumberType.AdditiveComposite :
                (verse_count_symbol == "C") ? NumberType.Composite :
                (verse_count_symbol == "O") ? NumberType.Odd :
                (verse_count_symbol == "E") ? NumberType.Even :
                (verse_count_symbol == "*") ? NumberType.Any :
                                              NumberType.None;
            string word_count_symbol = FindByNumbersWordsNumberTypeLabel.Enabled ? FindByNumbersWordsNumberTypeLabel.Text : "";
            NumberType word_count_number_type =
                (word_count_symbol == "PP") ? NumberType.PurePrime :
                (word_count_symbol == "AP") ? NumberType.AdditivePrime :
                (word_count_symbol == "P") ? NumberType.Prime :
                (word_count_symbol == "PC") ? NumberType.PureComposite :
                (word_count_symbol == "AC") ? NumberType.AdditiveComposite :
                (word_count_symbol == "C") ? NumberType.Composite :
                (word_count_symbol == "O") ? NumberType.Odd :
                (word_count_symbol == "E") ? NumberType.Even :
                (word_count_symbol == "*") ? NumberType.Any :
                                             NumberType.None;
            string letter_count_symbol = FindByNumbersLettersNumberTypeLabel.Enabled ? FindByNumbersLettersNumberTypeLabel.Text : "";
            NumberType letter_count_number_type =
                (letter_count_symbol == "PP") ? NumberType.PurePrime :
                (letter_count_symbol == "AP") ? NumberType.AdditivePrime :
                (letter_count_symbol == "P") ? NumberType.Prime :
                (letter_count_symbol == "PC") ? NumberType.PureComposite :
                (letter_count_symbol == "AC") ? NumberType.AdditiveComposite :
                (letter_count_symbol == "C") ? NumberType.Composite :
                (letter_count_symbol == "O") ? NumberType.Odd :
                (letter_count_symbol == "E") ? NumberType.Even :
                (letter_count_symbol == "*") ? NumberType.Any :
                                               NumberType.None;
            string unique_letter_count_symbol = FindByNumbersUniqueLettersNumberTypeLabel.Enabled ? FindByNumbersUniqueLettersNumberTypeLabel.Text : "";
            NumberType unique_letter_count_number_type =
                (unique_letter_count_symbol == "PP") ? NumberType.PurePrime :
                (unique_letter_count_symbol == "AP") ? NumberType.AdditivePrime :
                (unique_letter_count_symbol == "P") ? NumberType.Prime :
                (unique_letter_count_symbol == "PC") ? NumberType.PureComposite :
                (unique_letter_count_symbol == "AC") ? NumberType.AdditiveComposite :
                (unique_letter_count_symbol == "C") ? NumberType.Composite :
                (unique_letter_count_symbol == "O") ? NumberType.Odd :
                (unique_letter_count_symbol == "E") ? NumberType.Even :
                (unique_letter_count_symbol == "*") ? NumberType.Any :
                                                      NumberType.None;
            string value_symbol = FindByNumbersValueNumberTypeLabel.Enabled ? FindByNumbersValueNumberTypeLabel.Text : "";
            NumberType value_number_type =
                (value_symbol == "PP") ? NumberType.PurePrime :
                (value_symbol == "AP") ? NumberType.AdditivePrime :
                (value_symbol == "P") ? NumberType.Prime :
                (value_symbol == "PC") ? NumberType.PureComposite :
                (value_symbol == "AC") ? NumberType.AdditiveComposite :
                (value_symbol == "C") ? NumberType.Composite :
                (value_symbol == "O") ? NumberType.Odd :
                (value_symbol == "E") ? NumberType.Even :
                (value_symbol == "*") ? NumberType.Any :
                                        NumberType.None;
            string value_digit_sum_symbol = FindByNumbersValueDigitSumNumberTypeLabel.Enabled ? FindByNumbersValueDigitSumNumberTypeLabel.Text : "";
            NumberType value_digit_sum_number_type =
                (value_digit_sum_symbol == "PP") ? NumberType.PurePrime :
                (value_digit_sum_symbol == "AP") ? NumberType.AdditivePrime :
                (value_digit_sum_symbol == "P") ? NumberType.Prime :
                (value_digit_sum_symbol == "PC") ? NumberType.PureComposite :
                (value_digit_sum_symbol == "AC") ? NumberType.AdditiveComposite :
                (value_digit_sum_symbol == "C") ? NumberType.Composite :
                (value_digit_sum_symbol == "O") ? NumberType.Odd :
                (value_digit_sum_symbol == "E") ? NumberType.Even :
                (value_digit_sum_symbol == "*") ? NumberType.Any :
                                                  NumberType.None;
            string value_digital_root_symbol = FindByNumbersValueDigitalRootNumberTypeLabel.Enabled ? FindByNumbersValueDigitalRootNumberTypeLabel.Text : "";
            NumberType value_digital_root_number_type =
                (value_digital_root_symbol == "PP") ? NumberType.PurePrime :
                (value_digital_root_symbol == "AP") ? NumberType.AdditivePrime :
                (value_digital_root_symbol == "P") ? NumberType.Prime :
                (value_digital_root_symbol == "PC") ? NumberType.PureComposite :
                (value_digital_root_symbol == "AC") ? NumberType.AdditiveComposite :
                (value_digital_root_symbol == "C") ? NumberType.Composite :
                (value_digital_root_symbol == "O") ? NumberType.Odd :
                (value_digital_root_symbol == "E") ? NumberType.Even :
                (value_digital_root_symbol == "*") ? NumberType.Any :
                                                     NumberType.None;

            // 2. numbers
            int number = FindByNumbersNumberNumericUpDown.Enabled ? ((number_number_type == NumberType.None) ? (int)FindByNumbersNumberNumericUpDown.Value : 0) : 0;
            int chapter_count = FindByNumbersChaptersNumericUpDown.Enabled ? ((chapter_count_number_type == NumberType.None) ? (int)FindByNumbersChaptersNumericUpDown.Value : 0) : 0;
            int verse_count = FindByNumbersVersesNumericUpDown.Enabled ? ((verse_count_number_type == NumberType.None) ? (int)FindByNumbersVersesNumericUpDown.Value : 0) : 0;
            int word_count = FindByNumbersWordsNumericUpDown.Enabled ? ((word_count_number_type == NumberType.None) ? (int)FindByNumbersWordsNumericUpDown.Value : 0) : 0;
            int letter_count = FindByNumbersLettersNumericUpDown.Enabled ? ((letter_count_number_type == NumberType.None) ? (int)FindByNumbersLettersNumericUpDown.Value : 0) : 0;
            int unique_letter_count = FindByNumbersUniqueLettersNumericUpDown.Enabled ? ((unique_letter_count_number_type == NumberType.None) ? (int)FindByNumbersUniqueLettersNumericUpDown.Value : 0) : 0;
            long value = FindByNumbersValueNumericUpDown.Enabled ? ((value_number_type == NumberType.None) ? (long)FindByNumbersValueNumericUpDown.Value : 0) : 0;
            int value_digit_sum = FindByNumbersValueDigitSumNumericUpDown.Enabled ? ((value_digit_sum_number_type == NumberType.None) ? (int)FindByNumbersValueDigitSumNumericUpDown.Value : 0) : 0;
            int value_digital_root = FindByNumbersValueDigitalRootNumericUpDown.Enabled ? ((value_digital_root_number_type == NumberType.None) ? (int)FindByNumbersValueDigitalRootNumericUpDown.Value : 0) : 0;

            // 3. comparison operators = ≠ < ≤ > ≥
            string number_operator_symbol = FindByNumbersNumberComparisonOperatorLabel.Text;
            ComparisonOperator number_comparison_operator =
                (number_operator_symbol == "=") ? ComparisonOperator.Equal :
                (number_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (number_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (number_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (number_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (number_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                  ComparisonOperator.Unknown;
            string chapter_count_operator_symbol = FindByNumbersChaptersComparisonOperatorLabel.Text;
            ComparisonOperator chapter_count_comparison_operator =
                (chapter_count_operator_symbol == "=") ? ComparisonOperator.Equal :
                (chapter_count_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (chapter_count_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (chapter_count_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (chapter_count_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (chapter_count_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                         ComparisonOperator.Unknown;
            string verse_count_operator_symbol = FindByNumbersVersesComparisonOperatorLabel.Text;
            ComparisonOperator verse_count_comparison_operator =
                (verse_count_operator_symbol == "=") ? ComparisonOperator.Equal :
                (verse_count_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (verse_count_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (verse_count_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (verse_count_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (verse_count_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                       ComparisonOperator.Unknown;
            string word_count_operator_symbol = FindByNumbersWordsComparisonOperatorLabel.Text;
            ComparisonOperator word_count_comparison_operator =
                (word_count_operator_symbol == "=") ? ComparisonOperator.Equal :
                (word_count_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (word_count_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (word_count_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (word_count_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (word_count_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                      ComparisonOperator.Unknown;
            string letter_count_operator_symbol = FindByNumbersLettersComparisonOperatorLabel.Text;
            ComparisonOperator letter_count_comparison_operator =
                (letter_count_operator_symbol == "=") ? ComparisonOperator.Equal :
                (letter_count_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (letter_count_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (letter_count_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (letter_count_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (letter_count_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                        ComparisonOperator.Unknown;
            string unique_letter_count_operator_symbol = FindByNumbersUniqueLettersComparisonOperatorLabel.Text;
            ComparisonOperator unique_letter_count_comparison_operator =
                (unique_letter_count_operator_symbol == "=") ? ComparisonOperator.Equal :
                (unique_letter_count_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (unique_letter_count_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (unique_letter_count_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (unique_letter_count_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (unique_letter_count_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                               ComparisonOperator.Unknown;
            string value_operator_symbol = FindByNumbersValueComparisonOperatorLabel.Text;
            ComparisonOperator value_comparison_operator =
                (value_operator_symbol == "=") ? ComparisonOperator.Equal :
                (value_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (value_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (value_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (value_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (value_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                 ComparisonOperator.Unknown;
            string digit_sum_operator_symbol = FindByNumbersValueDigitSumComparisonOperatorLabel.Text;
            ComparisonOperator value_digit_sum_comparison_operator =
                (digit_sum_operator_symbol == "=") ? ComparisonOperator.Equal :
                (digit_sum_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (digit_sum_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (digit_sum_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (digit_sum_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (digit_sum_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                     ComparisonOperator.Unknown;
            string digital_root_operator_symbol = FindByNumbersValueDigitalRootComparisonOperatorLabel.Text;
            ComparisonOperator value_digital_root_comparison_operator =
                (digital_root_operator_symbol == "=") ? ComparisonOperator.Equal :
                (digital_root_operator_symbol == "≠") ? ComparisonOperator.NotEqual :
                (digital_root_operator_symbol == "<") ? ComparisonOperator.LessThan :
                (digital_root_operator_symbol == "≤") ? ComparisonOperator.LessThanOrEqual :
                (digital_root_operator_symbol == ">") ? ComparisonOperator.GreaterThan :
                (digital_root_operator_symbol == "≥") ? ComparisonOperator.GreaterThanOrEqual :
                                                        ComparisonOperator.Unknown;

            string text = null;
            text += "number" + number_operator_symbol + ((number > 0) ? number.ToString() : ((number_number_type != NumberType.None) ? FindByNumbersNumberNumberTypeLabel.Text : "*")) + " ";

            if (
                (m_numbers_result_type == NumberSearchType.ChapterRanges)
               )
            {
                text += "chapters" + chapter_count_operator_symbol + ((chapter_count > 0) ? chapter_count.ToString() : ((chapter_count_number_type != NumberType.None) ? FindByNumbersChaptersNumberTypeLabel.Text : "*")) + " ";
            }

            if (
                (m_numbers_result_type == NumberSearchType.Chapters) ||
                (m_numbers_result_type == NumberSearchType.ChapterRanges) ||
                (m_numbers_result_type == NumberSearchType.VerseRanges)
               )
            {
                text += "verses" + verse_count_operator_symbol + ((verse_count > 0) ? verse_count.ToString() : ((verse_count_number_type != NumberType.None) ? FindByNumbersVersesNumberTypeLabel.Text : "*")) + " ";
            }

            if (
                (m_numbers_result_type == NumberSearchType.Chapters) ||
                (m_numbers_result_type == NumberSearchType.ChapterRanges) ||
                (m_numbers_result_type == NumberSearchType.Verses) ||
                (m_numbers_result_type == NumberSearchType.VerseRanges) ||
                (m_numbers_result_type == NumberSearchType.Sentences) ||
                (m_numbers_result_type == NumberSearchType.WordRanges)
               )
            {
                text += "words" + word_count_operator_symbol + ((word_count > 0) ? word_count.ToString() : ((word_count_number_type != NumberType.None) ? FindByNumbersWordsNumberTypeLabel.Text : "*")) + " ";
            }

            text += "letters" + letter_count_operator_symbol + ((letter_count > 0) ? letter_count.ToString() : ((letter_count_number_type != NumberType.None) ? FindByNumbersLettersNumberTypeLabel.Text : "*")) + " ";
            text += "unique" + unique_letter_count_operator_symbol + ((unique_letter_count > 0) ? unique_letter_count.ToString() : ((unique_letter_count_number_type != NumberType.None) ? FindByNumbersUniqueLettersNumberTypeLabel.Text : "*")) + " ";
            text += "value" + value_operator_symbol + ((value > 0) ? value.ToString() : ((value_number_type != NumberType.None) ? FindByNumbersValueNumberTypeLabel.Text : "*")) + " ";
            text += "digit_sum" + digit_sum_operator_symbol + ((value_digit_sum > 0) ? value_digit_sum.ToString() : ((value_digit_sum_number_type != NumberType.None) ? FindByNumbersValueDigitSumNumberTypeLabel.Text : "*")) + " ";
            text += "digital_root" + digital_root_operator_symbol + ((value_digital_root > 0) ? value_digital_root.ToString() : ((value_digital_root_number_type != NumberType.None) ? FindByNumbersValueDigitalRootNumberTypeLabel.Text : "*")) + "";

            NumberQuery query = new NumberQuery();
            query.Number = number;
            query.ChapterCount = chapter_count;
            query.VerseCount = verse_count;
            query.WordCount = word_count;
            query.LetterCount = letter_count;
            query.UniqueLetterCount = unique_letter_count;
            query.Value = value;
            query.ValueDigitSum = value_digit_sum;
            query.ValueDigitalRoot = value_digital_root;
            query.NumberNumberType = number_number_type;
            query.ChapterCountNumberType = chapter_count_number_type;
            query.VerseCountNumberType = verse_count_number_type;
            query.WordCountNumberType = word_count_number_type;
            query.LetterCountNumberType = letter_count_number_type;
            query.UniqueLetterCountNumberType = unique_letter_count_number_type;
            query.ValueNumberType = value_number_type;
            query.ValueDigitSumNumberType = value_digit_sum_number_type;
            query.ValueDigitalRootNumberType = value_digital_root_number_type;
            query.NumberComparisonOperator = number_comparison_operator;
            query.ChapterCountComparisonOperator = chapter_count_comparison_operator;
            query.VerseCountComparisonOperator = verse_count_comparison_operator;
            query.WordCountComparisonOperator = word_count_comparison_operator;
            query.LetterCountComparisonOperator = letter_count_comparison_operator;
            query.UniqueLetterCountComparisonOperator = unique_letter_count_comparison_operator;
            query.ValueComparisonOperator = value_comparison_operator;
            query.ValueDigitSumComparisonOperator = value_digit_sum_comparison_operator;
            query.ValueDigitalRootComparisonOperator = value_digital_root_comparison_operator;

            if (!query.IsEmpty(m_numbers_result_type))
            {
                int match_count = -1;
                switch (m_numbers_result_type)
                {
                    case NumberSearchType.Words:
                        {
                            match_count = m_client.FindWords(query);
                            m_find_result_header = match_count + ((match_count == 1) ? " word" : " words") + " in " + m_client.FoundVerses.Count + ((m_client.FoundVerses.Count == 1) ? " verse" : " verses") + " with " + text + " in " + m_client.FindScope.ToString();
                            DisplayFoundVerses(true);
                        }
                        break;
                    case NumberSearchType.WordRanges:
                        {
                            match_count = m_client.FindWordRanges(query);
                            m_find_result_header = match_count + ((match_count == 1) ? " word range" : " word ranges") + " in " + m_client.FoundVerses.Count + ((m_client.FoundVerses.Count == 1) ? " verse" : " verses") + " with " + text + " in " + m_client.FindScope.ToString();
                            DisplayFoundVerses(true);
                        }
                        break;
                    case NumberSearchType.Sentences:
                        {
                            match_count = m_client.FindSentences(query);
                            m_find_result_header = match_count + ((match_count == 1) ? " sentence" : " sentences") + " with " + text + " in " + m_client.FindScope.ToString();
                            DisplayFoundVerses(true);
                        }
                        break;
                    case NumberSearchType.Verses:
                        {
                            match_count = m_client.FindVerses(query);
                            m_find_result_header = match_count + ((match_count == 1) ? " verse" : " verses") + " with " + text + " in " + m_client.FindScope.ToString();
                            DisplayFoundVerses(true);
                        }
                        break;
                    case NumberSearchType.VerseRanges:
                        {
                            match_count = m_client.FindVerseRanges(query);
                            m_find_result_header = match_count + ((match_count == 1) ? " verse range" : " verse ranges") + " with " + text + " in " + m_client.FindScope.ToString();
                            DisplayFoundVerseRanges(true);
                        }
                        break;
                    case NumberSearchType.Chapters:
                        {
                            match_count = m_client.FindChapters(query);
                            m_find_result_header = match_count + ((match_count == 1) ? " chapter" : " chapters") + " with " + text + " in " + m_client.FindScope.ToString();
                            DisplayFoundChapters(true);
                        }
                        break;
                    case NumberSearchType.ChapterRanges:
                        {
                            match_count = m_client.FindChapterRanges(query);
                            m_find_result_header = match_count + ((match_count == 1) ? " chapter range" : " chapter ranges") + " with " + text + " in " + m_client.FindScope.ToString();
                            DisplayFoundChapterRanges(true);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 20. Search By Revelation
    ///////////////////////////////////////////////////////////////////////////////
    private RevelationPlace m_find_by_revelation_place = RevelationPlace.Both;
    private void FindByRevelationPlaceControls_Enter(object sender, EventArgs e)
    {
        this.AcceptButton = null;
        FindType = FindType.Revelation;

        ResetFindBySimilarityResultTypeLabels();
        ResetFindByNumbersResultTypeLabels();
        ResetFindByFrequencyResultTypeLabels();
    }
    private void FindByRevelationPlaceRadioButton_CheckedChanged(object sender, EventArgs e)
    {
        //FindByRevelation();
    }
    private void FindByRevelationPlaceButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            FindByRevelation();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void FindByRevelation()
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            if (!FindByRevelationPlaceMakkahRadioButton.Checked && !FindByRevelationPlaceMedinaRadioButton.Checked)
            {
                m_find_by_revelation_place = RevelationPlace.None;
            }
            else if (FindByRevelationPlaceMakkahRadioButton.Checked && !FindByRevelationPlaceMedinaRadioButton.Checked)
            {
                m_find_by_revelation_place = RevelationPlace.Makkah;
            }
            else if (!FindByRevelationPlaceMakkahRadioButton.Checked && FindByRevelationPlaceMedinaRadioButton.Checked)
            {
                m_find_by_revelation_place = RevelationPlace.Medina;
            }
            else if (FindByRevelationPlaceMakkahRadioButton.Checked && FindByRevelationPlaceMedinaRadioButton.Checked)
            {
                m_find_by_revelation_place = RevelationPlace.Both;
            }

            m_client.FindChapters(m_find_by_revelation_place);
            if (m_client.FoundChapters != null)
            {
                m_client.FoundVerses = new List<Verse>();
                foreach (Chapter chapter in m_client.FoundChapters)
                {
                    m_client.FoundVerses.AddRange(chapter.Verses);
                }

                if (m_client.FoundVerses != null)
                {
                    int chapter_count = m_client.FoundChapters.Count;
                    m_find_result_header = chapter_count + ((chapter_count == 1) ? " chapter" : " chapters") + " revealed in " + m_find_by_revelation_place.ToString() + " in " + m_client.FindScope.ToString();

                    DisplayFoundChapters(true);
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 21. Search By Prostration
    ///////////////////////////////////////////////////////////////////////////////
    private ProstrationType m_find_by_prostration_type = ProstrationType.None;
    private void FindByProstrationTypeControls_Enter(object sender, EventArgs e)
    {
        this.AcceptButton = null;
        FindType = FindType.Prostration;

        ResetFindBySimilarityResultTypeLabels();
        ResetFindByNumbersResultTypeLabels();
        ResetFindByFrequencyResultTypeLabels();
    }
    private void FindByProstrationTypeRadioButton_CheckedChanged(object sender, EventArgs e)
    {
        //FindByProstration();
    }
    private void FindByProstrationTypeButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            FindByProstration();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void FindByProstration()
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            if (!FindByProstrationTypeObligatoryCheckBox.Checked && !FindByProstrationTypeRecommendedCheckBox.Checked)
            {
                m_find_by_prostration_type = ProstrationType.None;
            }
            else if (FindByProstrationTypeObligatoryCheckBox.Checked && !FindByProstrationTypeRecommendedCheckBox.Checked)
            {
                m_find_by_prostration_type = ProstrationType.Obligatory;
            }
            else if (!FindByProstrationTypeObligatoryCheckBox.Checked && FindByProstrationTypeRecommendedCheckBox.Checked)
            {
                m_find_by_prostration_type = ProstrationType.Recommended;
            }
            else if (FindByProstrationTypeObligatoryCheckBox.Checked && FindByProstrationTypeRecommendedCheckBox.Checked)
            {
                m_find_by_prostration_type = ProstrationType.Both;
            }

            m_client.FindVerses(m_find_by_prostration_type);
            if (m_client.FoundVerses != null)
            {
                int verse_count = m_client.FoundVerses.Count;
                m_find_result_header = verse_count + ((verse_count == 1) ? " verse" : " verses") + " with " + m_find_by_prostration_type.ToString() + " prostrations" + " in " + m_client.FindScope.ToString();
                DisplayFoundVerses(true);
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 22. Search By FrequencySum
    ///////////////////////////////////////////////////////////////////////////////
    private FrequencySearchType m_frequency_result_type = FrequencySearchType.Sentences;
    private void FindByFrequencyResultTypeWordsLabel_Click(object sender, EventArgs e)
    {
        m_frequency_result_type = FrequencySearchType.Words;
        FindByFrequencyControls_Enter(null, null);

        ResetFindByFrequencyResultTypeLabels();
        FindByFrequencyResultTypeWordsLabel.BackColor = Color.PaleVioletRed;
        //FindByFrequencyResultTypeWordsLabel.BorderStyle = BorderStyle.Fixed3D;
    }
    private void FindByFrequencyResultTypeSentencesLabel_Click(object sender, EventArgs e)
    {
        m_frequency_result_type = FrequencySearchType.Sentences;
        FindByFrequencyControls_Enter(null, null);

        ResetFindByFrequencyResultTypeLabels();
        FindByFrequencyResultTypeSentencesLabel.BackColor = Color.PaleVioletRed;
        //FindByFrequencyResultTypeSentencesLabel.BorderStyle = BorderStyle.Fixed3D;
    }
    private void FindByFrequencyResultTypeVersesLabel_Click(object sender, EventArgs e)
    {
        m_frequency_result_type = FrequencySearchType.Verses;
        FindByFrequencyControls_Enter(null, null);

        ResetFindByFrequencyResultTypeLabels();
        FindByFrequencyResultTypeVersesLabel.BackColor = Color.PaleVioletRed;
        //FindByFrequencyResultTypeVersesLabel.BorderStyle = BorderStyle.Fixed3D;
    }
    private void ResetFindByFrequencyResultTypeLabels()
    {
        FindByFrequencyResultTypeWordsLabel.BackColor = Color.DarkGray;
        FindByFrequencyResultTypeWordsLabel.BorderStyle = BorderStyle.None;

        FindByFrequencyResultTypeSentencesLabel.BackColor = Color.DarkGray;
        FindByFrequencyResultTypeSentencesLabel.BorderStyle = BorderStyle.None;

        FindByFrequencyResultTypeVersesLabel.BackColor = Color.DarkGray;
        FindByFrequencyResultTypeVersesLabel.BorderStyle = BorderStyle.None;
    }
    private FrequencySumType m_frequency_sum_type = FrequencySumType.Duplicates;
    private void FindByFrequencySumTypeLabel_Click(object sender, EventArgs e)
    {
        if (m_frequency_sum_type == FrequencySumType.NoDuplicates)
        {
            m_frequency_sum_type = FrequencySumType.Duplicates;
            FindByFrequencySumTypeLabel.Text = "DUPLICATE LETTERS";
            FindByFrequencySumTypeLabel.BackColor = Color.PaleVioletRed;
            ToolTip.SetToolTip(FindByFrequencySumTypeLabel, "include duplicate phrase letters");
        }
        else if (m_frequency_sum_type == FrequencySumType.Duplicates)
        {
            m_frequency_sum_type = FrequencySumType.NoDuplicates;
            FindByFrequencySumTypeLabel.Text = "NO DUPLICATE LETTERS";
            FindByFrequencySumTypeLabel.BackColor = Color.DarkGray;
            ToolTip.SetToolTip(FindByFrequencySumTypeLabel, "exclude duplicate phrase letters");
        }
        FindByFrequencyControls_Enter(null, null);

        CalculatePhraseLetterStatistics();
        DisplayPhraseLetterStatistics();
    }
    private void FindByFrequencyControls_Enter(object sender, EventArgs e)
    {
        this.AcceptButton = FindByFrequencySumButton;
        FindType = FindType.FrequencySum;

        ResetFindBySimilarityResultTypeLabels();
        ResetFindByNumbersResultTypeLabels();
        ResetFindByFrequencyResultTypeLabels();

        switch (m_frequency_result_type)
        {
            case FrequencySearchType.Words:
                {
                    FindByFrequencyResultTypeWordsLabel.BackColor = Color.PaleVioletRed;
                    FindByFrequencyResultTypeWordsLabel.BorderStyle = BorderStyle.None;
                }
                break;
            case FrequencySearchType.Sentences:
                {
                    FindByFrequencyResultTypeSentencesLabel.BackColor = Color.PaleVioletRed;
                    FindByFrequencyResultTypeSentencesLabel.BorderStyle = BorderStyle.None;
                }
                break;
            case FrequencySearchType.Verses:
                {
                    FindByFrequencyResultTypeVersesLabel.BackColor = Color.PaleVioletRed;
                    FindByFrequencyResultTypeVersesLabel.BorderStyle = BorderStyle.None;
                }
                break;
        }

        FindByTextButton.Enabled = false;
        FindBySimilarityButton.Enabled = false;
        FindByNumbersButton.Enabled = false;
        FindByFrequencySumButton.Enabled = true;
    }
    private void FindByFrequencyPhraseLabel_Click(object sender, EventArgs e)
    {
        LinkLabel_Click(sender, null);
    }
    private void FindByFrequencyPhraseTextBox_TextChanged(object sender, EventArgs e)
    {
        if (m_found_verses_displayed)
        {
            DisplaySelection(false);
        }

        CalculatePhraseLetterStatistics();
        DisplayPhraseLetterStatistics();
    }
    private void FindByFrequencySumNumericUpDown_ValueChanged(object sender, EventArgs e)
    {
        FindByFrequencySumNumericUpDown.ForeColor = GetNumberTypeColor((long)FindByFrequencySumNumericUpDown.Value);
        // don't do it, as search result will change the control value which would result in another search
        //if (sender == FindByFrequencySumNumericUpDown)
        //{
        //    FindByFrequencySum();
        //}
    }
    private void FindByFrequencySumNumericUpDown_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                FindByFrequencySum();
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
    private void FindByFrequencySumButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            FindByFrequencySum();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void FindByFrequencySum()
    {
        if (m_client != null)
        {
            PrepareNewSearch();

            m_find_matches.Clear();
            m_find_match_index = -1;

            string phrase = FindByFrequencyPhraseTextBox.Text;
            int letter_frequency_sum = (int)FindByFrequencySumNumericUpDown.Value;
            if (!String.IsNullOrEmpty(phrase))
            {
                if (letter_frequency_sum > 0)
                {
                    switch (m_frequency_result_type)
                    {
                        case FrequencySearchType.Words:
                            {
                                m_client.FindWords(phrase, letter_frequency_sum, m_frequency_sum_type);
                                if (m_client.FoundPhrases != null)
                                {
                                    int count = m_client.FoundVerses.Count;
                                    m_find_result_header = count + ((count == 1) ? " word" : " words") + " with " + "letter frequency sum in " + phrase + " = " + letter_frequency_sum + ((m_frequency_sum_type == FrequencySumType.Duplicates) ? "" : " without duplicates") + " in " + m_client.FindScope.ToString();
                                    DisplayFoundVerses(true);
                                }
                            }
                            break;
                        case FrequencySearchType.Sentences:
                            {
                                m_client.FindSentences(phrase, letter_frequency_sum, m_frequency_sum_type);
                                if (m_client.FoundPhrases != null)
                                {
                                    int count = m_client.FoundVerses.Count;
                                    m_find_result_header = count + ((count == 1) ? " sentence" : " sentences") + " with " + "letter frequency sum in " + phrase + " = " + letter_frequency_sum + ((m_frequency_sum_type == FrequencySumType.Duplicates) ? "" : " without duplicates") + " in " + m_client.FindScope.ToString();
                                    DisplayFoundVerses(true);
                                }
                            }
                            break;
                        case FrequencySearchType.Verses:
                        default:
                            {
                                m_client.FindVerses(phrase, letter_frequency_sum, m_frequency_sum_type);
                                if (m_client.FoundVerses != null)
                                {
                                    int count = m_client.FoundVerses.Count;
                                    m_find_result_header = count + ((count == 1) ? " verse" : " verses") + " with " + "letter frequency sum in " + phrase + " = " + letter_frequency_sum + ((m_frequency_sum_type == FrequencySumType.Duplicates) ? "" : " without duplicates") + " in " + m_client.FindScope.ToString();
                                    DisplayFoundVerses(true);
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 23. Display Search Results
    ///////////////////////////////////////////////////////////////////////////////
    // F3 and Shift+F3 Goto next/previous matches
    private struct FindMatch
    {
        public int Start;
        public int Length;
    }
    private List<FindMatch> m_find_matches = new List<FindMatch>();
    private void BuildFindMatch(int start, int length)
    {
        // build text_selections list for F3 and Shift+F3
        FindMatch text_selection = new FindMatch();
        if (m_find_matches != null)
        {
            text_selection.Start = start;
            text_selection.Length = length;
            m_find_matches.Add(text_selection);
        }
    }
    private int m_find_match_index = -1;
    private void GotoPreviousFindMatch()
    {
        m_find_match_index = -1;
        for (int i = 0; i < m_find_matches.Count; i++)
        {
            if (m_find_matches[i].Start > SearchResultTextBox.SelectionStart)
            {
                m_find_match_index = i - 1;
                break;
            }
        }
    }
    private void GotoNextFindMatch()
    {
        m_find_match_index = m_find_matches.Count;
        for (int i = m_find_matches.Count - 1; i >= 0; i--)
        {
            if (m_find_matches[i].Start < SearchResultTextBox.SelectionStart)
            {
                m_find_match_index = i + 1;
                break;
            }
        }
    }
    private void SelectNextFindMatch()
    {
        if (m_found_verses_displayed)
        {
            if (m_find_matches != null)
            {
                if (m_find_matches.Count > 0)
                {
                    // find the index prior to the current cursor postion
                    GotoPreviousFindMatch();
                    m_find_match_index++;

                    // round robin
                    if (m_find_match_index == m_find_matches.Count)
                    {
                        m_find_match_index = 0;
                    }

                    // find next match
                    if ((m_find_match_index >= 0) && (m_find_match_index < m_find_matches.Count))
                    {
                        int start = m_find_matches[m_find_match_index].Start;
                        int length = m_find_matches[m_find_match_index].Length;
                        if ((start >= 0) && (start < SearchResultTextBox.Text.Length))
                        {
                            SearchResultTextBox.Select(start, length);
                            SearchResultTextBox.SelectionColor = Color.Red;
                        }
                    }
                }
            }
        }
        UpdateFindMatchCaption();
    }
    private void SelectPreviousFindMatch()
    {
        if (m_found_verses_displayed)
        {
            if (m_find_matches != null)
            {
                if (m_find_matches.Count > 0)
                {
                    // find the index after the current cursor postion
                    GotoNextFindMatch();
                    m_find_match_index--;

                    // round robin
                    if (m_find_match_index < 0)
                    {
                        m_find_match_index = m_find_matches.Count - 1;
                    }

                    // find previous match
                    if ((m_find_match_index >= 0) && (m_find_match_index < m_find_matches.Count))
                    {
                        int start = m_find_matches[m_find_match_index].Start;
                        int length = m_find_matches[m_find_match_index].Length;
                        if ((start >= 0) && (start < SearchResultTextBox.Text.Length))
                        {
                            SearchResultTextBox.Select(start, length);
                            SearchResultTextBox.SelectionColor = Color.Red;
                        }
                    }
                }
            }
        }
        UpdateFindMatchCaption();
    }
    private void UpdateFindMatchCaption()
    {
        string caption = this.Text;
        int pos = caption.IndexOf(CAPTION_SEPARATOR);
        if (pos > -1)
        {
            caption = caption.Substring(0, pos);
        }

        if (m_found_verses_displayed)
        {
            caption += CAPTION_SEPARATOR + " Match " + ((m_find_match_index + 1) + "/" + m_find_matches.Count);
        }
        else
        {
            //caption += CAPTION_SEPARATOR;
        }

        this.Text = caption;
    }

    private void UpdateFindScope()
    {
        if (m_client != null)
        {
            if (m_client.FindScope == FindScope.Book)
            {
                FindScopeLabel.Text = "Entire Book";
                this.ToolTip.SetToolTip(FindScopeLabel, null);
            }
            else if (m_client.FindScope == FindScope.Selection)
            {
                FindScopeLabel.Text = "Selection";
                this.ToolTip.SetToolTip(FindScopeLabel, GetFindScope());
            }
            else if (m_client.FindScope == FindScope.Result)
            {
                FindScopeLabel.Text = "Search Result";
                this.ToolTip.SetToolTip(FindScopeLabel, null);
            }
        }
    }
    private void NextFindScope()
    {
        if (m_client != null)
        {
            if (ModifierKeys != Keys.Shift)
            {
                if (m_client.FindScope == FindScope.Book)
                {
                    m_client.FindScope = FindScope.Selection;
                }
                else if (m_client.FindScope == FindScope.Selection)
                {
                    m_client.FindScope = FindScope.Result;
                }
                else if (m_client.FindScope == FindScope.Result)
                {
                    m_client.FindScope = FindScope.Book;
                }
            }
            else
            {
                if (m_client.FindScope == FindScope.Result)
                {
                    m_client.FindScope = FindScope.Selection;
                }
                else if (m_client.FindScope == FindScope.Selection)
                {
                    m_client.FindScope = FindScope.Book;
                }
                else if (m_client.FindScope == FindScope.Book)
                {
                    m_client.FindScope = FindScope.Result;
                }
            }
        }
    }
    private string GetSummarizedFindScope()
    {
        string result = GetFindScope();
        if (result != null)
        {
            // trim if too long
            if (result.Length > SELECTON_SCOPE_TEXT_MAX_LENGTH)
            {
                result = result.Substring(0, SELECTON_SCOPE_TEXT_MAX_LENGTH) + " ...";
            }
        }
        return result;
    }
    private string GetFindScope()
    {
        string result = null;
        if (m_client != null)
        {
            if (m_client.Selection != null)
            {
                if ((m_client.Selection.Scope == SelectionScope.Word) || (m_client.Selection.Scope == SelectionScope.Letter))
                {
                    int verse_number = (int)VerseNumericUpDown.Value;
                    result = "Verse" + " " + m_client.Book.Verses[verse_number - 1].Address;
                }
                else // if scope is Chapter, Page, Station, Part, Group, Quarter, Bowing, Verse
                {
                    StringBuilder str = new StringBuilder();
                    if (AreConsecutive(m_client.Selection.Indexes))
                    {
                        if (m_client.Selection.Indexes.Count > 1)
                        {
                            int first_index = m_client.Selection.Indexes[0];
                            int last_index = m_client.Selection.Indexes[m_client.Selection.Indexes.Count - 1];
                            if (m_client.Selection.Scope == SelectionScope.Verse)
                            {
                                str.Append(m_client.Book.Verses[first_index].Address + " - ");
                                str.Append(m_client.Book.Verses[last_index].Address);
                            }
                            else
                            {
                                str.Append((first_index + 1).ToString() + "-");
                                str.Append((last_index + 1).ToString());
                            }
                        }
                        else if (m_client.Selection.Indexes.Count == 1)
                        {
                            int first_index = m_client.Selection.Indexes[0];
                            if (m_client.Selection.Scope == SelectionScope.Verse)
                            {
                                str.Append(m_client.Book.Verses[first_index].Address);
                            }
                            else
                            {
                                str.Append((m_client.Selection.Indexes[0] + 1).ToString());
                            }
                        }
                        else
                        {
                            // do nothing
                        }
                    }
                    else
                    {
                        if (m_client.Selection.Indexes.Count > 0)
                        {
                            foreach (int index in m_client.Selection.Indexes)
                            {
                                str.Append((index + 1).ToString() + " ");
                            }
                            if (str.Length > 1)
                            {
                                str.Remove(str.Length - 1, 1);
                            }
                        }
                    }

                    if (m_client.Selection.Indexes.Count == 1)
                    {
                        result = m_client.Selection.Scope.ToString() + " " + str.ToString();
                    }
                    else if (m_client.Selection.Indexes.Count > 1)
                    {
                        result = m_client.Selection.Scope.ToString() + "s" + " " + str.ToString();
                    }
                }
            }
        }
        return result;
    }
    private bool AreConsecutive(List<int> numbers)
    {
        if (numbers != null)
        {
            for (int i = 0; i < numbers.Count - 1; i++)
            {
                if (numbers[i + 1] != numbers[i] + 1)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    private string m_find_result_header = null;
    private void UpdateHeaderLabel()
    {
        if (m_client != null)
        {
            if (m_found_verses_displayed)
            {
                if (m_client.FoundVerses != null)
                {
                    int number = m_client.FoundVerses.Count;
                    if (m_text_search_type != TextSearchType.Proximity)
                    {
                        if (m_client.FoundPhrases != null)
                        {
                            number = m_client.FoundPhrases.Count;
                        }
                    }
                    HeaderLabel.ForeColor = GetNumberTypeColor(number);
                    HeaderLabel.Text = m_find_result_header;
                    HeaderLabel.Refresh();
                }
            }
            else
            {
                HeaderLabel.ForeColor = SystemColors.WindowText;
                if (m_client.Selection != null)
                {
                    if (m_client.Selection.Verses.Count > 0)
                    {
                        Verse verse = GetCurrentVerse();
                        if (verse != null)
                        {
                            if (verse.Chapter != null)
                            {
                                string display_text = " " + verse.Chapter.TransliteratedName + "/" + verse.Chapter.EnglishName + " " + verse.Chapter.Number;
                                HeaderLabel.Text = verse.Chapter.Name + " "
                                      + "   ءاية " + verse.NumberInChapter
                                    //+ "   منزل " + verse.Station.Number
                                      + "   جزء " + verse.Part.Number
                                    //+ "   حزب " + verse.Group.Number
                                    //+ "   ربع " + verse.Quarter.Number
                                    //+ "   ركوع " + verse.Bowing.Number
                                      + "   صفحة " + verse.Page.Number
                                      + display_text;
                                HeaderLabel.Refresh();
                            }
                        }
                    }
                }
            }
        }
    }
    private void FindScopeLabel_Click(object sender, EventArgs e)
    {
        NextFindScope();
        UpdateFindScope();
        FindByTextTextBox_TextChanged(null, null);
    }

    private RichTextBoxEx m_active_textbox = null;
    private void SwitchActiveTextBox()
    {
        m_found_verses_displayed = !m_found_verses_displayed;
        if (m_client != null)
        {
            if (m_found_verses_displayed)
            {
                SwitchToSearchResultTextBox();
            }
            else
            {
                SwitchToMainTextBox();
            }

            // this code has been moved out of SelectionChanged and brought to MouseClick and KeyUp
            // to keep all verse translations visible until the user clicks a verse then show one verse translation
            if (m_active_textbox.SelectionLength == 0)
            {
                Verse verse = GetCurrentVerse();
                if (verse != null)
                {
                    DisplayTranslations(verse);
                    DisplayTafseer(verse);
                }
                else
                {
                    TranslationTextBox.WordWrap = false;
                    TranslationTextBox.Text = "";
                    TranslationTextBox.Refresh();

                    m_readonly_mode = false;
                    TranslationTextBox_ToggleReadOnly();
                    EditVerseTranslationLabel.Visible = false;
                }
            }
            else
            {
                // selected text is dealt with by DisplayVersesWordsLetters 
            }
        }
        UpdateHeaderLabel();

        //FIX so user can use keyboard immediately
        m_active_textbox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged); // remove previous +=
        m_active_textbox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged); // add new +=
        m_active_textbox.Focus();
        MainTextBox_SelectionChanged(m_active_textbox, null);
    }

    private void SwitchToMainTextBox()
    {
        if (m_active_textbox != MainTextBox)
        {
            SearchResultTextBox.Visible = false;
            SearchResultTextBox.SendToBack();
            MainTextBox.Visible = true;
            MainTextBox.BringToFront();

            m_active_textbox = MainTextBox;
            m_found_verses_displayed = false;
            UpdateWordWrapLabel(MainTextBox.WordWrap);

            TranslatorComboBox.BringToFront();
            TranslationsApplySettingsLabel.BringToFront();

            GoldenRatioOrderLabel.Visible = true;
            GoldenRatioScopeLabel.Visible = true;
        }

        // on every browse forward/backword
        ColorizeMatchesLabel.Enabled = false;
        ColorizeMatchesLabel.Visible = false;

        this.Text = Application.ProductName + " | " + GetSummarizedFindScope();
    }
    private void SwitchToSearchResultTextBox()
    {
        if (m_active_textbox != SearchResultTextBox)
        {
            MainTextBox.Visible = false;
            MainTextBox.SendToBack();
            SearchResultTextBox.Visible = true;
            SearchResultTextBox.BringToFront();

            m_active_textbox = SearchResultTextBox;
            m_found_verses_displayed = true;
            UpdateWordWrapLabel(SearchResultTextBox.WordWrap);

            TranslatorComboBox.BringToFront();
            TranslationsApplySettingsLabel.BringToFront();

            GoldenRatioOrderLabel.Visible = false;
            GoldenRatioScopeLabel.Visible = false;
        }

        // on every browse forward/backword
        if ((m_language_type == LanguageType.Arabic) && (m_client.FoundPhrases != null))
        {
            ColorizeMatchesLabel.Enabled = true;
            ColorizeMatchesLabel.Visible = true;
        }
        else
        {
            ColorizeMatchesLabel.Enabled = false;
            ColorizeMatchesLabel.Visible = false;
        }

        this.Text = Application.ProductName + " | " + GetSummarizedFindScope();
        UpdateFindMatchCaption();
    }
    private bool m_found_verses_displayed = false;
    private void DisplayFoundVerses(bool add_to_history)
    {
        try
        {
            if ((m_mp3player.Playing) || (m_mp3player.Paused))
            {
                PlayerStopLabel_Click(null, null);
            }

            SwitchToSearchResultTextBox();

            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            BookmarkTextBox.Enabled = false;

            if (m_client != null)
            {
                if (m_client.FoundVerses != null)
                {
                    TranslationTextBox.Text = null;
                    ZoomInLabel.Enabled = (m_text_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin));
                    ZoomOutLabel.Enabled = (m_text_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin));

                    StringBuilder str = new StringBuilder();
                    if (m_client.FoundVerses.Count > 0)
                    {
                        foreach (Verse verse in m_client.FoundVerses)
                        {
                            if (verse != null)
                            {
                                str.Append(verse.ArabicAddress + "\t" + verse.Text + "\n");
                            }
                        }
                        if (str.Length > 1)
                        {
                            str.Remove(str.Length - 1, 1);
                        }
                    }
                    m_current_text = str.ToString();

                    m_is_selection_mode = true;
                    UpdateHeaderLabel();
                    SearchResultTextBox.Text = m_current_text;

                    CalculateCurrentValue();

                    DisplayVersesWordsLetters(m_client.FoundVerses);

                    CalculateLetterStatistics();
                    DisplayLetterStatistics();

                    CalculatePhraseLetterStatistics();
                    DisplayPhraseLetterStatistics();

                    if (m_language_type == LanguageType.Arabic)
                    {
                        // too slow and not needed
                        //ColorizeVerses();

                        if (m_client.FoundPhrases != null)
                        {
                            ColorizeVisibleMatches();
                            BuildTextSelections();
                        }
                    }

                    m_current_found_verse_index = 0;
                    DisplayCurrentPositions();

                    if (m_client.FoundVerses.Count > 0)
                    {
                        DisplayTranslations(m_client.FoundVerses);
                        DisplayTafseer(m_client.FoundVerses);
                    }

                    if (add_to_history)
                    {
                        AddFindHistoryItem();
                    }

                    m_current_found_verse_index = 0;
                    PrepareVerseToPlay();
                    RealignFoundMatchedToStart();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void DisplayFoundVerseRanges(bool add_to_history)
    {
        try
        {
            if ((m_mp3player.Playing) || (m_mp3player.Paused))
            {
                PlayerStopLabel_Click(null, null);
            }

            SwitchToSearchResultTextBox();

            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            BookmarkTextBox.Enabled = false;

            if (m_client != null)
            {
                if (m_client.FoundVerseRanges != null)
                {
                    TranslationTextBox.Text = null;
                    ZoomInLabel.Enabled = (m_text_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin));
                    ZoomOutLabel.Enabled = (m_text_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin));

                    StringBuilder str = new StringBuilder();
                    if (m_client.FoundVerseRanges.Count > 0)
                    {
                        foreach (List<Verse> range in m_client.FoundVerseRanges)
                        {
                            foreach (Verse verse in range)
                            {
                                if (verse != null)
                                {
                                    str.Append(verse.ArabicAddress + "\t" + verse.Text + "\n");
                                }
                            }
                        }
                        if (str.Length > 1)
                        {
                            str.Remove(str.Length - 1, 1);
                        }
                    }
                    m_current_text = str.ToString();

                    m_is_selection_mode = true;
                    UpdateHeaderLabel();
                    SearchResultTextBox.Text = m_current_text;

                    CalculateCurrentValue();

                    DisplayVersesWordsLetters(m_client.FoundVerses);

                    CalculateLetterStatistics();
                    DisplayLetterStatistics();

                    CalculatePhraseLetterStatistics();
                    DisplayPhraseLetterStatistics();

                    if (m_language_type == LanguageType.Arabic)
                    {
                        ColorizeVerseRanges();
                    }

                    m_current_found_verse_index = 0;
                    DisplayCurrentPositions();

                    if (m_client.FoundVerseRanges.Count > 0)
                    {
                        List<Verse> verses = new List<Verse>();
                        foreach (List<Verse> range in m_client.FoundVerseRanges)
                        {
                            foreach (Verse verse in range)
                            {
                                verses.AddRange(range);
                            }
                        }

                        DisplayTranslations(verses);
                        DisplayTafseer(verses);
                    }

                    if (add_to_history)
                    {
                        AddFindHistoryItem();
                    }

                    m_current_found_verse_index = 0;
                    PrepareVerseToPlay();
                    RealignFoundMatchedToStart();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void DisplayFoundChapters(bool add_to_history)
    {
        try
        {
            if ((m_mp3player.Playing) || (m_mp3player.Paused))
            {
                PlayerStopLabel_Click(null, null);
            }

            SwitchToSearchResultTextBox();

            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            BookmarkTextBox.Enabled = false;

            if (m_client != null)
            {
                if (m_client.FoundChapters != null)
                {
                    TranslationTextBox.Text = null;
                    ZoomInLabel.Enabled = (m_text_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin));
                    ZoomOutLabel.Enabled = (m_text_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin));

                    ChaptersListBox.SelectedIndexChanged -= new EventHandler(ChaptersListBox_SelectedIndexChanged);
                    if (m_client.FoundChapters.Count > 0)
                    {
                        ChaptersListBox.SelectedIndices.Clear();
                        foreach (Chapter chapter in m_client.FoundChapters)
                        {
                            if (((chapter.Number - 1) >= 0) && ((chapter.Number - 1) < ChaptersListBox.Items.Count))
                            {
                                ChaptersListBox.SelectedIndices.Add(chapter.Number - 1);
                            }
                        }
                    }
                    else
                    {
                        ChaptersListBox.SelectedIndices.Clear();
                    }
                    DisplayChapterRevelationInfo();
                    ChaptersListBox.SelectedIndexChanged += new EventHandler(ChaptersListBox_SelectedIndexChanged);

                    StringBuilder str = new StringBuilder();
                    if (m_client.FoundChapters.Count > 0)
                    {
                        foreach (Chapter chapter in m_client.FoundChapters)
                        {
                            foreach (Verse verse in chapter.Verses)
                            {
                                if (verse != null)
                                {
                                    str.Append(verse.ArabicAddress + "\t" + verse.Text + "\n");
                                }
                            }
                        }
                        if (str.Length > 1)
                        {
                            str.Remove(str.Length - 1, 1);
                        }
                    }
                    m_current_text = str.ToString();

                    m_is_selection_mode = true;
                    UpdateHeaderLabel();
                    SearchResultTextBox.Text = m_current_text;

                    CalculateCurrentValue();

                    DisplayVersesWordsLetters(m_client.FoundVerses);

                    CalculateLetterStatistics();
                    DisplayLetterStatistics();

                    CalculatePhraseLetterStatistics();
                    DisplayPhraseLetterStatistics();

                    if (m_language_type == LanguageType.Arabic)
                    {
                        // too slow and not needed
                        //ColorizeChapters();
                    }

                    m_current_found_verse_index = 0;
                    DisplayCurrentPositions();

                    if (m_client.FoundChapters.Count > 0)
                    {
                        List<Verse> verses = new List<Verse>();
                        foreach (Chapter chapter in m_client.FoundChapters)
                        {
                            verses.AddRange(chapter.Verses);
                        }

                        DisplayTranslations(verses);
                        DisplayTafseer(verses);
                    }

                    if (add_to_history)
                    {
                        AddFindHistoryItem();
                    }

                    m_current_found_verse_index = 0;
                    PrepareVerseToPlay();
                    RealignFoundMatchedToStart();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void DisplayFoundChapterRanges(bool add_to_history)
    {
        try
        {
            if ((m_mp3player.Playing) || (m_mp3player.Paused))
            {
                PlayerStopLabel_Click(null, null);
            }

            SwitchToSearchResultTextBox();

            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            BookmarkTextBox.Enabled = false;

            if (m_client != null)
            {
                if (m_client.FoundChapterRanges != null)
                {
                    TranslationTextBox.Text = null;
                    ZoomInLabel.Enabled = (m_text_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin));
                    ZoomOutLabel.Enabled = (m_text_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin));

                    ChaptersListBox.SelectedIndexChanged -= new EventHandler(ChaptersListBox_SelectedIndexChanged);
                    if (m_client.FoundChapterRanges.Count > 0)
                    {
                        ChaptersListBox.SelectedIndices.Clear();
                        foreach (List<Chapter> range in m_client.FoundChapterRanges)
                        {
                            foreach (Chapter chapter in range)
                            {
                                if (((chapter.Number - 1) >= 0) && ((chapter.Number - 1) < ChaptersListBox.Items.Count))
                                {
                                    ChaptersListBox.SelectedIndices.Add(chapter.Number - 1);
                                }
                            }
                        }
                    }
                    else
                    {
                        ChaptersListBox.SelectedIndices.Clear();
                    }
                    DisplayChapterRevelationInfo();
                    ChaptersListBox.SelectedIndexChanged += new EventHandler(ChaptersListBox_SelectedIndexChanged);

                    m_current_text = null;
                    if (m_client.FoundChapterRanges.Count > 0)
                    {
                        StringBuilder str = new StringBuilder();
                        foreach (List<Chapter> range in m_client.FoundChapterRanges)
                        {
                            foreach (Chapter chapter in range)
                            {
                                foreach (Verse verse in chapter.Verses)
                                {
                                    if (verse != null)
                                    {
                                        str.Append(verse.ArabicAddress + "\t" + verse.Text + "\n");
                                    }
                                }
                            }
                        }

                        m_current_text = str.ToString();
                    }

                    m_is_selection_mode = true;
                    UpdateHeaderLabel();
                    SearchResultTextBox.Text = m_current_text;

                    CalculateCurrentValue();

                    DisplayVersesWordsLetters(m_client.FoundVerses);

                    CalculateLetterStatistics();
                    DisplayLetterStatistics();

                    CalculatePhraseLetterStatistics();
                    DisplayPhraseLetterStatistics();

                    if (m_language_type == LanguageType.Arabic)
                    {
                        ColorizeChapterRanges();
                    }

                    m_current_found_verse_index = 0;
                    DisplayCurrentPositions();

                    if (m_client.FoundChapterRanges.Count > 0)
                    {
                        List<Verse> verses = new List<Verse>();
                        foreach (List<Chapter> range in m_client.FoundChapterRanges)
                        {
                            foreach (Chapter chapter in range)
                            {
                                verses.AddRange(chapter.Verses);
                            }
                        }

                        DisplayTranslations(verses);
                        DisplayTafseer(verses);
                    }

                    if (add_to_history)
                    {
                        AddFindHistoryItem();
                    }

                    m_current_found_verse_index = 0;
                    PrepareVerseToPlay();
                    RealignFoundMatchedToStart();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void ColorizeMatchesLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            ColorizeMatches();
            BuildTextSelections();

            RealignFoundMatchedToStart();

            ColorizeMatchesLabel.Enabled = false;
        }
        finally
        {
            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
            this.Cursor = Cursors.Default;
        }
    }
    private void ColorizeVisibleMatches()
    {
        try
        {
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.FoundPhrases != null)
                {
                    //int min = 0;
                    //int max = SearchResultTextBox.VisibleLines;
                    //for (int i = min; i < max; i++)
                    int max = Math.Min(m_client.FoundPhrases.Count, AUTO_COLORIZED_PHRASES);
                    for (int i = 0; i < max; i++)
                    {
                        Phrase phrase = m_client.FoundPhrases[i];
                        if (phrase != null)
                        {
                            int start = GetPhrasePositionInRichTextBox(phrase);
                            if ((start >= 0) && (start < SearchResultTextBox.Text.Length))
                            {
                                int length = phrase.Text.Length;
                                SearchResultTextBox.Select(start, length);
                                SearchResultTextBox.SelectionColor = Color.Red;
                            }
                        }
                    }

                    ColorizeMatchesLabel.Enabled = (m_language_type == LanguageType.Arabic) && (m_client.FoundPhrases.Count > AUTO_COLORIZED_PHRASES);

                    UpdateFindMatchCaption();
                }
            }
        }
        finally
        {
            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void ColorizeMatches()
    {
        try
        {
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.FoundPhrases != null)
                {
                    foreach (Phrase phrase in m_client.FoundPhrases)
                    {
                        if (phrase != null)
                        {
                            int start = GetPhrasePositionInRichTextBox(phrase);
                            if ((start >= 0) && (start < SearchResultTextBox.Text.Length))
                            {
                                int length = phrase.Text.Length;
                                SearchResultTextBox.Select(start, length);
                                SearchResultTextBox.SelectionColor = Color.Red;
                            }
                        }
                    }

                    UpdateFindMatchCaption();
                }
            }
        }
        finally
        {
            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void ColorizeVerses()
    {
        try
        {
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.FoundVerses != null)
                {
                    if (m_client.FoundVerses.Count > 0)
                    {
                        bool colorize = true; // colorize verses alternatively

                        int line_index = 0;
                        foreach (Verse verse in m_client.FoundVerses)
                        {
                            colorize = !colorize; // alternate colorization of ranges

                            int start = SearchResultTextBox.GetLinePosition(line_index);
                            int length = SearchResultTextBox.Lines[line_index].Length + 1; // "\n"

                            SearchResultTextBox.Select(start, length);
                            SearchResultTextBox.SelectionColor = colorize ? Color.Blue : Color.Navy;

                            line_index++;
                        }
                    }
                }
            }
        }
        finally
        {
            //FIX to reset SelectionColor
            SearchResultTextBox.Select(0, 1);
            SearchResultTextBox.SelectionColor = Color.Navy;
            SearchResultTextBox.Select(0, 0);
            SearchResultTextBox.SelectionColor = Color.Navy;

            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void ColorizeVerseRanges()
    {
        try
        {
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.FoundVerseRanges != null)
                {
                    if (m_client.FoundVerseRanges.Count > 0)
                    {
                        bool colorize = true; // colorize ranges alternatively

                        int line_index = 0;
                        foreach (List<Verse> range in m_client.FoundVerseRanges)
                        {
                            colorize = !colorize; // alternate colorization of ranges

                            int start = SearchResultTextBox.GetLinePosition(line_index);
                            int length = 0;
                            foreach (Verse verse in range)
                            {
                                length += SearchResultTextBox.Lines[line_index].Length + 1; // "\n"
                                line_index++;
                            }
                            SearchResultTextBox.Select(start, length);
                            SearchResultTextBox.SelectionColor = colorize ? Color.Blue : Color.Navy;
                        }
                    }
                }
            }
        }
        finally
        {
            //FIX to reset SelectionColor
            SearchResultTextBox.Select(0, 1);
            SearchResultTextBox.SelectionColor = Color.Navy;
            SearchResultTextBox.Select(0, 0);
            SearchResultTextBox.SelectionColor = Color.Navy;

            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void ColorizeChapters()
    {
        try
        {
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.FoundChapters != null)
                {
                    if (m_client.FoundChapters.Count > 0)
                    {
                        bool colorize = true; // colorize chapters alternatively

                        int line_index = 0;
                        foreach (Chapter chapter in m_client.FoundChapters)
                        {
                            colorize = !colorize; // alternate colorization of chapters

                            int start = SearchResultTextBox.GetLinePosition(line_index);
                            int length = 0;
                            foreach (Verse verse in chapter.Verses)
                            {
                                length += SearchResultTextBox.Lines[line_index].Length + 1; // "\n"
                                line_index++;
                            }
                            SearchResultTextBox.Select(start, length);
                            SearchResultTextBox.SelectionColor = colorize ? Color.Blue : Color.Navy;
                        }
                    }
                }
            }
        }
        finally
        {
            //FIX to reset SelectionColor
            SearchResultTextBox.Select(0, 1);
            SearchResultTextBox.SelectionColor = Color.Navy;
            SearchResultTextBox.Select(0, 0);
            SearchResultTextBox.SelectionColor = Color.Navy;

            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void ColorizeChapterRanges()
    {
        try
        {
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            if (m_client != null)
            {
                if (m_client.FoundChapterRanges != null)
                {
                    if (m_client.FoundChapterRanges.Count > 0)
                    {
                        bool colorize = true; // colorize ranges alternatively

                        int line_index = 0;
                        foreach (List<Chapter> range in m_client.FoundChapterRanges)
                        {
                            colorize = !colorize; // alternate colorization of ranges

                            int start = SearchResultTextBox.GetLinePosition(line_index);
                            int length = 0;
                            foreach (Chapter chapter in range)
                            {
                                foreach (Verse verse in chapter.Verses)
                                {
                                    length += SearchResultTextBox.Lines[line_index].Length + 1; // "\n"
                                    line_index++;
                                }
                            }
                            SearchResultTextBox.Select(start, length);
                            SearchResultTextBox.SelectionColor = colorize ? Color.Blue : Color.Navy;
                        }
                    }
                }
            }
        }
        finally
        {
            //FIX to reset SelectionColor
            SearchResultTextBox.Select(0, 1);
            SearchResultTextBox.SelectionColor = Color.Navy;
            SearchResultTextBox.Select(0, 0);
            SearchResultTextBox.SelectionColor = Color.Navy;

            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    private void BuildTextSelections()
    {
        try
        {
            if (m_client != null)
            {
                if (m_client.FoundPhrases != null)
                {
                    foreach (Phrase phrase in m_client.FoundPhrases)
                    {
                        if (phrase != null)
                        {
                            int start = GetPhrasePositionInRichTextBox(phrase);
                            if ((start >= 0) && (start < SearchResultTextBox.Text.Length))
                            {
                                int length = phrase.Text.Length;
                                BuildFindMatch(start, length);
                            }
                        }
                    }
                }
            }
            UpdateFindMatchCaption();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
    }
    private int GetPhrasePositionInRichTextBox(Phrase phrase)
    {
        if (phrase == null) return 0;

        if (m_client != null)
        {
            if (m_client.FoundVerses != null)
            {
                int start = 0;
                foreach (Verse verse in m_client.FoundVerses)
                {
                    if (verse != null)
                    {
                        if (phrase.Verse.Number == verse.Number)
                        {
                            if (m_language_type == LanguageType.Arabic)
                            {
                                start += verse.Address.Length + 1 + phrase.Position;
                            }
                            else if (m_language_type == LanguageType.Translation)
                            {
                                start += verse.Address.Length + 1 + phrase.Position;
                            }
                            return start;
                        }

                        // skip prior verses
                        if (m_language_type == LanguageType.Arabic)
                        {
                            start += verse.Address.Length + 1 + verse.Text.Length + 1; // 1 for \n
                        }
                        else if (m_language_type == LanguageType.Translation)
                        {
                            start += verse.Address.Length + 1 + verse.Text.Length + 1; // 1 for \n
                        }
                    }
                }
            }
        }
        return -1;
    }
    private void RealignFoundMatchedToStart()
    {
        try
        {
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.SelectionChanged -= new EventHandler(MainTextBox_SelectionChanged);
            SearchResultTextBox.BeginUpdate();

            if (m_client != null)
            {
                SearchResultTextBox.ClearHighlight();

                if (m_found_verses_displayed)
                {
                    List<Verse> displayed_verses = new List<Verse>();
                    if (m_client.FoundVerses != null)
                    {
                        displayed_verses.AddRange(m_client.FoundVerses);
                    }
                    else if (m_client.FoundChapters != null)
                    {
                        foreach (Chapter chapter in m_client.FoundChapters)
                        {
                            displayed_verses.AddRange(chapter.Verses);
                        }
                    }
                    else if (m_client.FoundVerseRanges != null)
                    {
                        foreach (List<Verse> range in m_client.FoundVerseRanges)
                        {
                            displayed_verses.AddRange(range);
                        }
                    }
                    else if (m_client.FoundChapterRanges != null)
                    {
                        foreach (List<Chapter> range in m_client.FoundChapterRanges)
                        {
                            foreach (Chapter chapter in range)
                            {
                                displayed_verses.AddRange(chapter.Verses);
                            }
                        }
                    }

                    int start = 0;
                    // scroll to beginning to show complete verse address because in Arabic, pos=0 is after the first number :(
                    if (m_client.FoundVerses.Count > 0)
                    {
                        Verse verse = m_client.FoundVerses[0];
                        if (verse != null)
                        {
                            if (verse.Chapter != null)
                            {
                                if (m_language_type == LanguageType.Arabic)
                                {
                                    if (verse.Chapter != null)
                                    {
                                        start = verse.Chapter.Number.ToString().Length;
                                    }
                                }
                                else // in non-Arabic (actually Left-to-Right languages) text
                                {
                                    if (verse.Chapter != null)
                                    {
                                        start = 0;
                                    }
                                }
                            }
                        }
                    }

                    // re-align to text start
                    if ((start >= 0) && (start < SearchResultTextBox.Text.Length))
                    {
                        SearchResultTextBox.ScrollToCaret();    // must be called first
                        SearchResultTextBox.Select(start, 0);   // must be called second
                    }

                    // prepare for Backspace
                    BrowseHistoryBackwardButton.Focus();
                }
            }
        }
        finally
        {
            SearchResultTextBox.EndUpdate();
            SearchResultTextBox.SelectionChanged += new EventHandler(MainTextBox_SelectionChanged);
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 24. Browse Navigator
    ///////////////////////////////////////////////////////////////////////////////
    private void AddFindHistoryItem()
    {
        if (m_client != null)
        {
            if (m_client.FoundVerses != null)
            {
                if (m_client.FoundVerses.Count > 0)
                {
                    FindHistoryItem item = new FindHistoryItem();
                    item.FindType = m_find_type;
                    item.NumberSearchType = m_numbers_result_type;
                    item.Text = (m_find_type == FindType.Numbers) ? null : FindByTextTextBox.Text;
                    item.LanguageType = m_language_type;

                    if (TranslatorComboBox.SelectedItem != null)
                    {
                        item.Translation = TranslatorComboBox.SelectedItem.ToString();
                    }

                    item.Verses = new List<Verse>(m_client.FoundVerses);
                    if (m_client.FoundPhrases == null)
                    {
                        item.Phrases = null;
                    }
                    else
                    {
                        item.Phrases = new List<Phrase>(m_client.FoundPhrases);
                    }
                    item.Header = m_find_result_header;
                    m_client.AddHistoryItem(item);
                    UpdateBrowseHistoryButtons();
                }
            }
        }
    }
    private void AddSelectionHistoryItem()
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if (m_client.Selection != null)
                {

                    SelectionHistoryItem item = new SelectionHistoryItem(m_client.Selection.Book, m_client.Selection.Scope, m_client.Selection.Indexes);
                    if (item != null)
                    {
                        m_client.AddHistoryItem(item);
                        UpdateBrowseHistoryButtons();
                    }
                }
            }
        }
    }
    private void BrowseHistoryBackwardButton_Click(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            object item = m_client.GotoPreviousHistoryItem();
            if (item != null)
            {
                if (item is FindHistoryItem)
                {
                    m_find_matches.Clear(); // to reset Matched count
                }
                else if (item is SelectionHistoryItem)
                {
                    CopySelectionToChaptersListBoxIndexes();
                }

                DisplayBrowseHistoryItem(item);
                UpdateBrowseHistoryButtons();
            }
        }
        this.AcceptButton = null;
    }
    private void BrowseHistoryForwardButton_Click(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            object item = m_client.GotoNextHistoryItem();
            if (item != null)
            {
                if (item is FindHistoryItem)
                {
                    m_find_matches.Clear(); // to reset Matched count
                }
                else if (item is SelectionHistoryItem)
                {
                    CopySelectionToChaptersListBoxIndexes();
                }

                DisplayBrowseHistoryItem(item);
                UpdateBrowseHistoryButtons();
            }
        }
        this.AcceptButton = null;
    }
    private void BrowseHistoryDeleteLabel_Click(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            m_client.DeleteCurrentHistoryItem();
            if (m_client.HistoryItems.Count == 0) // no item to display
            {
                DisplaySelection(false);
            }
            else // there is an item to display
            {
                object item = m_client.CurrentHistoryItem;
                if (item != null)
                {
                    CopySelectionToChaptersListBoxIndexes();

                    DisplayBrowseHistoryItem(item);
                }
            }

            UpdateBrowseHistoryButtons();
        }
    }
    private void BrowseHistoryClearLabel_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show(
            "Delete all browsing history?",
            Application.ProductName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes)
        {
            if (m_client != null)
            {
                m_client.ClearHistoryItems();
                DisplaySelection(false);

                FindByTextTextBox.Text = null;
                AutoCompleteHeaderLabel.Visible = false;
                AutoCompleteListBox.Visible = false;

                CopySelectionToChaptersListBoxIndexes();

                UpdateBrowseHistoryButtons();
            }
        }
    }
    private void DisplayBrowseHistoryItem(object item)
    {
        if (m_client != null)
        {
            if (item != null)
            {
                FindHistoryItem find_history_item = item as FindHistoryItem;
                if (find_history_item != null)
                {
                    FindByTextTextBox.Text = find_history_item.Text;
                    m_find_result_header = find_history_item.Header;
                    m_language_type = find_history_item.LanguageType;
                    TranslatorComboBox.SelectedItem = find_history_item.Translation;
                    if (find_history_item.Phrases != null)
                    {
                        m_client.FoundPhrases = new List<Phrase>(find_history_item.Phrases);
                    }
                    else
                    {
                        m_client.FoundPhrases = null;
                    }
                    m_client.FoundVerses = new List<Verse>(find_history_item.Verses);

                    if (!String.IsNullOrEmpty(find_history_item.Text))
                    {
                        FindByTextTextBox.SelectionStart = find_history_item.Text.Length;
                        DisplayFoundVerses(false);
                    }
                    else
                    {
                        // no NumberQuery is saved so we cannot colorize ranges
                        // so just display all verses and let user re-run search if they need colorization
                        //switch (find_history_item.NumberSearchType)
                        //{
                        //    case NumberSearchType.Words:
                        //    case NumberSearchType.WordRanges:
                        //    case NumberSearchType.Verses:
                        //        DisplayFoundVerses(false);
                        //        break;
                        //    case NumberSearchType.VerseRanges:
                        //        DisplayFoundVerseRanges(false);
                        //        break;
                        //    case NumberSearchType.Chapters:
                        //        DisplayFoundChapters(false);
                        //        break;
                        //    case NumberSearchType.ChapterRanges:
                        //        DisplayFoundChapterRanges(false);
                        //        break;
                        //}
                        DisplayFoundVerses(false);
                    }
                }
                else
                {
                    SelectionHistoryItem selection_history_item = item as SelectionHistoryItem;
                    if (selection_history_item != null)
                    {
                        m_client.Selection = new Selection(selection_history_item.Book, selection_history_item.Scope, selection_history_item.Indexes);
                        DisplaySelection(false);
                    }
                }
            }
        }
    }
    private void UpdateBrowseHistoryButtons()
    {
        if (m_client.HistoryItems != null)
        {
            BrowseHistoryBackwardButton.Enabled = (m_client.HistoryItems.Count > 0) && (m_client.HistoryItemIndex > 0);
            BrowseHistoryForwardButton.Enabled = (m_client.HistoryItems.Count >= 0) && (m_client.HistoryItemIndex < m_client.HistoryItems.Count - 1);
            BrowseHistoryDeleteLabel.Enabled = (m_client.HistoryItems.Count > 0);
            BrowseHistoryClearLabel.Enabled = (m_client.HistoryItems.Count > 0);
            BrowseHistoryClearLabel.BackColor = (m_client.HistoryItems.Count > 0) ? Color.LightCoral : SystemColors.ControlLight;
            BrowseHistoryCounterLabel.Text = (m_client.HistoryItemIndex + 1).ToString() + " / " + m_client.HistoryItems.Count.ToString();

            if (m_client.HistoryItems.Count == 0)
            {
                SearchResultTextBox.Text = "";
                m_find_result_header = "";
                UpdateHeaderLabel();
            }
        }
    }
    private void BrowseHistoryCounterLabel_Click(object sender, EventArgs e)
    {
        if (m_client.HistoryItems.Count > 0)
        {
            DisplayBrowseHistoryItem(m_client.CurrentHistoryItem);
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 25. Numerology Value System
    ///////////////////////////////////////////////////////////////////////////////
    private void NumerologySystemScopeRadioButton_CheckedChanged(object sender, EventArgs e)
    {
        if ((sender as RadioButton).Checked) // to ignore the other control's uncheck firing, so no double event handling
        {
            UpdateNumerologySystemScope();
            RebuildCurrentNumerologySystem();
        }

        NumerologySystemComboBox.Focus();
    }
    private void UpdateNumerologySystemScope()
    {
        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                if (ScopeBookRadioButton.Checked)
                {
                    m_client.NumerologySystem.Scope = NumerologySystemScope.Book;
                }
                else if (ScopeSelectionRadioButton.Checked)
                {
                    m_client.NumerologySystem.Scope = NumerologySystemScope.Selection;
                }
                else if (ScopeHighlightedTextRadioButton.Checked)
                {
                    m_client.NumerologySystem.Scope = NumerologySystemScope.HighlightedText;
                }
            }
        }
    }
    private void AddToControlCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        AddToControlCheckBox_EnabledChanged(sender, e);
    }
    private void AddToControlCheckBox_EnabledChanged(object sender, EventArgs e)
    {
        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                bool check_value = (sender as CheckBox).Enabled && (sender as CheckBox).Checked;

                //Letter value modifiers
                if (sender == AddToLetterLNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToLetterLNumber = check_value;
                }
                else if (sender == AddToLetterWNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToLetterWNumber = check_value;
                }
                else if (sender == AddToLetterVNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToLetterVNumber = check_value;
                }
                else if (sender == AddToLetterCNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToLetterCNumber = check_value;
                }
                else if (sender == AddToLetterLDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToLetterLDistance = check_value;
                }
                else if (sender == AddToLetterWDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToLetterWDistance = check_value;
                }
                else if (sender == AddToLetterVDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToLetterVDistance = check_value;
                }
                else if (sender == AddToLetterCDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToLetterCDistance = check_value;
                }
                // Word value modifiers
                else if (sender == AddToWordWNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToWordWNumber = check_value;
                }
                else if (sender == AddToWordVNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToWordVNumber = check_value;
                }
                else if (sender == AddToWordCNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToWordCNumber = check_value;
                }
                else if (sender == AddToWordWDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToWordWDistance = check_value;
                }
                else if (sender == AddToWordVDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToWordVDistance = check_value;
                }
                else if (sender == AddToWordCDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToWordCDistance = check_value;
                }
                // Verse value modifiers
                else if (sender == AddToVerseVNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToVerseVNumber = check_value;
                }
                else if (sender == AddToVerseCNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToVerseCNumber = check_value;
                }
                else if (sender == AddToVerseVDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToVerseVDistance = check_value;
                }
                else if (sender == AddToVerseCDistanceCheckBox)
                {
                    m_client.NumerologySystem.AddToVerseCDistance = check_value;
                }
                // Chapter value modifier
                else if (sender == AddToChapterCNumberCheckBox)
                {
                    m_client.NumerologySystem.AddToChapterCNumber = check_value;
                }

                CalculateCurrentValue();
            }
        }
    }
    private string GetDynamicText()
    {
        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                switch (m_client.NumerologySystem.Scope)
                {
                    case NumerologySystemScope.Book:
                        {
                            return m_client.Book.Text;
                        }
                    case NumerologySystemScope.Selection:
                        {
                            return m_client.Selection.Text;
                        }
                    case NumerologySystemScope.HighlightedText:
                        {
                            CalculateCurrentText();
                            return m_current_text;
                        }
                    default:
                        return null;
                }
            }
        }
        return null;
    }
    /// <summary>
    /// Build numerology systems dynamically as user changes selection or highlighted_text 
    /// </summary>
    /// <param name="letters_scope"></param>
    private void RebuildCurrentNumerologySystem()
    {
        if (m_client != null)
        {
            NumerologySystemScope letters_scope = m_client.NumerologySystem.Scope;
            switch (letters_scope)
            {
                case NumerologySystemScope.Book:
                    {
                        if (m_client.Book != null)
                        {
                            m_client.BuildNumerologySystem(m_client.Book.Text);
                        }
                    }
                    break;
                case NumerologySystemScope.Selection:
                    {
                        if (m_client.Selection != null)
                        {
                            m_client.BuildNumerologySystem(m_client.Selection.Text);
                        }
                    }
                    break;
                case NumerologySystemScope.HighlightedText:
                    {
                        CalculateCurrentText();
                        m_client.BuildNumerologySystem(m_current_text);
                    }
                    break;
                default:
                    break;
            }

            // BuildNumerologySystem at Server has no info about AddTo.. settings etc
            // The only thing has been rebuilt is the letters:values table
            // so we MUST re-apply the AddTo... and others settings manually
            UpdateCurrentNumerologySystem();
            // update the letters:values table
            UpdateObjectListView();

            CalculateCurrentValue();

            CalculateLetterStatistics();
            DisplayLetterStatistics();

            CalculatePhraseLetterStatistics();
            DisplayPhraseLetterStatistics();
        }
    }
    /// <summary>
    /// Copy values from GUI interface to NumerologySystem object
    /// </summary>
    private void UpdateCurrentNumerologySystem()
    {
        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                if (ScopeBookRadioButton.Checked)
                {
                    m_client.NumerologySystem.Scope = NumerologySystemScope.Book;
                }
                else if (ScopeSelectionRadioButton.Checked)
                {
                    m_client.NumerologySystem.Scope = NumerologySystemScope.Selection;
                }
                else if (ScopeHighlightedTextRadioButton.Checked)
                {
                    m_client.NumerologySystem.Scope = NumerologySystemScope.HighlightedText;
                }
                else
                {
                    m_client.NumerologySystem.Scope = NumerologySystemScope.Book;
                }

                // copy from gui controls to client
                m_client.NumerologySystem.AddToLetterLNumber = AddToLetterLNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToLetterWNumber = AddToLetterWNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToLetterVNumber = AddToLetterVNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToLetterCNumber = AddToLetterCNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToLetterLDistance = AddToLetterLDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToLetterWDistance = AddToLetterWDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToLetterVDistance = AddToLetterVDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToLetterCDistance = AddToLetterCDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToWordWNumber = AddToWordWNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToWordVNumber = AddToWordVNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToWordCNumber = AddToWordCNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToWordWDistance = AddToWordWDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToWordVDistance = AddToWordVDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToWordCDistance = AddToWordCDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToVerseVNumber = AddToVerseVNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToVerseCNumber = AddToVerseCNumberCheckBox.Checked;
                m_client.NumerologySystem.AddToVerseVDistance = AddToVerseVDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToVerseCDistance = AddToVerseCDistanceCheckBox.Checked;
                m_client.NumerologySystem.AddToChapterCNumber = AddToChapterCNumberCheckBox.Checked;
            }
        }
    }
    /// <summary>
    /// Copy values from NumerologySystem object to GUI interface
    /// </summary>
    private void RefreshCurrentNumerologySystem()
    {
        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                try
                {
                    NumerologySystemComboBox.SelectedIndexChanged -= new EventHandler(NumerologySystemComboBox_SelectedIndexChanged);

                    ScopeBookRadioButton.CheckedChanged -= new EventHandler(NumerologySystemScopeRadioButton_CheckedChanged);
                    ScopeSelectionRadioButton.CheckedChanged -= new EventHandler(NumerologySystemScopeRadioButton_CheckedChanged);
                    ScopeHighlightedTextRadioButton.CheckedChanged -= new EventHandler(NumerologySystemScopeRadioButton_CheckedChanged);

                    AddToLetterLNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterWNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterVNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterCNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterLDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterWDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterVDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterCDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordWNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordVNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordCNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordWDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordVDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordCDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToVerseVNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToVerseCNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToVerseVDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToVerseCDistanceCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToChapterCNumberCheckBox.CheckedChanged -= new EventHandler(AddToControlCheckBox_CheckedChanged);

                    NumerologySystemComboBox.SelectedItem = m_client.NumerologySystem.Name;
                    if (m_client.NumerologySystem != null)
                    {
                        UpdateKeyboard(m_client.NumerologySystem.TextMode);
                        GoldenRatioOrderLabel.Visible = !m_found_verses_displayed;
                        GoldenRatioScopeLabel.Visible = !m_found_verses_displayed;
                    }

                    switch (m_client.NumerologySystem.Scope)
                    {
                        case NumerologySystemScope.Book:
                            {
                                ScopeBookRadioButton.Checked = true;
                            }
                            break;
                        case NumerologySystemScope.Selection:
                            {
                                ScopeSelectionRadioButton.Checked = true;
                            }
                            break;
                        case NumerologySystemScope.HighlightedText:
                            {
                                ScopeHighlightedTextRadioButton.Checked = true;
                            }
                            break;
                        default:
                            break;
                    }

                    // copy from client to gui controls
                    AddToLetterLNumberCheckBox.Checked = m_client.NumerologySystem.AddToLetterLNumber;
                    AddToLetterWNumberCheckBox.Checked = m_client.NumerologySystem.AddToLetterWNumber;
                    AddToLetterVNumberCheckBox.Checked = m_client.NumerologySystem.AddToLetterVNumber;
                    AddToLetterCNumberCheckBox.Checked = m_client.NumerologySystem.AddToLetterCNumber;
                    AddToLetterLDistanceCheckBox.Checked = m_client.NumerologySystem.AddToLetterLDistance;
                    AddToLetterWDistanceCheckBox.Checked = m_client.NumerologySystem.AddToLetterWDistance;
                    AddToLetterVDistanceCheckBox.Checked = m_client.NumerologySystem.AddToLetterVDistance;
                    AddToLetterCDistanceCheckBox.Checked = m_client.NumerologySystem.AddToLetterCDistance;
                    AddToWordWNumberCheckBox.Checked = m_client.NumerologySystem.AddToWordWNumber;
                    AddToWordVNumberCheckBox.Checked = m_client.NumerologySystem.AddToWordVNumber;
                    AddToWordCNumberCheckBox.Checked = m_client.NumerologySystem.AddToWordCNumber;
                    AddToWordWDistanceCheckBox.Checked = m_client.NumerologySystem.AddToWordWDistance;
                    AddToWordVDistanceCheckBox.Checked = m_client.NumerologySystem.AddToWordVDistance;
                    AddToWordCDistanceCheckBox.Checked = m_client.NumerologySystem.AddToWordCDistance;
                    AddToVerseVNumberCheckBox.Checked = m_client.NumerologySystem.AddToVerseVNumber;
                    AddToVerseCNumberCheckBox.Checked = m_client.NumerologySystem.AddToVerseCNumber;
                    AddToVerseVDistanceCheckBox.Checked = m_client.NumerologySystem.AddToVerseVDistance;
                    AddToVerseCDistanceCheckBox.Checked = m_client.NumerologySystem.AddToVerseCDistance;
                    AddToChapterCNumberCheckBox.Checked = m_client.NumerologySystem.AddToChapterCNumber;
                }
                finally
                {
                    NumerologySystemComboBox.SelectedIndexChanged += new EventHandler(NumerologySystemComboBox_SelectedIndexChanged);

                    ScopeBookRadioButton.CheckedChanged += new EventHandler(NumerologySystemScopeRadioButton_CheckedChanged);
                    ScopeSelectionRadioButton.CheckedChanged += new EventHandler(NumerologySystemScopeRadioButton_CheckedChanged);
                    ScopeHighlightedTextRadioButton.CheckedChanged += new EventHandler(NumerologySystemScopeRadioButton_CheckedChanged);

                    AddToLetterLNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterWNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterVNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterCNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterLDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterWDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterVDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToLetterCDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordWNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordVNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordCNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordWDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordVDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToWordCDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToVerseVNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToVerseCNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToVerseVDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToVerseCDistanceCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                    AddToChapterCNumberCheckBox.CheckedChanged += new EventHandler(AddToControlCheckBox_CheckedChanged);
                }
            }
        }
    }
    private void ReloadCurrentNumerologySystem(string dynamic_text)
    {
        if (m_client != null)
        {
            try
            {
                m_client.ReloadNumerologySystem(dynamic_text);
                RefreshCurrentNumerologySystem();
                UpdateObjectListView();

                CalculateCurrentValue();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName);
            }
        }
    }
    private void ReloadNumerologySystem(string numerology_system_name, string dynamic_text)
    {
        if (m_client != null)
        {
            try
            {
                // backup book before potentially loading a different one
                Book backup_book = m_client.Book;

                // reload new numerology system
                // and if text_mode has changed
                // and if this a Research edition
                // then reload and simplify new text
                m_client.ReloadNumerologySystem(numerology_system_name, dynamic_text);
                RefreshCurrentNumerologySystem();
                UpdateObjectListView();
                // display newly reloaded and re-simplified text
                if (backup_book != m_client.Book)
                {
                    if (m_found_verses_displayed)
                    {
                        DisplayFoundVerses(false);
                    }
                    else
                    {
                        DisplaySelection(false);
                    }
                }
                else // update only letter statistics
                {
                    CalculateCurrentValue();

                    CalculateLetterStatistics();
                    DisplayLetterStatistics();

                    CalculatePhraseLetterStatistics();
                    DisplayPhraseLetterStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName);
            }
        }
    }
    private void ReloadDefaultNumerologySystem(string dynamic_text)
    {
        try
        {
            if (m_client != null)
            {
                if (m_client.NumerologySystem.Name != NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM)
                {
                    // backup book before potentially loading a different one
                    Book backup_book = m_client.Book;

                    // reload new numerology system
                    // and if text_mode changed
                    // and if this a Research edition
                    // then reload and simplify new text
                    m_client.ReloadDefaultNumerologySystem(dynamic_text);
                    RefreshCurrentNumerologySystem();
                    UpdateObjectListView();

                    // display newly reloaded and re-simplified text
                    if (backup_book != m_client.Book)
                    {
                        if (m_found_verses_displayed)
                        {
                            DisplayFoundVerses(false);
                        }
                        else
                        {
                            DisplaySelection(false);
                        }
                    }
                    else
                    {
                        CalculateCurrentValue();

                        CalculateLetterStatistics();
                        DisplayLetterStatistics();

                        CalculatePhraseLetterStatistics();
                        DisplayPhraseLetterStatistics();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
    }
    private void RestoreNumerologySystem(string numerology_system_name, string dynamic_text)
    {
        if (m_client != null)
        {
            try
            {
                // backup book before potentially loading a different one
                Book backup_book = m_client.Book;

                // reload new numerology system
                // and if text_mode has changed
                // and if this a Research edition
                // then reload and simplify new text
                m_client.RestoreNumerologySystem(numerology_system_name, dynamic_text);
                RefreshCurrentNumerologySystem();
                UpdateObjectListView();

                // display newly reloaded and re-simplified text
                if (backup_book != m_client.Book)
                {
                    if (m_found_verses_displayed)
                    {
                        DisplayFoundVerses(false);
                    }
                    else
                    {
                        DisplaySelection(false);
                    }
                }
                else // update only letter statistics
                {
                    CalculateCurrentValue();

                    CalculateLetterStatistics();
                    DisplayLetterStatistics();

                    CalculatePhraseLetterStatistics();
                    DisplayPhraseLetterStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName);
            }
        }
    }

    // Edit system in ObjectListView
    private char m_source_char = '\0';
    private char m_target_char = '\0';
    private int m_source_index = -1;
    private int m_target_index = -1;
    private void LetterValuesObjectListView_ModelCanDrop(object sender, BrightIdeasSoftware.ModelDropEventArgs e)
    {
        if (e.TargetModel != null)
        {
            e.DropSink.CanDropBetween = true;
            e.DropSink.CanDropOnItem = false;

            m_source_char = ((KeyValuePair<char, long>)e.SourceModels[0]).Key;
            m_target_char = ((KeyValuePair<char, long>)e.TargetModel).Key;

            if (m_client != null)
            {
                if (m_client.NumerologySystem != null)
                {
                    m_source_index = -1;
                    m_target_index = -1;

                    int index = 0;
                    foreach (char key in m_client.NumerologySystem.Keys)
                    {
                        if (key == m_source_char)
                        {
                            m_source_index = index;
                            break;
                        }
                        index++;
                    }
                    index = 0;
                    foreach (char key in m_client.NumerologySystem.Keys)
                    {
                        if (key == m_target_char)
                        {
                            m_target_index = index;
                            break;
                        }
                        index++;
                    }

                    if (m_source_index != m_target_index)
                    {
                        e.Effect = DragDropEffects.Move;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }
    private void LetterValuesObjectListView_ModelDropped(object sender, BrightIdeasSoftware.ModelDropEventArgs e)
    {
        if (e.TargetModel != null)
        {
            if (m_client != null)
            {
                if (m_client.NumerologySystem != null)
                {
                    List<char> keys = new List<char>();
                    List<long> values = new List<long>();

                    // setup Keys without any change in order
                    foreach (char key in m_client.NumerologySystem.Keys)
                    {
                        keys.Add(key);
                    }
                    // setup Values without any change in order
                    foreach (char key in m_client.NumerologySystem.Keys)
                    {
                        values.Add(m_client.NumerologySystem[key]);
                    }

                    // move source to before target
                    if ((m_target_index > m_source_index) && (m_target_index < m_client.NumerologySystem.Keys.Count - 1))
                    {
                        m_target_index--;
                    }
                    // do move
                    foreach (char key in m_client.NumerologySystem.Keys)
                    {
                        if (key == m_source_char)
                        {
                            keys.Remove(m_source_char);
                            keys.Insert(m_target_index, m_source_char);
                        }
                    }

                    m_client.NumerologySystem.Clear();
                    for (int i = 0; i < keys.Count; i++)
                    {
                        m_client.NumerologySystem.Add(keys[i], values[i]);
                    }
                }

                // force list to refresh doesn't work
                //e.RefreshObjects();
                UpdateObjectListView();

                CalculateCurrentValue();

                LetterValuesRestoreButton.Enabled = true;
                LetterValuesUndoButton.Enabled = true;
                LetterValuesSaveButton.Enabled = true;
            }
        }
    }
    private void LetterValuesObjectListView_CellEditFinishing(object sender, BrightIdeasSoftware.CellEditEventArgs e)
    {
        LetterValuesRestoreButton.Enabled = true;
        LetterValuesUndoButton.Enabled = true;
        LetterValuesSaveButton.Enabled = true;
    }
    private bool m_are_delegates_installed = false;
    private object ValuesValueColumnAspectGetter(object entry)
    {
        return (int)(((KeyValuePair<char, long>)entry).Value);
    }
    private void ValuesValueColumnAspectPutter(object entry, object value)
    {
        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                char key = (char)((KeyValuePair<char, long>)entry).Key;
                m_client.NumerologySystem[key] = (long)((int)value);
                UpdateObjectListView();

                CalculateCurrentValue();
            }
        }
    }
    private void EditLetterValuesLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (!m_are_delegates_installed)
            {
                ValuesValueColumn.AspectGetter = ValuesValueColumnAspectGetter;
                ValuesValueColumn.AspectPutter = ValuesValueColumnAspectPutter;

                //// auto drag and drop
                //if (LetterValuesObjectListView != null)
                //{
                //    LetterValuesObjectListView.DragSource = new BrightIdeasSoftware.SimpleDragSource();
                //    LetterValuesObjectListView.DropSink = new BrightIdeasSoftware.RearrangingDropSink(false);
                //}

                m_are_delegates_installed = true;
            }

            if (!LetterValuesPanel.Visible)
            {
                LetterValuesPanel.Visible = true;
                LetterValuesPanel.BringToFront();
            }
            else
            {
                LetterValuesPanel.Visible = false;
            }

            NumerologySystemComboBox.Focus();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void RestoreNumerologySystemButton_Click(object sender, EventArgs e)
    {
        LetterValuesRestoreButton_Click(null, null);
    }
    private void FavoriteNumerologySystemButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client.NumerologySystem.Name != NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM)
            {
                string dynamic_text = GetDynamicText();
                ReloadDefaultNumerologySystem(dynamic_text);
                CalculateCurrentValue();

                NumerologySystemComboBox.Focus();
            }
            GoldenRatioOrderLabel.Visible = !m_found_verses_displayed;
            GoldenRatioScopeLabel.Visible = !m_found_verses_displayed;

            PictureBoxPanel.Visible = false;
            PictureBoxPanel.SendToBack();
            FavoriteNumerologySystemButton.Visible = false;
            SaveAsFavoriteNumerologySystemLabel.Visible = false;
            RestoreFavoriteNumerologySystemLabel.Visible = (NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM != NumerologySystem.PRIMALOGY_NUMERORLOGY_SYSTEM);
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void LetterValuesUndoButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                string dynamic_text = GetDynamicText();
                ReloadCurrentNumerologySystem(dynamic_text);

                LetterValuesUndoButton.Enabled = false;
                LetterValuesRestoreButton.Enabled = true;
                LetterValuesSaveButton.Enabled = false;
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void LetterValuesRestoreButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (m_client.NumerologySystem != null)
                {
                    string dynamic_text = GetDynamicText();
                    RestoreNumerologySystem(m_client.NumerologySystem.Name, dynamic_text);

                    LetterValuesUndoButton.Enabled = false;
                    LetterValuesRestoreButton.Enabled = false;
                    LetterValuesSaveButton.Enabled = false;
                }
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void LetterValuesSaveButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                m_client.SaveNumerologySystem();

                LetterValuesUndoButton.Enabled = false;
                LetterValuesRestoreButton.Enabled = true;
                LetterValuesSaveButton.Enabled = false;
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void SaveAsFavoriteNumerologySystemLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (MessageBox.Show(
                    "Save " + NumerologySystemComboBox.SelectedItem.ToString() + " as your favorite system?",
                    Application.ProductName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (NumerologySystemComboBox.SelectedItem != null)
                    {
                        NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM = NumerologySystemComboBox.SelectedItem.ToString();
                        SaveApplicationOptions();

                        //ToolTip.SetToolTip(this.FavoriteNumerologySystemButton, NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM);
                        NumerologySystemComboBox.SelectedItem = NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM;
                        if (m_client.NumerologySystem != null)
                        {
                            UpdateKeyboard(m_client.NumerologySystem.TextMode);
                            GoldenRatioOrderLabel.Visible = !m_found_verses_displayed;
                            GoldenRatioScopeLabel.Visible = !m_found_verses_displayed;
                        }
                    }
                }
            }

            NumerologySystemComboBox.Focus();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void RestoreFavoriteNumerologySystemLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (MessageBox.Show(
                    "Restore " + NumerologySystem.PRIMALOGY_NUMERORLOGY_SYSTEM + " as your favorite system?",
                    Application.ProductName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM = NumerologySystem.PRIMALOGY_NUMERORLOGY_SYSTEM;
                    SaveApplicationOptions();

                    //ToolTip.SetToolTip(this.FavoriteNumerologySystemButton, "©2008 Primalogy System [Simplified29_Alphabet_Primes]");
                    NumerologySystemComboBox.SelectedItem = NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM;
                    if (m_client.NumerologySystem != null)
                    {
                        UpdateKeyboard(m_client.NumerologySystem.TextMode);
                        GoldenRatioOrderLabel.Visible = !m_found_verses_displayed;
                        GoldenRatioScopeLabel.Visible = !m_found_verses_displayed;
                    }
                }

                NumerologySystemComboBox.Focus();
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void UpdateObjectListView()
    {
        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                if (LetterValuesObjectListView != null)
                {
                    LetterValuesObjectListView.ClearObjects();
                    LetterValuesObjectListView.SetObjects(m_client.NumerologySystem.LetterValues);
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 26. Numerology Value Calculations
    ///////////////////////////////////////////////////////////////////////////////
    private void PopulateNumerologySystemComboBox()
    {
        try
        {
            NumerologySystemComboBox.SelectedIndexChanged -= new EventHandler(NumerologySystemComboBox_SelectedIndexChanged);
            if (m_client != null)
            {
                if (m_client.Book != null)
                {
                    NumerologySystemComboBox.BeginUpdate();

                    NumerologySystemComboBox.Items.Clear();
                    if (Globals.EDITION == Edition.Lite)
                    {
                        NumerologySystemComboBox.Items.Add(NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM);
                    }
                    else
                    {
                        foreach (string numerology_system_name in m_client.NumerologySystemNames)
                        {
                            NumerologySystemComboBox.Items.Add(numerology_system_name);
                        }
                    }
                }
            }
        }
        finally
        {
            NumerologySystemComboBox.EndUpdate();
            NumerologySystemComboBox.SelectedIndexChanged += new EventHandler(NumerologySystemComboBox_SelectedIndexChanged);
        }
    }
    private void NumerologySystemComboBox_DropDown(object sender, EventArgs e)
    {
        NumerologySystemComboBox.DropDownHeight = StatisticsGroupBox.Height - NumerologySystemComboBox.Top - NumerologySystemComboBox.Height - 1;
    }
    private void NumerologySystemComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (NumerologySystemComboBox.SelectedItem != null)
            {
                string numerology_system_name = NumerologySystemComboBox.SelectedItem.ToString();
                if (numerology_system_name.Contains("Images"))
                {
                    PictureBoxPanel.BringToFront();
                    PictureBoxPanel.Visible = true;
                    DisplayCurrentPage();
                }
                else
                {
                    PictureBoxPanel.Visible = false;
                    PictureBoxPanel.SendToBack();
                }

                string dynamic_text = GetDynamicText();
                ReloadNumerologySystem(numerology_system_name, dynamic_text);
                UpdateKeyboard(m_client.NumerologySystem.TextMode);
                GoldenRatioOrderLabel.Visible = !m_found_verses_displayed;
                GoldenRatioScopeLabel.Visible = !m_found_verses_displayed;

                // re-sort chapters by Value
                RefreshChapterSortComboBox();

                FavoriteNumerologySystemButton.Visible = (numerology_system_name != NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM);
                SaveAsFavoriteNumerologySystemLabel.Visible = (numerology_system_name != NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM);
                RestoreFavoriteNumerologySystemLabel.Visible = (NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM != NumerologySystem.PRIMALOGY_NUMERORLOGY_SYSTEM);
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DisplayCurrentPage()
    {
        Verse verse = GetCurrentVerse();
        if (verse != null)
        {
            DisplayPageImage(verse.Page.Number);
        }
    }
    private void RefreshChapterSortComboBox()
    {
        // reload ChapterSortComboBox if was sorted by Value and verses' value changed
        if (ChapterSortComboBox.SelectedItem != null)
        {
            if (ChapterSortComboBox.SelectedItem.ToString().Contains("Value"))
            {
                ChapterSortComboBox_SelectedIndexChanged(null, null);
            }
        }
    }
    private void CalculateCurrentText()
    {
        if (m_is_selection_mode)
        {
            m_current_text = m_active_textbox.Text;
        }
        else
        {
            if (m_active_textbox.SelectedText.Length == 0) // get text at current line
            {
                Verse verse = GetCurrentVerse();
                if (verse != null)
                {
                    m_current_text = verse.Text;
                }
                else
                {
                    m_current_text = "";
                }
            }
            else // get current selected text
            {
                m_current_text = m_active_textbox.SelectedText;
            }
        }

        if (!String.IsNullOrEmpty(m_current_text))
        {
            m_current_text = RemoveVerseAddresses(m_current_text);
            m_current_text = RemoveVerseEndMarks(m_current_text);
            m_current_text = m_current_text.Trim();
            m_current_text = m_current_text.Replace("\n", "\r\n");
        }
    }
    private void CalculateCurrentValue()
    {
        if (m_client != null)
        {
            ValueTextBox.Text = "0"; // reset displayed value

            CalculateCurrentText();
            if (!String.IsNullOrEmpty(m_current_text))
            {
                if (m_is_selection_mode)
                {
                    if (m_found_verses_displayed)
                    {
                        if (m_client.FoundVerses != null)
                        {
                            CalculateValueAndDisplayFactors(m_client.FoundVerses);
                            DisplayVersesWordsLetters(m_client.FoundVerses);
                        }
                    }
                    else
                    {
                        if (m_client.Selection != null)
                        {
                            if (m_client.Selection.Verses != null)
                            {
                                CalculateValueAndDisplayFactors(m_client.Selection.Verses);
                                DisplayVersesWordsLetters(m_client.Selection.Verses);
                            }
                        }
                    }
                }
                else // nothing highlighted but cursor in line OR something is highlighted
                {
                    if (m_active_textbox.SelectedText.Length == 0) // no text is highlighted
                    {
                        if (m_readonly_mode)
                        {
                            Verse verse = GetCurrentVerse();
                            if (verse != null)
                            {
                                CalculateValueAndDisplayFactors(verse);
                                DisplayVersesWordsLetters(verse);
                            }
                        }
                        else // edit mode so user can paste any text they like to calculate its value
                        {
                            string user_text = m_current_text;
                            CalculateValueAndDisplayFactors(user_text);
                        }
                    }
                    else // some text is selected
                    {
                        if (m_readonly_mode)
                        {
                            CalculateSelectedTextValue();
                        }
                        else // edit mode so user can paste any text they like to calculate its value
                        {
                            string user_text = m_current_text;
                            CalculateValueAndDisplayFactors(user_text);
                        }
                    }
                }
            }
        }
    }
    private void CalculateSelectedTextValue()
    {
        string selected_text = m_active_textbox.SelectedText;
        int first_char = m_active_textbox.SelectionStart;
        int last_char = m_active_textbox.SelectionStart + m_active_textbox.SelectionLength - 1;

        // skip any \n before selected text
        // skip any Endmark at beginning of selected text
        while (
                (selected_text.Length > 0) &&
                (
                  (selected_text[0] == '\n') ||
                  (selected_text[0] == '\r') ||
                  (selected_text[0] == '\t') ||
                  (selected_text[0] == '_') ||
                  (selected_text[0] == ' ') ||
                  (selected_text[0] == Verse.OPEN_BRACKET[0]) ||
                  (selected_text[0] == Verse.CLOSE_BRACKET[0]) ||
                  Constants.INDIAN_DIGITS.Contains(selected_text[0]) ||
                  Constants.STOPMARKS.Contains(selected_text[0]) ||
                  Constants.QURANMARKS.Contains(selected_text[0])
                )
              )
        {
            selected_text = selected_text.Remove(0, 1);
            first_char++;
        }

        // skip any \n after selected text
        // skip any Endmark at end of selected text
        while (
                (selected_text.Length > 0) &&
                (
                  (selected_text[selected_text.Length - 1] == '\n') ||
                  (selected_text[selected_text.Length - 1] == '\r') ||
                  (selected_text[selected_text.Length - 1] == '\t') ||
                  (selected_text[selected_text.Length - 1] == '_') ||
                  (selected_text[selected_text.Length - 1] == ' ') ||
                  (selected_text[selected_text.Length - 1] == Verse.OPEN_BRACKET[0]) ||
                  (selected_text[selected_text.Length - 1] == Verse.CLOSE_BRACKET[0]) ||
                  (selected_text[selected_text.Length - 1] == ' ') ||
                  Constants.INDIAN_DIGITS.Contains(selected_text[selected_text.Length - 1]) ||
                  Constants.STOPMARKS.Contains(selected_text[selected_text.Length - 1]) ||
                  Constants.QURANMARKS.Contains(selected_text[selected_text.Length - 1])
                )
              )
        {
            selected_text = selected_text.Remove(selected_text.Length - 1);
            last_char--;
        }

        List<Verse> highlighted_verses = new List<Verse>();
        Verse first_verse = GetVerseAtChar(first_char);
        if (first_verse != null)
        {
            Verse last_verse = GetVerseAtChar(last_char);
            if (last_verse != null)
            {
                List<Verse> verses = null;
                if (m_found_verses_displayed)
                {
                    verses = m_client.FoundVerses;
                }
                else
                {
                    if (m_client.Selection != null)
                    {
                        verses = m_client.Selection.Verses;
                    }
                }

                if (verses != null)
                {
                    int first_verse_index = GetVerseIndex(first_verse);
                    int last_verse_index = GetVerseIndex(last_verse);
                    for (int i = first_verse_index; i <= last_verse_index; i++)
                    {
                        highlighted_verses.Add(verses[i]);
                    }

                    Letter letter1 = GetLetterAtChar(first_char);
                    if (letter1 != null)
                    {
                        int first_verse_letter_index = letter1.NumberInVerse - 1;

                        Letter letter2 = GetLetterAtChar(last_char);
                        if (letter2 != null)
                        {
                            int last_verse_letter_index = letter2.NumberInVerse - 1;

                            // calculate Letters value
                            CalculateValueAndDisplayFactors(highlighted_verses, first_verse_letter_index, last_verse_letter_index);

                            // calculate and display verse_number_sum, word_number_sum, letter_number_sum
                            DisplayVersesWordsLetters(highlighted_verses, first_verse_letter_index, last_verse_letter_index);
                        }
                    }
                }
            }
        }
    }
    private int m_user_text_selection_start = 0;
    private int m_user_text_selection_length = 0;
    private void CalculateUserTextValue(Point location)
    {
        bool selection_changed = false;
        if (m_user_text_selection_length != UserTextTextBox.SelectedText.Length)
        {
            m_user_text_selection_length = UserTextTextBox.SelectedText.Length;
            selection_changed = true;
        }
        else if (m_user_text_selection_start != UserTextTextBox.SelectionStart)
        {
            m_user_text_selection_start = UserTextTextBox.SelectionStart;
            selection_changed = true;
        }

        if (selection_changed)
        {
            ////////////////////////////////////////////////////
            // overwrite m_current_text to show LetterStatistics
            ////////////////////////////////////////////////////
            if (UserTextTextBox.SelectionLength > 0)
            {
                // selected text only
                m_current_text = UserTextTextBox.SelectedText;
            }
            else
            {
                if ((location.X == 0) && (location.Y == 0))
                {
                    // all text
                    m_current_text = UserTextTextBox.Text;
                }
                else
                {
                    // current line text
                    int char_index = UserTextTextBox.GetCharIndexFromPosition(location);
                    int line_index = UserTextTextBox.GetLineFromCharIndex(char_index);
                    if ((line_index >= 0) && (line_index < UserTextTextBox.Lines.Length))
                    {
                        m_current_text = UserTextTextBox.Lines[line_index].ToString();
                    }
                    else
                    {
                        m_current_text = "";
                    }
                }
            }
            m_current_text = m_current_text.Trim();

            // calculate Letters value
            CalculateValueAndDisplayFactors(m_current_text);

            // calculate and display verse_number_sum, word_number_sum, letter_number_sum
            DisplayVersesWordsLetters(m_current_text);

            CalculateLetterStatistics();
            DisplayLetterStatistics();

            CalculatePhraseLetterStatistics();
            DisplayPhraseLetterStatistics();
        }
    }
    private void UserTextTextBox_KeyUp(object sender, KeyEventArgs e)
    {
        int char_index = UserTextTextBox.GetFirstCharIndexOfCurrentLine();
        if (char_index >= 0)
        {
            Point caret_position = UserTextTextBox.GetPositionFromCharIndex(char_index);
            CalculateUserTextValue(caret_position);
        }
    }
    bool m_mouse_down = false;
    private void UserTextTextBox_MouseDown(object sender, MouseEventArgs e)
    {
        m_mouse_down = true;
    }
    private void UserTextTextBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (m_mouse_down)
        {
            CalculateUserTextValue(e.Location);
        }
    }
    private void UserTextTextBox_MouseUp(object sender, MouseEventArgs e)
    {
        m_mouse_down = false;
        CalculateUserTextValue(e.Location);
    }
    private void UserTextTextBox_MouseEnter(object sender, EventArgs e)
    {
        CalculateUserTextValue(new Point(0, 0));
    }
    private void UserTextTextBox_Enter(object sender, EventArgs e)
    {
        CalculateUserTextValue(new Point(0, 0));
    }
    private void UserTextTextBox_TextChanged(object sender, EventArgs e)
    {
    }
    private string RemoveVerseAddresses(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        string[] lines = text.Split('\n');
        StringBuilder str = new StringBuilder();
        foreach (string line in lines)
        {
            if (line.Length > 0)
            {
                string[] line_parts = line.Split('\t'); // (TAB delimited)
                if (line_parts.Length > 1) // has address
                {
                    str.Append(line_parts[1] + "\n");  // remove verse address
                }
                else if (line_parts.Length > 0)
                {
                    str.Append(line_parts[0] + "\n");  // leave it as it is
                }
            }
        }
        if (str.Length > 1)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    private string RemoveVerseEndMarks(string text)
    {
        // RTL script misplaces brackets
        return text; // do nothing for now

        //if (string.IsNullOrEmpty(text)) return null;
        //while (text.Contains(Verse.OPEN_BRACKET) || text.Contains(Verse.CLOSE_BRACKET))
        //{
        //    int start = text.IndexOf(Verse.OPEN_BRACKET);
        //    int end = text.IndexOf(Verse.CLOSE_BRACKET);
        //    if ((start >= 0) && (end >= 0))
        //    {
        //        if (start < end)
        //        {
        //            text = text.Remove(start, (end - start) + 1); // remove space after it
        //        }
        //        else // Arabic script misplaces brackets
        //        {
        //            text = text.Remove(end, (start - end) + 1); // remove space after it
        //        }
        //    }
        //}
        //return text;
    }

    // used for non-Quran text
    private long CalculateValue(char user_char)
    {
        long result = 0L;
        if (m_client != null)
        {
            result = m_client.CalculateValue(user_char);
        }
        return result;
    }
    private long CalculateValue(string user_text)
    {
        long result = 0L;
        if (m_client != null)
        {
            result = m_client.CalculateValue(user_text);
        }
        return result;
    }
    private void CalculateValueAndDisplayFactors(string user_text)
    {
        if (m_client != null)
        {
            long value = CalculateValue(user_text);
            FactorizeValue(value, "Text");
        }
    }
    // used for Quran text only
    private void CalculateValueAndDisplayFactors(Verse verse)
    {
        if (m_client != null)
        {
            long value = m_client.CalculateValue(verse);
            FactorizeValue(value, "Value");
        }
    }
    private void CalculateValueAndDisplayFactors(List<Verse> verses)
    {
        if (m_client != null)
        {
            long value = m_client.CalculateValue(verses);
            FactorizeValue(value, "Value");
        }
    }
    private void CalculateValueAndDisplayFactors(Chapter chapter)
    {
        if (m_client != null)
        {
            long value = m_client.CalculateValue(chapter);
            FactorizeValue(value, "Value");
        }
    }
    private void CalculateValueAndDisplayFactors(List<Verse> verses, int letter_index_in_verse1, int letter_index_in_verse2)
    {
        if (m_client != null)
        {
            long value = m_client.CalculateValue(verses, letter_index_in_verse1, letter_index_in_verse2);
            FactorizeValue(value, "Value");
        }
    }
    private void ValueTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (ModifierKeys == Keys.Control)
        {
            if (e.KeyCode == Keys.A)
            {
                if (sender is TextBoxBase)
                {
                    (sender as TextBoxBase).SelectAll();
                }
            }
        }
        else if (e.KeyCode == Keys.Enter)
        {
            CalculateExpression();
        }
        else
        {
            ValueTextBox.ForeColor = Color.DarkGray;
        }
    }
    private void CalculateExpression()
    {
        string expression = ValueTextBox.Text;

        long value = 0L;
        if (long.TryParse(expression, out value))
        {
            m_double_value = (double)value;
            FactorizeValue(value, "Number");
        }
        else if (expression.IsArabic())
        {
            m_double_value = CalculateValue(expression);
            value = (long)Math.Round(m_double_value);
            FactorizeValue(value, "Text" + expression);
        }
        else
        {
            m_double_value = CalculateValue(expression, m_radix);
            value = (long)Math.Round(m_double_value);
            FactorizeValue(value, "Expression");
        }

        // if result has fraction, display it as is
        // PrimeFactorsTextBox_DoubleClick will toggle it as long
        if (m_double_value != value)
        {
            PrimeFactorsTextBox.Text = m_double_value.ToString();
        }
    }
    private double m_double_value = 0.0D;
    private bool m_double_value_displayed = false;
    private void PrimeFactorsTextBox_DoubleClick(object sender, EventArgs e)
    {
        // toggle double_value <--> prime factors
        if (m_double_value_displayed)
        {
            try
            {
                // display prime factors
                m_double_value = double.Parse(PrimeFactorsTextBox.Text);
                long value = (long)Math.Round(m_double_value);
                FactorizeValue(value, "Expression");
                m_double_value_displayed = false;
            }
            catch
            {
                // silence error and do nothing
            }
        }
        else // m_is_double_value_displayed == false
        {
            if (ValueLabel.Text == "Expression")
            {
                // display double_value
                PrimeFactorsTextBox.Text = m_double_value.ToString();
                m_double_value_displayed = true;
            }
            else
            {
                // do nothing
            }
        }
    }
    private double CalculateValue(string expression, long radix)
    {
        double result = 0D;
        try
        {
            result = Radix.Decode(expression, radix);
            this.ToolTip.SetToolTip(this.ValueTextBox, result.ToString());
        }
        catch // if expression
        {
            string text = CalculateExpression(expression, radix);
            this.ToolTip.SetToolTip(this.ValueTextBox, text); // display the decimal expansion

            try
            {
                result = double.Parse(text);
            }
            catch
            {
                if (m_client != null)
                {
                    result = m_client.CalculateValue(expression);
                }
            }
        }
        return result;
    }
    private string CalculateExpression(string expression, long radix)
    {
        try
        {
            return Calculator.Evaluate(expression, radix);
        }
        catch
        {
            return expression;
        }
    }
    private void FactorizeValue(long value, string caption)
    {
        try
        {
            m_double_value_displayed = false;

            if (caption.StartsWith("Text"))
            {
                ValueLabel.Text = "Text";
                ValueLabel.Refresh();

                this.ToolTip.SetToolTip(this.ValueTextBox, "Value of  " + caption.Substring(4));
            }
            else
            {
                ValueLabel.Text = caption;
                ValueLabel.Refresh();

                if (caption == "Value")
                {
                    this.ToolTip.SetToolTip(this.ValueTextBox, "Value of selection  القيمة حسب نظام الترقيم الحالي");
                }
                else if (caption == "Number")
                {
                    this.ToolTip.SetToolTip(this.ValueTextBox, "User number");
                }
                else if (caption == "Expression")
                {
                    this.ToolTip.SetToolTip(this.ValueTextBox, "User math expression");
                    m_double_value_displayed = true;
                }
                else
                {
                    this.ToolTip.SetToolTip(this.ValueTextBox, caption);
                }
            }

            DisplayValue(value);

            PrimeFactorsTextBox.Text = Numbers.FactorizeToString(value);
            PrimeFactorsTextBox.Refresh();

            int nth_prime_index = -1;
            int nth_additive_prime_index = -1;
            int nth_pure_prime_index = -1;
            NthPrimeTextBox.ForeColor = GetNumberTypeColor(nth_prime_index);
            NthPrimeTextBox.Text = nth_prime_index.ToString();
            NthPrimeTextBox.Refresh();
            NthAdditivePrimeTextBox.ForeColor = GetNumberTypeColor(nth_additive_prime_index);
            NthAdditivePrimeTextBox.Text = nth_additive_prime_index.ToString();
            NthAdditivePrimeTextBox.Refresh();
            NthPurePrimeTextBox.ForeColor = GetNumberTypeColor(nth_pure_prime_index);
            NthPurePrimeTextBox.Text = nth_pure_prime_index.ToString();
            NthPurePrimeTextBox.Refresh();

            if (value < 0L)
            {
                nth_prime_index = -1;
                nth_additive_prime_index = -1;
                nth_pure_prime_index = -1;
                NthPrimeLabel.Text = "P Index";
                NthPrimeLabel.Refresh();
                NthAdditivePrimeLabel.Text = "AP Index";
                NthAdditivePrimeLabel.Refresh();
                NthPurePrimeLabel.Text = "PP Index";
                NthPurePrimeLabel.Refresh();

                ToolTip.SetToolTip(NthPrimeTextBox, "Find prime by index");
                ToolTip.SetToolTip(NthAdditivePrimeTextBox, "Find additive prime by index");
                ToolTip.SetToolTip(NthPurePrimeTextBox, "Find pure prime by index");
            }
            else if (value == 0)
            {
                nth_prime_index = 0;
                nth_additive_prime_index = 0;
                nth_pure_prime_index = 0;
                NthPrimeLabel.Text = "C Index";
                NthPrimeLabel.Refresh();
                NthAdditivePrimeLabel.Text = "AC Index";
                NthAdditivePrimeLabel.Refresh();
                NthPurePrimeLabel.Text = "PC Index";
                NthPurePrimeLabel.Refresh();

                ToolTip.SetToolTip(NthPrimeTextBox, "Find composite by index");
                ToolTip.SetToolTip(NthAdditivePrimeTextBox, "Find additive composite by index");
                ToolTip.SetToolTip(NthPurePrimeTextBox, "Find pure composite by index");
            }
            else if (value == 1)
            {
                nth_prime_index = 0;
                nth_additive_prime_index = 0;
                nth_pure_prime_index = 0;
                NthPrimeLabel.Text = "P Index";
                NthPrimeLabel.Refresh();
                NthAdditivePrimeLabel.Text = "AP Index";
                NthAdditivePrimeLabel.Refresh();
                NthPurePrimeLabel.Text = "PP Index";
                NthPurePrimeLabel.Refresh();

                ToolTip.SetToolTip(NthPrimeTextBox, "Find prime by index");
                ToolTip.SetToolTip(NthAdditivePrimeTextBox, "Find additive prime by index");
                ToolTip.SetToolTip(NthPurePrimeTextBox, "Find pure prime by index");
            }
            else if (Numbers.IsPrime(value))
            {
                nth_prime_index = Numbers.IndexOfPrime(value);
                nth_additive_prime_index = Numbers.IndexOfAdditivePrime(value);
                nth_pure_prime_index = Numbers.IndexOfPurePrime(value);
                NthPrimeLabel.Text = "P Index";
                NthPrimeLabel.Refresh();
                NthAdditivePrimeLabel.Text = "AP Index";
                NthAdditivePrimeLabel.Refresh();
                NthPurePrimeLabel.Text = "PP Index";
                NthPurePrimeLabel.Refresh();

                ToolTip.SetToolTip(NthPrimeTextBox, "Find prime by index");
                ToolTip.SetToolTip(NthAdditivePrimeTextBox, "Find additive prime by index");
                ToolTip.SetToolTip(NthPurePrimeTextBox, "Find pure prime by index");
            }
            else
            {
                nth_prime_index = Numbers.IndexOfComposite(value) + 1;
                if (nth_prime_index == 0)
                {
                    nth_prime_index = -1;
                }
                nth_additive_prime_index = Numbers.IndexOfAdditiveComposite(value) + 1;
                if (nth_additive_prime_index == 0)
                {
                    nth_additive_prime_index = -1;
                }
                nth_pure_prime_index = Numbers.IndexOfPureComposite(value) + 1;
                if (nth_pure_prime_index == 0)
                {
                    nth_pure_prime_index = -1;
                }
                NthPrimeLabel.Text = "C Index";
                NthPrimeLabel.Refresh();
                NthAdditivePrimeLabel.Text = "AC Index";
                NthAdditivePrimeLabel.Refresh();
                NthPurePrimeLabel.Text = "PC Index";
                NthPurePrimeLabel.Refresh();

                ToolTip.SetToolTip(NthPrimeTextBox, "Find composite by index");
                ToolTip.SetToolTip(NthAdditivePrimeTextBox, "Find additive composite by index");
                ToolTip.SetToolTip(NthPurePrimeTextBox, "Find pure composite by index");
            }

            NthPrimeTextBox.ForeColor = GetNumberTypeColor(nth_prime_index);
            NthPrimeTextBox.Text = nth_prime_index.ToString();
            NthPrimeTextBox.Refresh();

            NthAdditivePrimeTextBox.ForeColor = GetNumberTypeColor(nth_additive_prime_index);
            NthAdditivePrimeTextBox.Text = nth_additive_prime_index.ToString();
            NthAdditivePrimeTextBox.Refresh();

            NthPurePrimeTextBox.ForeColor = GetNumberTypeColor(nth_pure_prime_index);
            NthPurePrimeTextBox.Text = nth_pure_prime_index.ToString();
            NthPurePrimeTextBox.Refresh();

            // update the ValueNavigator fields
            UpdateValueNavigator(value);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
    }
    private void UpdateValueNavigator(long value)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                Verse verse = m_client.Book.GetVerseByVerseNumber((int)value);
                if (verse != null)
                {
                    VerseByVerseNumberLabel.Text = verse.Address;
                }
                else
                {
                    VerseByVerseNumberLabel.Text = "---:---";
                }

                verse = m_client.Book.GetVerseByWordNumber((int)value);
                if (verse != null)
                {
                    VerseByWordNumberLabel.Text = verse.Address;
                }
                else
                {
                    VerseByWordNumberLabel.Text = "---:---";
                }

                verse = m_client.Book.GetVerseByLetterNumber((int)value);
                if (verse != null)
                {
                    VerseByLetterNumberLabel.Text = verse.Address;
                }
                else
                {
                    VerseByLetterNumberLabel.Text = "---:---";
                }
            }
        }
    }
    private void NthPrimeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            try
            {
                long number = -1L;
                int nth_index = int.Parse(NthPrimeTextBox.Text);
                if (nth_index < 0)
                {
                    number = -1L;
                }
                else
                {
                    NthPrimeTextBox.ForeColor = GetNumberTypeColor(nth_index);
                    if (NthPrimeLabel.Text == "P Index")
                    {
                        number = Numbers.Primes[nth_index];
                    }
                    else //if (NthPrimeLabel.Text == "C Index")
                    {
                        if (nth_index == 0)
                        {
                            number = 0L;
                        }
                        else
                        {
                            number = Numbers.Composites[nth_index - 1];
                        }
                    }
                }
                DisplayValue(number);
                FactorizeValue(number, NthPrimeLabel.Text);
            }
            catch
            {
                //MessageBox.Show(ex.Message, Application.ProductName);
                DisplayValue(0);
            }
        }
    }
    private void NthAdditivePrimeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            try
            {
                long number = -1L;
                int nth_index = int.Parse(NthAdditivePrimeTextBox.Text);
                if (nth_index < 0)
                {
                    number = -1L;
                }
                else
                {
                    NthAdditivePrimeTextBox.ForeColor = GetNumberTypeColor(nth_index);
                    if (NthAdditivePrimeLabel.Text == "AP Index")
                    {
                        number = Numbers.AdditivePrimes[nth_index];
                    }
                    else //if (NthAdditivePrimeLabel.Text == "AC Index")
                    {
                        if (nth_index == 0)
                        {
                            number = 0L;
                        }
                        else
                        {
                            number = Numbers.AdditiveComposites[nth_index - 1];
                        }
                    }
                }
                DisplayValue(number);
                FactorizeValue(number, NthAdditivePrimeLabel.Text);
            }
            catch
            {
                //MessageBox.Show(ex.Message, Application.ProductName);
                DisplayValue(0);
            }
        }
    }
    private void NthPurePrimeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            try
            {
                long number = -1L;
                int nth_index = int.Parse(NthPurePrimeTextBox.Text);
                if (nth_index < 0)
                {
                    number = -1L;
                }
                else
                {
                    NthPurePrimeTextBox.ForeColor = GetNumberTypeColor(nth_index);
                    if (NthPurePrimeLabel.Text == "PP Index")
                    {
                        number = Numbers.PurePrimes[nth_index];
                    }
                    else //if (NthPurePrimeLabel.Text == "PC Index")
                    {
                        if (nth_index == 0)
                        {
                            number = 0L;
                        }
                        else
                        {
                            number = Numbers.PureComposites[nth_index - 1];
                        }
                    }
                }
                DisplayValue(number);
                FactorizeValue(number, NthPurePrimeLabel.Text);
            }
            catch
            {
                //MessageBox.Show(ex.Message, Application.ProductName);
                DisplayValue(0);
            }
        }
    }
    private Color GetNumberTypeColor(long number)
    {
        return GetNumberTypeColor(number.ToString(), 10);
    }
    private Color GetNumberTypeColor(string value, long radix)
    {
        if (Numbers.IsPurePrime(value, radix))
        {
            return Color.DarkViolet;
        }
        else if (Numbers.IsAdditivePrime(value, radix))
        {
            return Color.Blue;
        }
        else if (Numbers.IsPrime(value, radix))
        {
            return Color.Green;
        }
        else if (Numbers.IsPureComposite(value, radix))
        {
            return Color.OrangeRed;
        }
        else if (Numbers.IsAdditiveComposite(value, radix))
        {
            return Color.Brown;
        }
        else if (Numbers.IsComposite(value, radix))
        {
            return Color.Black;
        }
        else
        {
            return Color.Black;
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 27. Numerology Value Display
    ///////////////////////////////////////////////////////////////////////////////
    private int m_radix = DEFAULT_RADIX;
    private void RadixValueLabel_Click(object sender, EventArgs e)
    {
        try
        {
            // get values in current radix
            int verse_count = (int)Radix.Decode(VersesTextBox.Text, m_radix);
            int word_count = (int)Radix.Decode(WordsTextBox.Text, m_radix);
            int letter_count = (int)Radix.Decode(LettersTextBox.Text, m_radix);
            ////int verse_number_sum = (int)Radix.Decode(VerseNumberSumTextBox.Text.Split()[1], m_radix);
            ////int word_number_sum = (int)Radix.Decode(WordNumberSumTextBox.Text.Split()[1], m_radix);
            ////int letter_number_sum = (int)Radix.Decode(LetterNumberSumTextBox.Text.Split()[1], m_radix);
            //int verse_number_sum = int.Parse(VerseNumberSumTextBox.Text.Split()[1]);
            //int word_number_sum = int.Parse(WordNumberSumTextBox.Text.Split()[1]);
            //int letter_number_sum = int.Parse(LetterNumberSumTextBox.Text.Split()[1]);
            long value = Radix.Decode(ValueTextBox.Text, m_radix);

            // toggle radix
            if (m_radix == 10)
            {
                m_radix = 19;
            }
            else
            {
                m_radix = 10;
            }
            RadixValueLabel.Text = m_radix.ToString();

            // display values in nex radix
            //DisplayVersesWordsLetters(verse_count, word_count, letter_count, verse_number_sum, word_number_sum, letter_number_sum);
            DisplayVersesWordsLetters(verse_count, word_count, letter_count, -1, -1, -1); // -1 means don't change what is displayed
            DisplayValue(value);
        }
        catch
        {
            // log exception
        }
    }
    private void RadixValueUpLabel_Click(object sender, EventArgs e)
    {
        try
        {
            // get values in current radix
            int verse_count = (int)Radix.Decode(VersesTextBox.Text, m_radix);
            int word_count = (int)Radix.Decode(WordsTextBox.Text, m_radix);
            int letter_count = (int)Radix.Decode(LettersTextBox.Text, m_radix);
            ////int verse_number_sum = (int)Radix.Decode(VerseNumberSumTextBox.Text.Split()[1], m_radix);
            ////int word_number_sum = (int)Radix.Decode(WordNumberSumTextBox.Text.Split()[1], m_radix);
            ////int letter_number_sum = (int)Radix.Decode(LetterNumberSumTextBox.Text.Split()[1], m_radix);
            //int verse_number_sum = int.Parse(VerseNumberSumTextBox.Text.Split()[1]);
            //int word_number_sum = int.Parse(WordNumberSumTextBox.Text.Split()[1]);
            //int letter_number_sum = int.Parse(LetterNumberSumTextBox.Text.Split()[1]);
            long value = Radix.Decode(ValueTextBox.Text, m_radix);

            // increment radix
            m_radix++;
            if (m_radix > 36) m_radix = 2;
            RadixValueLabel.Text = m_radix.ToString();

            // display values in nex radix
            //DisplayVersesWordsLetters(verse_count, word_count, letter_count, verse_number_sum, word_number_sum, letter_number_sum);
            DisplayVersesWordsLetters(verse_count, word_count, letter_count, -1, -1, -1); // -1 means don't change what is displayed
            DisplayValue(value);
        }
        catch
        {
            // log exception
        }
    }
    private void RadixValueDownLabel_Click(object sender, EventArgs e)
    {
        try
        {
            // get values in current radix
            int verse_count = (int)Radix.Decode(VersesTextBox.Text, m_radix);
            int word_count = (int)Radix.Decode(WordsTextBox.Text, m_radix);
            int letter_count = (int)Radix.Decode(LettersTextBox.Text, m_radix);
            ////int verse_number_sum = (int)Radix.Decode(VerseNumberSumTextBox.Text.Split()[1], m_radix);
            ////int word_number_sum = (int)Radix.Decode(WordNumberSumTextBox.Text.Split()[1], m_radix);
            ////int letter_number_sum = (int)Radix.Decode(LetterNumberSumTextBox.Text.Split()[1], m_radix);
            //int verse_number_sum = int.Parse(VerseNumberSumTextBox.Text.Split()[1]);
            //int word_number_sum = int.Parse(WordNumberSumTextBox.Text.Split()[1]);
            //int letter_number_sum = int.Parse(LetterNumberSumTextBox.Text.Split()[1]);
            long value = Radix.Decode(ValueTextBox.Text, m_radix);

            // increment radix
            m_radix--;
            if (m_radix < 2) m_radix = 36;
            RadixValueLabel.Text = m_radix.ToString();

            // display values in nex radix
            //DisplayVersesWordsLetters(verse_count, word_count, letter_count, verse_number_sum, word_number_sum, letter_number_sum);
            DisplayVersesWordsLetters(verse_count, word_count, letter_count, -1, -1, -1); // -1 means don't change what is displayed
            DisplayValue(value);
        }
        catch
        {
            // log exception
        }
    }
    private void DisplayValue(long value)
    {
        ValueTextBox.Text = Radix.Encode(value, m_radix);
        DecimalValueTextBox.Text = value.ToString();
        ValueTextBox.ForeColor = GetNumberTypeColor(ValueTextBox.Text, m_radix);
        ValueTextBox.SelectionStart = ValueTextBox.Text.Length;
        ValueTextBox.SelectionLength = 0;
        ValueTextBox.Refresh();
        DecimalValueTextBox.Visible = (m_radix != DEFAULT_RADIX);
        DecimalValueTextBox.ForeColor = GetNumberTypeColor(value);
        DecimalValueTextBox.Refresh();

        DigitSumTextBox.Text = Numbers.DigitSum(ValueTextBox.Text).ToString();
        DigitSumTextBox.ForeColor = GetNumberTypeColor(DigitSumTextBox.Text, m_radix);
        DigitSumTextBox.Refresh();

        DigitalRootTextBox.Text = Numbers.DigitalRoot(ValueTextBox.Text).ToString();
        DigitalRootTextBox.ForeColor = GetNumberTypeColor(DigitalRootTextBox.Text, m_radix);
        DigitalRootTextBox.Refresh();
    }
    private void DisplayVersesWordsLetters(string user_text)
    {
        if (!String.IsNullOrEmpty(user_text))
        {
            VerseNumberSumTextBox.Text = "";
            WordNumberSumTextBox.Text = "";
            LetterNumberSumTextBox.Text = "";

            if (!user_text.IsArabic())  // eg English
            {
                user_text = user_text.ToUpper();
            }

            // in all cases
            // simplify all text_modes (Original will be simplified29 automatically)
            user_text = user_text.SimplifyTo(m_client.NumerologySystem.TextMode);

            user_text = user_text.Replace("\t", " ");
            while (user_text.Contains("  "))
            {
                user_text = user_text.Replace("  ", " ");
            }
            user_text = user_text.Replace("\r\n", "\n");

            int verse_count = 1;
            int word_count = 1;
            int letter_count = 0;
            foreach (char c in user_text)
            {
                if (c == '\n')
                {
                    verse_count++;
                    if (letter_count > 0)
                    {
                        word_count++;
                    }
                }
                else if (c == ' ')
                {
                    word_count++;
                }
                else
                {
                    letter_count++;
                }
            }
            DisplayVersesWordsLetters(verse_count, word_count, letter_count, -1, -1, -1);
        }
        else
        {
            DisplayVersesWordsLetters(0, 0, 0, -1, -1, -1);
        }
    }
    private void DisplayVersesWordsLetters(int verse_count, int word_count, int letter_count)
    {
        VersesTextBox.Text = Radix.Encode(verse_count, m_radix);
        VersesTextBox.ForeColor = GetNumberTypeColor(VersesTextBox.Text, m_radix);
        VersesTextBox.Refresh();
        DecimalVersesTextBox.Text = verse_count.ToString();
        DecimalVersesTextBox.ForeColor = GetNumberTypeColor(verse_count);
        DecimalVersesTextBox.Visible = (m_radix != DEFAULT_RADIX);
        DecimalVersesTextBox.Refresh();

        WordsTextBox.Text = Radix.Encode(word_count, m_radix);
        WordsTextBox.ForeColor = GetNumberTypeColor(WordsTextBox.Text, m_radix);
        WordsTextBox.Refresh();
        DecimalWordsTextBox.Text = word_count.ToString();
        DecimalWordsTextBox.ForeColor = GetNumberTypeColor(word_count);
        DecimalWordsTextBox.Visible = (m_radix != DEFAULT_RADIX);
        DecimalWordsTextBox.Refresh();

        LettersTextBox.Text = Radix.Encode(letter_count, m_radix);
        LettersTextBox.ForeColor = GetNumberTypeColor(LettersTextBox.Text, m_radix);
        LettersTextBox.Refresh();
        DecimalLettersTextBox.Text = letter_count.ToString();
        DecimalLettersTextBox.ForeColor = GetNumberTypeColor(letter_count);
        DecimalLettersTextBox.Visible = (m_radix != DEFAULT_RADIX);
        DecimalLettersTextBox.Refresh();
    }
    private void DisplayVersesWordsLetters(int verse_count, int word_count, int letter_count, int verse_number_sum, int word_number_sum, int letter_number_sum)
    {
        DisplayVersesWordsLetters(verse_count, word_count, letter_count);

        if (verse_number_sum != -1)
        {
            //VerseNumberSumTextBox.Text = "Ʃ " + Radix.Encode(verse_number_sum, m_radix);
            //VerseNumberSumTextBox.ForeColor = GetNumberTypeColor(VerseNumberSumTextBox.Text.Split()[1], m_radix);
            VerseNumberSumTextBox.Text = "Ʃ " + verse_number_sum.ToString();
            VerseNumberSumTextBox.ForeColor = GetNumberTypeColor(verse_number_sum);
            VerseNumberSumTextBox.Refresh();
        }

        if (word_number_sum != -1)
        {
            //WordNumberSumTextBox.Text = "Ʃ " + Radix.Encode(word_number_sum, m_radix);
            //WordNumberSumTextBox.ForeColor = GetNumberTypeColor(WordNumberSumTextBox.Text.Split()[1], m_radix);
            WordNumberSumTextBox.Text = "Ʃ " + word_number_sum.ToString();
            WordNumberSumTextBox.ForeColor = GetNumberTypeColor(word_number_sum);
            WordNumberSumTextBox.Refresh();
        }

        if (letter_number_sum != -1)
        {
            //LetterNumberSumTextBox.Text = "Ʃ " + Radix.Encode(letter_number_sum, m_radix);
            //LetterNumberSumTextBox.ForeColor = GetNumberTypeColor(LetterNumberSumTextBox.Text.Split()[1], m_radix);
            LetterNumberSumTextBox.Text = "Ʃ " + letter_number_sum.ToString();
            LetterNumberSumTextBox.ForeColor = GetNumberTypeColor(letter_number_sum);
            LetterNumberSumTextBox.Refresh();
        }
    }
    private void DisplayVersesWordsLetters(Verse verse)
    {
        if (verse != null)
        {
            int verse_count = 1;
            int word_count = verse.Words.Count;
            int letter_count = verse.LetterCount;
            int verse_number_sum = verse.NumberInChapter;
            int word_number_sum = 0;
            int letter_number_sum = 0;
            foreach (Word word in verse.Words)
            {
                word_number_sum += word.NumberInVerse;
                foreach (Letter letter in word.Letters)
                {
                    letter_number_sum += letter.NumberInWord;
                }
            }
            DisplayVersesWordsLetters(verse_count, word_count, letter_count, verse_number_sum, word_number_sum, letter_number_sum);
        }
    }
    private void DisplayVersesWordsLetters(List<Verse> verses)
    {
        if (verses != null)
        {
            int verse_count = verses.Count;
            int word_count = 0;
            int letter_count = 0;
            int verse_number_sum = 0;
            int word_number_sum = 0;
            int letter_number_sum = 0;
            foreach (Verse verse in verses)
            {
                word_count += verse.Words.Count;
                letter_count += verse.LetterCount;

                verse_number_sum += verse.NumberInChapter;
                foreach (Word word in verse.Words)
                {
                    word_number_sum += word.NumberInVerse;
                    foreach (Letter letter in word.Letters)
                    {
                        letter_number_sum += letter.NumberInWord;
                    }
                }
            }
            DisplayVersesWordsLetters(verse_count, word_count, letter_count, verse_number_sum, word_number_sum, letter_number_sum);

            DisplayTranslations(verses); // display translations for selected verse
        }
    }
    private void CalculateMiddlePartValue(Verse verse, int from_letter_index, int to_letter_index)
    {
        int word_index = -1;   // in verse
        int letter_index = -1; // in verse
        foreach (Word word in verse.Words)
        {
            word_index++;
            foreach (Letter letter in word.Letters)
            {
                letter_index++;

                if (letter_index < from_letter_index) continue;
                if (letter_index > to_letter_index) break;
            }
        }
    }
    private void DisplayVersesWordsLetters(List<Verse> verses, int letter_index_in_verse1, int letter_index_in_verse2)
    {
        if (verses != null)
        {
            int verse_count = verses.Count;
            int word_count = 0;
            int letter_count = 0;
            int verse_number_sum = 0;
            int word_number_sum = 0;
            int letter_number_sum = 0;

            if (verses.Count == 1)
            {
                ///////////////////////////
                // Middle Verse Part (verse1, letter_index_in_verse1, letter_index_in_verse2);
                ///////////////////////////
                Verse verse1 = verses[0];
                foreach (Word word in verse1.Words)
                {
                    bool word_counted = false;
                    foreach (Letter letter in word.Letters)
                    {
                        if (letter.NumberInVerse - 1 < letter_index_in_verse1) continue;
                        if (letter.NumberInVerse - 1 > letter_index_in_verse2) break;
                        letter_count++;
                        letter_number_sum += letter.NumberInWord;

                        if (!word_counted)
                        {
                            word_count++;
                            word_number_sum += word.NumberInVerse;

                            word_counted = true;
                        }
                    }
                }
                verse_number_sum += verse1.NumberInChapter;
            }
            else if (verses.Count == 2)
            {
                ///////////////////////////
                // End Verse Part (verse1, letter_index_in_verse1);
                ///////////////////////////
                Verse verse1 = verses[0];
                foreach (Word word in verse1.Words)
                {
                    bool word_counted = false;
                    foreach (Letter letter in word.Letters)
                    {
                        if (letter.NumberInVerse - 1 < letter_index_in_verse1) continue;
                        if (letter.NumberInVerse - 1 > verse1.LetterCount - 1) break;
                        letter_count++;
                        letter_number_sum += letter.NumberInWord;

                        if (!word_counted)
                        {
                            word_count++;
                            word_number_sum += word.NumberInVerse;

                            word_counted = true;
                        }
                    }
                }
                verse_number_sum += verse1.NumberInChapter;

                ///////////////////////////
                // Beginning Verse Part (verse2, letter_index_in_verse2);
                ///////////////////////////
                Verse verse2 = verses[1];
                foreach (Word word in verse2.Words)
                {
                    bool word_counted = false;
                    foreach (Letter letter in word.Letters)
                    {
                        if (letter.NumberInVerse - 1 < 0) continue;
                        if (letter.NumberInVerse - 1 > letter_index_in_verse2) break;
                        letter_count++;
                        letter_number_sum += letter.NumberInWord;

                        if (!word_counted)
                        {
                            word_count++;
                            word_number_sum += word.NumberInVerse;

                            word_counted = true;
                        }
                    }
                }
                verse_number_sum += verse2.NumberInChapter;
            }
            else if (verses.Count > 2)
            {
                ///////////////////////////
                // End Verse Part (verse1, letter_index_in_verse1);
                ///////////////////////////
                Verse verse1 = verses[0];
                foreach (Word word in verse1.Words)
                {
                    bool word_counted = false;
                    foreach (Letter letter in word.Letters)
                    {
                        if (letter.NumberInVerse - 1 < letter_index_in_verse1) continue;
                        if (letter.NumberInVerse - 1 > verse1.LetterCount - 1) break;
                        letter_count++;
                        letter_number_sum += letter.NumberInWord;

                        if (!word_counted)
                        {
                            word_count++;
                            word_number_sum += word.NumberInVerse;

                            word_counted = true;
                        }
                    }
                }
                verse_number_sum += verse1.NumberInChapter;

                ///////////////////////////
                // Middle Verses
                ///////////////////////////
                for (int i = 1; i < verses.Count - 1; i++)
                {
                    Verse verse = verses[i];
                    word_count += verse.Words.Count;
                    letter_count += verse.LetterCount;

                    verse_number_sum += verse.NumberInChapter;
                    foreach (Word word in verse.Words)
                    {
                        word_number_sum += word.NumberInVerse;
                        foreach (Letter letter in word.Letters)
                        {
                            letter_number_sum += letter.NumberInWord;
                        }
                    }
                }

                ///////////////////////////
                // Beginning Verse Part (verse2, letter_index_in_verse2);
                ///////////////////////////
                Verse verse2 = verses[verses.Count - 1];
                foreach (Word word in verse2.Words)
                {
                    bool word_counted = false;
                    foreach (Letter letter in word.Letters)
                    {
                        if (letter.NumberInVerse - 1 < 0) continue;
                        if (letter.NumberInVerse - 1 > letter_index_in_verse2) break;
                        letter_count++;
                        letter_number_sum += letter.NumberInWord;

                        if (!word_counted)
                        {
                            word_count++;
                            word_number_sum += word.NumberInVerse;

                            word_counted = true;
                        }
                    }
                }
                verse_number_sum += verse2.NumberInChapter;
            }
            else // verses.Count == 0
            {
                // do nothing
            }
            DisplayVersesWordsLetters(verse_count, word_count, letter_count, verse_number_sum, word_number_sum, letter_number_sum);

            DisplayTranslations(verses); // display translations for selected verse
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 28. Numerology Value Navigator
    ///////////////////////////////////////////////////////////////////////////////
    private Stack<int> m_navigation_undo_stack = new Stack<int>();
    private Stack<int> m_navigation_redo_stack = new Stack<int>();
    private void VerseFromNumerologyValue_Click(object sender, EventArgs e)
    {
        string verse_address = (sender as Label).Text;
        if ((verse_address.Length > 0) && (verse_address != "---:---"))
        {
            Verse verse = GetCurrentVerse();
            if (verse != null)
            {
                int current_verse_number = verse.Number;
                m_navigation_undo_stack.Push(current_verse_number);
                m_navigation_redo_stack.Clear();
                DisplayVerse(verse_address);
            }
        }
    }
    private void DisplayVerse(Verse verse)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                if (verse != null)
                {
                    if (verse.Chapter != null)
                    {
                        int chapter_number = verse.Chapter.Number;
                        int verse_number_in_chapter = verse.NumberInChapter;
                        int verse_number = m_client.Book.GetVerseNumber(chapter_number, verse_number_in_chapter);
                        if ((verse_number >= VerseNumericUpDown.Minimum) && (verse_number <= VerseNumericUpDown.Maximum))
                        {
                            VerseNumericUpDown.Focus();
                            VerseNumericUpDown.Value = verse_number;
                        }
                    }
                }
            }
        }
    }
    private void DisplayVerse(string verse_address)
    {
        if (m_client != null)
        {
            if (m_client.Book != null)
            {
                int chapter_number = 0;
                int verse_number_in_chapter = 0;
                int verse_number = 0;

                string[] parts = verse_address.Split(':');
                if (parts.Length == 2)
                {
                    try
                    {
                        chapter_number = int.Parse(parts[0]);
                        verse_number_in_chapter = int.Parse(parts[1]);
                        verse_number = m_client.Book.GetVerseNumber(chapter_number, verse_number_in_chapter);
                        if ((verse_number >= VerseNumericUpDown.Minimum) && (verse_number <= VerseNumericUpDown.Maximum))
                        {
                            Verse verse = m_client.Book.GetVerseByVerseNumber(verse_number);
                            if (verse != null)
                            {
                                DisplayVerse(verse);
                            }
                        }
                    }
                    catch
                    {
                        // ---:--- or any non-numeric text
                        return;
                    }
                }
                UndoValueNavigationLabel.ForeColor = (m_navigation_undo_stack.Count > 0) ? Color.Yellow : SystemColors.Info;
                RedoValueNavigationLabel.ForeColor = (m_navigation_redo_stack.Count > 0) ? Color.Yellow : SystemColors.Info;
            }
        }
    }
    private void UndoGotoVerse()
    {
        if (m_client != null)
        {
            if (m_navigation_undo_stack.Count > 0)
            {
                Verse verse = GetCurrentVerse();
                if (verse != null)
                {
                    int current_verse_number = verse.Number;
                    m_navigation_redo_stack.Push(current_verse_number);
                    int verse_number = m_navigation_undo_stack.Pop();
                    string verse_address = m_client.Book.Verses[verse_number - 1].Address;
                    DisplayVerse(verse_address);
                }
            }
        }
    }
    private void RedoGotoVerse()
    {
        if (m_client != null)
        {
            if (m_navigation_redo_stack.Count > 0)
            {
                Verse verse = GetCurrentVerse();
                if (verse != null)
                {
                    int current_verse_number = verse.Number;
                    m_navigation_undo_stack.Push(current_verse_number);
                    int verse_number = m_navigation_redo_stack.Pop();
                    string verse_address = m_client.Book.Verses[verse_number - 1].Address;
                    DisplayVerse(verse_address);
                }
            }
        }
    }
    private void UndoValueNavigationLabel_Click(object sender, EventArgs e)
    {
        UndoGotoVerse();
    }
    private void RedoValueNavigationLabel_Click(object sender, EventArgs e)
    {
        RedoGotoVerse();
    }
    private void ValueNavigatorControls_Enter(object sender, EventArgs e)
    {
        SearchGroupBox_Leave(null, null);
        this.AcceptButton = null;
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 29. Numerology Value Drawings
    ///////////////////////////////////////////////////////////////////////////////
    private string m_current_drawing_type = "";
    private Bitmap m_bitmap = null;
    private int m_offscreen_left = 0;
    private int m_offscreen_top = 0;
    private int m_zoomed_width = 0;
    private int m_zoomed_height = 0;
    private Point m_mouse_down_point;
    private Point m_mouse_up_point;
    private void PictureBoxEx_Resize(object sender, EventArgs e)
    {
        m_zoomed_width = (int)((float)PictureBoxEx.Width * PictureBoxEx.ZoomFactor);
        m_zoomed_height = (int)((float)PictureBoxEx.Height * PictureBoxEx.ZoomFactor);
    }
    private void PictureBoxEx_MouseHover(object sender, EventArgs e)
    {
        //PictureBoxEx.Focus(); // to enable zooming
    }
    private void PictureBoxEx_MouseDown(object sender, MouseEventArgs e)
    {
        m_mouse_down_point = e.Location;
    }
    private void PictureBoxEx_MouseMove(object sender, MouseEventArgs e)
    {
        m_offscreen_left = PictureBoxEx.ClipRectangle.Left;
        m_offscreen_top = PictureBoxEx.ClipRectangle.Top;
    }
    private void PictureBoxEx_MouseUp(object sender, MouseEventArgs e)
    {
        m_mouse_up_point = e.Location;
        if (m_mouse_up_point == m_mouse_down_point)
        {
            int x = e.X - m_offscreen_left;
            int y = e.Y - m_offscreen_top;
        }
    }
    //private void PictureBoxEx_MouseWheel(object sender, EventArgs e)
    //{
    //    m_zoomed_width = (int)((float)PictureBoxEx.Width * PictureBoxEx.ZoomFactor);
    //    m_zoomed_height = (int)((float)PictureBoxEx.Height * PictureBoxEx.ZoomFactor);
    //    m_offscreen_left = PictureBoxEx.ClipRectangle.Left;
    //    m_offscreen_top = PictureBoxEx.ClipRectangle.Top;
    //}
    private void PictureBoxEx_MouseWheel(object sender, MouseEventArgs e)
    {
        if (PictureBoxEx.Visible)
        {
            if (PictureBoxEx.ZoomFactor <= (m_min_zoom_factor + m_error_margin))
            {
                PictureBoxEx.ZoomFactor = m_min_zoom_factor;
                ZoomOutLabel.Enabled = false;
                ZoomInLabel.Enabled = true;
            }
            else if (PictureBoxEx.ZoomFactor >= (m_max_zoom_factor - m_error_margin))
            {
                PictureBoxEx.ZoomFactor = m_max_zoom_factor;
                ZoomOutLabel.Enabled = true;
                ZoomInLabel.Enabled = false;
            }
            m_graphics_zoom_factor = PictureBoxEx.ZoomFactor;
            RedrawCurrentGraph();
        }
    }
    private DrawingShape m_drawing_shape = DrawingShape.SquareSpiral;
    private void ChangeDrawingShapeLabel_Click(object sender, EventArgs e)
    {
        if (ModifierKeys == Keys.Shift)
        {
            GotoPreviousShape();
        }
        else
        {
            GotoNextShape();
        }
        ToolTip.SetToolTip(ChangeDrawingShapeLabel, m_drawing_shape.ToString());

        // update graphs as we move between selections
        if (PictureBoxEx.Visible)
        {
            RedrawCurrentGraph();
        }
    }
    private void GotoNextShape()
    {
        switch (m_drawing_shape)
        {
            case DrawingShape.Spiral:
                {
                    m_drawing_shape = DrawingShape.SquareSpiral;
                    if (File.Exists("Images/squarespiral.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/squarespiral.png");
                    }
                }
                break;
            case DrawingShape.SquareSpiral:
                {
                    m_drawing_shape = DrawingShape.Square;
                    if (File.Exists("Images/square.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/square.png");
                    }
                }
                break;
            case DrawingShape.Square:
                {
                    m_drawing_shape = DrawingShape.HGoldenRect;
                    if (File.Exists("Images/hgoldenrect.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/hgoldenrect.png");
                    }
                }
                break;
            case DrawingShape.HGoldenRect:
                {
                    m_drawing_shape = DrawingShape.VGoldenRect;
                    if (File.Exists("Images/vgoldenrect.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/vgoldenrect.png");
                    }
                }
                break;
            case DrawingShape.VGoldenRect:
                {
                    m_drawing_shape = DrawingShape.Cube;
                    if (File.Exists("Images/cube.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/cube.png");
                    }
                }
                break;
            case DrawingShape.Cube:
                {
                    // short circuit to spiral directly for now 
                    m_drawing_shape = DrawingShape.Spiral;
                    if (File.Exists("Images/spiral.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/spiral.png");
                    }
                    //m_drawing_shape = DrawingShape.HGoldenCube;
                    //if (File.Exists("Images/hgoldencube.png"))
                    //{
                    //    ChangeDrawingShapeLabel.Image = new Bitmap("Images/hgoldencube.png");
                    //}
                }
                break;
            case DrawingShape.HGoldenCube:
                {
                    m_drawing_shape = DrawingShape.VGoldenCube;
                    if (File.Exists("Images/vgoldencube.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/vgoldencube.png");
                    }
                }
                break;
            case DrawingShape.VGoldenCube:
                {
                    m_drawing_shape = DrawingShape.Spiral;
                    if (File.Exists("Images/spiral.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/spiral.png");
                    }
                }
                break;
        }
    }
    private void GotoPreviousShape()
    {
        switch (m_drawing_shape)
        {
            case DrawingShape.Spiral:
                {
                    // short circuit to cube directly for now 
                    m_drawing_shape = DrawingShape.Cube;
                    if (File.Exists("Images/cube.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/cube.png");
                    }
                    //m_drawing_shape = DrawingShape.VGoldenCube;
                    //if (File.Exists("Images/vgoldencube.png"))
                    //{
                    //    ChangeDrawingShapeLabel.Image = new Bitmap("Images/vgoldencube.png");
                    //}
                }
                break;
            case DrawingShape.SquareSpiral:
                {
                    m_drawing_shape = DrawingShape.Spiral;
                    if (File.Exists("Images/spiral.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/spiral.png");
                    }
                }
                break;
            case DrawingShape.Square:
                {
                    m_drawing_shape = DrawingShape.SquareSpiral;
                    if (File.Exists("Images/squarespiral.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/squarespiral.png");
                    }
                }
                break;
            case DrawingShape.HGoldenRect:
                {
                    m_drawing_shape = DrawingShape.Square;
                    if (File.Exists("Images/square.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/square.png");
                    }
                }
                break;
            case DrawingShape.VGoldenRect:
                {
                    m_drawing_shape = DrawingShape.HGoldenRect;
                    if (File.Exists("Images/hgoldenrect.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/hgoldenrect.png");
                    }
                }
                break;
            case DrawingShape.Cube:
                {
                    m_drawing_shape = DrawingShape.VGoldenRect;
                    if (File.Exists("Images/vgoldenrect.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/vgoldenrect.png");
                    }
                }
                break;
            case DrawingShape.HGoldenCube:
                {
                    m_drawing_shape = DrawingShape.Cube;
                    if (File.Exists("Images/cube.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/cube.png");
                    }
                }
                break;
            case DrawingShape.VGoldenCube:
                {
                    m_drawing_shape = DrawingShape.HGoldenRect;
                    if (File.Exists("Images/hgoldencube.png"))
                    {
                        ChangeDrawingShapeLabel.Image = new Bitmap("Images/hgoldencube.png");
                    }
                }
                break;
        }
    }
    private void DrawLetterValuesLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (m_client.Selection != null)
                {
                    ShowDrawPictureBox();
                    m_current_drawing_type = "LetterValues";
                    HeaderLabel.Text = m_current_drawing_type;
                    HeaderLabel.Refresh();

                    m_bitmap = new Bitmap(PictureBoxEx.Width, PictureBoxEx.Height, PixelFormat.Format24bppRgb);
                    if (m_bitmap != null)
                    {
                        this.PictureBoxEx.Image = m_bitmap;

                        List<Verse> verses = m_client.Selection.Verses;
                        List<long> values = m_client.CalculateAllLetterValues(verses);
                        if (m_drawing_shape == DrawingShape.Cube)
                        {
                            // cubic-root
                            int width = (int)Math.Pow(values.Count + 1, 1.0 / 3.0);
                            int height = width;
                            int layers = width;

                            for (int n = 0; n < layers; n++)
                            {
                                int value_index = n * (width * height);
                                int value_count = (width * height);
                                Drawing.DrawValues(m_bitmap, values.GetRange(value_index, value_count), Color.Pink, m_drawing_shape);
                                this.Refresh();
                            }
                        }
                        else
                        {
                            Drawing.DrawValues(m_bitmap, values, Color.Pink, m_drawing_shape);
                            this.Refresh();
                        }
                    }
                }
            }
        }
        catch
        {
            HideDrawPictureBox();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DrawWordValuesLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (m_client.Selection != null)
                {
                    ShowDrawPictureBox();
                    m_current_drawing_type = "WordValues";
                    HeaderLabel.Text = m_current_drawing_type;
                    HeaderLabel.Refresh();

                    m_bitmap = new Bitmap(PictureBoxEx.Width, PictureBoxEx.Height, PixelFormat.Format24bppRgb);
                    if (m_bitmap != null)
                    {
                        this.PictureBoxEx.Image = m_bitmap;

                        List<Verse> verses = m_client.Selection.Verses;
                        List<long> values = m_client.CalculateAllWordValues(verses);
                        if (m_drawing_shape == DrawingShape.Cube)
                        {
                            // cubic-root
                            int width = (int)Math.Pow(values.Count + 1, 1.0 / 3.0);
                            int height = width;
                            int layers = width;

                            for (int n = 0; n < layers; n++)
                            {
                                int value_index = n * (width * height);
                                int value_count = (width * height);
                                Drawing.DrawValues(m_bitmap, values.GetRange(value_index, value_count), Color.Pink, m_drawing_shape);
                                this.Refresh();
                            }
                        }
                        else
                        {
                            Drawing.DrawValues(m_bitmap, values, Color.Pink, m_drawing_shape);
                            this.Refresh();
                        }
                    }
                }
            }
        }
        catch
        {
            HideDrawPictureBox();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DrawWordAllahLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (m_client.NumerologySystem != null)
                {
                    if (m_client.Selection != null)
                    {
                        ShowDrawPictureBox();
                        m_current_drawing_type = "WordAllah";

                        List<long> values = new List<long>();
                        m_bitmap = new Bitmap(PictureBoxEx.Width, PictureBoxEx.Height, PixelFormat.Format24bppRgb);
                        if (m_bitmap != null)
                        {
                            this.PictureBoxEx.Image = m_bitmap;

                            int count = 0;
                            int Allah_word_count = 0;

                            foreach (Verse verse in m_client.Selection.Verses)
                            {
                                foreach (Word word in verse.Words)
                                {
                                    // always simplify29 for Allah word comparison
                                    string simplified_text = word.Text.Simplify29();

                                    if (simplified_text == "الله")
                                    {
                                        values.Add(1L);
                                        Allah_word_count++;
                                    }
                                    else
                                    {
                                        values.Add(0L);
                                    }
                                    count++;
                                }
                            }

                            StringBuilder str = new StringBuilder();
                            str.Append("Allah words = " + Allah_word_count);
                            HeaderLabel.Text = str.ToString();
                            HeaderLabel.Refresh();

                            str.Length = 0;
                            str.AppendLine("Quran words\t= " + count);
                            str.AppendLine("Allah words\t= " + Allah_word_count);
                        }

                        Drawing.DrawValues(m_bitmap, values, Color.Pink, Color.LightPink, Color.Crimson, m_drawing_shape);
                        this.Refresh();
                    }
                }
            }
        }
        catch
        {
            HideDrawPictureBox();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DrawWordsWithAllahLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            if (m_client != null)
            {
                if (m_client.NumerologySystem != null)
                {
                    if (m_client.Selection != null)
                    {
                        ShowDrawPictureBox();
                        m_current_drawing_type = "WordsWithAllah";

                        List<long> values = new List<long>();
                        m_bitmap = new Bitmap(PictureBoxEx.Width, PictureBoxEx.Height, PixelFormat.Format24bppRgb);
                        if (m_bitmap != null)
                        {
                            this.PictureBoxEx.Image = m_bitmap;

                            int count = 0;
                            int Allah_word_count = 0;
                            int with_Allah_word_count = 0;
                            int with_lillah_word_count = 0;

                            foreach (Verse verse in m_client.Selection.Verses)
                            {
                                foreach (Word word in verse.Words)
                                {
                                    // always simplify29 for Allah word comparison
                                    string simplified_text = word.Text.Simplify29();

                                    if (simplified_text == "الله") // "Allah"
                                    {
                                        values.Add(1);
                                        Allah_word_count++;
                                    }
                                    else if ( // Prefix"Allah", Prefix"Allah"Suffix, "Allah"Suffix
                                              (simplified_text.Contains("الله")) &&        // +Allah+ words
                                              (!simplified_text.Contains("اللهو")) &&   // 1 word
                                              (!simplified_text.Contains("اللهب"))      // 1 word
                                            )
                                    {
                                        values.Add(2);
                                        with_Allah_word_count++;
                                    }
                                    else if ( // Prefix"Lillah", Prefix"Lillah"Suffix, "Lillah"Suffix
                                              (simplified_text.Contains("لله")) &&        // +LiAllah+
                                              (!simplified_text.Contains("اللهو")) &&   // 1 words
                                              (!simplified_text.Contains("اللهب")) &&   // 1 words
                                              (!simplified_text.Contains("ضلله")) &&    // 8 words
                                              (!simplified_text.Contains("ظلله")) &&    // 3 words
                                              (!simplified_text.Contains("كلله")) &&    // 2 words
                                              (!simplified_text.Contains("خلله")) &&    // 5 words
                                              (!simplified_text.Contains("سلله")) &&    // 2 words
                                              (!simplified_text.Contains("للهدي"))      // 1 word
                                            )
                                    {
                                        values.Add(3L);
                                        with_lillah_word_count++;
                                    }
                                    else
                                    {
                                        values.Add(0L);
                                    }
                                    count++;
                                }
                            }

                            StringBuilder str = new StringBuilder();
                            str.Append("Allah words = " + Allah_word_count + " | ");
                            str.Append("+Allah+ = " + with_Allah_word_count + " | ");
                            str.Append("+Lillah+ = " + with_lillah_word_count + " | ");
                            str.Append("Total = " + (Allah_word_count + with_Allah_word_count + with_lillah_word_count));
                            HeaderLabel.Text = str.ToString();
                            HeaderLabel.Refresh();

                            str.Length = 0;
                            str.AppendLine("Quran words\t= " + count);
                            str.AppendLine("Allah words\t= " + Allah_word_count);
                            str.AppendLine("Words with Allah\t= " + with_Allah_word_count + "\t  " + "No اللهو اللهب");
                            str.AppendLine("Words with Lillah\t= " + with_lillah_word_count + "\t  " + "No اللهو اللهب خلله كللة ضللة ظلله سللة للهدى");
                            str.AppendLine("All Allah words\t= " + (Allah_word_count + with_Allah_word_count + with_lillah_word_count));
                            str.AppendLine();
                            str.AppendLine("Excluding:");
                            str.AppendLine("2:16  الضللة");
                            str.AppendLine("2:175 الضللة");
                            str.AppendLine("4:44  الضللة");
                            str.AppendLine("4:12  كللة");
                            str.AppendLine("4:176 الكللة");
                            str.AppendLine("6:39  يضلله");
                            str.AppendLine("7:30  الضللة");
                            str.AppendLine("7:61  ضللة");
                            str.AppendLine("13:15 وظللهم");
                            str.AppendLine("16:36 الضللة");
                            str.AppendLine("16:48 ظلله");
                            str.AppendLine("17:91 خللها");
                            str.AppendLine("18:33 خللهما");
                            str.AppendLine("19:75 الضللة");
                            str.AppendLine("23:12 سللة");
                            str.AppendLine("24:43 خلله");
                            str.AppendLine("27:61 خللها");
                            str.AppendLine("30:48 خلله");
                            str.AppendLine("32:8  سللة");
                            str.AppendLine("62:11 اللهو");
                            str.AppendLine("76:14 ظللها");
                            str.AppendLine("77:31 اللهب");
                            str.AppendLine("92:12 للهدى");
                        }

                        Drawing.DrawValues(m_bitmap, values, Color.Pink, Color.LightPink, Color.Crimson, m_drawing_shape);
                        this.Refresh();
                    }
                }
            }
        }
        catch
        {
            HideDrawPictureBox();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DrawWordsWithAllahHelpLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            string filename = "Help/AllahWords.txt";
            if (File.Exists(filename))
            {
                System.Diagnostics.Process.Start("Notepad.exe", filename);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DrawPrimesLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            ShowDrawPictureBox();
            m_current_drawing_type = "Primes";
            HeaderLabel.Text = m_current_drawing_type;
            HeaderLabel.Refresh();

            m_bitmap = new Bitmap(PictureBoxEx.Width, PictureBoxEx.Height, PixelFormat.Format24bppRgb);
            if (m_bitmap != null)
            {
                this.PictureBoxEx.Image = m_bitmap;

                int width = (m_bitmap.Width > m_bitmap.Height) ? m_bitmap.Width : m_bitmap.Height;
                int height = width;
                int max = width * height;
                List<long> values = new List<long>(max);
                for (int i = 0; i < max; i++)
                {
                    if (Numbers.IsPrime(i + 1))
                    {
                        values.Add(1L);
                    }
                    else
                    {
                        values.Add(0L);
                    }
                }
                Drawing.DrawValues(m_bitmap, values, Color.LightGreen, Color.Black, Color.Black, m_drawing_shape);
                this.Refresh();
            }
        }
        catch
        {
            HideDrawPictureBox();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DrawAdditivePrimesLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            ShowDrawPictureBox();
            m_current_drawing_type = "AdditivePrimes";
            HeaderLabel.Text = m_current_drawing_type;
            HeaderLabel.Refresh();

            m_bitmap = new Bitmap(PictureBoxEx.Width, PictureBoxEx.Height, PixelFormat.Format24bppRgb);
            if (m_bitmap != null)
            {
                this.PictureBoxEx.Image = m_bitmap;

                int width = (m_bitmap.Width > m_bitmap.Height) ? m_bitmap.Width : m_bitmap.Height;
                int height = width;
                int max = width * height;
                List<long> values = new List<long>(max);
                for (int i = 0; i < max; i++)
                {
                    if (Numbers.IsAdditivePrime(i + 1))
                    {
                        values.Add(2L);
                    }
                    else
                    {
                        values.Add(0L);
                    }
                }
                Drawing.DrawValues(m_bitmap, values, Color.Black, Color.CornflowerBlue, Color.Black, m_drawing_shape);
                this.Refresh();
            }
        }
        catch
        {
            HideDrawPictureBox();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DrawPurePrimesLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            ShowDrawPictureBox();
            m_current_drawing_type = "PurePrimes";
            HeaderLabel.Text = m_current_drawing_type;
            HeaderLabel.Refresh();

            m_bitmap = new Bitmap(PictureBoxEx.Width, PictureBoxEx.Height, PixelFormat.Format24bppRgb);
            if (m_bitmap != null)
            {
                this.PictureBoxEx.Image = m_bitmap;

                int width = (m_bitmap.Width > m_bitmap.Height) ? m_bitmap.Width : m_bitmap.Height;
                int height = width;
                int max = width * height;
                List<long> values = new List<long>(max);
                for (int i = 0; i < max; i++)
                {
                    if (Numbers.IsPurePrime(i + 1))
                    {
                        values.Add(3L);
                    }
                    else
                    {
                        values.Add(0L);
                    }
                }
                Drawing.DrawValues(m_bitmap, values, Color.Black, Color.Black, Color.Violet, m_drawing_shape);
                this.Refresh();
            }
        }
        catch
        {
            HideDrawPictureBox();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void DrawAllPrimesLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            ShowDrawPictureBox();
            m_current_drawing_type = "AllPrimes";
            HeaderLabel.Text = m_current_drawing_type;
            HeaderLabel.Refresh();

            m_bitmap = new Bitmap(PictureBoxEx.Width, PictureBoxEx.Height, PixelFormat.Format24bppRgb);
            if (m_bitmap != null)
            {
                this.PictureBoxEx.Image = m_bitmap;

                int width = (m_bitmap.Width > m_bitmap.Height) ? m_bitmap.Width : m_bitmap.Height;
                int height = width;
                int max = width * height;
                List<long> values = new List<long>(max);
                for (int i = 0; i < max; i++)
                {
                    if (Numbers.IsPurePrime(i + 1))
                    {
                        values.Add(3);
                    }
                    else if (Numbers.IsAdditivePrime(i + 1))
                    {
                        values.Add(2);
                    }
                    else if (Numbers.IsPrime(i + 1))
                    {
                        values.Add(1L);
                    }
                    else
                    {
                        values.Add(0L);
                    }
                }
                Drawing.DrawValues(m_bitmap, values, Color.LightGreen, Color.CornflowerBlue, Color.Violet, m_drawing_shape);
                this.Refresh();
            }
        }
        catch
        {
            HideDrawPictureBox();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void GenerateAllPrimeDrawingsLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            m_current_drawing_type = "GenerateAllPrimeDrawings";
            HeaderLabel.Text = m_current_drawing_type + " needs few minutes to complete";
            HeaderLabel.Refresh();

            Drawing.GenerateAndSaveAllPrimeDrawings(Color.LightGreen, Color.CornflowerBlue, Color.Violet);
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void ShowDrawPictureBox()
    {
        PictureBoxEx.Visible = true;
        PictureBoxEx.BringToFront();
    }
    private void HideDrawPictureBox()
    {
        PictureBoxEx.Visible = false;

        ZoomInLabel.Enabled = (m_text_zoom_factor <= (m_max_zoom_factor - m_zoom_factor_increment + m_error_margin));
        ZoomOutLabel.Enabled = (m_text_zoom_factor >= (m_min_zoom_factor + m_zoom_factor_increment - m_error_margin));
    }
    private void RedrawCurrentGraph()
    {
        if (PictureBoxEx.Visible)
        {
            switch (m_current_drawing_type)
            {
                case "LetterValues":
                    DrawLetterValuesLabel_Click(null, null);
                    break;
                case "WordValues":
                    DrawWordValuesLabel_Click(null, null);
                    break;
                case "WordAllah":
                    DrawWordAllahLabel_Click(null, null);
                    break;
                case "WordsWithAllah":
                    DrawWordsWithAllahLabel_Click(null, null);
                    break;
                case "Primes":
                    DrawPrimesLabel_Click(null, null);
                    break;
                case "AdditivePrimes":
                    DrawAdditivePrimesLabel_Click(null, null);
                    break;
                case "PurePrimes":
                    DrawPurePrimesLabel_Click(null, null);
                    break;
                case "AllPrimes":
                    DrawAllPrimesLabel_Click(null, null);
                    break;
                default:
                    break;
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 30. Statistics
    ///////////////////////////////////////////////////////////////////////////////
    private void LetterFrequencySumLabel_TextChanged(object sender, EventArgs e)
    {
        Control control = (sender as Control);
        if (control != null)
        {
            control.Width = (control.Text.Length <= 4) ? 48 : 64;
        }
    }
    private void LetterFrequencySumLabel_Click(object sender, EventArgs e)
    {
        // Ctrl+Click factorizes number
        if (ModifierKeys == Keys.Control)
        {
            Control control = (sender as Control);
            if (control != null)
            {
                if (!string.IsNullOrEmpty(control.Text))
                {
                    try
                    {
                        long value = long.Parse(control.Text);
                        if (sender == LetterCountValueLabel)
                        {
                            FactorizeValue(value, "ULetters");
                        }
                        else
                        {
                            FactorizeValue(value, "FreqSum");
                        }
                    }
                    catch
                    {
                        // log exception
                    }
                }
            }
        }
    }
    private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        if (sender is ListView)
        {
            ListView listview = sender as ListView;
            try
            {
                if (m_client != null)
                {
                    if (listview == LetterStatisticsListView)
                    {
                        m_client.SortLetterStatistics((StatisticSortMethod)e.Column);
                        DisplayLetterStatistics();
                    }
                    else if (listview == FindByFrequencyListView)
                    {
                        m_client.SortPhraseLetterStatistics((StatisticSortMethod)e.Column);
                        DisplayPhraseLetterStatistics();
                    }
                    else
                    {
                        // do nothing
                    }

                    // display sort marker
                    string sort_marker = (LetterStatistic.SortOrder == StatisticSortOrder.Ascending) ? "▼" : "▲";
                    // empty all sort markers
                    foreach (ColumnHeader column in listview.Columns)
                    {
                        if (column.Text.EndsWith("▲"))
                        {
                            column.Text = column.Text.Replace("▲", " ");
                        }
                        else if (column.Text.EndsWith("▼"))
                        {
                            column.Text = column.Text.Replace("▼", " ");
                        }
                    }
                    // mark clicked column
                    listview.Columns[e.Column].Text = listview.Columns[e.Column].Text.Replace("  ", " " + sort_marker);
                }
            }
            catch
            {
                // log exception
            }
        }
    }
    private void ListView_MouseDown(object sender, MouseEventArgs e)
    {
        // Ctrl+Click factorizes number
        if (ModifierKeys == Keys.Control)
        {
            ListView listview = (sender as ListView);
            if (listview != null)
            {
                if (listview.Items.Count > 1)
                {
                    //col_height is the height of the listview column header
                    int col_height = listview.Items[1].Bounds.Height + 6;

                    //items_height and items_width are used for checking if the mouse click was outside all items.
                    int items_height = 0;
                    for (int item_count = 0; item_count < listview.Items.Count; item_count++)
                    {
                        items_height += listview.Items[item_count].Bounds.Height;
                    }
                    items_height += col_height;

                    int items_width = 0;
                    for (int col_count = 0; col_count < listview.Columns.Count; col_count++)
                    {
                        items_width += listview.Columns[col_count].Width + 1;
                    }

                    //if the mouse click was outside all items and columns, just return.
                    if (e.X >= items_width || e.Y >= items_height)
                    {
                        return;
                    }

                    //i and j are the index of the subitem.
                    int i = 0, j = 0;
                    int width_len = listview.Columns[0].Width;
                    int height_len = col_height + listview.Items[0].Bounds.Height;
                    while (e.X > width_len)
                    {
                        i++;
                        width_len += listview.Columns[i].Width + 1;
                    }

                    while (e.Y > height_len)
                    {
                        j++;
                        height_len += listview.Items[j].Bounds.Height;
                    }

                    if (i == 2)
                    {
                        try
                        {
                            char letter = (listview.Items[j + listview.TopItem.Index].SubItems[1].Text)[0];
                            long value = long.Parse(listview.Items[j + listview.TopItem.Index].SubItems[2].Text);
                            FactorizeValue(value, letter.ToString() + " Freq");
                        }
                        catch
                        {
                            // log exception
                        }
                    }
                }
            }
        }
    }

    private void CalculateLetterStatistics()
    {
        if (m_client != null)
        {
            // update the letter frequency table
            if (!String.IsNullOrEmpty(m_current_text))
            {
                if (m_current_text.Length >= 0) // if some text is selected
                {
                    m_client.CalculateLetterStatistics(m_current_text);
                }
            }
        }
    }
    private void DisplayLetterStatistics()
    {
        if (m_client != null)
        {
            if (m_client.LetterStatistics != null)
            {
                LetterCountValueLabel.Text = 0.ToString();
                LetterCountValueLabel.ForeColor = GetNumberTypeColor(0);
                LetterCountValueLabel.Refresh();
                LetterFrequencySumValueLabel.Text = 0.ToString();
                LetterFrequencySumValueLabel.ForeColor = GetNumberTypeColor(0);
                LetterFrequencySumValueLabel.Refresh();

                int count = m_client.LetterStatistics.Count;
                int frequency_sum = 0;
                LetterStatisticsListView.Items.Clear();
                for (int i = 0; i < count; i++)
                {
                    string[] item_parts = new string[3];
                    item_parts[0] = m_client.LetterStatistics[i].Order.ToString();
                    item_parts[1] = m_client.LetterStatistics[i].Letter.ToString();
                    item_parts[2] = m_client.LetterStatistics[i].Frequency.ToString();
                    LetterStatisticsListView.Items.Add(new ListViewItem(item_parts, i));
                    frequency_sum += m_client.LetterStatistics[i].Frequency;
                }
                LetterCountValueLabel.Text = count.ToString();
                LetterCountValueLabel.ForeColor = GetNumberTypeColor(count);
                LetterCountValueLabel.Refresh();
                LetterFrequencySumValueLabel.Text = frequency_sum.ToString();
                LetterFrequencySumValueLabel.ForeColor = GetNumberTypeColor(frequency_sum);
                LetterFrequencySumValueLabel.Refresh();

                // reset sort-markers
                foreach (ColumnHeader column in LetterStatisticsListView.Columns)
                {
                    if (column.Text.EndsWith("▲"))
                    {
                        column.Text = column.Text.Replace("▲", " ");
                    }
                    else if (column.Text.EndsWith("▼"))
                    {
                        column.Text = column.Text.Replace("▼", " ");
                    }
                }
                LetterStatisticsListView.Columns[0].Text = LetterStatisticsListView.Columns[0].Text.Replace("  ", " ▲");
                LetterStatisticsListView.Refresh();
            }
        }
    }
    private void CalculatePhraseLetterStatistics()
    {
        if (m_client != null)
        {
            FindByFrequencySumNumericUpDown.Value = 0;
            FindByFrequencySumNumericUpDown.ForeColor = GetNumberTypeColor(0);
            FindByFrequencySumNumericUpDown.Refresh();

            if (!String.IsNullOrEmpty(m_current_text))
            {
                if (m_current_text.Length >= 0) // if some text is selected
                {
                    string phrase = FindByFrequencyPhraseTextBox.Text;

                    int letter_frequency_sum = m_client.CalculatePhraseLetterStatistics(m_current_text, phrase, m_frequency_sum_type);
                    if (letter_frequency_sum >= 0)
                    {
                        FindByFrequencySumNumericUpDown.Value = letter_frequency_sum;
                    }
                }
            }
        }
    }
    private void DisplayPhraseLetterStatistics()
    {
        if (m_client != null)
        {
            if (m_client.PhraseLetterStatistics != null)
            {
                FindByFrequencyListView.Items.Clear();
                for (int i = 0; i < m_client.PhraseLetterStatistics.Count; i++)
                {
                    string[] item_parts = new string[3];
                    item_parts[0] = m_client.PhraseLetterStatistics[i].Order.ToString();
                    item_parts[1] = m_client.PhraseLetterStatistics[i].Letter.ToString();
                    item_parts[2] = m_client.PhraseLetterStatistics[i].Frequency.ToString();
                    FindByFrequencyListView.Items.Add(new ListViewItem(item_parts, i));
                }
                FindByFrequencyListView.Refresh();

                // reset sort-markers
                foreach (ColumnHeader column in FindByFrequencyListView.Columns)
                {
                    if (column.Text.EndsWith("▲"))
                    {
                        column.Text = column.Text.Replace("▲", " ");
                    }
                    else if (column.Text.EndsWith("▼"))
                    {
                        column.Text = column.Text.Replace("▼", " ");
                    }
                }
                FindByFrequencyListView.Columns[0].Text = FindByFrequencyListView.Columns[0].Text.Replace("  ", " ▲");
                FindByFrequencyListView.Refresh();
            }
        }
    }
    private void SaveTextStatisticsButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            SaveTextStatistics();

            NumerologySystemComboBox.Focus();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void SaveTextStatistics()
    {
        if (m_client != null)
        {
            if (m_client.NumerologySystem != null)
            {
                string filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + "_" + m_client.NumerologySystem.Name + ".txt";

                StringBuilder str = new StringBuilder();
                str.AppendLine(m_current_text);
                str.AppendLine("----------------------------------------");
                str.AppendLine("Verses\t=\t" + VersesTextBox.Text);
                str.AppendLine("Words\t=\t" + WordsTextBox.Text);
                str.AppendLine("Letters\t=\t" + LettersTextBox.Text);
                str.AppendLine("Value\t=\t" + ValueTextBox.Text + ((m_radix == DEFAULT_RADIX) ? "" : " in base " + m_radix.ToString()));
                str.AppendLine("----------------------------------------");

                m_client.SaveValueCalculation(filename, str.ToString());
            }
        }
    }
    private void SaveLetterStatisticsButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            SaveLetterStatistics();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void SaveLetterStatistics()
    {
        string text = m_current_text;
        if (!String.IsNullOrEmpty(text))
        {
            if (m_client != null)
            {
                string filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + "_" + ".txt";
                if (m_client.NumerologySystem != null)
                {
                    filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + "_" + m_client.NumerologySystem.Name + ".txt";
                }

                m_client.SaveLetterStatistics(filename, text);
            }
        }
    }
    private void SavePhraseLetterStatisticsButton_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            SavePhraseLetterStatistics();
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void SavePhraseLetterStatistics()
    {
        string text = m_current_text;
        if (!String.IsNullOrEmpty(text))
        {
            string phrase = FindByFrequencyPhraseTextBox.Text;
            if (!String.IsNullOrEmpty(phrase))
            {
                if (m_client != null)
                {
                    string filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + "_" + ".txt";
                    if (m_client.NumerologySystem != null)
                    {
                        filename = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + "_" + m_client.NumerologySystem.Name + ".txt";
                    }

                    m_client.SavePhraseLetterStatistics(filename, text, phrase);
                }
            }
        }
    }

    private void StatisticsControls_Enter(object sender, EventArgs e)
    {
        SearchGroupBox_Leave(null, null);
        this.AcceptButton = null;
    }
    private void StatisticsControls_Click(object sender, EventArgs e)
    {
        // Ctrl+Click factorizes number
        if (ModifierKeys == Keys.Control)
        {
            TextBox control = (sender as TextBox);
            if (control != null)
            {
                if (
                    (control != ValueTextBox)
                    &&
                    (control != PrimeFactorsTextBox)
                    //&&
                    //(control != DigitSumTextBox)
                    //&&
                    //(control != NthPrimeTextBox)
                    //&&
                    //(control != NthAdditivePrimeTextBox)
                    //&&
                    //(control != NthPurePrimeTextBox)
                   )
                {
                    long value = 0L;
                    try
                    {
                        string text = control.Text;
                        if (!String.IsNullOrEmpty(text))
                        {
                            if (control.Name.StartsWith("Decimal"))
                            {
                                value = Radix.Decode(text, 10L);
                            }
                            else if (text.StartsWith("Ʃ "))
                            {
                                text = text.Substring(2, text.Length - 2);
                                value = Radix.Decode(text, 10L);
                            }
                            else
                            {
                                value = Radix.Decode(text, m_radix);
                            }
                        }
                    }
                    catch
                    {
                        value = -1L; // error
                    }

                    FactorizeValue(value, "Number");
                }
            }
        }
    }
    private void StatusControls_Enter(object sender, EventArgs e)
    {
        SearchGroupBox_Leave(null, null);
        this.AcceptButton = null;
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
    #region 31. Help
    ///////////////////////////////////////////////////////////////////////////////
    private int m_help_message_index = 0;
    private void HelpMessageLabel_Click(object sender, EventArgs e)
    {
        int maximum = m_client.HelpMessages.Count - 1;
        if (ModifierKeys == Keys.Shift)
        {
            m_help_message_index--;
            if (m_help_message_index < 0) m_help_message_index = maximum;
        }
        else
        {
            m_help_message_index++;
            if (m_help_message_index > maximum) m_help_message_index = 0;
        }

        if (m_client.HelpMessages.Count > m_help_message_index)
        {
            HelpMessageLabel.Text = m_client.HelpMessages[m_help_message_index];
        }
    }
    private void LinkLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            Control control = (sender as Control);
            if (control != null)
            {
                if (control.Tag != null)
                {
                    if (!String.IsNullOrEmpty(control.Tag.ToString()))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(control.Tag.ToString());
                        }
                        catch (Exception ex)
                        {
                            while (ex != null)
                            {
                                //Console.WriteLine(ex.Message);
                                MessageBox.Show(ex.Message, Application.ProductName);
                                ex = ex.InnerException;
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void EncryptedQuranLinkLabel_Click(object sender, EventArgs e)
    {
        this.Cursor = Cursors.WaitCursor;
        try
        {
            string pdf = "EncryptedQuran.pdf";
            string filename = Globals.HELP_FOLDER + "/" + pdf;
            if (!File.Exists(filename))
            {
                DownloadFile("http://heliwave.com/" + pdf, filename);
            }
            if (File.Exists(filename))
            {
                System.Diagnostics.Process.Start(Application.StartupPath + "/" + filename);
            }
        }
        catch (Exception ex)
        {
            while (ex != null)
            {
                //Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, Application.ProductName);
                ex = ex.InnerException;
            }
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
    private void VersionLabel_Click(object sender, EventArgs e)
    {
        if (m_about_box != null)
        {
            m_about_box.ShowDialog(this);
        }
    }
    ///////////////////////////////////////////////////////////////////////////////
    #endregion
}
