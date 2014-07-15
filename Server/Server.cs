using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Model;

public static class Server
{
    public const string DEFAULT_EMLAAEI_TEXT = "ar.emlaaei";
    public const string DEFAULT_NEW_TRANSLATION = "en.qarai";
    public const string DEFAULT_OLD_TRANSLATION = "en.pickthall";
    public const string DEFAULT_WORD_TRANSLATION = "en.wordbyword";
    public const string DEFAULT_TRANSLITERATION = "en.transliteration";
    public const string DEFAULT_TAFSEER = "English/Al-Mizan";
    public const string DEFAULT_RECITATION = "Alafasy_64kbps";

    static Server()
    {
        if (!Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            Directory.CreateDirectory(Globals.STATISTICS_FOLDER);
        }

        if (!Directory.Exists(Globals.RULES_FOLDER))
        {
            Directory.CreateDirectory(Globals.RULES_FOLDER);
        }

        if (!Directory.Exists(Globals.VALUES_FOLDER))
        {
            Directory.CreateDirectory(Globals.VALUES_FOLDER);
        }

        if (!Directory.Exists(Globals.HELP_FOLDER))
        {
            Directory.CreateDirectory(Globals.HELP_FOLDER);
        }

        // load numerology system names
        if (Globals.EDITION == Edition.Lite)
        {
            LoadFavoriteNumerologySystemName();
        }
        else
        {
            LoadNumerologySystemNames();
        }

        // load help messages
        LoadHelpMessages();
    }

