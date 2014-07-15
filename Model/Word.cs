using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public class Word
    {
        private Verse verse = null;
        public Verse Verse
        {
            get { return verse; }
        }

        private int number = 0;
        public int Number
        {
            set { number = value; }
            get
            {
                if (number == 0)
                {
                    if (verse.Book != null)
                    {
                        foreach (Verse v in verse.Book.Verses)
                        {
                            if (v.Number == verse.Number)
                            {
                                number += number_in_verse;
                                break;
                            }
                            number += v.Words.Count;
                        }
                    }
                }
                return number;
            }
        }

        private int number_in_chapter = 0;
        public int NumberInChapter
        {
            set { number_in_chapter = value; }
            get
            {
                if (number_in_chapter == 0)
                {
                    if (verse != null)
                    {
                        if (verse.Chapter != null)
                        {
                            foreach (Verse v in verse.Chapter.Verses)
                            {
                                if (v.Number == verse.Number)
                                {
                                    number_in_chapter += number_in_verse;
                                    break;
                                }
                                number_in_chapter += v.Words.Count;
                            }
                        }
                    }
                }
                return number_in_chapter;
            }
        }

        private int number_in_verse = 0;
        public int NumberInVerse
        {
            set { number_in_verse = value; }
            get { return number_in_verse; }
        }

        public Distance DistanceToPrevious = new Distance();

        public string Address
        {
            get
            {
                if (verse != null)
                {
                    if (verse.Chapter != null)
                    {
                        return (this.verse.Chapter.Number.ToString() + ":" + verse.NumberInChapter.ToString() + ":" + number_in_verse.ToString());
                    }
                }
                return "XXX:XXX:XXX";
            }
        }

        private string transliteration = null;
        public string Transliteration
        {
            get
            {
                if (transliteration == null)
                {
                    if (this.Text == "و")
                    {
                        transliteration = "wa";
                    }
                    else
                    {
                        if (this.Verse.Translations.ContainsKey("en.transliteration"))
                        {
                            string verse_transliteration = this.Verse.Translations["en.transliteration"];
                            string[] parts = verse_transliteration.Split();

                            int index = this.NumberInVerse - 1;
                            for (int i = 0; i < this.NumberInVerse; i++)
                            {
                                if (this.Verse.Words[i].Text == "و")
                                {
                                    index--;
                                }
                            }

                            // remove wa from words following wa
                            int w = this.NumberInVerse - 1;
                            if (w > 0)
                            {
                                if (this.Verse.Words[w - 1].Text == "و")
                                {
                                    parts[index] = parts[index].Substring(2);
                                }
                            }

                            transliteration = parts[index];
                        }
                    }
                }
                return transliteration;
            }
        }

        private string meaning = null;
        public string Meaning
        {
            set { meaning = value; }
            get { return meaning; }
        }

        private List<string> roots = null;
        public List<string> Roots
        {
            set { roots = value; }
            get { return roots; }
        }

        private List<WordPart> parts = null;
        public List<WordPart> Parts
        {
            get { return parts; }
        }

        private string corpus_root = null;
        public string CorpusRoot
        {
            get
            {
                if (String.IsNullOrEmpty(corpus_root))
                {
                    if (parts != null)
                    {
                        foreach (WordPart part in parts)
                        {
                            if (!String.IsNullOrEmpty(part.Grammar.Root))
                            {
                                corpus_root = part.Grammar.Root.ToArabic();
                                break;
                            }
                        }
                    }
                }
                return corpus_root;
            }
        }

        private string corpus_lemma = null;
        public string CorpusLemma
        {
            get
            {
                if (String.IsNullOrEmpty(corpus_lemma))
                {
                    if (parts != null)
                    {
                        foreach (WordPart part in parts)
                        {
                            if (!String.IsNullOrEmpty(part.Grammar.Lemma))
                            {
                                corpus_lemma += part.Grammar.Lemma.ToArabic();
                                break;
                            }
                        }
                    }
                }
                return corpus_lemma;
            }
        }

        private string corpus_special_group = null;
        public string CorpusSpecialGroup
        {
            get
            {
                if (String.IsNullOrEmpty(corpus_special_group))
                {
                    if (parts != null)
                    {
                        foreach (WordPart part in parts)
                        {
                            if (!String.IsNullOrEmpty(part.Grammar.SpecialGroup))
                            {
                                corpus_special_group += part.Grammar.SpecialGroup.ToArabic();
                                break;
                            }
                        }
                    }
                }
                return corpus_special_group;
            }
        }

        private string arabic_grammar = null;
        public string ArabicGrammar
        {
            get
            {
                if (String.IsNullOrEmpty(arabic_grammar))
                {
                    StringBuilder result = new StringBuilder();

                    StringBuilder str = new StringBuilder();
                    string previous_word_part_address = null;
                    for (int i = 0; i < Parts.Count; i++)
                    {
                        if (previous_word_part_address == Parts[i].Word.Address)
                        {
                            // continue with current word
                            str.AppendLine(Parts[i].ToArabic());
                        }
                        else // new word
                        {
                            previous_word_part_address = Parts[i].Word.Address;

                            // finish up previous word
                            if (str.Length > 2)
                            {
                                str.Remove(str.Length - 2, 2);
                                result.Append(str.ToString());
                                // clear str for new word
                                str.Length = 0;
                            }

                            // continue with current word
                            str.AppendLine(Parts[i].ToArabic());
                        }

                        if (i == Parts.Count - 1)
                        {
                            // finish up last word
                            if (str.Length > 2)
                            {
                                str.Remove(str.Length - 2, 2);
                                result.Append(str.ToString());
                            }
                        }
                    }

                    arabic_grammar = result.ToString();
                }

                return arabic_grammar;
            }
        }

        private string english_grammar = null;
        public string EnglishGrammar
        {
            get
            {
                if (String.IsNullOrEmpty(english_grammar))
                {
                    StringBuilder result = new StringBuilder();

                    StringBuilder str = new StringBuilder();
                    string previous_word_part_address = null;
                    for (int i = 0; i < Parts.Count; i++)
                    {
                        if (previous_word_part_address == Parts[i].Word.Address)
                        {
                            // continue with current word
                            str.AppendLine(Parts[i].ToEnglish());
                        }
                        else // new word
                        {
                            previous_word_part_address = Parts[i].Word.Address;

                            // finish up previous word
                            if (str.Length > 2)
                            {
                                str.Remove(str.Length - 2, 2);
                                result.Append(str.ToString());
                                // clear str for new word
                                str.Length = 0;
                            }

                            // continue with current word
                            str.AppendLine(Parts[i].ToEnglish());
                        }

                        if (i == Parts.Count - 1)
                        {
                            // finish up last word
                            if (str.Length > 2)
                            {
                                str.Remove(str.Length - 2, 2);
                                result.Append(str.ToString());
                            }
                        }
                    }

                    english_grammar = result.ToString();
                }

                return english_grammar;
            }
        }

        private int occurrence = 0;
        public int Occurrence
        {
            get
            {
                if (occurrence == 0)
                {
                    bool found = false;
                    foreach (Verse verse in this.Verse.Book.Verses)
                    {
                        if (!found)
                        {
                            foreach (Word word in verse.Words)
                            {
                                if (word.Text == this.text)
                                {
                                    occurrence++;
                                }

                                if (word == this)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                return occurrence;
            }
        }

        private int occurrences = 0;
        public int Occurrences
        {
            get
            {
                if (occurrences == 0)
                {
                    foreach (Verse verse in this.Verse.Book.Verses)
                    {
                        foreach (Word word in verse.Words)
                        {
                            if (word.Text == this.Text)
                            {
                                occurrences++;
                            }
                        }
                    }
                }
                return occurrences;
            }
        }

        private List<Letter> letters = null;
        public List<Letter> Letters
        {
            get { return letters; }
        }

        private List<char> unique_letters = null;
        public List<char> UniqueLetters
        {
            get
            {
                //if (unique_letters == null)
                {
                    unique_letters = new List<char>();
                    foreach (Letter letter in this.Letters)
                    {
                        if (!unique_letters.Contains(letter.Character))
                        {
                            unique_letters.Add(letter.Character);
                        }
                    }
                }
                return unique_letters;
            }
        }

        private int position = -1;
        public int Position
        {
            get { return position; }
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

        private Stopmark stopmark = Stopmark.None;
        public Stopmark Stopmark
        {
            get { return stopmark; }
        }

        public Word(Verse verse, int number_in_verse, int position_in_verse, string text, Stopmark stopmark)
        {
            this.verse = verse;
            this.number_in_verse = number_in_verse;
            this.position = position_in_verse;
            this.text = text;
            this.stopmark = stopmark;

            if ((Globals.EDITION == Edition.Grammar) || (Globals.EDITION == Edition.Research))
            {
                this.parts = new List<WordPart>();
            }

            this.letters = new List<Letter>();
            if (this.Letters != null)
            {
                int letter_number_in_word = 0;

                // for correct UniqueLetters calculation in Original text
                string simplified_text = this.text;
                if (this.text.IsArabicWithDiacritics())
                {
                    simplified_text = this.text.Simplify29();
                }

                foreach (char character in simplified_text)
                {
                    if (Constants.ARABIC_LETTERS.Contains(character))
                    {
                        letter_number_in_word++;

                        Letter letter = new Letter(this, character, letter_number_in_word);
                        this.Letters.Add(letter);
                    }
                }
            }
        }
    }
}
