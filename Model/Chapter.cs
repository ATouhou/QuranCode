using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public enum ChapterSortMethod { ByCompilation, ByRevelation, ByVerses, ByWords, ByLetters, ByValue }
    public enum ChapterSortOrder { Ascending, Descending }
    public class Chapter : IComparable<Chapter>
    {
        /// <summary>
        /// <para>false = pin   chapter Al-Fatiha in place regardless of sorting</para>
        /// <para>true  = allow chapter Al-Fatiha to move freely</para>
        /// <para>null  = allow chapter Al-Fatiha to move in ByCompilation and ByRevelation only</para>
        /// </summary>
        public static bool? PinTheKey = false;

        public static ChapterSortMethod SortMethod;
        public static ChapterSortOrder SortOrder;
        public int CompareTo(Chapter obj)
        {
            if (this == obj) return 0;

            if ((Chapter.PinTheKey == true) && (this.Number == 1)) return -1;
            if ((Chapter.PinTheKey == true) && (obj.Number == 1)) return 1;

            if (SortOrder == ChapterSortOrder.Ascending)
            {
                switch (SortMethod)
                {
                    case ChapterSortMethod.ByCompilation:
                        {
                            return this.Number.CompareTo(obj.Number);
                        }
                    case ChapterSortMethod.ByRevelation:
                        {
                            if (this.RevelationOrder.CompareTo(obj.RevelationOrder) == 0)
                            {
                                return this.Number.CompareTo(obj.Number);
                            }
                            return this.RevelationOrder.CompareTo(obj.RevelationOrder);
                        }
                    case ChapterSortMethod.ByVerses:
                        {
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return 1;
                            if (this.verses.Count.CompareTo(obj.Verses.Count) == 0)
                            {
                                return this.Number.CompareTo(obj.Number);
                            }
                            return this.verses.Count.CompareTo(obj.Verses.Count);
                        }
                    case ChapterSortMethod.ByWords:
                        {
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return 1;
                            if (this.WordCount.CompareTo(obj.WordCount) == 0)
                            {
                                return this.Number.CompareTo(obj.Number);
                            }
                            return this.WordCount.CompareTo(obj.WordCount);
                        }
                    case ChapterSortMethod.ByLetters:
                        {
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return 1;
                            if (this.LetterCount.CompareTo(obj.LetterCount) == 0)
                            {
                                return this.Number.CompareTo(obj.Number);
                            }
                            return this.LetterCount.CompareTo(obj.LetterCount);
                        }
                    case ChapterSortMethod.ByValue:
                        {
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return 1;
                            if (this.Value.CompareTo(obj.Value) == 0)
                            {
                                return this.Number.CompareTo(obj.Number);
                            }
                            return this.Value.CompareTo(obj.Value);
                        }
                    default:
                        {
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return 1;
                            return this.Number.CompareTo(obj.Number);
                        }
                }
            }
            else
            {
                switch (SortMethod)
                {
                    case ChapterSortMethod.ByCompilation:
                        {
                            return obj.Number.CompareTo(this.Number);
                        }
                    case ChapterSortMethod.ByRevelation:
                        {
                            if (obj.RevelationOrder.CompareTo(this.RevelationOrder) == 0)
                            {
                                return obj.Number.CompareTo(this.Number);
                            }
                            return obj.RevelationOrder.CompareTo(this.RevelationOrder);
                        }
                    case ChapterSortMethod.ByVerses:
                        {
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return 1;
                            if (obj.verses.Count.CompareTo(this.Verses.Count) == 0)
                            {
                                return obj.Number.CompareTo(this.Number);
                            }
                            return obj.verses.Count.CompareTo(this.Verses.Count);
                        }
                    case ChapterSortMethod.ByWords:
                        {
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return 1;
                            if (obj.WordCount.CompareTo(this.WordCount) == 0)
                            {
                                return obj.Number.CompareTo(this.Number);
                            }
                            return obj.WordCount.CompareTo(this.WordCount);
                        }
                    case ChapterSortMethod.ByLetters:
                        {
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return 1;
                            if (obj.LetterCount.CompareTo(this.LetterCount) == 0)
                            {
                                return obj.Number.CompareTo(this.Number);
                            }
                            return obj.LetterCount.CompareTo(this.LetterCount);
                        }
                    case ChapterSortMethod.ByValue:
                        {
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return 1;
                            if (obj.Value.CompareTo(this.Value) == 0)
                            {
                                return obj.Number.CompareTo(this.Number);
                            }
                            return obj.Value.CompareTo(this.Value);
                        }
                    default:
                        {
                            if ((Chapter.PinTheKey == null) && (obj.Number == 1)) return -1;
                            if ((Chapter.PinTheKey == null) && (this.Number == 1)) return 1;
                            return obj.Number.CompareTo(this.Number);
                        }
                }
            }
        }

        public const int MIN_NUMBER = 1;
        public const int MAX_NUMBER = 114;
        public const int MIN_VERSE_NUMBER = 3;
        public const int MAX_VERSE_NUMBER = 286;

        private List<Verse> verses = null;
        public List<Verse> Verses
        {
            get { return verses; }
        }

        private int number = 0;
        public int Number
        {
            get { return number; }
            set { number = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
        }

        private string transliterated_name;
        public string TransliteratedName
        {
            get { return transliterated_name; }
        }

        private string english_name;
        public string EnglishName
        {
            get { return english_name; }
        }

        private int revelation_order;
        public int RevelationOrder
        {
            get { return revelation_order; }
        }

        private RevelationPlace revelation_place;
        public RevelationPlace RevelationPlace
        {
            get { return revelation_place; }
        }

        private int bowing_count;
        public int BowingCount
        {
            get { return bowing_count; }
        }

        public Word GetWord(int index)
        {
            foreach (Verse verse in this.Verses)
            {
                if (index >= verse.Words.Count)
                {
                    index -= verse.Words.Count;
                }
                else
                {
                    return verse.Words[index];
                }
            }
            return null;
        }
        public int WordCount
        {
            get
            {
                int word_count = 0;
                foreach (Verse verse in this.verses)
                {
                    word_count += verse.Words.Count;
                }
                return word_count;
            }
        }

        public Letter GetLetter(int index)
        {
            foreach (Verse verse in this.Verses)
            {
                foreach (Word word in verse.Words)
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
            }
            return null;
        }
        public int LetterCount
        {
            get
            {
                int letter_count = 0;
                foreach (Verse verse in this.verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        letter_count += word.Letters.Count;
                    }
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
                    foreach (Verse verse in this.verses)
                    {
                        foreach (char character in verse.UniqueLetters)
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
            foreach (Verse verse in this.verses)
            {
                foreach (Word word in verse.Words)
                {
                    foreach (Letter letter in word.Letters)
                    {
                        if (letter.Character == character)
                        {
                            result++;
                        }
                    }
                }
            }
            return result;
        }

        public Chapter(int number,
                        string name,
                        string transliterated_name,
                        string english_name,
                        int revelation_order,
                        RevelationPlace revelation_place,
                        int bowing_count,
                        List<Verse> verses)
        {
            this.number = number;
            this.name = name;
            this.transliterated_name = transliterated_name;
            this.english_name = english_name;
            this.revelation_order = revelation_order;
            this.revelation_place = revelation_place;
            this.bowing_count = bowing_count;
            this.verses = verses;
            if (this.verses != null)
            {
                foreach (Verse verse in this.verses)
                {
                    verse.Chapter = this;
                }
            }
        }

        public string Text
        {
            get
            {
                StringBuilder str = new StringBuilder();
                if (this.verses != null)
                {
                    if (this.verses.Count > 0)
                    {
                        foreach (Verse verse in this.verses)
                        {
                            str.AppendLine(verse.Text);
                        }
                        if (str.Length > 2)
                        {
                            str.Remove(str.Length - 2, 2);
                        }
                    }
                }
                return str.ToString();
            }
        }
        public override string ToString()
        {
            return this.Text;
        }

        private long value;
        public long Value
        {
            get
            {
                value = 0L;
                foreach (Verse verse in this.verses)
                {
                    value += verse.Value;
                }

                return value;
            }
        }
    }
}
