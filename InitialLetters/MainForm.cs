using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace InitialLetters
{
    public partial class MainForm : Form
    {
        private List<Entry> m_dictionary = null;
        private DateTime m_start_time = DateTime.Now;
        private List<Sentence> m_found_sentences = new List<Sentence>();
        private bool m_display_sentences_live = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Letters.Test();
        }
        private void MainForm_Shown(object sender, EventArgs e)
        {
            string filename = "Dictionary/QuranWords.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    StatusLabel.Text = "Compiling dictionary ...";
                    ProgressBar.Value = 0;
                    ProgressBar.Maximum = (int)reader.BaseStream.Length;
                    ListView_Resize(sender, e);
                    try
                    {
                        int count = 0;
                        string line = null;
                        Hashtable hashtable = new Hashtable();
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.ToLower();

                            Letters letters = new Letters(line);
                            if (!hashtable.ContainsKey(letters))
                            {
                                Sentence sentence = new Sentence();
                                sentence.Add(line);
                                hashtable.Add(letters, sentence);
                            }
                            else
                            {
                                Sentence sentence = (Sentence)hashtable[letters];
                                if (!sentence.Contains(line))
                                    sentence.Add(line);
                            }
                            count++;
                            ProgressBar.Increment((line.Length + 2) * 2); // the *2 is to deal with unicode characters
                            Application.DoEvents();
                        }

                        // convert the hashtable into a list for generating sentences
                        m_dictionary = new List<Entry>();
                        foreach (DictionaryEntry entry in hashtable)
                        {
                            m_dictionary.Add(new Entry((Letters)entry.Key, (Sentence)entry.Value));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Dictionary: " + ex.Message);
                    }

                    StatusLabel.Text = "Dictionary is compiled.";
                    ListView.Enabled = true;
                    LettersTextBox.Enabled = true;
                    LettersTextBox.Focus();
                    UniqueLettersToolStripMenuItem_Click(sender, e);

                    m_display_sentences_live = DisplaySentencesLiveCheckBox.Checked;
                }
            }
        }
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                GenerateSentences();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                SentenceGenerator.Cancel = true;
            }
        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void ListView_Resize(object sender, EventArgs e)
        {
            // trial and error shows that we must make the column
            // header four pixels narrower than the containing
            // listview in order to avoid a scrollbar.
            ListView.Columns[0].Width = ListView.Width - 4;

            // if the listview is big enough to show all the items, then make sure
            // the first item is at the top.  This works around behavior (which I assume is 
            // a bug in C# or .NET or something) whereby 
            // some blank lines appear before the first item

            if (ListView.Items.Count > 0
                &&
                ListView.TopItem != null
                &&
                ListView.TopItem.Index == 0)
                ListView.EnsureVisible(0);
        }
        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            Clipboard.Clear();

            string selected_text = "";
            ListView me = (ListView)sender;
            foreach (ListViewItem it in me.SelectedItems)
            {
                if (selected_text.Length > 0)
                    selected_text += Environment.NewLine;
                selected_text += it.Text;
            }
            // Under some circumstances -- probably a bug in my code somewhere --
            // we can get blank lines in the listview.  And if you click on one, since it
            // has no text, selected_text will be blank; _and_, apparantly, calling
            // Clipboard.set_text with an empty string provokes an access violation ...
            // so avoid that AV.
            if (selected_text.Length > 0)
                Clipboard.SetText(selected_text);
        }
        private void ListView_SortColumnClick(object sender, ColumnClickEventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                ListView.Sorting = SortOrder.Ascending;
                ListView.Sorting = SortOrder.None;
                ListView.Columns[0].Text = "Generated Sentences";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void LettersTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // stop annoying beep
            if ((e.KeyChar == (char)Keys.Enter) || (e.KeyChar == (char)Keys.Escape))
            {
                e.Handled = true;
            }
            // allow Ctrl+A to SelectAll
            if ((ModifierKeys == Keys.Control) && (e.KeyChar == (char)Keys.A))
            {
                (sender as TextBoxBase).SelectAll();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed_time = DateTime.Now - m_start_time;
            ElapsedTimeLabel.Text =
                  elapsed_time.Hours.ToString("00") + ":"
                + elapsed_time.Minutes.ToString("00") + ":"
                + elapsed_time.Seconds.ToString("00");
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog.FileName = LettersTextBox.Text + ".txt";
            SaveFileDialog.InitialDirectory = Application.ExecutablePath;
            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(SaveFileDialog.OpenFile()))
                {
                    sw.WriteLine("{0} sentences of '{1}'",
                        ListView.Items.Count, LettersTextBox.Text);
                    sw.WriteLine("-----------------------");
                    foreach (ListViewItem it in ListView.Items)
                    {
                        sw.WriteLine(it.Text);
                    }
                }
            }
        }
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void UniqueLettersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LettersTextBox.Text = "ق ص ر ع س ن م ل ك ي ط ح ه ا";
            UniqueLettersToolStripMenuItem.Checked = true;
            UniqueWordsToolStripMenuItem.Checked = false;
            AllWordsToolStripMenuItem.Checked = false;
        }
        private void UniqueWordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LettersTextBox.Text = "الم المص الر المر كهيعص طه طسم طس يس ص حم عسق ق ن";
            UniqueLettersToolStripMenuItem.Checked = false;
            UniqueWordsToolStripMenuItem.Checked = true;
            AllWordsToolStripMenuItem.Checked = false;
        }
        private void AllWordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LettersTextBox.Text = "الم الم المص الر الر الر المر الر الر كهيعص طه طسم طس طسم الم الم الم الم يس ص حم حم عسق حم حم حم حم حم ق ن";
            UniqueLettersToolStripMenuItem.Checked = false;
            UniqueWordsToolStripMenuItem.Checked = false;
            AllWordsToolStripMenuItem.Checked = true;
        }
        private void RunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GenerateSentences();
        }
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(String.Format(
                "Initial Letters - v1.0" + "\r\n" + "\r\n"
                + "Based on https://github.com/offby1/anagrams" + "\r\n"
                + "©2007 OffBy1 - ©2012 Ali Adams" + "\r\n" + "\r\n"
                + "www.qurancode.com" + "\r\n"
                ),
                Application.ProductName);
        }
        private void DisplaySentencesLiveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            m_display_sentences_live = DisplaySentencesLiveCheckBox.Checked;
        }

        private void GenerateSentences()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                LettersTextBox.Enabled = false;
                Letters letters = new Letters(LettersTextBox.Text);
                ListView.Items.Clear();
                FileToolStripMenuItem.Enabled = false;
                TypeToolStripMenuItem.Enabled = false;
                m_start_time = DateTime.Now;
                ElapsedTimeLabel.Text = "00:00:00";
                Timer.Enabled = true;
                ProgressBar.Value = 0;
                m_found_sentences.Clear();
                ListView.Columns[0].Text = "Generated Sentences";

                SentenceGenerator.Run(letters, m_dictionary, 0,
                    // end of main loop
                    delegate()
                    {
                        ProgressBar.PerformStep();
                        Application.DoEvents();
                    },

                    // done pruning
                    delegate(uint recursion_level, List<Entry> pruned_dictionary)
                    {
                        if (recursion_level == 0)
                        {
                            ProgressBar.Maximum = pruned_dictionary.Count;
                            Application.DoEvents();
                        }
                    },

                    // found a top-level sentence
                    delegate(Sentence words)
                    {
                        m_found_sentences.Add(words);
                        StatusLabel.Text = m_found_sentences.Count.ToString() + " Sentences found.";

                        if (m_display_sentences_live)
                        {
                            StringBuilder str = new StringBuilder();
                            if (words.Count > 0)
                            {
                                foreach (string word in words)
                                {
                                    str.Append(word + " ");
                                }
                                if (str.Length > 1)
                                {
                                    str.Remove(str.Length - 1, 1); // remove last space
                                }
                            }
                            ListView.Items.Add(str.ToString());
                            ListView.EnsureVisible(ListView.Items.Count - 1);
                        }

                        if ((m_found_sentences.Count % 1000) == 0)
                        {
                            Application.DoEvents();
                        }
                    }
                );

                Timer.Enabled = false;
                StatusLabel.Text = String.Format("Done. {0} Sentences", m_found_sentences.Count);
                if (ListView.Items.Count > 0)
                {
                    ListView.EnsureVisible(0);
                }
                LettersTextBox.Enabled = true;
                LettersTextBox.Focus();
                ListView.Columns[0].Text = "Sort Sentences";
                FileToolStripMenuItem.Enabled = true;
                TypeToolStripMenuItem.Enabled = true;

                if (m_found_sentences != null)
                {
                    if (!m_display_sentences_live)
                    {
                        ListView.Items.Clear();
                        foreach (Sentence sentence in m_found_sentences)
                        {
                            ListView.Items.Add(sentence.ToString());
                        }
                    }

                    if (m_found_sentences.Count > 0)
                    {
                        SaveFoundSentences();
                    }
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        private void SaveFoundSentences()
        {
            string filename = "Dictionary/Sentences.txt";
            SaveFileDialog.InitialDirectory = Application.ExecutablePath;
            try
            {
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    writer.WriteLine("--------------------------------------------------------------------");
                    writer.WriteLine("{0} sentences from '{1}'", m_found_sentences.Count, LettersTextBox.Text);
                    writer.WriteLine("--------------------------------------------------------------------");
                    foreach (Sentence sentence in m_found_sentences)
                    {
                        writer.WriteLine(sentence.ToString());
                    }
                    writer.WriteLine("--------------------------------------------------------------------");
                }

                System.Diagnostics.Process.Start("Notepad.exe", filename);
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }
        }
    }
}
