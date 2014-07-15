using System;
using System.Text;
using System.Collections.Generic;

namespace Model
{
    public class Verse
    {
        public const int MIN_NUMBER = 1;
        public const int MAX_NUMBER = 6236;

        private Book book = null;
        public Book Book
        {
            get { return book; }
            set { book = value; }
        }

        private Chapter chapter = null;
        public Chapter Chapter
        {
            get { return chapter; }
            set { chapter = value; }
        }

        private int number = 0;
        public int Number
        {
            get { return number; }
            set { number = value; }
        }

        private int number_in_chapter = 0;
        public int NumberInChapter
        {
            set { number_in_chapter = value; }
            get
            {
                if (number_in_chapter == 0)
                {
                    if (this.book != null)
                    {
                        if (this.book.Chapters != null)
                        {
                            number_in_chapter = number;
                            foreach (Chapter chapter in this.book.Chapters)
                            {
                                if (chapter == this.chapter)
                                {
                                    break;
                                }
                                number_in_chapter -= chapter.Verses.Count;
                            }
                        }
                    }
                }
                return number_in_chapter;
            }
        }

        public Distance DistanceToPrevious = new Distance();

        public string Address
        {
            get
            {
                if (chapter != null)
                {
                    return (this.chapter.Number.ToString() + ":" + NumberInChapter.ToString());
                }
                return "0:0";
            }
        }
        public string PaddedAddress
        {
            get
            {
                if (chapter != null)
                {
                    return (this.chapter.Number.ToString("000") + ":" + NumberInChapter.ToString("000"));
                }
                return "000:000";
            }
        }
        public string ArabicAddress
        {
            get
            {
                if (chapter != null)
                {
                    return (this.chapter.Number.ToArabic() + "_" + NumberInChapter.ToArabic());
                }
                return "٠_٠";
            }
        }

        private Station station = null;
        public Station Station
        {
            get { return station; }
            set { station = value; }
        }

        private Part part = null;
        public Part Part
        {
            get { return part; }
            set { part = value; }
        }

        private Group group = null;
        public Group Group
        {
            get { return group; }
            set { group = value; }
        }

        private Quarter quarter = null;
        public Quarter Quarter
        {
            get { return quarter; }
            set { quarter = value; }
        }

        private Bowing bowing = null;
        public Bowing Bowing
        {
            get { return bowing; }
            set { bowing = value; }
        }

        private Page page = null;
        public Page Page
        {
            get { return page; }
            set { page = value; }
        }

        private Prostration prostration = null;
        public Prostration Prostration
        {
            get { return prostration; }
            set { prostration = value; }
        }

        private List<Word> words = null;
        public List<Word> Words
        {
            get { return words; }
        }

        public Letter GetLetter(int index)
        {
            foreach (Word word in this.words)
            {
                if (index >= word.Letters.Count)
                {
                    index -= word.Letters.Count;
                }
                else
                {
                    return word.Letters[index];
                }
            }
            return null;
        }
        public int LetterCount
        {
            get
            {
                int letter_count = 0;
                foreach (Word word in this.words)
                {
                    letter_count += word.Letters.Count;
                }
                return letter_count;
            }
        }

