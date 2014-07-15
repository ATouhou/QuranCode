using System;
using System.Text;
using System.Collections.Generic;

namespace Model
{
    public class FindHistoryItem
    {
        public FindType FindType = FindType.Text;
        public NumberSearchType NumberSearchType = NumberSearchType.Verses;
        public string Text = null;
        public string Header = null;
        public LanguageType LanguageType = LanguageType.Arabic;
        public string Translation = null;
        public List<Verse> Verses = new List<Verse>();
        public List<Phrase> Phrases = new List<Phrase>();
    }
}