    private static SimplificationSystem s_simplification_system = null;
    public static Book BuildSimplifiedBook(string numerology_system_name)
    {
        Book book = null;

        string[] parts = numerology_system_name.Split('_');
        if (parts.Length == 3)
        {
            string text_mode = parts[0];

            string[] verse_stopmarks = DataAccess.LoadVerseStopmarks(); // (c)2014 Hadi Al-Thehabi
            string[] verse_texts = DataAccess.LoadVerseTexts(text_mode);    // load Original text

            // load simplification rules
            LoadSimplificationSystem(text_mode);

            if (text_mode.Contains("Shadda"))
            {
                for (int l = 0; l < verse_texts.Length; l++)
                {
                    StringBuilder str = new StringBuilder(verse_texts[l]);
                    for (int i = 1; i < str.Length; i++)
                    {
                        if (str[i] == 'ّ') // replace shedda with previous letter
                        {
                            str[i] = str[i - 1];
                        }
                    }
                    verse_texts[l] = str.ToString();
                }
            }

            List<Verse> verses = new List<Verse>(Verse.MAX_NUMBER);
            for (int i = 0; i < verse_texts.Length; i++)
            {
                string simplified_text = s_simplification_system.Simplify(verse_texts[i]);

                // set stopmark for verse as complied by Hadi Al-Thehabi
                Stopmark verse_stopmark = Stopmark.None;
                switch (verse_stopmarks[i])
                {
                    case "": // treat empty line as Meem
                        verse_stopmark = Stopmark.MustStop;
                        break;
                    case "ۙ": // Laaa
                        verse_stopmark = Stopmark.MustContinue;
                        break;
                    case "ۖ": // Sala
                        verse_stopmark = Stopmark.ShouldContinue;
                        break;
                    case "ۚ": // Jeem
                        verse_stopmark = Stopmark.CanStop;
                        break;
                    case "ۛ": // Dots
                        verse_stopmark = Stopmark.CanStopAtOneOnly;
                        break;
                    case "ۗ": // Qala
                        verse_stopmark = Stopmark.ShouldStop;
                        break;
                    case "ۜ": // Seen
                        verse_stopmark = Stopmark.MustPause;
                        break;
                    case "ۘ": // Meem
                        verse_stopmark = Stopmark.MustStop;
                        break;
                    default:
                        verse_stopmark = Stopmark.MustStop;
                        break;
                }
                Verse verse = new Verse(i + 1, simplified_text, verse_stopmark);

                // update word stopmarks
                verse.BuildWords(text_mode, verse_texts[i]);
                verses.Add(verse);
            }

            book = new Book(text_mode, verses);
            if (book != null)
            {
                DataAccess.LoadRecitationInfos(book);
                DataAccess.LoadTranslationInfos(book);
                DataAccess.LoadTranslations(book);
                DataAccess.LoadWordMeanings(book);
                DataAccess.LoadRootWords(book);
                DataAccess.LoadWordRoots(book);
                if ((Globals.EDITION == Edition.Grammar) || (Globals.EDITION == Edition.Research))
                {
                    DataAccess.LoadWordParts(book);
                }

                // load numerology system
                LoadNumerologySystem(numerology_system_name, book, book.Text);

                // must be done after loading word info (because they assume 77878 words)
                if (text_mode.Contains("Waw"))
                {
                    UpdateBookBySplittingWawWordsIntoTwo(book);
                }
            }
        }

        return book;
    }
    private static void UpdateBookBySplittingWawWordsIntoTwo(Book book)
    {
        if (book != null)
        {
            if (File.Exists(Globals.DATA_FOLDER + "/" + "waw-words.txt"))
            {
                List<string> waw_words = DataAccess.LoadLines(Globals.DATA_FOLDER + "/" + "waw-words.txt");
                if (waw_words != null)
                {
                    foreach (Verse verse in book.Verses)
                    {
                        StringBuilder str = new StringBuilder();
                        if (verse.Words.Count > 0)
                        {
                            for (int w = 0; w < verse.Words.Count; w++)
                            {
                                if (verse.Words[w].Text.StartsWith("و"))
                                {
                                    if (!waw_words.Contains(verse.Words[w].Text))
                                    {
                                        str.Append(verse.Words[w].Text.Insert(1, " ") + " ");
                                    }
                                    else // cases where sometimes waw-word and sometimes not
                                    {
                                        // و رد الله الذين كفروا versus ولما ورد ماء مدين
                                        if ((verse.Words[w].Text == "ورد") && (verse.Words[w + 1].Text == "الله"))
                                        {
                                            str.Append(verse.Words[w].Text.Insert(1, " ") + " ");
                                        }
                                        else
                                        {
                                            str.Append(verse.Words[w].Text + " ");
                                        }
                                    }
                                }
                                else
                                {
                                    str.Append(verse.Words[w].Text + " ");
                                }
                            }
                            if (str.Length > 1)
                            {
                                str.Remove(str.Length - 1, 1); // " "
                            }
                        }

                        // don't create new Verse, create new Words
                        verse.BuildWords(book.Title, str.ToString());
                    }
                }
            }
        }
    }
    private static void LoadSimplificationSystem(string title)
    {
        if (!String.IsNullOrEmpty(title))
        {
            if (title.Contains("Waw"))
            {
                int index = title.IndexOf("Waw");
                title = title.Remove(index, "Waw".Length);
            }
            // also (not else :)
            if (title.Contains("Shadda"))
            {
                int index = title.IndexOf("Shadda");
                title = title.Remove(index, "Shadda".Length);
            }

            s_simplification_system = new SimplificationSystem(title);
            if (s_simplification_system != null)
            {
                string filename = Globals.RULES_FOLDER + "/" + title + ".txt";
                if (!File.Exists(filename))
                {
                    string[] parts = NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM.Split('_');
                    if (parts.Length == 3)
                    {
                        filename = Globals.RULES_FOLDER + "/" + parts[0] + ".txt";
                    }
                }

                if (File.Exists(filename))
                {
                    List<string> lines = DataAccess.LoadLines(filename);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length == 2)
                        {
                            SimplificationRule rule = new SimplificationRule(parts[0], parts[1]);
                            s_simplification_system.Rules.Add(rule);
                        }
                        else
                        {
                            throw new Exception(filename + " file format must be:\r\n\tText TAB Replacement");
                        }
                    }
                }
            }
        }
    }

    private static List<string> s_numerology_system_names = new List<string>();
    public static List<string> NumerologySystemNames
    {
        get { return s_numerology_system_names; }
    }
    private static void LoadNumerologySystemNames()
    {
        s_numerology_system_names.Clear();

        string path = Globals.VALUES_FOLDER + "/" + "Offline";
        DirectoryInfo folder = new DirectoryInfo(path);
        if (folder != null)
        {
            FileInfo[] files = folder.GetFiles("*.txt");
            if ((files != null) && (files.Length > 0))
            {
                foreach (FileInfo file in files)
                {
                    try
                    {
                        string numerology_system_name = file.Name.Remove(file.Name.Length - 4, 4);
                        string[] parts = numerology_system_name.Split('_');
                        if (parts.Length == 3)
                        {
                            s_numerology_system_names.Add(numerology_system_name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: " + file.FullName + " has " + ex.Message);
                    }
                }
            }
        }
    }
    private static void LoadFavoriteNumerologySystemName()
    {
        s_numerology_system_names.Clear();

        string path = Globals.VALUES_FOLDER + "/" + "Offline";
        DirectoryInfo folder = new DirectoryInfo(path);
        if (folder != null)
        {
            FileInfo[] files = folder.GetFiles("*.txt");
            if ((files != null) && (files.Length > 0))
            {
                foreach (FileInfo file in files)
                {
                    try
                    {
                        string numerology_system_name = file.Name.Remove(file.Name.Length - 4, 4);
                        if (numerology_system_name == NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM)
                        {
                            string[] parts = numerology_system_name.Split('_');
                            if (parts.Length == 3)
                            {
                                s_numerology_system_names.Add(numerology_system_name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: " + file.FullName + " has " + ex.Message);
                    }
                }
            }
        }
    }

    private static NumerologySystem s_numerology_system = null;
    public static NumerologySystem NumerologySystem
    {
        get { return s_numerology_system; }
        set { s_numerology_system = value; }
    }
    private static NumerologySystem s_loaded_numerology_system = null;
    private static void LoadNumerologySystem(string numerology_system_name, Book book, string dynamic_text, bool overwrite)
    {
        try
        {
            string filename = Globals.VALUES_FOLDER + "/" + numerology_system_name + ".txt";
            if ((overwrite) || (!File.Exists(filename)))
            {
                File.Copy(Globals.VALUES_FOLDER + "/" + "Offline" + "/" + numerology_system_name + ".txt", filename, true);
                if (!File.Exists(filename))
                {
                    filename = Globals.VALUES_FOLDER + "/" + "Offline" + "/" + NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM + ".txt";
                }
            }

            if (File.Exists(filename))
            {
                List<string> lines = DataAccess.LoadLines(filename);

                if (s_numerology_system != null)
                {
                    // copy current numerology system and only change the name
                    s_loaded_numerology_system = new NumerologySystem(s_numerology_system, numerology_system_name);
                }
                else
                {
                    // create a new numerology system
                    s_loaded_numerology_system = new NumerologySystem(numerology_system_name);
                }

                // update letter-values
                if (s_loaded_numerology_system != null)
                {
                    s_loaded_numerology_system.LetterValues.Clear();
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length == 2)
                        {
                            s_loaded_numerology_system.LetterValues.Add(parts[0][0], long.Parse(parts[1]));
                        }
                        else
                        {
                            throw new Exception(filename + " file format must be:\r\n\tLetter TAB Value");
                        }
                    }

                    // create a new object and rebuild it in case its scope is not Book
                    s_numerology_system = new NumerologySystem(s_loaded_numerology_system);
                    BuildNumerologySystem(book, dynamic_text);
                }
            }
        }
        catch
        {
            // ignore
        }
    }
    private static void LoadDefaultNumerologySystem(Book book, string dynamic_text)
    {
        LoadNumerologySystem(NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM, book, dynamic_text, true);
    }
    private static void LoadNumerologySystem(string numerology_system_name, Book book, string dynamic_text)
    {
        LoadNumerologySystem(numerology_system_name, book, dynamic_text, false);
    }
    private static void RestoreNumerologySystem(string numerology_system_name, Book book, string dynamic_text)
    {
        LoadNumerologySystem(numerology_system_name, book, dynamic_text, true);
    }
    public static void ReloadNumerologySystem(Book book, string dynamic_text)
    {
        LoadNumerologySystem(s_numerology_system.Name, book, dynamic_text, false);
    }
    public static void ReloadNumerologySystem(string numerology_system_name, ref Book book, string dynamic_text)
    {
        string[] parts = numerology_system_name.Split('_');
        if (parts.Length == 3)
        {
            // if TextMode has changed
            if (s_loaded_numerology_system != null)
            {
                if (s_loaded_numerology_system.TextMode != parts[0])
                {
                    // reload text and re-simplify book text
                    book = BuildSimplifiedBook(numerology_system_name);
                }
            }
        }

        LoadNumerologySystem(numerology_system_name, book, dynamic_text);
    }
    public static void RestoreNumerologySystem(string numerology_system_name, ref Book book, string dynamic_text)
    {
        string[] parts = numerology_system_name.Split('_');
        if (parts.Length == 3)
        {
            // if TextMode has changed
            if (s_loaded_numerology_system != null)
            {
                if (s_loaded_numerology_system.TextMode != parts[0])
                {
                    // reload text and re-simplify book text
                    book = BuildSimplifiedBook(numerology_system_name);
                }
            }
        }

        LoadNumerologySystem(numerology_system_name, book, dynamic_text, true);
    }
    public static void ReloadDefaultNumerologySystem(ref Book book, string dynamic_text)
    {
        if (s_loaded_numerology_system != null)
        {
            if (s_loaded_numerology_system.Name != NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM)
            {
                book = BuildSimplifiedBook(NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM);
            }
        }

        if (s_numerology_system != null)
        {
            LoadDefaultNumerologySystem(book, dynamic_text);
        }
    }
    public static void SaveNumerologySystem()
    {
        if (!Directory.Exists(Globals.VALUES_FOLDER))
        {
            Directory.CreateDirectory(Globals.VALUES_FOLDER);
        }

        string filename = Globals.VALUES_FOLDER + "/" + s_numerology_system.Name + ".txt";
        try
        {
            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
            {
                foreach (char key in s_numerology_system.Keys)
                {
                    if (Constants.INDIAN_DIGITS.Contains(key)) continue;
                    if (Constants.STOPMARKS.Contains(key)) continue;
                    if (Constants.QURANMARKS.Contains(key)) continue;
                    if (key == '{') continue;
                    if (key == '}') continue;

                    writer.WriteLine(key + "\t" + s_numerology_system[key].ToString());
                }
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }
    }
    /// <summary>
    /// Build numerology system dynamically as user changes selection or highlighted_text 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="letters_scope"></param>
    public static void BuildNumerologySystem(Book book, string dynamic_text)
    {
        if (String.IsNullOrEmpty(dynamic_text)) return;

        dynamic_text = dynamic_text.SimplifyTo(s_numerology_system.TextMode);
        dynamic_text = dynamic_text.Replace("\r", "");
        dynamic_text = dynamic_text.Replace("\n", "");
        dynamic_text = dynamic_text.Replace("\t", "");
        dynamic_text = dynamic_text.Replace(" ", "");
        dynamic_text = dynamic_text.Replace(Verse.OPEN_BRACKET, "");
        dynamic_text = dynamic_text.Replace(Verse.CLOSE_BRACKET, "");

        UpdateLetterStatistics(dynamic_text);

        UpdateNumerologySystem(dynamic_text);

        UpdateBookVerseValues(book);
    }
    private static void UpdateBookVerseValues(Book book)
    {
        foreach (Verse verse in book.Verses)
        {
            CalculateValue(verse);
        }
    }

    private static List<LetterStatistic> s_letter_statistics = new List<LetterStatistic>();
    private static void UpdateLetterStatistics(string dynamic_text)
    {
        if (String.IsNullOrEmpty(dynamic_text)) return;

        if (s_letter_statistics != null)
        {
            s_letter_statistics.Clear();
            for (int i = 0; i < dynamic_text.Length; i++)
            {
                // calculate letter frequency
                bool is_found = false;
                for (int j = 0; j < s_letter_statistics.Count; j++)
                {
                    if (dynamic_text[i] == s_letter_statistics[j].Letter)
                    {
                        s_letter_statistics[j].Frequency++;
                        is_found = true;
                        break;
                    }
                }

                // add entry into dictionary
                if (!is_found)
                {
                    LetterStatistic letter_statistic = new LetterStatistic();
                    letter_statistic.Order = s_letter_statistics.Count + 1;
                    letter_statistic.Letter = dynamic_text[i];
                    letter_statistic.Frequency++;
                    s_letter_statistics.Add(letter_statistic);
                }
            }
        }
    }

    private static void UpdateNumerologySystem(string dynamic_text)
    {
        if (String.IsNullOrEmpty(dynamic_text)) return;

        if (s_numerology_system.Scope == NumerologySystemScope.Book)    // static numerology system
        {
            // build letter_order using the book text
            if (s_numerology_system != null)
            {
                List<char> letter_order = new List<char>();
                List<long> letter_values = new List<long>();
                if (s_loaded_numerology_system != null)
                {
                    foreach (char letter in s_loaded_numerology_system.Keys)
                    {
                        letter_order.Add(letter);
                        letter_values.Add(s_loaded_numerology_system[letter]);
                    }
                }

                s_numerology_system.Clear();
                for (int i = 0; i < letter_order.Count; i++)
                {
                    s_numerology_system.Add(letter_order[i], letter_values[i]);
                }
            }
        }
        else // dynamic numerology system
        {
            // build letter_order using letters in dynamic_text only
            List<char> letter_order = new List<char>();
            if (s_loaded_numerology_system != null)
            {
                foreach (char letter in s_loaded_numerology_system.Keys)
                {
                    if (dynamic_text.Contains(letter.ToString()))
                    {
                        letter_order.Add(letter);
                    }
                }
            }

            // re-generate the letter_values if numerology system is known
            if (letter_order.Count > 0)
            {
                if (s_numerology_system != null)
                {
                    List<long> letter_values = new List<long>();
                    if (letter_order.Count > 0)
                    {
                        if (s_numerology_system.Name.EndsWith("QuranNumbers"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.QuranNumbers.Count)
                                {
                                    letter_values.Add(Numbers.QuranNumbers[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Linear"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                letter_values.Add(i + 1L);
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Odds"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                letter_values.Add(2L * i + 1L);
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Evens"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                letter_values.Add(2L * (i + 1L));
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Primes"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.Primes.Count)
                                {
                                    letter_values.Add(Numbers.Primes[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("AdditivePrimes"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.AdditivePrimes.Count)
                                {
                                    letter_values.Add(Numbers.AdditivePrimes[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("PurePrimes"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.PurePrimes.Count)
                                {
                                    letter_values.Add(Numbers.PurePrimes[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Composites"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.Composites.Count)
                                {
                                    letter_values.Add(Numbers.Composites[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("AdditiveComposites"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.AdditiveComposites.Count)
                                {
                                    letter_values.Add(Numbers.AdditiveComposites[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("PureComposites"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.PureComposites.Count)
                                {
                                    letter_values.Add(Numbers.PureComposites[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("MersennePrimes"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.MersennePrimes.Count)
                                {
                                    letter_values.Add(Numbers.MersennePrimes[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Fibonacci"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.Fibonaccis.Count)
                                {
                                    letter_values.Add(Numbers.Fibonaccis[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Frequency▲"))
                        {
                            // letter-frequency mismacth: different letters for different frequencies
                            LetterStatistic.SortOrder = StatisticSortOrder.Ascending;
                            LetterStatistic.SortMethod = StatisticSortMethod.ByFrequency;
                            s_letter_statistics.Sort();
                            foreach (LetterStatistic letter_statistic in s_letter_statistics)
                            {
                                letter_values.Add(letter_statistic.Frequency);
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Frequency"))
                        {
                            // letter-frequency mismacth: different letters for different frequencies
                            LetterStatistic.SortOrder = StatisticSortOrder.Descending;
                            LetterStatistic.SortMethod = StatisticSortMethod.ByFrequency;
                            s_letter_statistics.Sort();
                            foreach (LetterStatistic letter_statistic in s_letter_statistics)
                            {
                                letter_values.Add(letter_statistic.Frequency);
                            }
                        }
                        else if (s_numerology_system.Name.EndsWith("Gematria"))
                        {
                            for (int i = 0; i < letter_order.Count; i++)
                            {
                                if (i < Numbers.Gematria.Count)
                                {
                                    letter_values.Add(Numbers.Gematria[i]);
                                }
                                else
                                {
                                    letter_values.Add(0L);
                                }
                            }
                        }
                        else // if not pre-defined in Numbers, then use loaded letter_values
                        {
                            if (s_loaded_numerology_system != null)
                            {
                                foreach (char letter in s_loaded_numerology_system.Keys)
                                {
                                    letter_values.Add(s_loaded_numerology_system[letter]);
                                }
                            }
                        }
                    }

                    // finally, re-build the numerology system
                    s_numerology_system.Clear();
                    for (int i = 0; i < letter_order.Count; i++)
                    {
                        s_numerology_system.Add(letter_order[i], letter_values[i]);
                    }
                }
            }
        }
    }
    // used for non-Quran text
    public static long CalculateValue(char user_char)
    {
        if (user_char == '\0') return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            result = s_numerology_system.CalculateValue(user_char);
        }
        return result;
    }
    public static long CalculateValue(string user_text)
    {
        if (string.IsNullOrEmpty(user_text)) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            result = s_numerology_system.CalculateValue(user_text);
        }
        return result;
    }
    // used for Quran text only
    public static long CalculateValue(Letter letter)
    {
        if (letter == null) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            // adjust value of letter
            result += AdjustLetterValue(letter);

            // calculate the letter static value
            result += s_numerology_system.CalculateValue(letter.Character);
        }
        return result;
    }
    public static long CalculateValue(Word word)
    {
        if (word == null) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            // adjust value of verse
            if (word.Verse.Words.Count == 1)
            {
                result += AdjustVerseValue(word.Verse);
            }

            // adjust value of word
            result += AdjustWordValue(word);

            foreach (Letter letter in word.Letters)
            {
                // adjust value of letter
                result += AdjustLetterValue(letter);

                // calculate the letter static value
                result += s_numerology_system.CalculateValue(letter.Character);
            }
        }
        return result;
    }
    public static long CalculateValue(Verse verse)
    {
        if (verse == null) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            // adjust value of verse
            result += AdjustVerseValue(verse);

            foreach (Word word in verse.Words)
            {
                // adjust value of word
                result += AdjustWordValue(word);

                foreach (Letter letter in word.Letters)
                {
                    // adjust value of letter
                    result += AdjustLetterValue(letter);

                    // calculate the letter static value
                    result += s_numerology_system.CalculateValue(letter.Character);
                }
            }

            verse.Value = result;
        }
        return result;
    }
    public static long CalculateValue(Sentence sentence)
    {
        if (sentence == null) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            // adjust value of verses
            List<Verse> verses = GetCompleteVerses(sentence);
            if (verses != null)
            {
                foreach (Verse verse in verses)
                {
                    result += AdjustVerseValue(verse);
                }
            }

            // adjust value of word
            List<Word> words = GetCompleteWords(sentence);
            if (words != null)
            {
                foreach (Word word in words)
                {
                    result += AdjustWordValue(word);

                    foreach (Letter letter in word.Letters)
                    {
                        // adjust value of letter
                        result += AdjustLetterValue(letter);

                        // calculate the letter static value
                        result += s_numerology_system.CalculateValue(letter.Character);
                    }
                }
            }
        }
        return result;
    }
    private static List<Word> GetCompleteWords(Sentence sentence)
    {
        if (sentence == null) return null;

        List<Word> result = new List<Word>();
        if (sentence.FirstVerse.Number == sentence.LastVerse.Number)
        {
            foreach (Word word in sentence.FirstVerse.Words)
            {
                if ((word.Position >= sentence.StartPosition) && (word.Position < sentence.EndPosition))
                {
                    result.Add(word);
                }
            }
        }
        else // multi-verse
        {
            // first verse
            foreach (Word word in sentence.FirstVerse.Words)
            {
                if (word.Position >= sentence.StartPosition)
                {
                    result.Add(word);
                }
            }

            // middle verses
            int after_first = sentence.FirstVerse.Number + 1;
            int before_last = sentence.LastVerse.Number - 1;
            if (after_first >= before_last) // 1 or more middle verses
            {
                for (int i = after_first - 1; i <= before_last - 1; i++)
                {
                    result.AddRange(sentence.FirstVerse.Book.Verses[i].Words);
                }
            }

            // last verse
            foreach (Word word in sentence.LastVerse.Words)
            {
                if (word.Position < sentence.EndPosition) // not <= because EndPosition is after the start of the last word in the sentence
                {
                    result.Add(word);
                }
            }
        }
        return result;
    }
    private static List<Verse> GetCompleteVerses(Sentence sentence)
    {
        if (sentence == null) return null;

        List<Verse> result = new List<Verse>();
        if (sentence.FirstVerse.Number == sentence.LastVerse.Number)
        {
            if ((sentence.StartPosition == 0) && (sentence.EndPosition == sentence.Text.Length - 1))
            {
                result.Add(sentence.FirstVerse);
            }
        }
        else // multi-verse
        {
            // first verse
            if (sentence.StartPosition == 0)
            {
                result.Add(sentence.FirstVerse);
            }

            // middle verses
            int after_first = sentence.FirstVerse.Number + 1;
            int before_last = sentence.LastVerse.Number - 1;
            if (after_first >= before_last) // 1 or more middle verses
            {
                for (int i = after_first - 1; i <= before_last - 1; i++)
                {
                    result.Add(sentence.FirstVerse.Book.Verses[i]);
                }
            }

            // last verse
            if (sentence.EndPosition == sentence.LastVerse.Text.Length - 1)
            {
                result.Add(sentence.LastVerse);
            }
        }
        return result;
    }
    public static long CalculateValue(List<Verse> verses)
    {
        if (verses == null) return 0L;
        if (verses.Count == 0) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            // adjust value of chapters
            List<Chapter> chapters = GetCompleteChapters(verses, 0, verses[verses.Count - 1].LetterCount - 1);
            if (chapters != null)
            {
                foreach (Chapter chapter in chapters)
                {
                    result += AdjustChapterValue(chapter);
                }
            }

            foreach (Verse verse in verses)
            {
                result += CalculateValue(verse);
            }
        }
        return result;
    }
    public static long CalculateValue(Chapter chapter)
    {
        if (chapter == null) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            result = CalculateValue(chapter.Verses);
        }
        return result;
    }
    public static long CalculateValue(List<Verse> verses, int letter_index_in_verse1, int letter_index_in_verse2)
    {
        if (verses == null) return 0L;
        if (verses.Count == 0) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            // adjust value of chapters
            List<Chapter> chapters = GetCompleteChapters(verses, letter_index_in_verse1, letter_index_in_verse2);
            if (chapters != null)
            {
                foreach (Chapter chapter in chapters)
                {
                    result += AdjustChapterValue(chapter);
                }
            }

            if (verses.Count == 1)
            {
                result += CalculateMiddlePartValue(verses[0], letter_index_in_verse1, letter_index_in_verse2);
            }
            else if (verses.Count == 2)
            {
                result += CalculateEndPartValue(verses[0], letter_index_in_verse1);
                result += CalculateBeginningPartValue(verses[1], letter_index_in_verse2);
            }
            else //if (verses.Count > 2)
            {
                result += CalculateEndPartValue(verses[0], letter_index_in_verse1);

                // middle verses
                for (int i = 1; i < verses.Count - 1; i++)
                {
                    result += CalculateValue(verses[i]);
                }

                result += CalculateBeginningPartValue(verses[verses.Count - 1], letter_index_in_verse2);
            }
        }
        return result;
    }
    private static List<Chapter> GetCompleteChapters(List<Verse> verses, int letter_index_in_verse1, int letter_index_in_verse2)
    {
        if (verses == null) return null;
        if (verses.Count == 0) return null;

        List<Chapter> result = new List<Chapter>();
        List<Verse> complete_verses = new List<Verse>(verses); // make a copy so we don't change the passed verses

        if (complete_verses != null)
        {
            if (complete_verses.Count > 0)
            {
                Verse first_verse = complete_verses[0];
                if (first_verse != null)
                {
                    if (letter_index_in_verse1 != 0)
                    {
                        complete_verses.Remove(first_verse);
                    }
                }

                if (complete_verses.Count > 0) // check again after maybe removing a verse
                {
                    Verse last_verse = complete_verses[complete_verses.Count - 1];
                    if (last_verse != null)
                    {
                        if (letter_index_in_verse2 != last_verse.LetterCount - 1)
                        {
                            complete_verses.Remove(last_verse);
                        }
                    }
                }

                if (complete_verses.Count > 0) // check again after maybe removing a verse
                {
                    Book book = complete_verses[0].Book;
                    foreach (Chapter chapter in book.Chapters)
                    {
                        bool include_chapter = true;
                        foreach (Verse v in chapter.Verses)
                        {
                            if (!complete_verses.Contains(v))
                            {
                                include_chapter = false;
                                break;
                            }
                        }

                        if (include_chapter)
                        {
                            result.Add(chapter);
                        }
                    }
                }
            }
        }

        return result;
    }
    private static long CalculateBeginningPartValue(Verse verse, int to_letter_index)
    {
        return CalculateMiddlePartValue(verse, 0, to_letter_index);
    }
    private static long CalculateMiddlePartValue(Verse verse, int from_letter_index, int to_letter_index)
    {
        if (verse == null) return 0L;

        long result = 0L;
        if (s_numerology_system != null)
        {
            // adjust value of verse
            if ((from_letter_index == 0) && (to_letter_index == verse.LetterCount - 1))
            {
                result += AdjustVerseValue(verse);
            }

            int word_index = -1;   // in verse
            int letter_index = -1; // in verse
            foreach (Word word in verse.Words)
            {
                word_index++;

                // adjust value of word (if fully selected)
                if ((from_letter_index <= word.Letters[0].NumberInVerse - 1)                    // if selection starts before or at first letter in word
                    &&                                                                          // AND
                    (to_letter_index >= word.Letters[word.Letters.Count - 1].NumberInVerse - 1) // if selection ends   at or after  last  letter in word
                   )
                {
                    result += AdjustWordValue(word);
                }

                foreach (Letter letter in word.Letters)
                {
                    letter_index++;

                    if (letter_index < from_letter_index) continue;
                    if (letter_index > to_letter_index) break;

                    // adjust value of letter
                    result += AdjustLetterValue(letter);

                    // calculate the letter static value
                    result += s_numerology_system.CalculateValue(letter.Character);
                }
            }
        }
        return result;
    }
    private static long CalculateEndPartValue(Verse verse, int from_letter_index)
    {
        return CalculateMiddlePartValue(verse, from_letter_index, verse.LetterCount - 1);
    }
    // AddTo... 19 parameters
    private static long AdjustLetterValue(Letter letter)
    {
        long result = 0L;
        if (s_numerology_system != null)
        {
            if (letter != null)
            {
                if (s_numerology_system.AddToLetterLNumber)
                {
                    result += letter.NumberInWord;
                }
                if (s_numerology_system.AddToLetterWNumber)
                {
                    result += letter.Word.NumberInVerse;
                }
                if (s_numerology_system.AddToLetterVNumber)
                {
                    result += letter.Word.Verse.NumberInChapter;
                }
                if (s_numerology_system.AddToLetterCNumber)
                {
                    result += letter.Word.Verse.Chapter.Number;
                }
                if (s_numerology_system.AddToLetterLDistance)
                {
                    result += letter.DistanceToPrevious.dL;
                }
                if (s_numerology_system.AddToLetterWDistance)
                {
                    result += letter.DistanceToPrevious.dW;
                }
                if (s_numerology_system.AddToLetterVDistance)
                {
                    result += letter.DistanceToPrevious.dV;
                }
                if (s_numerology_system.AddToLetterCDistance)
                {
                    result += letter.DistanceToPrevious.dC;
                }
            }
        }
        return result;
    }
    private static long AdjustWordValue(Word word)
    {
        long result = 0L;
        if (s_numerology_system != null)
        {
            if (word != null)
            {
                if (s_numerology_system.AddToWordWNumber)
                {
                    result += word.NumberInVerse;
                }
                if (s_numerology_system.AddToWordVNumber)
                {
                    result += word.Verse.NumberInChapter;
                }
                if (s_numerology_system.AddToWordCNumber)
                {
                    result += word.Verse.Chapter.Number;
                }
                if (s_numerology_system.AddToWordWDistance)
                {
                    result += word.DistanceToPrevious.dW;
                }
                if (s_numerology_system.AddToWordVDistance)
                {
                    result += word.DistanceToPrevious.dV;
                }
                if (s_numerology_system.AddToWordCDistance)
                {
                    result += word.DistanceToPrevious.dC;
                }
            }
        }
        return result;
    }
    private static long AdjustVerseValue(Verse verse)
    {
        long result = 0L;
        if (s_numerology_system != null)
        {
            if (verse != null)
            {
                if (s_numerology_system.AddToVerseVNumber)
                {
                    result += verse.NumberInChapter;
                }
                if (s_numerology_system.AddToVerseCNumber)
                {
                    result += verse.Chapter.Number;
                }
                if (s_numerology_system.AddToVerseVDistance)
                {
                    result += verse.DistanceToPrevious.dV;
                }
                if (s_numerology_system.AddToVerseCDistance)
                {
                    result += verse.DistanceToPrevious.dC;
                }
            }
        }
        return result;
    }
    private static long AdjustChapterValue(Chapter chapter)
    {
        long result = 0L;
        if (s_numerology_system != null)
        {
            if (chapter != null)
            {
                if (s_numerology_system.AddToChapterCNumber)
                {
                    result += chapter.Number;
                }
            }
        }
        return result;
    }

    // helper methods for finds
    public static List<Verse> GetSourceVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result)
    {
        List<Verse> result = new List<Verse>();
        if (book != null)
        {
            if (find_scope == FindScope.Book)
            {
                result = book.Verses;
            }
            else if (find_scope == FindScope.Selection)
            {
                result = current_selection.Verses;
            }
            else if (find_scope == FindScope.Result)
            {
                if (previous_result != null)
                {
                    result = new List<Verse>(previous_result);
                }
            }
        }
        return result;
    }
    public static List<Verse> GetVerses(List<Phrase> phrases)
    {
        List<Verse> result = new List<Verse>();
        if (phrases != null)
        {
            foreach (Phrase phrase in phrases)
            {
                if (phrase != null)
                {
                    if (!result.Contains(phrase.Verse))
                    {
                        result.Add(phrase.Verse);
                    }
                }
            }
        }
        return result;
    }
    public static List<Phrase> BuildPhrases(Verse verse, MatchCollection matches)
    {
        List<Phrase> result = new List<Phrase>();
        foreach (Match match in matches)
        {
            foreach (Capture capture in match.Captures)
            {
                string text = capture.Value;
                int position = capture.Index;
                Phrase phrase = new Phrase(verse, position, text);
                if (phrase != null)
                {
                    result.Add(phrase);
                }
            }
        }
        return result;
    }
    public static List<Phrase> BuildPhrasesAndOriginify(Verse verse, MatchCollection matches)
    {
        List<Phrase> result = new List<Phrase>();
        foreach (Match match in matches)
        {
            foreach (Capture capture in match.Captures)
            {
                string text = capture.Value;
                int position = capture.Index;
                Phrase phrase = new Phrase(verse, position, text);

                if (s_numerology_system.TextMode.Contains("Original"))
                {
                    OriginifyPhrase(ref phrase);
                }

                if (phrase != null)
                {
                    result.Add(phrase);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Phrase is in Simplified text, so correct its position and length for Original text
    /// </summary>
    /// <param name="phrase"></param>
    public static void OriginifyPhrase(ref Phrase phrase)
    {
        if (phrase != null)
        {
            // simple phrase
            Verse verse = phrase.Verse;
            string text = phrase.Text;
            int position = phrase.Position;

            // convert to original
            if (verse != null)
            {
                int start = 0;
                for (int i = 0; i < verse.Text.Length; i++)
                {
                    char character = verse.Text[i];

                    if ((character == ' ') || (Constants.ARABIC_LETTERS.Contains(character)))
                    {
                        start++;
                    }

                    if ((Constants.STOPMARKS.Contains(character)) || (Constants.QURANMARKS.Contains(character)))
                    {
                        start--; // ignore space after stopmark
                        if (start < 0)
                        {
                            start = 0;
                        }
                    }

                    // i has reached phrase start
                    if (start > position)
                    {
                        int phrase_length = text.Trim().Length;
                        StringBuilder str = new StringBuilder();

                        int length = 0;
                        for (int j = i; j < verse.Text.Length; j++)
                        {
                            character = verse.Text[j];
                            str.Append(character);

                            if ((character == ' ') || (Constants.ARABIC_LETTERS.Contains(character)))
                            {
                                length++;
                            }

                            if ((Constants.STOPMARKS.Contains(character)) || (Constants.QURANMARKS.Contains(character)))
                            {
                                length--; // ignore space after stopmark
                                if (length < 0)
                                {
                                    length = 0;
                                }
                            }

                            // j has reached phrase end
                            if (length == phrase_length)
                            {
                                phrase = new Phrase(verse, i, str.ToString());
                                if (phrase != null)
                                {
                                    return; // ref phrase filled with Originified phrase
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Phrase is in Original text, so correct its position and length for Simplified text
    /// </summary>
    /// <param name="phrase"></param>
    public static void SimplifyPhrase(ref Phrase phrase)
    {
        if (phrase != null)
        {
            //TODO: approximate Original-to-Simplified for now
            Verse verse = phrase.Verse;
            string text = phrase.Text.Substring(0, phrase.Text.Length / 2);
            int position = phrase.Position / 2;
            phrase = new Phrase(verse, position, text);
        }
    }

    // find by text - Exact
    public static List<Phrase> FindPhrases(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string text, LanguageType language_type, string translation, TextLocation text_location, bool case_sensitive, TextWordness wordness, int multiplicity, bool at_word_start, bool with_diacritics)
    {
        List<Phrase> result = new List<Phrase>();

        if (language_type == LanguageType.Arabic)
        {
            result = DoFindPhrases(book, find_scope, current_selection, previous_result, text, language_type, translation, text_location, case_sensitive, wordness, multiplicity, at_word_start, with_diacritics, true);
        }
        else if (language_type == LanguageType.Translation)
        {
            if (book.Verses != null)
            {
                if (book.Verses.Count > 0)
                {
                    foreach (string key in book.Verses[0].Translations.Keys)
                    {
                        List<Phrase> new_phrases = DoFindPhrases(book, find_scope, current_selection, previous_result, text, language_type, key, text_location, case_sensitive, wordness, multiplicity, at_word_start, with_diacritics, false);

                        result.AddRange(new_phrases);
                    }
                }
            }
        }
        return result;
    }
    private static List<Phrase> DoFindPhrases(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string text, LanguageType language_type, string translation, TextLocation text_location, bool case_sensitive, TextWordness wordness, int multiplicity, bool at_word_start, bool with_diacritics, bool try_emlaaei_if_nothing_found)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        if (language_type == LanguageType.Arabic)
        {
            return DoFindPhrases(source, find_scope, current_selection, previous_result, text, text_location, wordness, multiplicity, at_word_start, with_diacritics, try_emlaaei_if_nothing_found);
        }
        else //if (language_type == FindByTextLanguageType.Translation)
        {
            return DoFindPhrases(translation, source, find_scope, current_selection, previous_result, text, text_location, case_sensitive, wordness, multiplicity, at_word_start);
        }
    }
    private static List<Phrase> DoFindPhrases(List<Verse> source, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string text, TextLocation text_location, TextWordness wordness, int multiplicity, bool at_word_start, bool with_diacritics, bool try_emlaaei_if_nothing_found)
    {
        List<Phrase> result = new List<Phrase>();
        List<Verse> found_verses = new List<Verse>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                if (!String.IsNullOrEmpty(text))
                {
                    RegexOptions regex_options = RegexOptions.IgnoreCase | RegexOptions.RightToLeft;

                    string pattern = null;
                    List<string> unsigned_words = null;
                    List<string> positive_words = null;
                    List<string> negative_words = null;

                    try
                    {
                        if (with_diacritics)
                        {
                            // search in original text first (without simplification)
                            pattern = BuildPattern(text, text_location, wordness, at_word_start, out unsigned_words, out positive_words, out negative_words);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Verse verse in source)
                                {
                                    /////////////////////////
                                    // process negative_words
                                    /////////////////////////
                                    if (negative_words.Count > 0)
                                    {
                                        bool found = false;
                                        foreach (string negative_word in negative_words)
                                        {
                                            foreach (Word word in verse.Words)
                                            {
                                                string word_text = word.Text;
                                                if (wordness == TextWordness.Any)
                                                {
                                                    if (word_text.Contains(negative_word))
                                                    {
                                                        found = true; // next verse
                                                        break;
                                                    }
                                                }
                                                else if (wordness == TextWordness.PartOfWord)
                                                {
                                                    if ((word_text.Contains(negative_word)) && (word_text.Length > negative_word.Length))
                                                    {
                                                        found = true; // next verse
                                                        break;
                                                    }
                                                }
                                                else if (wordness == TextWordness.WholeWord)
                                                {
                                                    if (word_text == negative_word)
                                                    {
                                                        found = true; // next verse
                                                        break;
                                                    }
                                                }
                                            }
                                            if (found)
                                            {
                                                break;
                                            }
                                        }
                                        if (found) continue; // next verse
                                    }

                                    /////////////////////////
                                    // process positive_words
                                    /////////////////////////
                                    if (positive_words.Count > 0)
                                    {
                                        int matches = 0;
                                        foreach (string positive_word in positive_words)
                                        {
                                            foreach (Word word in verse.Words)
                                            {
                                                string word_text = word.Text;
                                                if (wordness == TextWordness.Any)
                                                {
                                                    if (word_text.Contains(positive_word))
                                                    {
                                                        matches++;
                                                        break; // next positive_word
                                                    }
                                                }
                                                else if (wordness == TextWordness.PartOfWord)
                                                {
                                                    if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                    {
                                                        matches++;
                                                        break; // next positive_word
                                                    }
                                                }
                                                else if (wordness == TextWordness.WholeWord)
                                                {
                                                    if (word_text == positive_word)
                                                    {
                                                        matches++;
                                                        break; // next positive_word
                                                    }
                                                }
                                            }
                                        }

                                        // verse failed test, so skip it
                                        if (matches < positive_words.Count)
                                        {
                                            continue; // next verse
                                        }
                                    }

                                    //////////////////////////////////////////////////////
                                    // both negative and positive conditions have been met
                                    //////////////////////////////////////////////////////

                                    /////////////////////////
                                    // process unsigned_words
                                    /////////////////////////
                                    //////////////////////////////////////////////////////////
                                    // FindByText WORDS All
                                    //////////////////////////////////////////////////////////
                                    if (text_location == TextLocation.AllWords)
                                    {
                                        int matches = 0;
                                        foreach (string unsigned_word in unsigned_words)
                                        {
                                            foreach (Word word in verse.Words)
                                            {
                                                string word_text = word.Text;
                                                if (wordness == TextWordness.Any)
                                                {
                                                    if (word_text.Contains(unsigned_word))
                                                    {
                                                        matches++;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                                else if (wordness == TextWordness.PartOfWord)
                                                {
                                                    if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                    {
                                                        matches++;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                                else if (wordness == TextWordness.WholeWord)
                                                {
                                                    if (word_text == unsigned_word)
                                                    {
                                                        matches++;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                            }
                                        }

                                        if (matches == unsigned_words.Count)
                                        {
                                            ///////////////////////////////////////////////////////////////
                                            // all negative, positive and unsigned conditions have been met
                                            ///////////////////////////////////////////////////////////////

                                            // add positive matches
                                            foreach (string positive_word in positive_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    string word_text = word.Text;
                                                    if (wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(positive_word))
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == positive_word)
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                }
                                            }

                                            // add unsigned matches
                                            foreach (string unsigned_word in unsigned_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    string word_text = word.Text;
                                                    if (wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(unsigned_word))
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == unsigned_word)
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else // verse failed test, so skip it
                                        {
                                            continue; // next verse
                                        }
                                    }
                                    //////////////////////////////////////////////////////////
                                    // FindByText WORDS Any
                                    //////////////////////////////////////////////////////////
                                    else if (text_location == TextLocation.AnyWord)
                                    {
                                        bool found = false;
                                        foreach (string unsigned_word in unsigned_words)
                                        {
                                            foreach (Word word in verse.Words)
                                            {
                                                string word_text = word.Text;
                                                if (wordness == TextWordness.Any)
                                                {
                                                    if (word_text.Contains(unsigned_word))
                                                    {
                                                        found = true;
                                                        break; // next unsigned_word
                                                    }
                                                }
                                                else if (wordness == TextWordness.PartOfWord)
                                                {
                                                    if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                    {
                                                        found = true;
                                                        break; // next unsigned_word
                                                    }
                                                }
                                                else if (wordness == TextWordness.WholeWord)
                                                {
                                                    if (word_text == unsigned_word)
                                                    {
                                                        found = true;
                                                        break; // next unsigned_word
                                                    }
                                                }
                                            }
                                            if (found)
                                            {
                                                break;
                                            }
                                        }

                                        if (found) // found 1 unsigned word in verse, which is enough
                                        {
                                            ///////////////////////////////////////////////////////////////
                                            // all negative, positive and unsigned conditions have been met
                                            ///////////////////////////////////////////////////////////////

                                            // add positive matches
                                            foreach (string positive_word in positive_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    string word_text = word.Text;
                                                    if (wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(positive_word))
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == positive_word)
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                }
                                            }

                                            // add unsigned matches
                                            foreach (string unsigned_word in unsigned_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    string word_text = word.Text;
                                                    if (wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(unsigned_word))
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == unsigned_word)
                                                        {
                                                            found_verses.Add(verse);
                                                            result.Add(new Phrase(verse, word.Position, word.Text));
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else // verse failed test, so skip it
                                        {
                                            continue; // next verse
                                        }
                                    }
                                    //////////////////////////////////////////////////////////
                                    // FindByText EXACT 
                                    //////////////////////////////////////////////////////////
                                    else // at start, middle, end, or anywhere
                                    {
                                        string verse_text = verse.Text;
                                        //??? whole_word still needs verification in border cases in all text_modes
                                        MatchCollection matches = Regex.Matches(verse_text, pattern, regex_options);
                                        if (multiplicity != -1) // with multiplicity
                                        {
                                            if (matches.Count >= multiplicity)
                                            {
                                                found_verses.Add(verse);
                                                if (matches.Count > 0)
                                                {
                                                    result.AddRange(BuildPhrases(verse, matches));
                                                }
                                                else
                                                {
                                                    result.Add(new Phrase(verse, 0, ""));
                                                }
                                            }
                                        }
                                        else // without multiplicity
                                        {
                                            if (matches.Count > 0)
                                            {
                                                found_verses.Add(verse);
                                                result.AddRange(BuildPhrases(verse, matches));
                                            }
                                        }
                                    }
                                } // end for
                            }
                        }
                        //DON'T use else{} in case with_diacritics didn't find any result, so try simplified text first before emlaaei text
                        if (result.Count == 0)
                        {
                            // simplify all text_modes (Original will be simplified29 automatically)
                            text = text.SimplifyTo(s_numerology_system.TextMode);
                            if (!String.IsNullOrEmpty(text)) // re-test in case text was just harakaat which is simplifed to nothing
                            {
                                pattern = BuildPattern(text, text_location, wordness, at_word_start, out unsigned_words, out positive_words, out negative_words);
                                if (!String.IsNullOrEmpty(pattern))
                                {
                                    foreach (Verse verse in source)
                                    {
                                        /////////////////////////
                                        // process negative_words
                                        /////////////////////////
                                        if (negative_words.Count > 0)
                                        {
                                            bool found = false;
                                            foreach (string negative_word in negative_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    // simplify all text_modes (Original will be simplified29 automatically)
                                                    string word_text = word.Text.SimplifyTo(s_numerology_system.TextMode);
                                                    if (wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(negative_word))
                                                        {
                                                            found = true; // next verse
                                                            break;
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(negative_word)) && (word_text.Length > negative_word.Length))
                                                        {
                                                            found = true; // next verse
                                                            break;
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == negative_word)
                                                        {
                                                            found = true; // next verse
                                                            break;
                                                        }
                                                    }
                                                }
                                                if (found)
                                                {
                                                    break;
                                                }
                                            }
                                            if (found) continue; // next verse
                                        }

                                        /////////////////////////
                                        // process positive_words
                                        /////////////////////////
                                        if (positive_words.Count > 0)
                                        {
                                            int matches = 0;
                                            foreach (string positive_word in positive_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    // simplify all text_modes (Original will be simplified29 automatically)
                                                    string word_text = word.Text.SimplifyTo(s_numerology_system.TextMode);
                                                    if (wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(positive_word))
                                                        {
                                                            matches++;
                                                            break; // next positive_word
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                        {
                                                            matches++;
                                                            break; // next positive_word
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == positive_word)
                                                        {
                                                            matches++;
                                                            break; // next positive_word
                                                        }
                                                    }
                                                }
                                            }

                                            // verse failed test, so skip it
                                            if (matches < positive_words.Count)
                                            {
                                                continue; // next verse
                                            }
                                        }

                                        //////////////////////////////////////////////////////
                                        // both negative and positive conditions have been met
                                        //////////////////////////////////////////////////////

                                        /////////////////////////
                                        // process unsigned_words
                                        /////////////////////////
                                        //////////////////////////////////////////////////////////
                                        // FindByText WORDS All
                                        //////////////////////////////////////////////////////////
                                        if (text_location == TextLocation.AllWords)
                                        {
                                            int matches = 0;
                                            foreach (string unsigned_word in unsigned_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    // simplify all text_modes (Original will be simplified29 automatically)
                                                    string word_text = word.Text.SimplifyTo(s_numerology_system.TextMode);
                                                    if (wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(unsigned_word))
                                                        {
                                                            matches++;
                                                            break; // no need to continue even if there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                        {
                                                            matches++;
                                                            break; // no need to continue even if there are more matches
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == unsigned_word)
                                                        {
                                                            matches++;
                                                            break; // no need to continue even if there are more matches
                                                        }
                                                    }
                                                }
                                            }

                                            if (matches == unsigned_words.Count)
                                            {
                                                ///////////////////////////////////////////////////////////////
                                                // all negative, positive and unsigned conditions have been met
                                                ///////////////////////////////////////////////////////////////

                                                // add positive matches
                                                foreach (string positive_word in positive_words)
                                                {
                                                    foreach (Word word in verse.Words)
                                                    {
                                                        // simplify all text_modes (Original will be simplified29 automatically)
                                                        string word_text = word.Text.SimplifyTo(s_numerology_system.TextMode);
                                                        if (wordness == TextWordness.Any)
                                                        {
                                                            if (word_text.Contains(positive_word))
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                        else if (wordness == TextWordness.PartOfWord)
                                                        {
                                                            if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                        else if (wordness == TextWordness.WholeWord)
                                                        {
                                                            if (word_text == positive_word)
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                    }
                                                }

                                                // add unsigned matches
                                                foreach (string unsigned_word in unsigned_words)
                                                {
                                                    foreach (Word word in verse.Words)
                                                    {
                                                        // simplify all text_modes (Original will be simplified29 automatically)
                                                        string word_text = word.Text.SimplifyTo(s_numerology_system.TextMode);
                                                        if (wordness == TextWordness.Any)
                                                        {
                                                            if (word_text.Contains(unsigned_word))
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                        else if (wordness == TextWordness.PartOfWord)
                                                        {
                                                            if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                        else if (wordness == TextWordness.WholeWord)
                                                        {
                                                            if (word_text == unsigned_word)
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else // verse failed test, so skip it
                                            {
                                                continue; // next verse
                                            }
                                        }
                                        //////////////////////////////////////////////////////////
                                        // FindByText WORDS Any
                                        //////////////////////////////////////////////////////////
                                        else if (text_location == TextLocation.AnyWord)
                                        {
                                            bool found = false;
                                            foreach (string unsigned_word in unsigned_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    // simplify all text_modes (Original will be simplified29 automatically)
                                                    string word_text = word.Text.SimplifyTo(s_numerology_system.TextMode);
                                                    if (wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(unsigned_word))
                                                        {
                                                            found = true;
                                                            break; // next unsigned_word
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                        {
                                                            found = true;
                                                            break; // next unsigned_word
                                                        }
                                                    }
                                                    else if (wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == unsigned_word)
                                                        {
                                                            found = true;
                                                            break; // next unsigned_word
                                                        }
                                                    }
                                                }
                                                if (found)
                                                {
                                                    break;
                                                }
                                            }

                                            if (found) // found 1 unsigned word in verse, which is enough
                                            {
                                                ///////////////////////////////////////////////////////////////
                                                // all negative, positive and unsigned conditions have been met
                                                ///////////////////////////////////////////////////////////////

                                                // add positive matches
                                                foreach (string positive_word in positive_words)
                                                {
                                                    foreach (Word word in verse.Words)
                                                    {
                                                        // simplify all text_modes (Original will be simplified29 automatically)
                                                        string word_text = word.Text.SimplifyTo(s_numerology_system.TextMode);
                                                        if (wordness == TextWordness.Any)
                                                        {
                                                            if (word_text.Contains(positive_word))
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                        else if (wordness == TextWordness.PartOfWord)
                                                        {
                                                            if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                        else if (wordness == TextWordness.WholeWord)
                                                        {
                                                            if (word_text == positive_word)
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                    }
                                                }

                                                // add unsigned matches
                                                foreach (string unsigned_word in unsigned_words)
                                                {
                                                    foreach (Word word in verse.Words)
                                                    {
                                                        // simplify all text_modes (Original will be simplified29 automatically)
                                                        string word_text = word.Text.SimplifyTo(s_numerology_system.TextMode);
                                                        if (wordness == TextWordness.Any)
                                                        {
                                                            if (word_text.Contains(unsigned_word))
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                        else if (wordness == TextWordness.PartOfWord)
                                                        {
                                                            if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                        else if (wordness == TextWordness.WholeWord)
                                                        {
                                                            if (word_text == unsigned_word)
                                                            {
                                                                found_verses.Add(verse);
                                                                result.Add(new Phrase(verse, word.Position, word.Text));
                                                                //break; // no break in case there are more matches
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else // verse failed test, so skip it
                                            {
                                                continue; // next verse
                                            }
                                        }
                                        //////////////////////////////////////////////////////////
                                        // FindByText EXACT 
                                        //////////////////////////////////////////////////////////
                                        else // at start, middle, end, or anywhere
                                        {
                                            // simplify all text_modes (Original will be simplified29 automatically)
                                            string verse_text = verse.Text.SimplifyTo(s_numerology_system.TextMode);
                                            MatchCollection matches = Regex.Matches(verse_text, pattern, regex_options);
                                            if (multiplicity != -1) // with multiplicity
                                            {
                                                if (matches.Count >= multiplicity)
                                                {
                                                    found_verses.Add(verse);
                                                    if (matches.Count > 0)
                                                    {
                                                        result.AddRange(BuildPhrasesAndOriginify(verse, matches));
                                                    }
                                                    else
                                                    {
                                                        result.Add(new Phrase(verse, 0, ""));
                                                    }
                                                }
                                            }
                                            else // without multiplicity
                                            {
                                                if (matches.Count > 0)
                                                {
                                                    found_verses.Add(verse);
                                                    result.AddRange(BuildPhrasesAndOriginify(verse, matches));
                                                }
                                            }
                                        }
                                    } // end for
                                }
                            }
                        }

                        // if nothing found
                        if ((multiplicity != 0) && (result.Count == 0))
                        {
                            //  search in emlaaei
                            if (try_emlaaei_if_nothing_found)
                            {
                                // always simplify29 for emlaaei comparison
                                pattern = pattern.Simplify29();
                                pattern = pattern.Trim();
                                while (pattern.Contains("  "))
                                {
                                    pattern = pattern.Replace("  ", " ");
                                }

                                if ((source != null) && (source.Count > 0))
                                {
                                    foreach (Verse verse in source)
                                    {
                                        // always simplify29 for emlaaei comparison
                                        string simplified_emlaaei_text = verse.Translations[DEFAULT_EMLAAEI_TEXT].Simplify29();
                                        simplified_emlaaei_text = simplified_emlaaei_text.Trim();
                                        while (simplified_emlaaei_text.Contains("  "))
                                        {
                                            simplified_emlaaei_text = simplified_emlaaei_text.Replace("  ", " ");
                                        }

                                        if (text_location == TextLocation.AllWords)
                                        {
                                            bool found = false;
                                            foreach (string pattern_word in negative_words)
                                            {
                                                if (simplified_emlaaei_text.Contains(pattern_word))
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found) continue;

                                            foreach (string pattern_word in positive_words)
                                            {
                                                if (!simplified_emlaaei_text.Contains(pattern_word))
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found) continue;

                                            if (
                                                 (unsigned_words.Count == 0) ||
                                                 (simplified_emlaaei_text.ContainsWordsOf(unsigned_words))
                                               )
                                            {
                                                result.Add(new Phrase(verse, 0, ""));
                                            }
                                        }
                                        else if (text_location == TextLocation.AnyWord)
                                        {
                                            bool found = false;
                                            foreach (string pattern_word in negative_words)
                                            {
                                                if (simplified_emlaaei_text.Contains(pattern_word))
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found) continue;

                                            foreach (string pattern_word in positive_words)
                                            {
                                                if (!simplified_emlaaei_text.Contains(pattern_word))
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found) continue;

                                            if (
                                                 (negative_words.Count > 0) ||
                                                 (positive_words.Count > 0) ||
                                                 (
                                                   (unsigned_words.Count == 0) ||
                                                   (simplified_emlaaei_text.ContainsWordOf(unsigned_words))
                                                 )
                                               )
                                            {
                                                result.Add(new Phrase(verse, 0, ""));
                                            }
                                        }
                                        else // at start, middle, end, or anywhere
                                        {
                                            MatchCollection matches = Regex.Matches(simplified_emlaaei_text, pattern, regex_options);
                                            if (multiplicity != -1) // with multiplicity
                                            {
                                                if (matches.Count >= multiplicity)
                                                {
                                                    // don't colorize emaleei matches to let user know this is unofficial spelling
                                                    //result.AddRange(BuildPhrases(verse, matches));
                                                    result.Add(new Phrase(verse, 0, ""));
                                                }
                                            }
                                            else // without multiplicity
                                            {
                                                if (matches.Count > 0)
                                                {
                                                    // don't colorize emaleei matches to let user know this is unofficial spelling
                                                    //result.AddRange(BuildPhrases(verse, matches));
                                                    result.Add(new Phrase(verse, 0, ""));
                                                }
                                            }
                                        }
                                    } // end for
                                }
                            }
                        }
                    }
                    catch
                    {
                        // log exception
                    }
                }
            }
        }
        return result;
    }
    private static List<Phrase> DoFindPhrases(string translation, List<Verse> source, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string text, TextLocation text_location, bool case_sensitive, TextWordness wordness, int multiplicity, bool at_word_start)
    {
        List<Phrase> result = new List<Phrase>();
        List<Verse> found_verses = new List<Verse>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                if (!String.IsNullOrEmpty(text))
                {
                    RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    if (text.IsArabic()) // Arabic letters in translation (Emlaaei, Urdu, Farsi, etc.) 
                    {
                        regex_options |= RegexOptions.RightToLeft;
                    }

                    try
                    {
                        string pattern_empty_line = @"^$";
                        string pattern_whole_line = "(" + @"^" + text + @"$" + ")";

                        string pattern_any_with_prefix = "(" + @"\S+?" + text + ")";
                        string pattern_any_with_prefix_and_suffix = "(" + @"\S+?" + text + @"\S+?" + ")";
                        string pattern_any_with_suffix = "(" + text + @"\S+?" + ")";

                        string pattern_word_with_prefix = "(" + pattern_any_with_prefix + @"\b" + ")";
                        string pattern_word_with_prefix_and_suffix = "(" + pattern_any_with_prefix_and_suffix + ")";
                        string pattern_word_with_suffix = "(" + @"\b" + pattern_any_with_suffix + ")";
                        string pattern_word_with_any_fixes = "(" + pattern_word_with_prefix + "|" + pattern_word_with_prefix_and_suffix + "|" + pattern_any_with_suffix + ")";

                        // Whole word
                        string pattern_whole_word_at_start = "(" + pattern_whole_line + "|" + @"^" + text + @"\b" + ")";
                        string pattern_whole_word_at_middle = "(" + pattern_whole_line + "|" + @"(?<!^)" + @"\b" + text + @"\b" + @"(?!$)" + ")";
                        string pattern_whole_word_at_end = "(" + pattern_whole_line + "|" + @"\b" + text + @"$" + ")";
                        string pattern_whole_word_anywhere = "(" + pattern_whole_line + "|" + @"\b" + text + @"\b" + ")";

                        // Part of word
                        string pattern_part_word_at_start = "(" + @"^" + pattern_word_with_any_fixes + ")";
                        string pattern_part_word_at_middle = "(" + @"(?<!^)" + pattern_word_with_any_fixes + @"(?!$)" + ")";
                        string pattern_part_word_at_end = "(" + pattern_word_with_any_fixes + @"$" + ")";
                        string pattern_part_word_anywhere = "(" + pattern_part_word_at_start + "|" + pattern_part_word_at_middle + "|" + pattern_part_word_at_end + ")";

                        // Any == Whole word | Part of word
                        string pattern_any_at_start = "(" + pattern_whole_line + "|" + @"^" + text + ")";
                        string pattern_any_at_middle = "(" + pattern_whole_line + "|" + @"(?<!^)" + text + @"(?!$)" + ")";
                        string pattern_any_at_end = "(" + pattern_whole_line + "|" + text + @"$" + ")";
                        string pattern_any_anywhere = text;

                        string pattern = null;
                        List<string> negative_words = new List<string>();
                        List<string> positive_words = new List<string>();
                        List<string> unsigned_words = new List<string>();

                        if (at_word_start)
                        {
                            pattern = @"(?<=\b)(" + pattern + @")"; // positive lookbehind
                        }

                        switch (text_location)
                        {
                            case TextLocation.Anywhere:
                                {
                                    if (wordness == TextWordness.WholeWord)
                                    {
                                        pattern += pattern_whole_word_anywhere;
                                    }
                                    else if (wordness == TextWordness.PartOfWord)
                                    {
                                        pattern += pattern_part_word_anywhere;
                                    }
                                    else if (wordness == TextWordness.Any)
                                    {
                                        pattern += pattern_any_anywhere;
                                    }
                                    else
                                    {
                                        pattern += pattern_empty_line;
                                    }
                                }
                                break;
                            case TextLocation.AtStart:
                                {
                                    if (wordness == TextWordness.WholeWord)
                                    {
                                        pattern += pattern_whole_word_at_start;
                                    }
                                    else if (wordness == TextWordness.PartOfWord)
                                    {
                                        pattern += pattern_part_word_at_start;
                                    }
                                    else if (wordness == TextWordness.Any)
                                    {
                                        pattern += pattern_any_at_start;
                                    }
                                    else
                                    {
                                        pattern += pattern_empty_line;
                                    }
                                }
                                break;
                            case TextLocation.AtMiddle:
                                {
                                    if (wordness == TextWordness.WholeWord)
                                    {
                                        pattern += pattern_whole_word_at_middle;
                                    }
                                    else if (wordness == TextWordness.PartOfWord)
                                    {
                                        pattern += pattern_part_word_at_middle;
                                    }
                                    else if (wordness == TextWordness.Any)
                                    {
                                        pattern += pattern_any_at_middle;
                                    }
                                    else
                                    {
                                        pattern += pattern_empty_line;
                                    }
                                }
                                break;
                            case TextLocation.AtEnd:
                                {
                                    if (wordness == TextWordness.WholeWord)
                                    {
                                        pattern += pattern_whole_word_at_end;
                                    }
                                    else if (wordness == TextWordness.PartOfWord)
                                    {
                                        pattern += pattern_part_word_at_end;
                                    }
                                    else if (wordness == TextWordness.Any)
                                    {
                                        pattern += pattern_any_at_end;
                                    }
                                    else
                                    {
                                        pattern += pattern_empty_line;
                                    }
                                }
                                break;
                            case TextLocation.AllWords:
                            case TextLocation.AnyWord:
                                {
                                    pattern = Regex.Replace(text.Trim(), @"\s+", " "); // remove double space or higher if any

                                    string[] pattern_words = pattern.Split();
                                    foreach (string pattern_word in pattern_words)
                                    {
                                        if (pattern_word.StartsWith("-"))
                                        {
                                            negative_words.Add(pattern_word.Substring(1));
                                        }
                                        else if (pattern_word.EndsWith("-"))
                                        {
                                            negative_words.Add(pattern_word.Substring(0, pattern_word.Length - 1));
                                        }
                                        else if (pattern_word.StartsWith("+"))
                                        {
                                            positive_words.Add(pattern_word.Substring(1));
                                        }
                                        else if (pattern_word.EndsWith("+"))
                                        {
                                            positive_words.Add(pattern_word.Substring(0, pattern_word.Length - 1));
                                        }
                                        else
                                        {
                                            unsigned_words.Add(pattern_word);
                                        }
                                    }
                                }
                                break;
                            default:
                                {
                                    return new List<Phrase>();
                                }
                        }

                        // do actual search
                        foreach (Verse verse in source)
                        {
                            if (text_location == TextLocation.AllWords)
                            {
                                bool found = false;
                                foreach (string negative_word in negative_words)
                                {
                                    if (verse.Translations[translation].Contains(negative_word))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (found) continue;

                                foreach (string positive_word in positive_words)
                                {
                                    if (!verse.Translations[translation].Contains(positive_word))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (found) continue;

                                if (
                                     (unsigned_words.Count == 0) ||
                                     (verse.Translations[translation].ContainsWordsOf(unsigned_words))
                                   )
                                {
                                    found_verses.Add(verse);
                                    result.Add(new Phrase(verse, 0, ""));
                                }
                            }
                            else if (text_location == TextLocation.AnyWord)
                            {
                                bool skip = false;
                                foreach (string negative_word in negative_words)
                                {
                                    if (verse.Translations[translation].Contains(negative_word))
                                    {
                                        skip = true;
                                        break;
                                    }
                                }
                                if (skip) continue;

                                foreach (string positive_word in positive_words)
                                {
                                    if (!verse.Translations[translation].Contains(positive_word))
                                    {
                                        skip = true;
                                        break;
                                    }
                                }
                                if (skip) continue;

                                if (
                                     (negative_words.Count > 0) ||
                                     (positive_words.Count > 0) ||
                                     (
                                       (unsigned_words.Count == 0) ||
                                       (verse.Translations[translation].ContainsWordOf(unsigned_words))
                                     )
                                   )
                                {
                                    found_verses.Add(verse);
                                    result.Add(new Phrase(verse, 0, ""));
                                }
                            }
                            else // at start, middle, end, or anywhere
                            {
                                MatchCollection matches = Regex.Matches(verse.Translations[translation], pattern, regex_options);
                                if (multiplicity != -1) // with multiplicity
                                {
                                    if (matches.Count >= multiplicity)
                                    {
                                        found_verses.Add(verse);
                                        if (matches.Count > 0)
                                        {
                                            result.AddRange(BuildPhrasesAndOriginify(verse, matches));
                                        }
                                        else
                                        {
                                            result.Add(new Phrase(verse, 0, ""));
                                        }
                                    }
                                }
                                else // without multiplicity
                                {
                                    if (matches.Count > 0)
                                    {
                                        found_verses.Add(verse);
                                        result.AddRange(BuildPhrasesAndOriginify(verse, matches));
                                    }
                                }
                            }
                        } // end for
                    }
                    catch
                    {
                        // log exception
                    }
                }
            }
        }
        return result;
    }
    private static string BuildPattern(string text,
                                       TextLocation text_location, TextWordness wordness, bool at_word_start,
                                       out List<string> unsigned_words,
                                       out List<string> positive_words,
                                       out List<string> negative_words
                                      )
    {
        string pattern = null;
        unsigned_words = new List<string>();
        positive_words = new List<string>();
        negative_words = new List<string>();

        if (String.IsNullOrEmpty(text)) return text;
        text = text.Trim();

        /*
        =====================================================================
        Regular Expressions (RegEx)
        =====================================================================
        Best Reference: http://www.regular-expressions.info/
        =====================================================================
        Matches	Characters 
        x	character x 
        \\	backslash character 
        \0n	character with octal value 0n (0 <= n <= 7) 
        \0nn	character with octal value 0nn (0 <= n <= 7) 
        \0mnn	character with octal value 0mnn (0 <= m <= 3, 0 <= n <= 7) 
        \xhh	character with hexadecimal value 0xhh 
        \uhhhh	character with hexadecimal value 0xhhhh 
        \t	tab character ('\u0009') 
        \n	newline (line feed) character ('\u000A') 
        \r	carriage-return character ('\u000D') 
        \f	form-feed character ('\u000C') 
        \a	alert (bell) character ('\u0007') 
        \e	escape character ('\u001B') 
        \cx	control character corresponding to x 
                                  
        Character Classes 
        [abc]		    a, b, or c				                    (simple class) 
        [^abc]		    any character except a, b, or c		        (negation) 
        [a-zA-Z]	    a through z or A through Z, inclusive	    (range) 
        [a-d[m-p]]	    a through d, or m through p: [a-dm-p]	    (union) 
        [a-z&&[def]]	d, e, or f				                    (intersection) 
        [a-z&&[^bc]]	a through z, except for b and c: [ad-z]	    (subtraction) 
        [a-z&&[^m-p]]	a through z, and not m through p: [a-lq-z]  (subtraction) 
                                  
        Predefined 
        .	any character (inc line terminators) except newline 
        \d	digit				            [0-9] 
        \D	non-digit			            [^0-9] 
        \s	whitespace character		    [ \t\n\x0B\f\r] 
        \S	non-whitespace character	    [^\s] 
        \w	word character (alphanumeric)	[a-zA-Z_0-9] 
        \W	non-word character		        [^\w] 

        Boundary Matchers 
        ^	beginning of a line	(in Multiline)
        $	end of a line  		(in Multiline)
        \b	word boundary 
        \B	non-word boundary 
        \A	beginning of the input 
        \G	end of the previous match 
        \Z	end of the input but for the final terminator, if any 
        \z	end of the input

        Greedy quantifiers 
        X?	X, once or not at all 
        X*	X, zero or more times 
        X+	X, one or more times 
        X{n}	X, exactly n times 
        X{n,}	X, at least n times 
        X{n,m}	X, at least n but not more than m times 
                                  
        Reluctant quantifiers 
        X??	X, once or not at all 
        X*?	X, zero or more times 
        X+?	X, one or more times 
        X{n}?	X, exactly n times 
        X{n,}?	X, at least n times 
        X{n,m}?	X, at least n but not more than m times 
                                  
        Possessive quantifiers 
        X?+	X, once or not at all 
        X*+	X, zero or more times 
        X++	X, one or more times 
        X{n}+	X, exactly n times 
        X{n,}+	X, at least n times 
        X{n,m}+	X, at least n but not more than m times 

        positive lookahead	(?=text)
        negative lookahead	(?!text)
        // eg: not at end of line 	    (?!$)
        positive lookbehind	(?<=text)
        negative lookbehind	(?<!text)
        // eg: not at start of line 	(?<!^)
        =====================================================================
        */

        string pattern_empty_line = @"^$";
        string pattern_whole_line = "(" + @"^" + text + @"$" + ")";

        string pattern_any_with_prefix = "(" + @"\S+?" + text + ")";
        string pattern_any_with_prefix_and_suffix = "(" + @"\S+?" + text + @"\S+?" + ")";
        string pattern_any_with_suffix = "(" + text + @"\S+?" + ")";

        string pattern_word_with_prefix = "(" + pattern_any_with_prefix + @"\b" + ")";
        string pattern_word_with_prefix_and_suffix = "(" + pattern_any_with_prefix_and_suffix + ")";
        string pattern_word_with_suffix = "(" + @"\b" + pattern_any_with_suffix + ")";
        string pattern_word_with_any_fixes = "(" + pattern_word_with_prefix + "|" + pattern_word_with_prefix_and_suffix + "|" + pattern_any_with_suffix + ")";

        // Whole word
        string pattern_whole_word_at_start = "(" + pattern_whole_line + "|" + @"^" + text + @"\b" + ")";
        string pattern_whole_word_at_middle = "(" + pattern_whole_line + "|" + @"(?<!^)" + @"\b" + text + @"\b" + @"(?!$)" + ")";
        string pattern_whole_word_at_end = "(" + pattern_whole_line + "|" + @"\b" + text + @"$" + ")";
        string pattern_whole_word_anywhere = "(" + pattern_whole_line + "|" + @"\b" + text + @"\b" + ")";

        // Part of word
        string pattern_part_word_at_start = "(" + @"^" + pattern_word_with_any_fixes + ")";
        string pattern_part_word_at_middle = "(" + @"(?<!^)" + pattern_word_with_any_fixes + @"(?!$)" + ")";
        string pattern_part_word_at_end = "(" + pattern_word_with_any_fixes + @"$" + ")";
        string pattern_part_word_anywhere = "(" + pattern_part_word_at_start + "|" + pattern_part_word_at_middle + "|" + pattern_part_word_at_end + ")";

        // Any == Whole word | Part of word
        string pattern_any_at_start = "(" + pattern_whole_line + "|" + @"^" + text + ")";
        string pattern_any_at_middle = "(" + pattern_whole_line + "|" + @"(?<!^)" + text + @"(?!$)" + ")";
        string pattern_any_at_end = "(" + pattern_whole_line + "|" + text + @"$" + ")";
        string pattern_any_anywhere = text;

        if (at_word_start)
        {
            pattern = @"(?<=\b)(" + pattern + @")"; // positive lookbehind
        }

        switch (text_location)
        {
            case TextLocation.Anywhere:
                {
                    if (wordness == TextWordness.WholeWord)
                    {
                        pattern += pattern_whole_word_anywhere;
                    }
                    else if (wordness == TextWordness.PartOfWord)
                    {
                        pattern += pattern_part_word_anywhere;
                    }
                    else if (wordness == TextWordness.Any)
                    {
                        pattern += pattern_any_anywhere;
                    }
                    else
                    {
                        pattern += pattern_empty_line;
                    }
                }
                break;
            case TextLocation.AtStart:
                {
                    if (wordness == TextWordness.WholeWord)
                    {
                        pattern += pattern_whole_word_at_start;
                    }
                    else if (wordness == TextWordness.PartOfWord)
                    {
                        pattern += pattern_part_word_at_start;
                    }
                    else if (wordness == TextWordness.Any)
                    {
                        pattern += pattern_any_at_start;
                    }
                    else
                    {
                        pattern += pattern_empty_line;
                    }
                }
                break;
            case TextLocation.AtMiddle:
                {
                    if (wordness == TextWordness.WholeWord)
                    {
                        pattern += pattern_whole_word_at_middle;
                    }
                    else if (wordness == TextWordness.PartOfWord)
                    {
                        pattern += pattern_part_word_at_middle;
                    }
                    else if (wordness == TextWordness.Any)
                    {
                        pattern += pattern_any_at_middle;
                    }
                    else
                    {
                        pattern += pattern_empty_line;
                    }
                }
                break;
            case TextLocation.AtEnd:
                {
                    if (wordness == TextWordness.WholeWord)
                    {
                        pattern += pattern_whole_word_at_end;
                    }
                    else if (wordness == TextWordness.PartOfWord)
                    {
                        pattern += pattern_part_word_at_end;
                    }
                    else if (wordness == TextWordness.Any)
                    {
                        pattern += pattern_any_at_end;
                    }
                    else
                    {
                        pattern += pattern_empty_line;
                    }
                }
                break;
            case TextLocation.AllWords:
            case TextLocation.AnyWord:
                {
                    pattern = Regex.Replace(text.Trim(), @"\s+", " "); // remove double space or higher if any

                    string[] pattern_words = pattern.Split();
                    foreach (string pattern_word in pattern_words)
                    {
                        if (pattern_word.StartsWith("-"))
                        {
                            if (negative_words != null)
                            {
                                negative_words.Add(pattern_word.Substring(1));
                            }
                        }
                        else if (pattern_word.EndsWith("-"))
                        {
                            if (negative_words != null)
                            {
                                negative_words.Add(pattern_word.Substring(0, pattern_word.Length - 1));
                            }
                        }
                        else if (pattern_word.StartsWith("+"))
                        {
                            if (positive_words != null)
                            {
                                positive_words.Add(pattern_word.Substring(1));
                            }
                        }
                        else if (pattern_word.EndsWith("+"))
                        {
                            if (positive_words != null)
                            {
                                positive_words.Add(pattern_word.Substring(0, pattern_word.Length - 1));
                            }
                        }
                        else
                        {
                            if (unsigned_words != null)
                            {
                                unsigned_words.Add(pattern_word);
                            }
                        }
                    }
                }
                break;
        }

        return pattern;
    }
    // find by text - Root
    public static List<Phrase> FindPhrases(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string roots, int multiplicity, bool with_diacritics)
    {
        List<Phrase> result = new List<Phrase>();
        List<Verse> found_verses = null;

        while (roots.Contains("  "))
        {
            roots = roots.Replace("  ", " ");
        }

        string[] parts = roots.Split();
        if (parts.Length == 0)
        {
            return result;
        }
        else if (parts.Length == 1)
        {
            return DoFindPhrases(book, find_scope, current_selection, previous_result, roots, multiplicity, with_diacritics);
        }
        else if (parts.Length > 1) // enable nested searches
        {
            if (roots.Length > 1) // enable nested searches
            {
                List<Phrase> phrases = null;

                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> neutral_words = new List<string>();
                foreach (string part in parts)
                {
                    if ((part.StartsWith("-")) || (part.EndsWith("-")))
                    {
                        int index = part.IndexOf("-");
                        negative_words.Add(part.Remove(index, 1));
                    }
                    else if ((part.StartsWith("+")) || (part.EndsWith("+")))
                    {
                        int index = part.IndexOf("+");
                        positive_words.Add(part.Remove(index, 1));
                    }
                    else
                    {
                        neutral_words.Add(part);
                    }
                }

                foreach (string negative_word in negative_words)
                {
                    phrases = DoFindPhrases(book, find_scope, current_selection, found_verses, negative_word, 0, with_diacritics); // multiplicity = 0 for exclude
                    AddToResult(phrases, ref found_verses, ref find_scope, ref result);
                }

                foreach (string positive_word in positive_words)
                {
                    phrases = DoFindPhrases(book, find_scope, current_selection, found_verses, positive_word, multiplicity, with_diacritics);
                    AddToResult(phrases, ref found_verses, ref find_scope, ref result);
                }

                foreach (string neutral_word in neutral_words)
                {
                    phrases = DoFindPhrases(book, find_scope, current_selection, found_verses, neutral_word, multiplicity, with_diacritics);
                    AddToResult(phrases, ref found_verses, ref find_scope, ref result);
                }
            }
        }
        return result;
    }
    private static List<Phrase> DoFindPhrases(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string root, int multiplicity, bool with_diacritics)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindPhrases(source, find_scope, current_selection, previous_result, root, multiplicity, with_diacritics);
    }
    private static List<Phrase> DoFindPhrases(List<Verse> source, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string root, int multiplicity, bool with_diacritics)
    {
        List<Phrase> result = new List<Phrase>();
        List<Verse> found_verses = new List<Verse>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                if (root.Length > 0)
                {
                    try
                    {
                        Dictionary<string, List<Word>> root_words_dictionary = book.RootWords;
                        if (root_words_dictionary != null)
                        {
                            List<Word> root_words = null;
                            if (root_words_dictionary.ContainsKey(root))
                            {
                                // get all pre-identified root_words
                                root_words = root_words_dictionary[root];
                            }
                            else // if no such root, search for the matching root_word by its verse position and get its root and then get all root_words
                            {
                                string new_root = book.GetBestRoot(root, with_diacritics);
                                if (!String.IsNullOrEmpty(new_root))
                                {
                                    // get all pre-identified root_words for new root
                                    root_words = root_words_dictionary[new_root];
                                }
                            }

                            if (root_words != null)
                            {
                                result = GetPhrasesWithRootWords(source, root_words, multiplicity, with_diacritics);
                                foreach (Phrase phrase in result)
                                {
                                    if (phrase != null)
                                    {
                                        if (!found_verses.Contains(phrase.Verse))
                                        {
                                            found_verses.Add(phrase.Verse);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // log exception
                    }
                }
            }
        }
        return result;
    }
    private static List<Phrase> GetPhrasesWithRootWords(List<Verse> source, List<Word> root_words, int multiplicity, bool with_diacritics)
    {
        //with_diacritics is not used. may not be needed.
        List<Phrase> result = new List<Phrase>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                Dictionary<Verse, int> multiplicity_dictionary = new Dictionary<Verse, int>();
                foreach (Word word in root_words)
                {
                    Verse verse = book.Verses[word.Verse.Number - 1];
                    if (source.Contains(verse))
                    {
                        if (multiplicity_dictionary.ContainsKey(verse))
                        {
                            multiplicity_dictionary[verse]++;
                        }
                        else // first find
                        {
                            multiplicity_dictionary.Add(verse, 1);
                        }
                    }
                }

                if (multiplicity == 0) // verses not containg word
                {
                    foreach (Verse verse in source)
                    {
                        if (!multiplicity_dictionary.ContainsKey(verse))
                        {
                            Phrase phrase = new Phrase(verse, 0, "");
                            result.Add(phrase);
                        }
                    }
                }
                else // add only matching multiplicity or wildcard (-1)
                {
                    foreach (Word root_word in root_words)
                    {
                        Verse verse = book.Verses[root_word.Verse.Number - 1];
                        if ((multiplicity == -1) || (multiplicity_dictionary[verse] >= multiplicity))
                        {
                            if (source.Contains(verse))
                            {
                                int word_index = root_word.NumberInVerse - 1;
                                string word_text = verse.Words[word_index].Text;
                                int word_position = verse.Words[word_index].Position;
                                Phrase phrase = new Phrase(verse, word_position, word_text);
                                result.Add(phrase);
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
    private static void AddToResult(List<Phrase> phrases, ref List<Verse> source, ref FindScope find_scope, ref List<Phrase> previous_result)
    {
        if (phrases != null)
        {
            List<Verse> verses = new List<Verse>(GetVerses(phrases));
            // if first result
            if (source == null)
            {
                // fill it up with a copy of the first root search result
                previous_result = new List<Phrase>(phrases);
                source = new List<Verse>(verses);

                // prepare for nested search by search
                find_scope = FindScope.Result;
            }
            else // subsequent search result
            {
                source = new List<Verse>(verses);

                List<Phrase> union_phrases = new List<Phrase>(phrases);
                if (previous_result != null)
                {
                    foreach (Phrase phrase in previous_result)
                    {
                        if (phrase != null)
                        {
                            if (verses.Contains(phrase.Verse))
                            {
                                union_phrases.Add(phrase);
                            }
                        }
                    }
                }
                previous_result = union_phrases;
            }
        }
    }

    // find by similarity - phrases similar to given text
    public static List<Phrase> FindPhrases(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string text, double similarity_percentage)
    {
        List<Phrase> result = new List<Phrase>();
        List<Verse> found_verses = null;

        while (text.Contains("  "))
        {
            text = text.Replace("  ", " ");
        }
        while (text.Contains("+"))
        {
            text = text.Replace("+", "");
        }
        while (text.Contains("-"))
        {
            text = text.Replace("-", "");
        }

        string[] word_texts = text.Split();
        if (word_texts.Length == 0)
        {
            return result;
        }
        else if (word_texts.Length == 1)
        {
            return DoFindPhrases(book, find_scope, current_selection, previous_result, text, similarity_percentage);
        }
        else if (word_texts.Length > 1) // enable nested searches
        {
            if (text.Length > 1) // enable nested searches
            {
                List<Phrase> phrases = null;
                List<Verse> verses = null;

                foreach (string word_text in word_texts)
                {
                    phrases = DoFindPhrases(book, find_scope, current_selection, found_verses, word_text, similarity_percentage);
                    verses = new List<Verse>(GetVerses(phrases));

                    // if first result
                    if (found_verses == null)
                    {
                        // fill it up with a copy of the first similar word search result
                        result = new List<Phrase>(phrases);
                        found_verses = new List<Verse>(verses);

                        // prepare for nested search by search
                        find_scope = FindScope.Result;
                    }
                    else // subsequent search result
                    {
                        found_verses = new List<Verse>(verses);

                        List<Phrase> union_phrases = new List<Phrase>(phrases);
                        foreach (Phrase phrase in result)
                        {
                            if (phrase != null)
                            {
                                if (verses.Contains(phrase.Verse))
                                {
                                    union_phrases.Add(phrase);
                                }
                            }
                        }
                        result = union_phrases;
                    }
                }
            }
        }
        return result;
    }
    private static List<Phrase> DoFindPhrases(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string word_text, double similarity_percentage)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindPhrases(source, find_scope, current_selection, previous_result, word_text, similarity_percentage);
    }
    private static List<Phrase> DoFindPhrases(List<Verse> source, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string word_text, double similarity_percentage)
    {
        List<Phrase> result = new List<Phrase>();
        List<Verse> found_verses = new List<Verse>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                if (!String.IsNullOrEmpty(word_text))
                {
                    Book book = source[0].Book;
                    if (!String.IsNullOrEmpty(word_text))
                    {
                        try
                        {
                            foreach (Verse verse in source)
                            {
                                foreach (Word word in verse.Words)
                                {
                                    if (word.Text.IsSimilarTo(word_text, similarity_percentage))
                                    {
                                        if (!found_verses.Contains(verse))
                                        {
                                            found_verses.Add(verse);
                                        }
                                        result.Add(new Phrase(verse, word.Position, word.Text));
                                    }
                                }
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
        return result;
    }
    // find by similarity - verse similar to given verse
    public static List<Verse> FindVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, Verse verse, SimilarityMethod similarity_method, double similarity_percentage)
    {
        return DoFindVerses(book, find_scope, current_selection, previous_result, verse, similarity_method, similarity_percentage);
    }
    private static List<Verse> DoFindVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, Verse verse, SimilarityMethod similarity_method, double similarity_percentage)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindVerses(source, find_scope, current_selection, previous_result, verse, similarity_method, similarity_percentage);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, FindScope find_scope, Selection current_selection, List<Verse> previous_result, Verse verse, SimilarityMethod find_similarity_method, double similarity_percentage)
    {
        List<Verse> result = new List<Verse>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                if (verse != null)
                {
                    switch (find_similarity_method)
                    {
                        case SimilarityMethod.SimilarText:
                            {
                                for (int j = 0; j < source.Count; j++)
                                {
                                    if (verse.Text.IsSimilarTo(source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarWords:
                            {
                                for (int j = 0; j < source.Count; j++)
                                {
                                    if (verse.Text.HasSimilarWordsTo(source[j].Text, (int)Math.Round((Math.Min(verse.Words.Count, source[j].Words.Count) * similarity_percentage)), 1.0))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarFirstHalf:
                            {
                                for (int j = 0; j < source.Count; j++)
                                {
                                    if (verse.Text.HasSimilarFirstHalfTo(source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarLastHalf:
                            {
                                for (int j = 0; j < source.Count; j++)
                                {
                                    if (verse.Text.HasSimilarLastHalfTo(source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarFirstWord:
                            {
                                for (int j = 0; j < source.Count; j++)
                                {
                                    if (verse.Text.HasSimilarFirstWordTo(source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarLastWord:
                            {
                                for (int j = 0; j < source.Count; j++)
                                {
                                    if (verse.Text.HasSimilarLastWordTo(source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        return result;
    }
    // find by similarity - all similar verses to each other throughout the book
    public static List<List<Verse>> FindVerseRanges(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, SimilarityMethod similarity_method, double similarity_percentage)
    {
        return DoFindVerseRanges(book, find_scope, current_selection, previous_result, similarity_method, similarity_percentage);
    }
    private static List<List<Verse>> DoFindVerseRanges(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, SimilarityMethod similarity_method, double similarity_percentage)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindVerseRanges(source, find_scope, current_selection, previous_result, similarity_method, similarity_percentage);
    }
    private static List<List<Verse>> DoFindVerseRanges(List<Verse> source, FindScope find_scope, Selection current_selection, List<Verse> previous_result, SimilarityMethod find_similarity_method, double similarity_percentage)
    {
        List<List<Verse>> result = new List<List<Verse>>();
        Dictionary<Verse, List<Verse>> verse_ranges = new Dictionary<Verse, List<Verse>>(); // need dictionary to check if key exist
        bool[] already_compared = new bool[Verse.MAX_NUMBER];
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                switch (find_similarity_method)
                {
                    case SimilarityMethod.SimilarText:
                        {
                            for (int i = 0; i < source.Count - 1; i++)
                            {
                                for (int j = i + 1; j < source.Count; j++)
                                {
                                    if (!already_compared[j])
                                    {
                                        if (source[i].Text.IsSimilarTo(source[j].Text, similarity_percentage))
                                        {
                                            if (!verse_ranges.ContainsKey(source[i])) // first time matching verses found
                                            {
                                                List<Verse> similar_verses = new List<Verse>();
                                                verse_ranges.Add(source[i], similar_verses);
                                                similar_verses.Add(source[i]);
                                                similar_verses.Add(source[j]);
                                                already_compared[i] = true;
                                                already_compared[j] = true;
                                            }
                                            else // matching verses already exists
                                            {
                                                List<Verse> similar_verses = verse_ranges[source[i]];
                                                similar_verses.Add(source[j]);
                                                already_compared[j] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case SimilarityMethod.SimilarWords:
                        {
                            for (int i = 0; i < source.Count - 1; i++)
                            {
                                for (int j = i + 1; j < source.Count; j++)
                                {
                                    if (!already_compared[j])
                                    {
                                        if (source[i].Text.HasSimilarWordsTo(source[j].Text, (int)Math.Round((Math.Min(source[i].Words.Count, source[j].Words.Count) * similarity_percentage)), 1.0))
                                        {
                                            if (!verse_ranges.ContainsKey(source[i])) // first time matching verses found
                                            {
                                                List<Verse> similar_verses = new List<Verse>();
                                                verse_ranges.Add(source[i], similar_verses);
                                                similar_verses.Add(source[i]);
                                                similar_verses.Add(source[j]);
                                                already_compared[i] = true;
                                                already_compared[j] = true;
                                            }
                                            else // matching verses already exists
                                            {
                                                List<Verse> similar_verses = verse_ranges[source[i]];
                                                similar_verses.Add(source[j]);
                                                already_compared[j] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case SimilarityMethod.SimilarFirstWord:
                        {
                            for (int i = 0; i < source.Count - 1; i++)
                            {
                                for (int j = i + 1; j < source.Count; j++)
                                {
                                    if (!already_compared[j])
                                    {
                                        if (source[j].Text.HasSimilarFirstWordTo(source[j].Text, similarity_percentage))
                                        {
                                            if (!verse_ranges.ContainsKey(source[i])) // first time matching verses found
                                            {
                                                List<Verse> similar_verses = new List<Verse>();
                                                verse_ranges.Add(source[i], similar_verses);
                                                similar_verses.Add(source[i]);
                                                similar_verses.Add(source[j]);
                                                already_compared[i] = true;
                                                already_compared[j] = true;
                                            }
                                            else // matching verses already exists
                                            {
                                                List<Verse> similar_verses = verse_ranges[source[i]];
                                                similar_verses.Add(source[j]);
                                                already_compared[j] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case SimilarityMethod.SimilarLastWord:
                        {
                            for (int i = 0; i < source.Count - 1; i++)
                            {
                                for (int j = i + 1; j < source.Count; j++)
                                {
                                    if (!already_compared[j])
                                    {
                                        if (source[i].Text.HasSimilarLastWordTo(source[j].Text, similarity_percentage))
                                        {
                                            if (!verse_ranges.ContainsKey(source[i])) // first time matching verses found
                                            {
                                                List<Verse> similar_verses = new List<Verse>();
                                                verse_ranges.Add(source[i], similar_verses);
                                                similar_verses.Add(source[i]);
                                                similar_verses.Add(source[j]);
                                                already_compared[i] = true;
                                                already_compared[j] = true;
                                            }
                                            else // matching verses already exists
                                            {
                                                List<Verse> similar_verses = verse_ranges[source[i]];
                                                similar_verses.Add(source[j]);
                                                already_compared[j] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        // copy dictionary to list of list
        if (verse_ranges.Count > 0)
        {
            foreach (List<Verse> verse_range in verse_ranges.Values)
            {
                result.Add(verse_range);
            }
        }
        return result;
    }

    // find by numbers - helper methods
    private static bool Compare(Word word, NumberQuery query)
    {
        if (word != null)
        {
            long value = 0L;

            if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(word.NumberInVerse, query.Number, query.NumberComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(word.NumberInVerse, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (!Numbers.Compare(word.Letters.Count, query.LetterCount, query.LetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(word.Letters.Count, query.LetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(word.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(word.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (value == 0L) { value = CalculateValue(word); }
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(word); }
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitSumNumberType == NumberType.None)
            {
                if (query.ValueDigitSum > 0)
                {
                    if (value == 0L) { value = CalculateValue(word); }
                    if (!Numbers.Compare(Numbers.DigitSum(value), query.ValueDigitSum, query.ValueDigitSumComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(word); }
                if (!Numbers.IsNumberType(Numbers.DigitSum(value), query.ValueDigitSumNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitalRootNumberType == NumberType.None)
            {
                if (query.ValueDigitalRoot > 0)
                {
                    if (value == 0L) { value = CalculateValue(word); }
                    if (!Numbers.Compare(Numbers.DigitalRoot(value), query.ValueDigitalRoot, query.ValueDigitalRootComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(word); }
                if (!Numbers.IsNumberType(Numbers.DigitalRoot(value), query.ValueDigitalRootNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Word> range, NumberQuery query)
    {
        if (range != null)
        {
            int sum = 0;
            long value = 0L;

            if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    sum = 0;
                    foreach (Word word in range)
                    {
                        sum += word.NumberInVerse;
                    }
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Word word in range)
                {
                    sum += word.NumberInVerse;
                }
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    sum = 0;
                    foreach (Word word in range)
                    {
                        sum += word.Letters.Count;
                    }
                    if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Word word in range)
                {
                    sum += word.Letters.Count;
                }
                if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    List<char> unique_letters = new List<char>();
                    foreach (Word word in range)
                    {
                        foreach (char character in word.UniqueLetters)
                        {
                            if (!unique_letters.Contains(character))
                            {
                                unique_letters.Add(character);
                            }
                        }
                    }
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                List<char> unique_letters = new List<char>();
                foreach (Word word in range)
                {
                    foreach (char character in word.UniqueLetters)
                    {
                        if (!unique_letters.Contains(character))
                        {
                            unique_letters.Add(character);
                        }
                    }
                }
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Word word in range)
                        {
                            value += CalculateValue(word);
                        }
                    }
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Word word in range)
                    {
                        value += CalculateValue(word);
                    }
                }
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitSumNumberType == NumberType.None)
            {
                if (query.ValueDigitSum > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Word word in range)
                        {
                            value += CalculateValue(word);
                        }
                    }
                    if (!Numbers.Compare(Numbers.DigitSum(value), query.ValueDigitSum, query.ValueDigitSumComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Word word in range)
                    {
                        value += CalculateValue(word);
                    }
                }
                if (!Numbers.IsNumberType(Numbers.DigitSum(value), query.ValueDigitSumNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitalRootNumberType == NumberType.None)
            {
                if (query.ValueDigitalRoot > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Word word in range)
                        {
                            value += CalculateValue(word);
                        }
                    }
                    if (!Numbers.Compare(Numbers.DigitalRoot(value), query.ValueDigitalRoot, query.ValueDigitalRootComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Word word in range)
                    {
                        value += CalculateValue(word);
                    }
                }
                if (!Numbers.IsNumberType(Numbers.DigitalRoot(value), query.ValueDigitalRootNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Sentence sentence, NumberQuery query)
    {
        if (sentence != null)
        {
            long value = 0L;

            if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (!Numbers.Compare(sentence.WordCount, query.WordCount, query.WordCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sentence.WordCount, query.WordCountNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (!Numbers.Compare(sentence.LetterCount, query.LetterCount, query.LetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sentence.LetterCount, query.LetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(sentence.UniqueLetterCount, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sentence.UniqueLetterCount, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (value == 0L) { value = CalculateValue(sentence); }
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(sentence); }
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitSumNumberType == NumberType.None)
            {
                if (query.ValueDigitSum > 0)
                {
                    if (value == 0L) { value = CalculateValue(sentence); }
                    if (!Numbers.Compare(Numbers.DigitSum(value), query.ValueDigitSum, query.ValueDigitSumComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(sentence); }
                if (!Numbers.IsNumberType(Numbers.DigitSum(value), query.ValueDigitSumNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitalRootNumberType == NumberType.None)
            {
                if (query.ValueDigitalRoot > 0)
                {
                    if (value == 0L) { value = CalculateValue(sentence); }
                    if (!Numbers.Compare(Numbers.DigitalRoot(value), query.ValueDigitalRoot, query.ValueDigitalRootComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(sentence); }
                if (!Numbers.IsNumberType(Numbers.DigitalRoot(value), query.ValueDigitalRootNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Verse verse, NumberQuery query)
    {
        if (verse != null)
        {
            long value = 0L;

            if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(verse.NumberInChapter, query.Number, query.NumberComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(verse.NumberInChapter, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (!Numbers.Compare(verse.Words.Count, query.WordCount, query.WordCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(verse.Words.Count, query.WordCountNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (!Numbers.Compare(verse.LetterCount, query.LetterCount, query.LetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(verse.LetterCount, query.LetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(verse.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(verse.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (value == 0L) { value = CalculateValue(verse); }
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(verse); }
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitSumNumberType == NumberType.None)
            {
                if (query.ValueDigitSum > 0)
                {
                    if (value == 0L) { value = CalculateValue(verse); }
                    if (!Numbers.Compare(Numbers.DigitSum(value), query.ValueDigitSum, query.ValueDigitSumComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(verse); }
                if (!Numbers.IsNumberType(Numbers.DigitSum(value), query.ValueDigitSumNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitalRootNumberType == NumberType.None)
            {
                if (query.ValueDigitalRoot > 0)
                {
                    if (value == 0L) { value = CalculateValue(verse); }
                    if (!Numbers.Compare(Numbers.DigitalRoot(value), query.ValueDigitalRoot, query.ValueDigitalRootComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(verse); }
                if (!Numbers.IsNumberType(Numbers.DigitalRoot(value), query.ValueDigitalRootNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Verse> range, NumberQuery query)
    {
        if (range != null)
        {
            int sum = 0;
            long value = 0L;

            if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    sum = 0;
                    foreach (Verse verse in range)
                    {
                        sum += verse.NumberInChapter;
                    }
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Verse verse in range)
                {
                    sum += verse.NumberInChapter;
                }
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    sum = 0;
                    foreach (Verse verse in range)
                    {
                        sum += verse.Words.Count;
                    }
                    if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Verse verse in range)
                {
                    sum += verse.Words.Count;
                }
                if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    sum = 0;
                    foreach (Verse verse in range)
                    {
                        sum += verse.LetterCount;
                    }
                    if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Verse verse in range)
                {
                    sum += verse.LetterCount;
                }
                if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    List<char> unique_letters = new List<char>();
                    foreach (Verse verse in range)
                    {
                        foreach (char character in verse.UniqueLetters)
                        {
                            if (!unique_letters.Contains(character))
                            {
                                unique_letters.Add(character);
                            }
                        }
                    }
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                List<char> unique_letters = new List<char>();
                foreach (Verse verse in range)
                {
                    foreach (char character in verse.UniqueLetters)
                    {
                        if (!unique_letters.Contains(character))
                        {
                            unique_letters.Add(character);
                        }
                    }
                }
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Verse verse in range)
                        {
                            value += CalculateValue(verse);
                        }
                    }
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Verse verse in range)
                    {
                        value += CalculateValue(verse);
                    }
                }
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitSumNumberType == NumberType.None)
            {
                if (query.ValueDigitSum > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Verse verse in range)
                        {
                            value += CalculateValue(verse);
                        }
                    }
                    if (!Numbers.Compare(Numbers.DigitSum(value), query.ValueDigitSum, query.ValueDigitSumComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Verse verse in range)
                    {
                        value += CalculateValue(verse);
                    }
                }
                if (!Numbers.IsNumberType(Numbers.DigitSum(value), query.ValueDigitSumNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitalRootNumberType == NumberType.None)
            {
                if (query.ValueDigitalRoot > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Verse verse in range)
                        {
                            value += CalculateValue(verse);
                        }
                    }
                    if (!Numbers.Compare(Numbers.DigitalRoot(value), query.ValueDigitalRoot, query.ValueDigitalRootComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Verse verse in range)
                    {
                        value += CalculateValue(verse);
                    }
                }
                if (!Numbers.IsNumberType(Numbers.DigitalRoot(value), query.ValueDigitalRootNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Chapter chapter, NumberQuery query)
    {
        if (chapter != null)
        {
            long value = 0L;

            if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(chapter.Number, query.Number, query.NumberComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(chapter.Number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (!Numbers.Compare(chapter.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(chapter.Verses.Count, query.VerseCountNumberType))
                {
                    return false;
                }
            }

            if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (!Numbers.Compare(chapter.WordCount, query.WordCount, query.WordCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(chapter.WordCount, query.WordCountNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (!Numbers.Compare(chapter.LetterCount, query.LetterCount, query.LetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(chapter.LetterCount, query.LetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(chapter.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(chapter.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (value == 0L) { value = CalculateValue(chapter); }
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(chapter); }
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitSumNumberType == NumberType.None)
            {
                if (query.ValueDigitSum > 0)
                {
                    if (value == 0L) { value = CalculateValue(chapter); }
                    if (!Numbers.Compare(Numbers.DigitSum(value), query.ValueDigitSum, query.ValueDigitSumComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(chapter); }
                if (!Numbers.IsNumberType(Numbers.DigitSum(value), query.ValueDigitSumNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitalRootNumberType == NumberType.None)
            {
                if (query.ValueDigitalRoot > 0)
                {
                    if (value == 0L) { value = CalculateValue(chapter); }
                    if (!Numbers.Compare(Numbers.DigitalRoot(value), query.ValueDigitalRoot, query.ValueDigitalRootComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(chapter); }
                if (!Numbers.IsNumberType(Numbers.DigitalRoot(value), query.ValueDigitalRootNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Chapter> range, NumberQuery query)
    {
        if (range != null)
        {
            int sum = 0;
            long value = 0L;

            if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    sum = 0;
                    foreach (Chapter chapter in range)
                    {
                        sum += chapter.Number;
                    }
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Chapter chapter in range)
                {
                    sum += chapter.Number;
                }
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    sum = 0;
                    foreach (Chapter chapter in range)
                    {
                        sum += chapter.Verses.Count;
                    }
                    if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Chapter chapter in range)
                {
                    sum += chapter.Verses.Count;
                }
                if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                {
                    return false;
                }
            }

            if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    sum = 0;
                    foreach (Chapter chapter in range)
                    {
                        sum += chapter.WordCount;
                    }
                    if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Chapter chapter in range)
                {
                    sum += chapter.WordCount;
                }
                if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    sum = 0;
                    foreach (Chapter chapter in range)
                    {
                        sum += chapter.LetterCount;
                    }
                    if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sum = 0;
                foreach (Chapter chapter in range)
                {
                    sum += chapter.LetterCount;
                }
                if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    List<char> unique_letters = new List<char>();
                    foreach (Chapter chapter in range)
                    {
                        foreach (char character in chapter.UniqueLetters)
                        {
                            if (!unique_letters.Contains(character))
                            {
                                unique_letters.Add(character);
                            }
                        }
                    }
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                List<char> unique_letters = new List<char>();
                foreach (Chapter chapter in range)
                {
                    foreach (char character in chapter.UniqueLetters)
                    {
                        if (!unique_letters.Contains(character))
                        {
                            unique_letters.Add(character);
                        }
                    }
                }
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Chapter chapter in range)
                        {
                            value += CalculateValue(chapter);
                        }
                    }
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Chapter chapter in range)
                    {
                        value += CalculateValue(chapter);
                    }
                }
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitSumNumberType == NumberType.None)
            {
                if (query.ValueDigitSum > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Chapter chapter in range)
                        {
                            value += CalculateValue(chapter);
                        }
                    }
                    if (!Numbers.Compare(Numbers.DigitSum(value), query.ValueDigitSum, query.ValueDigitSumComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Chapter chapter in range)
                    {
                        value += CalculateValue(chapter);
                    }
                }
                if (!Numbers.IsNumberType(Numbers.DigitSum(value), query.ValueDigitSumNumberType))
                {
                    return false;
                }
            }

            if (query.ValueDigitalRootNumberType == NumberType.None)
            {
                if (query.ValueDigitalRoot > 0)
                {
                    if (value == 0L)
                    {
                        foreach (Chapter chapter in range)
                        {
                            value += CalculateValue(chapter);
                        }
                    }
                    if (!Numbers.Compare(Numbers.DigitalRoot(value), query.ValueDigitalRoot, query.ValueDigitalRootComparisonOperator))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L)
                {
                    foreach (Chapter chapter in range)
                    {
                        value += CalculateValue(chapter);
                    }
                }
                if (!Numbers.IsNumberType(Numbers.DigitalRoot(value), query.ValueDigitalRootNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    // find by numbers - Words
    public static List<Word> FindWords(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        return DoFindWords(book, find_scope, current_selection, previous_result, query);
    }
    private static List<Word> DoFindWords(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindWords(source, query);
    }
    private static List<Word> DoFindWords(List<Verse> source, NumberQuery query)
    {
        List<Word> result = new List<Word>();
        if (source != null)
        {
            if (query.WordCount <= 1) // ensure no range search
            {
                foreach (Verse verse in source)
                {
                    foreach (Word word in verse.Words)
                    {
                        if (Compare(word, query))
                        {
                            result.Add(word);
                        }
                    }
                }
            }
        }
        return result;
    }
    // find by numbers - WordRanges
    public static List<List<Word>> FindWordRanges(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        return DoFindWordRanges(book, find_scope, current_selection, previous_result, query);
    }
    private static List<List<Word>> DoFindWordRanges(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindWordRanges(source, query);
    }
    private static List<List<Word>> DoFindWordRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Word>> result = new List<List<Word>>();
        if (source != null)
        {
            if (query.WordCount > 1) // ensure range search
            {
                foreach (Verse verse in source) // find words within verse boundaries
                {
                    for (int i = 0; i < verse.Words.Count - query.WordCount + 1; i++)
                    {
                        // build required range
                        List<Word> range = new List<Word>();
                        for (int j = i; j < i + query.WordCount; j++)
                        {
                            range.Add(verse.Words[j]);
                        }

                        // check range
                        if (Compare(range, query))
                        {
                            result.Add(range);
                        }
                    }
                }
            }
        }
        return result;
    }
    // find by numbers - Sentences
    public static List<Sentence> FindSentences(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        return DoFindSentences(book, find_scope, current_selection, previous_result, query);
    }
    private static List<Sentence> DoFindSentences(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindSentences(source, query);
    }
    private static List<Sentence> DoFindSentences(List<Verse> source, NumberQuery query)
    {
        List<Sentence> result = new List<Sentence>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                List<Word> words = new List<Word>();
                foreach (Verse verse in source)
                {
                    words.AddRange(verse.Words);
                }

                // scan linearly for sequence of words with total Text matching letter_frequency_sum
                for (int i = 0; i < words.Count - 1; i++)
                {
                    StringBuilder str = new StringBuilder();

                    // start building word sequence
                    str.Append(words[i].Text);

                    string stopmark_text;
                    switch (words[i].Stopmark)
                    {
                        case Stopmark.None: // none
                            stopmark_text = "";
                            break;
                        case Stopmark.MustContinue:
                            stopmark_text = "ۙ"; // Laaa
                            break;
                        case Stopmark.ShouldContinue:
                            stopmark_text = "ۖ"; // Sala
                            break;
                        case Stopmark.CanStop:
                            stopmark_text = "ۚ"; // Jeem
                            break;
                        case Stopmark.CanStopAtOneOnly:
                            stopmark_text = "ۛ"; // Dots
                            break;
                        case Stopmark.ShouldStop:
                            stopmark_text = "ۗ"; // Qala
                            break;
                        case Stopmark.MustPause:
                            stopmark_text = "ۜ"; // Seen
                            break;
                        case Stopmark.MustStop:
                            stopmark_text = "ۘ"; // Meem
                            break;
                        default:
                            stopmark_text = "ۘ"; // Meem;
                            break;
                    }

                    // if word has no stopmark or must continue
                    if ((words[i].Stopmark == Stopmark.None) || (words[i].Stopmark == Stopmark.MustContinue))
                    {
                        // continue building with next words until a stopmark
                        for (int j = i + 1; j < words.Count; j++)
                        {
                            str.Append(" " + words[j].Text);

                            if (words[j].Stopmark == Stopmark.None)
                            {
                                continue; // continue building sentence
                            }
                            else // there is a stopmark
                            {
                                if (NumerologySystem.TextMode.Contains("Original"))
                                {
                                    str.Append(" " + stopmark_text);
                                }

                                if (words[j].Stopmark == Stopmark.MustContinue)
                                {
                                    continue; // continue building sentence
                                }
                                else if (
                                    (words[j].Stopmark == Stopmark.CanStopAtOneOnly) ||
                                    (words[j].Stopmark == Stopmark.ShouldContinue) ||
                                    (words[j].Stopmark == Stopmark.CanStop) ||
                                    (words[j].Stopmark == Stopmark.ShouldStop)
                                    )
                                {
                                    // a sub sentence completed
                                    Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                    if (sentence != null)
                                    {
                                        if (Compare(sentence, query))
                                        {
                                            result.Add(sentence);
                                        }
                                    }
                                    continue; // continue building a longer senetence
                                }
                                else if (words[j].Stopmark == Stopmark.MustPause)
                                {
                                    if (
                                        (words[j].Text.Simplify29() == "مَنْ".Simplify29()) ||
                                        (words[j].Text.Simplify29() == "بَلْ".Simplify29())
                                       )
                                    {
                                        continue; // continue building a longer senetence
                                    }
                                    else if (
                                        (words[j].Text.Simplify29() == "عِوَجَا".Simplify29()) ||
                                        (words[j].Text.Simplify29() == "مَّرْقَدِنَا".Simplify29()) ||
                                        (words[j].Text.Simplify29() == "مَالِيَهْ".Simplify29())
                                        )
                                    {
                                        // a sub sentence completed
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            if (Compare(sentence, query))
                                            {
                                                result.Add(sentence);
                                            }
                                        }
                                        continue; // continue building a longer senetence
                                    }
                                    else // unknown case
                                    {
                                        throw new Exception("Unknown stopmark in Quran text.");
                                    }
                                }
                                else if (words[j].Stopmark == Stopmark.MustStop)
                                {
                                    // the sentence completed
                                    Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                    if (sentence != null)
                                    {
                                        if (Compare(sentence, query))
                                        {
                                            result.Add(sentence);
                                        }
                                    }

                                    i = j; // start a new sentence after j
                                    break; // break j, start next i
                                }
                                else // unknown case
                                {
                                    throw new Exception("Unknown stopmark in Quran text.");
                                }
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
    // find by numbers - Verses
    public static List<Verse> FindVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        return DoFindVerses(book, find_scope, current_selection, previous_result, query);
    }
    private static List<Verse> DoFindVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindVerses(source, query);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, NumberQuery query)
    {
        List<Verse> result = new List<Verse>();
        if (source != null)
        {
            if (query.VerseCount <= 1) // ensure no range search
            {
                foreach (Verse verse in source)
                {
                    if (Compare(verse, query))
                    {
                        result.Add(verse);
                    }
                }
            }
        }
        return result;
    }
    // find by numbers - VerseRanges
    public static List<List<Verse>> FindVerseRanges(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        return DoFindVerseRanges(book, find_scope, current_selection, previous_result, query);
    }
    private static List<List<Verse>> DoFindVerseRanges(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindVerseRanges(source, query);
    }
    private static List<List<Verse>> DoFindVerseRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Verse>> result = new List<List<Verse>>();
        if (source != null)
        {
            if (query.VerseCount > 1) // ensure range search
            {
                for (int i = 0; i < source.Count - query.VerseCount + 1; i++)
                {
                    // build required range
                    List<Verse> range = new List<Verse>();
                    for (int j = i; j < i + query.VerseCount; j++)
                    {
                        range.Add(source[j]);
                    }

                    // check range
                    if (Compare(range, query))
                    {
                        result.Add(range);
                    }
                }
            }
        }
        return result;
    }
    // find by numbers - Chapters
    public static List<Chapter> FindChapters(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        return DoFindChapters(book, find_scope, current_selection, previous_result, query);
    }
    private static List<Chapter> DoFindChapters(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindChapters(source, query);
    }
    private static List<Chapter> DoFindChapters(List<Verse> source, NumberQuery query)
    {
        List<Chapter> result = new List<Chapter>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                List<Chapter> chapters = book.GetChapters(source);
                if (chapters != null)
                {
                    if (query.ChapterCount <= 1) // ensure no range search
                    {
                        foreach (Chapter chapter in chapters)
                        {
                            if (Compare(chapter, query))
                            {
                                result.Add(chapter);
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
    // find by numbers - ChapterRanges
    public static List<List<Chapter>> FindChapterRanges(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        return DoFindChapterRanges(book, find_scope, current_selection, previous_result, query);
    }
    private static List<List<Chapter>> DoFindChapterRanges(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindChapterRanges(source, query);
    }
    private static List<List<Chapter>> DoFindChapterRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Chapter>> result = new List<List<Chapter>>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                List<Chapter> chapters = book.GetChapters(source);
                if (chapters != null)
                {
                    if (query.ChapterCount > 1) // ensure range search
                    {
                        for (int i = 0; i < chapters.Count - query.ChapterCount + 1; i++)
                        {
                            // build required range
                            List<Chapter> range = new List<Chapter>();
                            for (int j = i; j < i + query.ChapterCount; j++)
                            {
                                range.Add(chapters[j]);
                            }

                            // check range
                            if (Compare(range, query))
                            {
                                result.Add(range);
                            }
                        }
                    }
                }
            }
        }
        return result;
    }

    // find by prostration type
    public static List<Verse> FindVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, ProstrationType prostration_type)
    {
        return DoFindVerses(book, find_scope, current_selection, previous_result, prostration_type);
    }
    private static List<Verse> DoFindVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, ProstrationType prostration_type)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindVerses(source, prostration_type);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, ProstrationType prostration_type)
    {
        List<Verse> result = new List<Verse>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                switch (prostration_type)
                {
                    case ProstrationType.None:
                        {
                            // add nothing
                        }
                        break;
                    case ProstrationType.Obligatory:
                        {
                            foreach (Verse verse in source)
                            {
                                if (verse.Prostration != null)
                                {
                                    if (verse.Prostration.Type == ProstrationType.Obligatory)
                                    {
                                        result.Add(verse);
                                    }
                                }
                            }
                        }
                        break;
                    case ProstrationType.Recommended:
                        {
                            foreach (Verse verse in source)
                            {
                                if (verse.Prostration != null)
                                {
                                    if (verse.Prostration.Type == ProstrationType.Recommended)
                                    {
                                        result.Add(verse);
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        {
                            foreach (Verse verse in source)
                            {
                                if (verse.Prostration != null)
                                {
                                    result.Add(verse);
                                }
                            }
                        }
                        break;
                }
            }
        }
        return result;
    }

    // find by revelation place
    public static List<Chapter> FindChapters(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, RevelationPlace revelation_place)
    {
        return DoFindChapters(book, find_scope, current_selection, previous_result, revelation_place);
    }
    private static List<Chapter> DoFindChapters(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, RevelationPlace revelation_place)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindChapters(source, revelation_place);
    }
    private static List<Chapter> DoFindChapters(List<Verse> source, RevelationPlace revelation_place)
    {
        List<Chapter> result = new List<Chapter>();
        List<Verse> found_verses = new List<Verse>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                Book book = source[0].Book;
                foreach (Verse verse in source)
                {
                    switch (revelation_place)
                    {
                        case RevelationPlace.None:
                            {
                                // add nothing
                            }
                            break;
                        case RevelationPlace.Makkah:
                            {
                                if (verse.Chapter != null)
                                {
                                    if (verse.Chapter.RevelationPlace == RevelationPlace.Makkah)
                                    {
                                        found_verses.Add(verse);
                                    }
                                }
                            }
                            break;
                        case RevelationPlace.Medina:
                            {
                                if (verse.Chapter != null)
                                {
                                    if (verse.Chapter.RevelationPlace == RevelationPlace.Medina)
                                    {
                                        found_verses.Add(verse);
                                    }
                                }
                            }
                            break;
                        case RevelationPlace.Both:
                            {
                                found_verses.Add(verse);
                            }
                            break;
                    }
                }
            }
        }

        int current_chapter_number = -1;
        foreach (Verse verse in found_verses)
        {
            if (verse.Chapter != null)
            {
                if (current_chapter_number != verse.Chapter.Number)
                {
                    current_chapter_number = verse.Chapter.Number;
                    result.Add(verse.Chapter);
                }
            }
        }
        return result;
    }

    // find by letter frequency sum
    public static List<Word> FindWords(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        return DoFindWords(book, find_scope, current_selection, previous_result, phrase, letter_frequency_sum, frequency_sum_type);
    }
    private static List<Word> DoFindWords(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindWords(source, phrase, letter_frequency_sum, frequency_sum_type);
    }
    private static List<Word> DoFindWords(List<Verse> source, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        List<Word> result = new List<Word>();
        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    Book book = source[0].Book;
                    if (!String.IsNullOrEmpty(phrase))
                    {
                        if (letter_frequency_sum > 0)
                        {
                            foreach (Verse verse in source)
                            {
                                if (verse != null)
                                {
                                    foreach (Word word in verse.Words)
                                    {
                                        string text = word.Text;
                                        if (CalculateLetterFrequencySum(text, phrase, frequency_sum_type) == letter_frequency_sum)
                                        {
                                            result.Add(word);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
    public static List<Sentence> FindSentences(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        return DoFindSentences(book, find_scope, current_selection, previous_result, phrase, letter_frequency_sum, frequency_sum_type);
    }
    private static List<Sentence> DoFindSentences(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindSentences(source, phrase, letter_frequency_sum, frequency_sum_type);
    }
    private static List<Sentence> DoFindSentences(List<Verse> source, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        List<Sentence> result = new List<Sentence>();
        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    if (!String.IsNullOrEmpty(phrase))
                    {
                        if (letter_frequency_sum > 0)
                        {
                            List<Word> words = new List<Word>();
                            foreach (Verse verse in source)
                            {
                                words.AddRange(verse.Words);
                            }

                            // scan linearly for sequence of words with total Text matching letter_frequency_sum
                            for (int i = 0; i < words.Count - 1; i++)
                            {
                                StringBuilder str = new StringBuilder();

                                // start building word sequence
                                str.Append(words[i].Text);

                                string stopmark_text;
                                switch (words[i].Stopmark)
                                {
                                    case Stopmark.None: // none
                                        stopmark_text = "";
                                        break;
                                    case Stopmark.MustContinue:
                                        stopmark_text = "ۙ"; // Laaa
                                        break;
                                    case Stopmark.ShouldContinue:
                                        stopmark_text = "ۖ"; // Sala
                                        break;
                                    case Stopmark.CanStop:
                                        stopmark_text = "ۚ"; // Jeem
                                        break;
                                    case Stopmark.CanStopAtOneOnly:
                                        stopmark_text = "ۛ"; // Dots
                                        break;
                                    case Stopmark.ShouldStop:
                                        stopmark_text = "ۗ"; // Qala
                                        break;
                                    case Stopmark.MustPause:
                                        stopmark_text = "ۜ"; // Seen
                                        break;
                                    case Stopmark.MustStop:
                                        stopmark_text = "ۘ"; // Meem
                                        break;
                                    default:
                                        stopmark_text = "ۘ"; // Meem;
                                        break;
                                }

                                // if word has no stopmark or must continue
                                if ((words[i].Stopmark == Stopmark.None) || (words[i].Stopmark == Stopmark.MustContinue))
                                {
                                    // continue building with next words until a stopmark
                                    for (int j = i + 1; j < words.Count; j++)
                                    {
                                        str.Append(" " + words[j].Text);

                                        if (words[j].Stopmark == Stopmark.None)
                                        {
                                            continue; // continue building sentence
                                        }
                                        else // there is a stopmark
                                        {
                                            if (NumerologySystem.TextMode.Contains("Original"))
                                            {
                                                str.Append(" " + stopmark_text);
                                            }

                                            if (words[j].Stopmark == Stopmark.MustContinue)
                                            {
                                                continue; // continue building sentence
                                            }
                                            else if (
                                                (words[j].Stopmark == Stopmark.CanStopAtOneOnly) ||
                                                (words[j].Stopmark == Stopmark.ShouldContinue) ||
                                                (words[j].Stopmark == Stopmark.CanStop) ||
                                                (words[j].Stopmark == Stopmark.ShouldStop)
                                                )
                                            {
                                                // a sub sentence completed
                                                Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                                if (sentence != null)
                                                {
                                                    if (CalculateLetterFrequencySum(str.ToString(), phrase, frequency_sum_type) == letter_frequency_sum)
                                                    {
                                                        result.Add(sentence);
                                                    }
                                                }
                                                continue; // continue building a longer senetence
                                            }
                                            else if (words[j].Stopmark == Stopmark.MustPause)
                                            {
                                                if (
                                                    (words[j].Text.Simplify29() == "مَنْ".Simplify29()) ||
                                                    (words[j].Text.Simplify29() == "بَلْ".Simplify29())
                                                   )
                                                {
                                                    continue; // continue building a longer senetence
                                                }
                                                else if (
                                                    (words[j].Text.Simplify29() == "عِوَجَا".Simplify29()) ||
                                                    (words[j].Text.Simplify29() == "مَّرْقَدِنَا".Simplify29()) ||
                                                    (words[j].Text.Simplify29() == "مَالِيَهْ".Simplify29())
                                                    )
                                                {
                                                    // a sub sentence completed
                                                    Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        if (CalculateLetterFrequencySum(str.ToString(), phrase, frequency_sum_type) == letter_frequency_sum)
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }
                                                    continue; // continue building a longer senetence
                                                }
                                                else // unknown case
                                                {
                                                    throw new Exception("Unknown stopmark in Quran text.");
                                                }
                                            }
                                            else if (words[j].Stopmark == Stopmark.MustStop)
                                            {
                                                // sentence completed
                                                Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                                if (sentence != null)
                                                {
                                                    if (CalculateLetterFrequencySum(str.ToString(), phrase, frequency_sum_type) == letter_frequency_sum)
                                                    {
                                                        result.Add(sentence);
                                                    }
                                                }

                                                i = j; // start a new sentence after j
                                                break; // break j, start next i = j
                                            }
                                            else // unknown case
                                            {
                                                throw new Exception("Unknown stopmark in Quran text.");
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
        return result;
    }
    public static List<Verse> FindVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        return DoFindVerses(book, find_scope, current_selection, previous_result, phrase, letter_frequency_sum, frequency_sum_type);
    }
    private static List<Verse> DoFindVerses(Book book, FindScope find_scope, Selection current_selection, List<Verse> previous_result, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        List<Verse> source = GetSourceVerses(book, find_scope, current_selection, previous_result);
        return DoFindVerses(source, phrase, letter_frequency_sum, frequency_sum_type);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        List<Verse> result = new List<Verse>();
        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    Book book = source[0].Book;
                    if (!String.IsNullOrEmpty(phrase))
                    {
                        if (letter_frequency_sum > 0)
                        {
                            foreach (Verse verse in source)
                            {
                                if (verse != null)
                                {
                                    string text = verse.Text;
                                    if (CalculateLetterFrequencySum(text, phrase, frequency_sum_type) == letter_frequency_sum)
                                    {
                                        result.Add(verse);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
    private static int CalculateLetterFrequencySum(string text, string phrase, FrequencySumType frequency_sum_type)
    {
        if (String.IsNullOrEmpty(phrase)) return 0;
        if (s_numerology_system == null) return -1;

        int result = 0;

        text = text.SimplifyTo(NumerologySystem.TextMode);
        text = text.Replace("\r", "");
        text = text.Replace("\n", "");
        text = text.Replace("\t", "");
        text = text.Replace(" ", "");
        text = text.Replace(Verse.OPEN_BRACKET, "");
        text = text.Replace(Verse.CLOSE_BRACKET, "");

        phrase = phrase.SimplifyTo(NumerologySystem.TextMode);
        phrase = phrase.Replace("\r", "");
        phrase = phrase.Replace("\n", "");
        phrase = phrase.Replace("\t", "");
        phrase = phrase.Replace(" ", "");
        phrase = phrase.Replace(Verse.OPEN_BRACKET, "");
        phrase = phrase.Replace(Verse.CLOSE_BRACKET, "");
        if (frequency_sum_type == FrequencySumType.NoDuplicates)
        {
            phrase = phrase.RemoveDuplicates();
        }

        for (int i = 0; i < phrase.Length; i++)
        {
            int frequency = 0;
            for (int j = 0; j < text.Length; j++)
            {
                if (phrase[i] == text[j])
                {
                    frequency++;
                }
            }

            if (frequency > 0)
            {
                result += frequency;
            }
        }

        return result;
    }

    public static string GetTranslationKey(Book book, string translation)
    {
        string result = null;
        if (book.TranslationInfos != null)
        {
            foreach (string key in book.TranslationInfos.Keys)
            {
                if (book.TranslationInfos[key].Name == translation)
                {
                    result = key;
                }
            }
        }
        return result;
    }
    public static void LoadTranslation(Book book, string translation)
    {
        DataAccess.LoadTranslation(book, translation);
    }
    public static void UnloadTranslation(Book book, string translation)
    {
        DataAccess.UnloadTranslation(book, translation);
    }
    public static void SaveTranslation(Book book, string translation)
    {
        DataAccess.SaveTranslation(book, translation);
    }

    // help messages
    private static List<string> s_help_messages = new List<string>();
    public static List<string> HelpMessages
    {
        get { return s_help_messages; }
    }
    private static void LoadHelpMessages()
    {
        string filename = Globals.HELP_FOLDER + "/" + "Messages.txt";
        if (File.Exists(filename))
        {
            s_help_messages = DataAccess.LoadLines(filename);
        }
    }
}