        private List<char> unique_letters = null;
        public List<char> UniqueLetters
        {
            get
            {
                //if (unique_letters == null)
                {
                    unique_letters = new List<char>();
                    foreach (Word word in this.words)
                    {
                        foreach (char character in word.UniqueLetters)
                        {
                            if (!unique_letters.Contains(character))
                            {
                                unique_letters.Add(character);
                            }
                        }
                    }
                }
                return unique_letters;
            }
        }
        public int GetLetterFrequency(char character)
        {
            int result = 0;
            foreach (Word word in this.words)
            {
                foreach (Letter letter in word.Letters)
                {
                    if (letter.Character == character)
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        public const string OPEN_BRACKET = "{";
        public const string CLOSE_BRACKET = "}";
        public string Endmark
        {
            get
            {
                if (s_include_number)
                {
                    return (" " + OPEN_BRACKET + NumberInChapter.ToString().ToArabicNumber() + CLOSE_BRACKET + " ");
                }
                else
                {
                    return "\n"; // this is compatible with a RichTextBox
                }
            }
        }
        private static bool s_include_number;
        public static bool IncludeNumber
        {
            get { return s_include_number; }
            set { s_include_number = value; }
        }

        public Verse(int number, string text, Stopmark stopmark)
        {
            text = text.Replace("\r", "");
            text = text.Replace("\n", "");
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            this.number = number;
            this.text = text;
            this.stopmark = stopmark;
        }

        private string text;
        public string Text
        {
            get { return text; }
        }
        public override string ToString()
        {
            return this.Text;
        }

        // assume all verse complete their sentences at end
        Stopmark stopmark = Stopmark.MustStop;
        public Stopmark Stopmark
        {
            get { return stopmark; }
        }

        /// <summary>
        /// build verse words with appropriate stopmarks regardless of text_mode
        /// </summary>
        /// <param name="text_mode"></param>
        /// <param name="original_text"></param>
        public void BuildWords(string text_mode, string original_text)
        {
            if (String.IsNullOrEmpty(text_mode)) return;
            if (String.IsNullOrEmpty(original_text)) return;
            original_text = original_text.Trim();

            this.words = new List<Word>();
            Stopmark word_stopmark;

            int word_number_in_verse = 0;
            int word_position_in_verse = 0;

            string[] original_word_texts = original_text.Split();
            for (int i = 0; i < original_word_texts.Length; i++)
            {
                word_number_in_verse++;
                word_stopmark = Stopmark.None;

                string original_word_text = original_word_texts[i];
                string word_text = original_word_text;
                if (text_mode.Contains("Simplified"))
                {
                    word_text = original_word_text.SimplifyTo(text_mode);
                }

                // skip stopmarks/quranmarks
                if (
                     (original_word_text.Length == 1)
                     &&
                     !((original_word_text == "ص") || (original_word_text == "ق") || (original_word_text == "ن") || (original_word_text == "و"))
                   )
                {
                    if (text_mode.Contains("Original"))
                    {
                        if (Constants.STOPMARKS.Contains(original_word_text[0]))
                        {
                            // advance position if original and stopmark
                            word_position_in_verse += 2; // 2 for stopmark and space after it
                        }
                        else
                        {
                            // ignore quranmarks
                        }
                    }
                    continue;
                }
                else
                { // lookahead to determine word stopmark

                    // if not last word in verse
                    if (i < original_word_texts.Length - 1)
                    {
                        string next_original_word_text = original_word_texts[i + 1];
                        switch (next_original_word_text)
                        {
                            case "":  // next word is never empty (never comes here)
                                word_stopmark = Stopmark.None;
                                break;
                            case "ۙ": // Laaa
                                word_stopmark = Stopmark.MustContinue;
                                break;
                            case "ۖ": // Sala
                                word_stopmark = Stopmark.ShouldContinue;
                                break;
                            case "ۚ": // Jeem
                                word_stopmark = Stopmark.CanStop;
                                break;
                            case "ۛ": // Dots
                                word_stopmark = Stopmark.CanStopAtOneOnly;
                                break;
                            case "ۗ": // Qala
                                word_stopmark = Stopmark.ShouldStop;
                                break;
                            case "ۜ": // Seen
                                word_stopmark = Stopmark.MustPause;
                                break;
                            case "ۘ": // Meem
                                word_stopmark = Stopmark.MustStop;
                                break;
                            default: // Quran word
                                word_stopmark = Stopmark.None;
                                break;
                        }

                        // add stopmark.CanStop after بسم الله الرحمن الرحيم except 1:1 and 27:30
                        if (word_number_in_verse == 4)
                        {
                            if (word_text.Simplify29() == "الرحيم")
                            {
                                word_stopmark = Stopmark.CanStop;
                            }
                        }
                    }
                    else // last word in verse
                    {
                        // if no stopmark after word
                        if (word_stopmark == Stopmark.None)
                        {
                            // use verse stopmark
                            word_stopmark = this.Stopmark;
                        }
                    }

                    Word word = new Word(this, word_number_in_verse, word_position_in_verse, word_text, word_stopmark);
                    this.words.Add(word);
                }

                // in all cases
                word_position_in_verse += word_text.Length + 1; // 1 for space
            }
        }

        private long value;
        public long Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        /// <summary>
        /// Language --> Text
        /// </summary>
        public Dictionary<string, string> Translations = new Dictionary<string, string>();
    }
}
