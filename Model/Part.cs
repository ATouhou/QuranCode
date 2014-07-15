using System;
using System.Text;
using System.Collections.Generic;

namespace Model
{
    public class Part
    {
        public const int MIN_NUMBER = 1;
        public const int MAX_NUMBER = 30;

        private List<Verse> verses = null;
        public List<Verse> Verses
        {
            get { return verses; }
        }

        private int number = 0;
        public int Number
        {
            get { return number; }
        }

        public Part(int number, List<Verse> verses)
        {
            this.number = number;
            this.verses = verses;
            if (this.verses != null)
            {
                foreach (Verse verse in this.verses)
                {
                    verse.Part = this;
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
    }
}
