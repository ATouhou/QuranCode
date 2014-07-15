using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Model;

public class Client
{
    public const string DEFAULT_EMLAAEI_TEXT = Server.DEFAULT_EMLAAEI_TEXT;
    public const string DEFAULT_NEW_TRANSLATION = Server.DEFAULT_NEW_TRANSLATION;
    public const string DEFAULT_OLD_TRANSLATION = Server.DEFAULT_OLD_TRANSLATION;
    public const string DEFAULT_WORD_TRANSLATION = Server.DEFAULT_WORD_TRANSLATION;
    public const string DEFAULT_TRANSLITERATION = Server.DEFAULT_TRANSLITERATION;
    public const string DEFAULT_TAFSEER = Server.DEFAULT_TAFSEER;
    public const string DEFAULT_RECITATION = Server.DEFAULT_RECITATION;

    private string m_machine = null;
    public string Machine
    {
        get { return m_machine; }
    }
    private string m_username = null;
    public string Username
    {
        get { return m_username; }
    }
    private string m_password = null;
    public string Password
    {
        get { return m_password; }
        set
        {
            if (m_password != value)
            {
                m_password = value;
            }
        }
    }
    public List<string> NumerologySystemNames
    {
        get { return Server.NumerologySystemNames; }
    }
    public NumerologySystem NumerologySystem
    {
        get { return Server.NumerologySystem; }
        set { Server.NumerologySystem = value; }
    }
    public void BuildNumerologySystem(string dynamic_text)
    {
        Server.BuildNumerologySystem(m_book, dynamic_text);
    }
    public void ReloadNumerologySystem(string dynamic_text)
    {
        Server.ReloadNumerologySystem(m_book, dynamic_text);
    }
    public void ReloadNumerologySystem(string numerology_system_name, string dynamic_text)
    {
        Book backup_book = m_book;

        // load named numerology system
        Server.ReloadNumerologySystem(numerology_system_name, ref m_book, dynamic_text);

        // if text_mode changed, reload Original text and rebuild a new simplified book
        if (m_book != backup_book)
        {
            UpdatePhrasePositionsAndLengths(m_book);
        }
    }
    public void RestoreNumerologySystem(string numerology_system_name, string dynamic_text)
    {
        Book backup_book = m_book;

        // load named numerology system
        Server.RestoreNumerologySystem(numerology_system_name, ref m_book, dynamic_text);

        // if text_mode changed, reload Original text and rebuild a new simplified book
        if (m_book != backup_book)
        {
            UpdatePhrasePositionsAndLengths(m_book);
        }
    }
    public void ReloadDefaultNumerologySystem(string dynamic_text)
    {
        if (NumerologySystem.Name != NumerologySystem.FAVORITE_NUMERORLOGY_SYSTEM)
        {
            Book backup_book = m_book;

            // load default numerology system
            Server.ReloadDefaultNumerologySystem(ref m_book, dynamic_text);

            // if text_mode changed, reload Original text and rebuild a new simplified book
            if (m_book != backup_book)
            {
                UpdatePhrasePositionsAndLengths(m_book);
            }
        }
    }
    private void UpdatePhrasePositionsAndLengths(Book book)
    {
        if (book != null)
        {
            // update Selection to point at new book object
            if (m_selection != null)
            {
                m_selection = new Selection(book, m_selection.Scope, m_selection.Indexes);
            }

            // update FoundVerses to point at new book object
            if (book.Verses != null)
            {
                if (m_found_verses != null)
                {
                    List<Verse> verses = new List<Verse>();
                    foreach (Verse verse in m_found_verses)
                    {
                        int index = verse.Number - 1;
                        if ((index >= 0) && (index < book.Verses.Count))
                        {
                            verses.Add(book.Verses[index]);
                        }
                    }
                    m_found_verses = verses;
                }
            }

            // update FoundPhrases to point at new book object
            if (book.Verses != null)
            {
                if (m_found_phrases != null)
                {
                    List<Verse> verses = new List<Verse>();
                    for (int i = 0; i < m_found_phrases.Count; i++)
                    {
                        Phrase phrase = m_found_phrases[i];
                        int index = phrase.Verse.Number - 1;
                        if ((index >= 0) && (index < book.Verses.Count))
                        {
                            phrase = new Phrase(book.Verses[index], phrase.Position, phrase.Text);

                            if (NumerologySystem.TextMode.Contains("Original"))
                            {
                                Server.OriginifyPhrase(ref phrase);
                            }
                            else
                            {
                                Server.SimplifyPhrase(ref phrase);
                            }
                        }
                    }
                }
            }

            // ALSO should update these less used collections as they are already held by FoundVersess
            // update FoundVerseRanges to point at new book object
            // update FoundChapters to point at new book object
            // update FoundChapterRanges to point at new book object
        }
    }
    public void SaveNumerologySystem()
    {
        Server.SaveNumerologySystem();
    }

    private Book m_book = null;
    public Book Book
    {
        get { return m_book; }
    }
    public Client(string machine, string username, string password, string numerology_system_name)
    {
        m_machine = machine;
        m_username = username;
        m_password = password;
        m_book = BuildSimplifiedBook(numerology_system_name);
    }
    public Book BuildSimplifiedBook(string numerology_system_name)
    {
        return Server.BuildSimplifiedBook(numerology_system_name);
    }

    public string GetTranslationKey(string translation)
    {
        return Server.GetTranslationKey(m_book, translation);
    }
    public void LoadTranslation(string translation)
    {
        Server.LoadTranslation(m_book, translation);
    }
    public void UnloadTranslation(string translation)
    {
        Server.UnloadTranslation(m_book, translation);
    }
    public void SaveTranslation(string translation)
    {
        Server.SaveTranslation(m_book, translation);
    }

    // used for non-Quran text
    public long CalculateValue(char user_char)
    {
        return Server.CalculateValue(user_char);
    }
    public long CalculateValue(string user_text)
    {
        return Server.CalculateValue(user_text);
    }
    // used for Quran text only
    public long CalculateValue(Letter letter)
    {
        return Server.CalculateValue(letter);
    }
    public long CalculateValue(Word word)
    {
        return Server.CalculateValue(word);
    }
    public long CalculateValue(Verse verse)
    {
        return Server.CalculateValue(verse);
    }
    public long CalculateValue(List<Verse> verses)
    {
        return Server.CalculateValue(verses);
    }
    public long CalculateValue(Chapter chapter)
    {
        return Server.CalculateValue(chapter);
    }
    public long CalculateValue(List<Verse> verses, int letter_index_in_verse1, int letter_index_in_verse2)
    {
        return Server.CalculateValue(verses, letter_index_in_verse1, letter_index_in_verse2);
    }
    public List<long> CalculateAllVerseValues(List<Verse> verses)
    {
        List<long> result = new List<long>();
        foreach (Verse verse in verses)
        {
            long value = Server.CalculateValue(verse);
            result.Add(value);
        }
        return result;
    }
    public List<long> CalculateAllWordValues(List<Verse> verses)
    {
        List<long> result = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                long value = Server.CalculateValue(word);
                result.Add(value);
            }
        }
        return result;
    }
    public List<long> CalculateAllLetterValues(List<Verse> verses)
    {
        List<long> letter_values = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                foreach (Letter letter in word.Letters)
                {
                    long value = Server.CalculateValue(letter);
                    letter_values.Add(value);
                }
            }
        }
        return letter_values;
    }
    public long MaximumVerseValue
    {
        get
        {
            long result = 0L;
            foreach (Verse verse in m_book.Verses)
            {
                long value = Server.CalculateValue(verse);
                if (result < value)
                {
                    result = value;
                }
            }
            return result;
        }
    }
    public long MaximumWordValue
    {
        get
        {
            long result = 0L;
            foreach (Verse verse in m_book.Verses)
            {
                foreach (Word word in verse.Words)
                {
                    long value = Server.CalculateValue(word);
                    if (result < value)
                    {
                        result = value;
                    }
                }
            }
            return result;
        }
    }
    public long MaximumLetterValue
    {
        get
        {
            long result = 0L;
            foreach (long value in NumerologySystem.Values)
            {
                if (result < value)
                {
                    result = value;
                }
            }
            return result;
        }
    }
    public void SaveValueCalculation(string filename, string text)
    {
        if (!Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            Directory.CreateDirectory(Globals.STATISTICS_FOLDER);
        }

        filename = Globals.STATISTICS_FOLDER + "/" + filename;
        try
        {
            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
            {
                writer.WriteLine("----------------------------------------");
                switch (NumerologySystem.Scope)
                {
                    case NumerologySystemScope.Book:
                        {
                            writer.Write(NumerologySystem.Name + "\r\n" + "Scope = Entire Book");
                        }
                        break;
                    case NumerologySystemScope.Selection:
                        {
                            StringBuilder s = new StringBuilder();
                            foreach (int index in Selection.Indexes)
                            {
                                s.Append((index + 1).ToString() + ", ");
                            }
                            if (s.Length > 0)
                            {
                                s.Remove(s.Length - 2, 2);
                            }
                            writer.Write(NumerologySystem.Name + "\r\n" + "Scope = " + Selection.Scope.ToString() + ((Selection.Indexes.Count > 1) ? "s " : " ") + s.ToString());
                        }
                        break;
                    case NumerologySystemScope.HighlightedText:
                        {
                            writer.Write(NumerologySystem.Name + "\r\n" + "Scope = Text");
                        }
                        break;
                    default:
                        break;
                }
                writer.WriteLine();
                writer.WriteLine(NumerologySystem.ToOverview());
                writer.WriteLine();
                writer.WriteLine("----------------------------------------");
                writer.WriteLine("Text");
                writer.WriteLine("----------------------------------------");
                writer.WriteLine(text);
                writer.WriteLine("----------------------------------------");
                writer.WriteLine("Letter" + "\t" + "Value");
                writer.WriteLine("----------------------------------------");
                foreach (char character in text)
                {
                    if (character == '-')
                    {
                        break;
                    }
                    else if (character == ' ')
                    {
                        writer.WriteLine(" " + "\t" + "");
                    }
                    else if (Constants.ARABIC_LETTERS.Contains(character))
                    {
                        writer.WriteLine(character.ToString() + "\t" + CalculateValue(character));
                    }
                }
                writer.WriteLine("----------------------------------------");
                writer.WriteLine("Total" + "\t" + CalculateValue(text));
                writer.WriteLine("----------------------------------------");
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }

        // show file content after save
        if (File.Exists(filename))
        {
            System.Diagnostics.Process.Start("Notepad.exe", filename);
        }
    }

    private Selection m_selection = null;
    public Selection Selection
    {
        get
        {
            if (m_book != null)
            {
                return m_selection;
            }
            return null;
        }
        set
        {
            if (m_book != null)
            {
                m_selection = value;
            }
        }
    }
    private void ClearSelection()
    {
        if (m_book != null)
        {
            if (m_selection != null)
            {
                m_selection = new Selection(m_book, SelectionScope.Chapter, new List<int>() { 0 });
            }
        }
    }
    private FindScope m_find_scope = FindScope.Book;
    public FindScope FindScope
    {
        set { m_find_scope = value; }
        get { return m_find_scope; }
    }
    private List<Verse> m_found_verses = null;
    public List<Verse> FoundVerses
    {
        set { m_found_verses = value; }
        get { return m_found_verses; }
    }
    private List<Phrase> m_found_phrases = null;
    public List<Phrase> FoundPhrases
    {
        set { m_found_phrases = value; }
        get
        {
            return m_found_phrases;
        }
    }
    private List<Sentence> m_found_sentences = null;
    public List<Sentence> FoundSentences
    {
        set { m_found_sentences = value; }
        get
        {
            return m_found_sentences;
        }
    }
    private List<Word> m_found_words = null;
    public List<Word> FoundWords
    {
        set { m_found_words = value; }
        get { return m_found_words; }
    }
    private List<List<Word>> m_found_word_ranges = null;
    public List<List<Word>> FoundWordRanges
    {
        set { m_found_word_ranges = value; }
        get { return m_found_word_ranges; }
    }
    private List<List<Verse>> m_found_verse_ranges = null;
    public List<List<Verse>> FoundVerseRanges
    {
        set { m_found_verse_ranges = value; }
        get { return m_found_verse_ranges; }
    }
    private List<Chapter> m_found_chapters = null;
    public List<Chapter> FoundChapters
    {
        set { m_found_chapters = value; }
        get { return m_found_chapters; }
    }
    private List<List<Chapter>> m_found_chapter_ranges = null;
    public List<List<Chapter>> FoundChapterRanges
    {
        set { m_found_chapter_ranges = value; }
        get { return m_found_chapter_ranges; }
    }

    // helper methods with GetSourceVerses (not entire book verses)
    public Dictionary<string, int> GetWordsWith(string text, bool at_word_start, bool with_diacritics)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        List<Verse> source = Server.GetSourceVerses(m_book, m_find_scope, m_selection, m_found_verses);
        if (m_book != null)
        {
            result = m_book.GetWordsWith(source, text, at_word_start, with_diacritics);
        }
        return result;
    }
    public Dictionary<string, int> GetCurrentWords(string text, bool at_word_start, bool with_diacritics)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        List<Verse> source = Server.GetSourceVerses(m_book, m_find_scope, m_selection, m_found_verses);
        if (m_book != null)
        {
            result = m_book.GetCurrentWords(source, text, at_word_start, with_diacritics);
        }
        return result;
    }
    public Dictionary<string, int> GetNextWords(string text, bool at_word_start, bool with_diacritics)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        List<Verse> source = Server.GetSourceVerses(m_book, m_find_scope, m_selection, m_found_verses);
        if (m_book != null)
        {
            result = m_book.GetNextWords(source, text, at_word_start, with_diacritics);
        }
        return result;
    }

    // find by text - Exact
    /// <summary>
    /// Find phrases for given exact text that meet all parameters.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="language_type"></param>
    /// <param name="translation"></param>
    /// <param name="text_location"></param>
    /// <param name="case_sensitive"></param>
    /// <param name="wordness"></param>
    /// <param name="multiplicity"></param>
    /// <param name="at_word_start"></param>
    /// <returns>Number of found phrases. Result is stored in FoundPhrases.</returns>
    public int FindPhrases(string text, LanguageType language_type, string translation, TextLocation text_location, bool case_sensitive, TextWordness wordness, int multiplicity, bool at_word_start, bool with_diacritics)
    {
        m_found_phrases = Server.FindPhrases(m_book, m_find_scope, m_selection, m_found_verses, text, language_type, translation, text_location, case_sensitive, wordness, multiplicity, at_word_start, with_diacritics);
        if (m_found_phrases != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Phrase phrase in m_found_phrases)
            {
                if (phrase != null)
                {
                    if (!m_found_verses.Contains(phrase.Verse))
                    {
                        m_found_verses.Add(phrase.Verse);
                    }
                }
            }

            return m_found_phrases.Count;
        }
        return 0;
    }
    /// <summary>
    /// Find phrases for given root( or space separate roots) that meet all parameters.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="multiplicity"></param>
    /// <returns>Number of found phrases. Result is stored in FoundPhrases.</returns>
    public int FindPhrases(string root, int multiplicity, bool with_diacritics)
    {
        m_found_phrases = Server.FindPhrases(m_book, m_find_scope, m_selection, m_found_verses, root, multiplicity, with_diacritics);
        if (m_found_phrases != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Phrase phrase in m_found_phrases)
            {
                if (phrase != null)
                {
                    if (!m_found_verses.Contains(phrase.Verse))
                    {
                        m_found_verses.Add(phrase.Verse);
                    }
                }
            }

            return m_found_phrases.Count;
        }
        return 0;
    }

    // find by similarity - phrases similar to given text
    /// <summary>
    /// Find phrases with similar text to given text to given similarity percentage or above.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="similarity_percentage"></param>
    /// <returns>Number of found phrases. Result is stored in FoundPhrases.</returns>
    public int FindPhrases(string text, double similarity_percentage)
    {
        m_found_phrases = Server.FindPhrases(m_book, m_find_scope, m_selection, m_found_verses, text, similarity_percentage);
        if (m_found_phrases != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Phrase phrase in m_found_phrases)
            {
                if (phrase != null)
                {
                    if (!m_found_verses.Contains(phrase.Verse))
                    {
                        m_found_verses.Add(phrase.Verse);
                    }
                }
            }

            return m_found_phrases.Count;
        }
        return 0;
    }
    // find by similarity - verse similar to given verse
    /// <summary>
    /// Find verses with similar text to verse text to given similarity percentage or above with give similarity method
    /// </summary>
    /// <param name="verse"></param>
    /// <param name="similarity_method"></param>
    /// <param name="similarity_percentage"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(Verse verse, SimilarityMethod similarity_method, double similarity_percentage)
    {
        m_found_verses = Server.FindVerses(m_book, m_find_scope, m_selection, m_found_verses, verse, similarity_method, similarity_percentage);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }
    // find by similarity - all similar verses to each other throughout the book
    /// <summary>
    /// Find verse ranges with similar text to each other to given similarity percentage or above.
    /// </summary>
    /// <param name="similarity_method"></param>
    /// <param name="similarity_percentage"></param>
    /// <returns>Number of found verse ranges. Result is stored in FoundVerseRanges.</returns>
    public int FindVerseRanges(SimilarityMethod similarity_method, double similarity_percentage)
    {
        m_found_verse_ranges = Server.FindVerseRanges(m_book, m_find_scope, m_selection, m_found_verses, similarity_method, similarity_percentage);
        if (m_found_verse_ranges != null)
        {
            m_found_verses = new List<Verse>();
            foreach (List<Verse> verse_range in m_found_verse_ranges)
            {
                m_found_verses.AddRange(verse_range);
            }

            return m_found_verse_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Words
    /// <summary>
    /// Find words that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found words. Result is stored in FoundWords.</returns>
    public int FindWords(NumberQuery query)
    {
        m_found_words = Server.FindWords(m_book, m_find_scope, m_selection, m_found_verses, query);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            m_found_phrases = new List<Phrase>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    Verse verse = word.Verse;
                    if (!m_found_verses.Contains(verse))
                    {
                        m_found_verses.Add(verse);
                    }
                }

                Phrase phrase = new Phrase(word.Verse, word.Position, word.Text);
                m_found_phrases.Add(phrase);
            }

            return m_found_words.Count;
        }
        return 0;
    }
    // find by numbers - WordRanges
    /// <summary>
    /// Find word ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found word ranges. Result is stored in FoundWordRanges.</returns>
    public int FindWordRanges(NumberQuery query)
    {
        m_found_word_ranges = Server.FindWordRanges(m_book, m_find_scope, m_selection, m_found_verses, query);
        if (m_found_word_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_phrases = new List<Phrase>();
            foreach (List<Word> range in m_found_word_ranges)
            {
                if (range != null)
                {
                    if (range.Count > 0)
                    {
                        // prepare found phrase verse
                        Verse verse = range[0].Verse;

                        // build found verses // prevent duplicate verses in case more than 1 range is found in same verse
                        if (!m_found_verses.Contains(verse))
                        {
                            m_found_verses.Add(verse);
                        }

                        // prepare found phrase text
                        string range_text = null;
                        foreach (Word word in range)
                        {
                            range_text += word.Text + " ";
                        }
                        range_text = range_text.Remove(range_text.Length - 1, 1);

                        // prepare found phrase position
                        int range_position = range[0].Position;

                        // build found phrases // allow multiple phrases even if overlapping inside same verse
                        Phrase phrase = new Phrase(verse, range_position, range_text);
                        m_found_phrases.Add(phrase);
                    }
                }
            }

            return m_found_word_ranges.Count;
        }
        return 0;
    }
    // find by numbers - Sentences
    /// <summary>
    /// Find sentences across verses that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found sentences. Result is stored in FoundSentences.</returns>
    public int FindSentences(NumberQuery query)
    {
        m_found_sentences = Server.FindSentences(m_book, m_find_scope, m_selection, m_found_verses, query);
        if (m_found_sentences != null)
        {
            BuildSentencePhrases();

            return m_found_sentences.Count;
        }
        return 0;
    }
    // find by numbers - Verses
    /// <summary>
    /// Find verses that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(NumberQuery query)
    {
        m_found_verses = Server.FindVerses(m_book, m_find_scope, m_selection, m_found_verses, query);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }
    // find by numbers - VerseRanges
    /// <summary>
    /// Find verse ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found verse ranges. Result is stored in FoundVerseRanges.</returns>
    public int FindVerseRanges(NumberQuery query)
    {
        m_found_verse_ranges = Server.FindVerseRanges(m_book, m_find_scope, m_selection, m_found_verses, query);
        if (m_found_verse_ranges != null)
        {
            m_found_verses = new List<Verse>();
            foreach (List<Verse> range in m_found_verse_ranges)
            {
                m_found_verses.AddRange(range);
            }

            return m_found_verse_ranges.Count;
        }
        return 0;
    }
    // find by numbers - Chapters
    /// <summary>
    /// Find chapters that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found chapters. Result is stored in FoundChapters.</returns>
    public int FindChapters(NumberQuery query)
    {
        m_found_chapters = Server.FindChapters(m_book, m_find_scope, m_selection, m_found_verses, query);
        if (m_found_chapters != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Chapter chapter in m_found_chapters)
            {
                if (chapter != null)
                {
                    m_found_verses.AddRange(chapter.Verses);
                }
            }

            return m_found_chapters.Count;
        }
        return 0;
    }
    // find by numbers - ChapterRanges
    /// <summary>
    /// Find chapter ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found chapter ranges. Result is stored in FoundChapterRanges.</returns>
    public int FindChapterRanges(NumberQuery query)
    {
        m_found_chapter_ranges = Server.FindChapterRanges(m_book, m_find_scope, m_selection, m_found_verses, query);
        if (m_found_chapter_ranges != null)
        {
            m_found_verses = new List<Verse>();
            foreach (List<Chapter> range in m_found_chapter_ranges)
            {
                foreach (Chapter chapter in range)
                {
                    if (chapter != null)
                    {
                        m_found_verses.AddRange(chapter.Verses);
                    }
                }
            }

            return m_found_chapter_ranges.Count;
        }
        return 0;
    }

    // find by prostration type
    /// <summary>
    /// Find verses with given prostration type.
    /// </summary>
    /// <param name="prostration_type"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(ProstrationType prostration_type)
    {
        m_found_verses = Server.FindVerses(m_book, m_find_scope, m_selection, m_found_verses, prostration_type);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }

    // find by revelation place
    /// <summary>
    /// Find chapters that were revealed at given revelation place.
    /// </summary>
    /// <param name="revelation_place"></param>
    /// <returns>Number of found chapters. Result is stored in FoundChapters.</returns>
    public int FindChapters(RevelationPlace revelation_place)
    {
        m_found_chapters = Server.FindChapters(m_book, m_find_scope, m_selection, m_found_verses, revelation_place);
        if (m_found_chapters != null)
        {
            return m_found_chapters.Count;
        }
        return 0;
    }

    // find by letter frequency sum
    /// <summary>
    /// Find words with required letter frequency sum in their text of the given phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="letter_frequency_sum"></param>
    /// <param name="frequency_sum_type"></param>
    /// <returns>Number of found words. Result is stored in FoundWords.</returns>
    public int FindWords(string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        m_found_words = Server.FindWords(m_book, m_find_scope, m_selection, m_found_verses, phrase, letter_frequency_sum, frequency_sum_type);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            m_found_phrases = new List<Phrase>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    Verse verse = word.Verse;
                    if (!m_found_verses.Contains(verse))
                    {
                        m_found_verses.Add(verse);
                    }
                }

                Phrase word_phrase = new Phrase(word.Verse, word.Position, word.Text);
                m_found_phrases.Add(word_phrase);
            }

            return m_found_words.Count;
        }
        return 0;
    }
    /// <summary>
    /// Find sentences across verses with required letter frequency sum in their text of the given phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="letter_frequency_sum"></param>
    /// <param name="frequency_sum_type"></param>
    /// <returns>Number of found sentences. Result is stored in FoundSentences.</returns>
    public int FindSentences(string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        m_found_sentences = Server.FindSentences(m_book, m_find_scope, m_selection, m_found_verses, phrase, letter_frequency_sum, frequency_sum_type);
        if (m_found_sentences != null)
        {
            BuildSentencePhrases();

            return m_found_sentences.Count;
        }
        return 0;
    }
    private void BuildSentencePhrases()
    {
        if (m_found_sentences != null)
        {
            m_found_verses = new List<Verse>();
            m_found_phrases = new List<Phrase>();
            foreach (Sentence sentence in m_found_sentences)
            {
                if (sentence != null)
                {
                    Verse first_verse = sentence.FirstVerse;
                    Verse last_verse = sentence.LastVerse;
                    if ((first_verse != null) && (last_verse != null))
                    {
                        int start = first_verse.Number - 1;
                        int end = last_verse.Number - 1;
                        if (end >= start)
                        {
                            // add unique verses
                            for (int i = start; i <= end; i++)
                            {
                                if (!m_found_verses.Contains(Book.Verses[i]))
                                {
                                    m_found_verses.Add(Book.Verses[i]);
                                }
                            }

                            // build phrases for colorization
                            if (start == end) // sentence within verse
                            {
                                Phrase sentence_phrase = new Phrase(first_verse, sentence.StartPosition, sentence.Text);
                                m_found_phrases.Add(sentence_phrase);
                            }
                            else // sentence across verses
                            {
                                // first verse
                                string start_text = first_verse.Text.Substring(sentence.StartPosition);
                                Phrase start_phrase = new Phrase(sentence.FirstVerse, sentence.StartPosition, start_text);
                                m_found_phrases.Add(start_phrase);

                                // middle verses
                                for (int i = start + 1; i < end; i++)
                                {
                                    Verse verse = Book.Verses[i];
                                    if (verse != null)
                                    {
                                        Phrase middle_phrase = new Phrase(verse, 0, verse.Text);
                                        m_found_phrases.Add(middle_phrase);
                                    }
                                }

                                // last verse
                                string end_text = last_verse.Text.Substring(0, sentence.EndPosition);
                                Phrase end_phrase = new Phrase(last_verse, 0, end_text);
                                m_found_phrases.Add(end_phrase);
                            }
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Find verses with required letter frequency sum in their text of the given phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="letter_frequency_sum"></param>
    /// <param name="frequency_sum_type"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(string phrase, int letter_frequency_sum, FrequencySumType frequency_sum_type)
    {
        m_found_verses = Server.FindVerses(m_book, m_find_scope, m_selection, m_found_verses, phrase, letter_frequency_sum, frequency_sum_type);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }

    private List<object> m_history_items = new List<object>();
    public List<object> HistoryItems
    {
        get { return m_history_items; }
    }
    private int m_history_item_index = -1;
    public int HistoryItemIndex
    {
        get { return m_history_item_index; }
    }
    public object CurrentHistoryItem
    {
        get
        {
            if (m_history_items != null)
            {
                if ((m_history_item_index >= 0) && (m_history_item_index < m_history_items.Count))
                {
                    return m_history_items[m_history_item_index];
                }
            }
            return null;
        }
    }
    public void AddHistoryItem(object item)
    {
        if (m_history_items != null)
        {
            m_history_items.Add(item);
            m_history_item_index = m_history_items.Count - 1;
        }
    }
    public void DeleteHistoryItem(object item)
    {
        if (m_history_items != null)
        {
            m_history_items.Remove(item);
            m_history_item_index = m_history_items.Count - 1;
        }
    }
    public void DeleteCurrentHistoryItem()
    {
        if (m_history_items != null)
        {
            if ((m_history_item_index >= 0) && (m_history_item_index < m_history_items.Count))
            {
                object item = m_history_items[m_history_item_index];
                m_history_items.Remove(item);
                m_history_item_index = m_history_items.Count - 1;
            }

            if (m_history_items.Count == 0) // all items deleted
            {
                m_history_item_index = -1;
            }
            else // there are still some item(s)
            {
                // if index becomes outside list, move back into list
                if (m_history_item_index == m_history_items.Count)
                {
                    m_history_item_index = m_history_items.Count - 1;
                }
            }
        }
    }
    public void ClearHistoryItems()
    {
        if (m_history_items != null)
        {
            m_history_items.Clear();
            m_history_item_index = -1;
        }
    }
    public object GotoPreviousHistoryItem()
    {
        object result = null;
        if (m_history_items != null)
        {
            if ((m_history_item_index > 0) && (m_history_item_index < m_history_items.Count))
            {
                m_history_item_index--;
                result = m_history_items[m_history_item_index];
            }
        }
        return result;
    }
    public object GotoNextHistoryItem()
    {
        object result = null;
        if (m_history_items != null)
        {
            if ((m_history_item_index >= -1) && (m_history_item_index < m_history_items.Count - 1))
            {
                m_history_item_index++;
                result = m_history_items[m_history_item_index];
            }
        }
        return result;
    }
    public void SaveHistoryItems()
    {
        if (m_history_items != null)
        {
            string user_history_directory = Globals.HISTORY_FOLDER + "/" + m_username;
            if (!Directory.Exists(user_history_directory))
            {
                Directory.CreateDirectory(user_history_directory);
            }

            string filename = user_history_directory + "/" + "History" + ".txt";
            try
            {
                using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                {
                    StringBuilder str = new StringBuilder();

                    foreach (object history_item in m_history_items)
                    {
                        if (history_item is SelectionHistoryItem)
                        {
                            SelectionHistoryItem item = history_item as SelectionHistoryItem;
                            if (item != null)
                            {
                                str.AppendLine("BrowseHistoryItem");
                                str.AppendLine(item.Scope.ToString());
                                if (item.Indexes.Count > 0)
                                {
                                    foreach (int index in item.Indexes)
                                    {
                                        str.Append(index.ToString() + ",");
                                    }
                                    if (str.Length > 1)
                                    {
                                        str.Remove(str.Length - 1, 1);
                                    }
                                }
                                str.AppendLine();
                                str.AppendLine(END_OF_HISTORY_ITME_MARKER);
                            }
                        }
                        else if (history_item is FindHistoryItem)
                        {
                            FindHistoryItem item = history_item as FindHistoryItem;
                            if (item != null)
                            {
                                str.AppendLine("FindHistoryItem");
                                str.AppendLine("FindType" + "\t" + item.FindType);
                                str.AppendLine("NumberSearchType" + "\t" + item.NumberSearchType);
                                str.AppendLine("Text" + "\t" + item.Text);
                                str.AppendLine("Header" + "\t" + item.Header);
                                str.AppendLine("Language" + "\t" + item.LanguageType);
                                str.AppendLine("Translation" + "\t" + item.Translation);

                                if ((item.Phrases != null) && (item.Phrases.Count > 0))
                                {
                                    foreach (Phrase phrase in item.Phrases)
                                    {
                                        if (phrase != null)
                                        {
                                            str.AppendLine(phrase.Verse.Number.ToString() + "," + phrase.Text + "," + phrase.Position.ToString());
                                        }
                                    }
                                }
                                else // verse.Number
                                {
                                    //TODO: Save NumberQuery with each search result

                                    foreach (Verse verse in item.Verses)
                                    {
                                        str.AppendLine(verse.Number.ToString());
                                    }
                                }
                                str.AppendLine(END_OF_HISTORY_ITME_MARKER);
                            }
                        }
                    }
                    writer.Write(str.ToString());
                }
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }
        }
    }
    public void LoadHistoryItems()
    {
        string filename = Globals.HISTORY_FOLDER + "/" + m_username + "/" + "History" + ".txt";
        if (File.Exists(filename))
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                try
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] parts = null;

                        if (line == "")
                        {
                            continue;
                        }
                        else if (line == "BrowseHistoryItem")
                        {
                            line = reader.ReadLine();
                            SelectionScope scope = (SelectionScope)Enum.Parse(typeof(SelectionScope), line);
                            List<int> indexes = new List<int>();

                            line = reader.ReadLine();
                            parts = line.Split(',');
                            foreach (string part in parts)
                            {
                                try
                                {
                                    int index = int.Parse(part);
                                    indexes.Add(index);
                                }
                                catch
                                {
                                    continue;
                                }
                            }

                            SelectionHistoryItem item = new SelectionHistoryItem(m_book, scope, indexes);
                            AddHistoryItem(item);
                        }
                        else if (line == "FindHistoryItem")
                        {
                            FindHistoryItem item = new FindHistoryItem();

                            line = reader.ReadLine();
                            parts = line.Split('\t');
                            if ((parts.Length == 2) && (parts[0].Trim() == "FindType"))
                            {
                                item.FindType = (FindType)Enum.Parse(typeof(FindType), parts[1].Trim());
                            }

                            line = reader.ReadLine();
                            parts = line.Split('\t');
                            if ((parts.Length == 2) && (parts[0].Trim() == "NumberSearchType"))
                            {
                                item.NumberSearchType = (NumberSearchType)Enum.Parse(typeof(NumberSearchType), parts[1].Trim());
                            }

                            line = reader.ReadLine();
                            parts = line.Split('\t');
                            if ((parts.Length == 2) && (parts[0].Trim() == "Text"))
                            {
                                item.Text = parts[1].Trim();
                            }

                            line = reader.ReadLine();
                            parts = line.Split('\t');
                            if ((parts.Length == 2) && (parts[0].Trim() == "Header"))
                            {
                                item.Header = parts[1].Trim();
                            }

                            line = reader.ReadLine();
                            parts = line.Split('\t');
                            if ((parts.Length == 2) && (parts[0].Trim() == "Language"))
                            {
                                item.LanguageType = (LanguageType)Enum.Parse(typeof(LanguageType), parts[1].Trim());
                            }

                            line = reader.ReadLine();
                            parts = line.Split('\t');
                            if ((parts.Length == 2) && (parts[0].Trim() == "Translation"))
                            {
                                item.Translation = parts[1].Trim();
                            }

                            // CSV: Phrase.Verse.Number, Phrase.Text, Phrase.Position
                            while (true)
                            {
                                line = reader.ReadLine();
                                if (line == END_OF_HISTORY_ITME_MARKER)
                                {
                                    break;
                                }
                                parts = line.Split(',');
                                if (parts.Length == 1) // verse.Number
                                {
                                    //TODO: Load NumberQuery with each search result

                                    int verse_index = int.Parse(parts[0].Trim()) - 1;
                                    if ((verse_index >= 0) && (verse_index < m_book.Verses.Count))
                                    {
                                        Verse verse = m_book.Verses[verse_index];
                                        if (!item.Verses.Contains(verse))
                                        {
                                            item.Verses.Add(verse);
                                        }
                                    }
                                }
                                else if (parts.Length == 3) // phrase.Verse.Number,phrase.Text,phrase.Position
                                {
                                    int verse_index = int.Parse(parts[0].Trim()) - 1;
                                    if ((verse_index >= 0) && (verse_index < m_book.Verses.Count))
                                    {
                                        Verse verse = m_book.Verses[verse_index];
                                        if (!item.Verses.Contains(verse))
                                        {
                                            item.Verses.Add(verse);
                                        }

                                        string phrase_text = parts[1].Trim();
                                        if (phrase_text.Length > 0)
                                        {
                                            int phrase_position = int.Parse(parts[2].Trim());
                                            Phrase phrase = new Phrase(verse, phrase_position, phrase_text);
                                            item.Phrases.Add(phrase);
                                        }
                                    }
                                }
                            } // while

                            AddHistoryItem(item);
                        }
                    } // while
                }
                catch
                {
                    throw new Exception("Invalid " + filename + " format.");
                }
            }
        }
    }
    private string END_OF_HISTORY_ITME_MARKER = "-------------------";

    private List<LetterStatistic> m_letter_statistics = new List<LetterStatistic>();
    public List<LetterStatistic> LetterStatistics
    {
        get { return m_letter_statistics; }
    }
    /// <summary>
    /// Calculate letter statistics for the given text.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="phrase"></param>
    /// <param name="frequency_sum_type"></param>
    /// <returns>Result is stored in LetterStatistics.</returns>
    public void CalculateLetterStatistics(string text)
    {
        if (String.IsNullOrEmpty(text)) return;

        if (NumerologySystem != null)
        {
            text = text.SimplifyTo(NumerologySystem.TextMode);
        }
        text = text.Replace("\r", "");
        text = text.Replace("\n", "");
        text = text.Replace("\t", "");
        text = text.Replace(" ", "");
        text = text.Replace(Verse.OPEN_BRACKET, "");
        text = text.Replace(Verse.CLOSE_BRACKET, "");

        m_letter_statistics.Clear();
        for (int i = 0; i < text.Length; i++)
        {
            bool is_found = false;
            for (int j = 0; j < m_letter_statistics.Count; j++)
            {
                if (text[i] == m_letter_statistics[j].Letter)
                {
                    is_found = true;
                    m_letter_statistics[j].Frequency++;
                }
            }

            if (!is_found)
            {
                LetterStatistic letter_statistic = new LetterStatistic();
                letter_statistic.Order = m_letter_statistics.Count + 1;
                letter_statistic.Letter = text[i];
                letter_statistic.Frequency++;
                m_letter_statistics.Add(letter_statistic);
            }
        }
    }
    public void SortLetterStatistics(StatisticSortMethod sort_method)
    {
        LetterStatistic.SortMethod = sort_method;
        m_letter_statistics.Sort();
        if (LetterStatistic.SortOrder == StatisticSortOrder.Ascending)
        {
            LetterStatistic.SortOrder = StatisticSortOrder.Descending;
        }
        else
        {
            LetterStatistic.SortOrder = StatisticSortOrder.Ascending;
        }
    }
    public void SaveLetterStatistics(string filename, string text)
    {
        if (String.IsNullOrEmpty(filename)) return;
        if (String.IsNullOrEmpty(text)) return;

        if (!Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            Directory.CreateDirectory(Globals.STATISTICS_FOLDER);
        }

        filename = Globals.STATISTICS_FOLDER + "/" + filename;
        try
        {
            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
            {
                writer.WriteLine("----------------------------------------");
                writer.WriteLine(NumerologySystem.Name);
                writer.WriteLine("----------------------------------------");
                writer.WriteLine();
                writer.WriteLine("Text");
                writer.WriteLine("----------------------------------------");
                writer.WriteLine(text);
                writer.WriteLine("----------------------------------------");
                writer.WriteLine();
                writer.WriteLine("----------------------------------------");
                writer.WriteLine("Order" + "\t" + "Letter" + "\t" + "Frequency");
                writer.WriteLine("----------------------------------------");
                int count = 0;
                int frequency_sum = 0;
                foreach (LetterStatistic letter_statistic in m_letter_statistics)
                {
                    writer.WriteLine(letter_statistic.Order.ToString() + "\t" + letter_statistic.Letter.ToString() + '\t' + letter_statistic.Frequency.ToString());
                    count++;
                    frequency_sum += letter_statistic.Frequency;
                }
                writer.WriteLine("----------------------------------------");
                writer.WriteLine("Total" + "\t" + count.ToString() + "\t" + frequency_sum.ToString());
                writer.WriteLine("----------------------------------------");
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }

        // show file content after save
        if (File.Exists(filename))
        {
            System.Diagnostics.Process.Start("Notepad.exe", filename);
        }
    }

    private List<LetterStatistic> m_phrase_letter_statistics = new List<LetterStatistic>();
    public List<LetterStatistic> PhraseLetterStatistics
    {
        get { return m_phrase_letter_statistics; }
    }
    /// <summary>
    /// Calculate letter statistics for the given phrase in text.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="phrase"></param>
    /// <param name="frequency_sum_type"></param>
    /// <returns>Letter frequency sum. Result is stored in PhraseLetterStatistics.</returns>
    public int CalculatePhraseLetterStatistics(string text, string phrase, FrequencySumType frequency_sum_type)
    {
        if (String.IsNullOrEmpty(text)) return 0;
        if (String.IsNullOrEmpty(phrase)) return 0;
        if (NumerologySystem == null) return -1;
        if (m_phrase_letter_statistics == null) return -1;

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

        int letter_frequency_sum = 0;
        m_phrase_letter_statistics.Clear();
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
                LetterStatistic phrase_letter_statistic = new LetterStatistic();
                phrase_letter_statistic.Order = m_phrase_letter_statistics.Count + 1;
                phrase_letter_statistic.Letter = phrase[i];
                phrase_letter_statistic.Frequency = frequency;
                m_phrase_letter_statistics.Add(phrase_letter_statistic);
                letter_frequency_sum += frequency;
            }
        }

        return letter_frequency_sum;
    }
    public void SortPhraseLetterStatistics(StatisticSortMethod sort_method)
    {
        LetterStatistic.SortMethod = sort_method;
        m_phrase_letter_statistics.Sort();
        if (LetterStatistic.SortOrder == StatisticSortOrder.Ascending)
        {
            LetterStatistic.SortOrder = StatisticSortOrder.Descending;
        }
        else
        {
            LetterStatistic.SortOrder = StatisticSortOrder.Ascending;
        }
    }
    public void SavePhraseLetterStatistics(string filename, string text, string phrase)
    {
        if (String.IsNullOrEmpty(filename)) return;
        if (String.IsNullOrEmpty(text)) return;

        if (!Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            Directory.CreateDirectory(Globals.STATISTICS_FOLDER);
        }

        filename = Globals.STATISTICS_FOLDER + "/" + "Phrase_" + filename;
        try
        {
            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
            {
                writer.WriteLine("----------------------------------------");
                writer.WriteLine(NumerologySystem.Name);
                writer.WriteLine("----------------------------------------");
                writer.WriteLine();
                writer.WriteLine("Text");
                writer.WriteLine("----------------------------------------");
                writer.WriteLine(text);
                writer.WriteLine("----------------------------------------");
                writer.WriteLine();
                writer.WriteLine("Phrase");
                writer.WriteLine("----------------------------------------");
                writer.WriteLine(phrase);
                writer.WriteLine("----------------------------------------");
                writer.WriteLine();
                writer.WriteLine("----------------------------------------");
                writer.WriteLine("Order" + "\t" + "Letter" + "\t" + "Frequency");
                writer.WriteLine("----------------------------------------");
                int count = m_phrase_letter_statistics.Count;
                int frequency_sum = 0;
                for (int i = 0; i < count; i++)
                {
                    writer.WriteLine(m_phrase_letter_statistics[i].Order.ToString() + "\t" + m_phrase_letter_statistics[i].Letter.ToString() + '\t' + m_phrase_letter_statistics[i].Frequency.ToString());
                    frequency_sum += m_phrase_letter_statistics[i].Frequency;
                }
                writer.WriteLine("----------------------------------------");
                writer.WriteLine("Total" + "\t" + count.ToString() + "\t" + frequency_sum.ToString());
                writer.WriteLine("----------------------------------------");
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }

        // show file content after save
        if (File.Exists(filename))
        {
            System.Diagnostics.Process.Start("Notepad.exe", filename);
        }
    }

    private List<Bookmark> m_bookmarks = new List<Bookmark>();
    public List<Bookmark> Bookmarks
    {
        get { return m_bookmarks; }
    }
    private Bookmark m_current_bookmark;
    public Bookmark CurrentBookmark
    {
        get { return m_current_bookmark; }
    }
    private int m_current_bookmark_index = -1;
    public int CurrentBookmarkIndex
    {
        get
        {
            if (m_bookmarks != null)
            {
                for (int i = 0; i < m_bookmarks.Count; i++)
                {
                    if (m_bookmarks[i] == m_current_bookmark)
                    {
                        if (i == m_current_bookmark_index)
                        {
                            return i;
                        }
                        else
                        {
                            throw new Exception("current=" + m_current_bookmark_index + "\t\tbookmark_index=" + i);
                        }
                    }
                }
            }
            return -1;
        }
    }
    public int GetBookmarkIndex(Bookmark bookmark)
    {
        if (m_bookmarks != null)
        {
            for (int i = 0; i < m_bookmarks.Count; i++)
            {
                if (m_bookmarks[i] == bookmark)
                {
                    return i;
                }
            }
        }
        return -1;
    }
    public Bookmark GetBookmark(Selection selection)
    {
        if (selection != null)
        {
            // selection is mutable so we cannot use ==
            //foreach (Bookmark bookmark in m_bookmarks)
            //{
            //    if (bookmark.Selection == selection)
            //    {
            //        return bookmark;
            //    }
            //}
            return GetBookmark(selection.Scope, selection.Indexes);
        }
        return null;
    }
    public Bookmark GetBookmark(SelectionScope scope, List<int> indexes)
    {
        if (m_bookmarks != null)
        {
            foreach (Bookmark bookmark in m_bookmarks)
            {
                if (bookmark.Selection.Scope == scope)
                {
                    if (bookmark.Selection.Indexes.Count == indexes.Count)
                    {
                        int matching_indexes = 0;
                        for (int i = 0; i < bookmark.Selection.Indexes.Count; i++)
                        {
                            if (bookmark.Selection.Indexes[i] == indexes[i])
                            {
                                matching_indexes++;
                            }
                        }
                        if (indexes.Count == matching_indexes)
                        {
                            return bookmark;
                        }
                    }
                }
            }
        }
        return null;
    }
    public Bookmark GotoBookmark(Selection selection)
    {
        Bookmark bookmark = null;
        if (selection != null)
        {
            bookmark = GetBookmark(selection.Scope, selection.Indexes);
            if (bookmark != null)
            {
                m_current_bookmark = bookmark;
                m_current_bookmark_index = GetBookmarkIndex(bookmark);
            }
        }
        return bookmark;
    }
    public Bookmark GotoBookmark(SelectionScope scope, List<int> indexes)
    {
        Bookmark bookmark = GetBookmark(scope, indexes);
        if (bookmark != null)
        {
            m_current_bookmark = bookmark;
            m_current_bookmark_index = GetBookmarkIndex(bookmark);
        }
        return bookmark;
    }
    public Bookmark GotoNextBookmark()
    {
        if (m_bookmarks != null)
        {
            if (m_bookmarks.Count > 0)
            {
                if (m_current_bookmark_index < m_bookmarks.Count - 1)
                {
                    m_current_bookmark_index++;
                    m_current_bookmark = m_bookmarks[m_current_bookmark_index];
                }
            }
        }
        return m_current_bookmark;
    }
    public Bookmark GotoPreviousBookmark()
    {
        if (m_bookmarks != null)
        {
            if (m_bookmarks.Count > 0)
            {
                if (m_current_bookmark_index > 0)
                {
                    m_current_bookmark_index--;
                    m_current_bookmark = m_bookmarks[m_current_bookmark_index];
                }
            }
        }
        return m_current_bookmark;
    }
    public Bookmark CreateBookmark(Selection selection, string note)
    {
        Bookmark bookmark = GetBookmark(selection.Scope, selection.Indexes);
        if (bookmark != null) // overwrite existing bookmark
        {
            bookmark.Note = note;
            bookmark.LastModifiedTime = DateTime.Now;
            m_current_bookmark = bookmark;
        }
        else // create a new bookmark
        {
            bookmark = new Bookmark(selection, note);
            m_bookmarks.Insert(m_current_bookmark_index + 1, bookmark);
            m_current_bookmark_index++;
            m_current_bookmark = m_bookmarks[m_current_bookmark_index];
        }
        return m_current_bookmark;
    }
    public void AddBookmark(Selection selection, string note, DateTime created_time, DateTime last_modified_time)
    {
        if (m_bookmarks != null)
        {
            Bookmark bookmark = CreateBookmark(selection, note);
            if (bookmark != null)
            {
                bookmark.CreatedTime = created_time;
                bookmark.LastModifiedTime = last_modified_time;
            }
        }
    }
    public void DeleteCurrentBookmark()
    {
        Bookmark current_bookmark = CurrentBookmark;
        if (current_bookmark != null)
        {
            if (m_bookmarks != null)
            {
                m_bookmarks.Remove(current_bookmark);
                if (m_bookmarks.Count == 0) // no bookmark to display
                {
                    m_current_bookmark_index = -1;
                    m_current_bookmark = null;
                }
                else // there are bookmarks still
                {
                    // if index becomes outside list, move back into list
                    if (m_current_bookmark_index == m_bookmarks.Count)
                    {
                        m_current_bookmark_index = m_bookmarks.Count - 1;
                    }
                    m_current_bookmark = m_bookmarks[m_current_bookmark_index];
                }
            }
        }
    }
    public void ClearBookmarks()
    {
        if (m_bookmarks != null)
        {
            m_bookmarks.Clear();
            m_current_bookmark_index = -1;
            m_current_bookmark = null;
        }
    }
    public void SaveBookmarks()
    {
        if (m_book != null)
        {
            if (!Directory.Exists(Globals.BOOKMARKS_FOLDER))
            {
                Directory.CreateDirectory(Globals.BOOKMARKS_FOLDER);
            }

            string filename = Globals.BOOKMARKS_FOLDER + "/" + m_book.Title + ".txt";
            try
            {
                using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                {
                    if (m_bookmarks != null)
                    {
                        foreach (Bookmark bookmark in m_bookmarks)
                        {
                            if (bookmark.Note.Length > 0)
                            {
                                string scope_str = bookmark.Selection.Scope.ToString();

                                StringBuilder str = new StringBuilder();
                                if (bookmark.Selection.Indexes.Count > 0)
                                {
                                    for (int i = 0; i < bookmark.Selection.Indexes.Count; i++)
                                    {
                                        str.Append((bookmark.Selection.Indexes[i] + 1).ToString() + "+");
                                    }
                                    if (str.Length > 1)
                                    {
                                        str.Remove(str.Length - 1, 1);
                                    }
                                }

                                string created_time = bookmark.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss");
                                string last_modified_time = bookmark.LastModifiedTime.ToString("yyyy-MM-dd HH:mm:ss");
                                string note = bookmark.Note;

                                string line = scope_str + "," + str.ToString() + "," + created_time + "," + last_modified_time + "," + note;
                                writer.WriteLine(line);
                            }
                        }
                    }
                }
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }
        }
    }
    public void LoadBookmarks()
    {
        if (m_book != null)
        {
            string filename = Globals.BOOKMARKS_FOLDER + "/" + m_book.Title + ".txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] parts = line.Split(',');
                        if (parts.Length == 5)
                        {
                            try
                            {
                                SelectionScope scope = (SelectionScope)Enum.Parse(typeof(SelectionScope), parts[0]);

                                string part = parts[1].Trim();
                                string[] sub_parts = part.Split('+');
                                List<int> indexes = new List<int>();
                                foreach (string sub_part in sub_parts)
                                {
                                    indexes.Add(int.Parse(sub_part.Trim()) - 1);
                                }
                                Selection selection = new Selection(m_book, scope, indexes);

                                DateTime created_time = DateTime.ParseExact(parts[2], "yyyy-MM-dd HH:mm:ss", null);
                                DateTime last_modified_time = DateTime.ParseExact(parts[3], "yyyy-MM-dd HH:mm:ss", null);
                                string note = parts[4];

                                AddBookmark(selection, note, created_time, last_modified_time);
                            }
                            catch
                            {
                                throw new Exception("Invalid data format in " + filename);
                            }
                        }
                    }
                }
            }
        }
    }

    public List<string> HelpMessages
    {
        get { return Server.HelpMessages; }
    }
}
