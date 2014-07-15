using System;
using System.Text;
using System.Collections.Generic;

namespace Model
{
    public class NumerologySystem
    {
        // Favorite numerology system (©2008 Primalogy System - Ali Adams - www.heliwave.com)
        public const string PRIMALOGY_NUMERORLOGY_SYSTEM = "Simplified29_Alphabet_Primes";

        // User's favorite numerology system
        public static string FAVORITE_NUMERORLOGY_SYSTEM = PRIMALOGY_NUMERORLOGY_SYSTEM;

        private string name = "";
        public string Name
        {
            get { return name; }
            private set
            {
                if (name != value)
                {
                    name = value;
                    string[] parts = name.Split('_');
                    if (parts.Length == 3)
                    {
                        text_mode = parts[0];
                        letter_order = parts[1];
                        letter_value = parts[2];
                    }
                    else
                    {
                        throw new Exception("Name must be in this format:\r\nTextMode_LetterOrder_LetterValue");
                    }
                }
            }
        }
        private string text_mode = "";
        public string TextMode
        {
            get { return text_mode; }
        }
        private string letter_order = "";
        public string LetterOrder
        {
            get { return letter_order; }
        }
        private string letter_value = "";
        public string LetterValue
        {
            get { return letter_value; }
        }

        private NumerologySystemScope scope = NumerologySystemScope.Book;
        public NumerologySystemScope Scope
        {
            get { return scope; }
            set { scope = value; }
        }

        private Dictionary<char, long> letter_values = new Dictionary<char, long>();
        public Dictionary<char, long> LetterValues
        {
            get { return letter_values; }
        }
        // shortcut for LetterValues
        public long this[char letter]
        {
            get
            {
                if (letter_values.ContainsKey(letter))
                {
                    return letter_values[letter];
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
            set
            {
                if (letter_values.ContainsKey(letter))
                {
                    letter_values[letter] = value;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
        }
        public void Clear()
        {
            letter_values.Clear();
        }
        public int Count
        {
            get { return letter_values.Count; }
        }
        public void Add(char letter, long value)
        {
            if (letter_values.ContainsKey(letter))
            {
                throw new ArgumentException();
            }
            else
            {
                letter_values.Add(letter, value);
            }
        }
        public Dictionary<char, long>.KeyCollection Keys
        {
            get { return letter_values.Keys; }
        }
        public Dictionary<char, long>.ValueCollection Values
        {
            get { return letter_values.Values; }
        }
        public bool ContainsKey(char letter)
        {
            return letter_values.ContainsKey(letter);
        }

        public bool AddToLetterLNumber;
        public bool AddToLetterWNumber;
        public bool AddToLetterVNumber;
        public bool AddToLetterCNumber;
        public bool AddToLetterLDistance;
        public bool AddToLetterWDistance;
        public bool AddToLetterVDistance;
        public bool AddToLetterCDistance;
        public bool AddToWordWNumber;
        public bool AddToWordVNumber;
        public bool AddToWordCNumber;
        public bool AddToWordWDistance;
        public bool AddToWordVDistance;
        public bool AddToWordCDistance;
        public bool AddToVerseVNumber;
        public bool AddToVerseCNumber;
        public bool AddToVerseVDistance;
        public bool AddToVerseCDistance;
        public bool AddToChapterCNumber;

        public void Reset()
        {
            Name = FAVORITE_NUMERORLOGY_SYSTEM;
            Scope = NumerologySystemScope.Book;
            LetterValues.Clear();

            AddToLetterLNumber = false;
            AddToLetterWNumber = false;
            AddToLetterVNumber = false;
            AddToLetterCNumber = false;
            AddToLetterLDistance = false;
            AddToLetterWDistance = false;
            AddToLetterVDistance = false;
            AddToLetterCDistance = false;
            AddToWordWNumber = false;
            AddToWordVNumber = false;
            AddToWordCNumber = false;
            AddToWordWDistance = false;
            AddToWordVDistance = false;
            AddToWordCDistance = false;
            AddToVerseVNumber = false;
            AddToVerseCNumber = false;
            AddToVerseVDistance = false;
            AddToVerseCDistance = false;
            AddToChapterCNumber = false;
        }

        public long CalculateValue(char character)
        {
            string text = character.ToString();
            return CalculateValue(text); // simplify all text_modes (Original will be simplified29 automatically)
        }
        public long CalculateValue(string text)
        {
            if (String.IsNullOrEmpty(text)) return 0L;

            if (!text.IsArabic())  // eg English
            {
                text = text.ToUpper();
            }

            // in all cases
            // simplify all text_modes (Original will be simplified29 automatically)
            text = text.SimplifyTo(text_mode);

            long result = 0L;
            for (int i = 0; i < text.Length; i++)
            {
                if (letter_values.ContainsKey(text[i]))
                {
                    result += letter_values[text[i]];
                }
            }
            return result;
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine(Name);
            str.AppendLine(Scope.ToString());
            str.AppendLine(this.ToOverview());
            return str.ToString();
        }
        public string ToOverview()
        {
            StringBuilder str = new StringBuilder();

            if (
                 AddToLetterLNumber ||
                 AddToLetterWNumber ||
                 AddToLetterVNumber ||
                 AddToLetterCNumber ||
                 AddToLetterLDistance ||
                 AddToLetterWDistance ||
                 AddToLetterVDistance ||
                 AddToLetterCDistance
               )
            {
                str.Append("Add to each letter value");
                if (AddToLetterLNumber) str.Append("\t" + "L");
                if (AddToLetterWNumber) str.Append("\t" + "W");
                if (AddToLetterVNumber) str.Append("\t" + "V");
                if (AddToLetterCNumber) str.Append("\t" + "C");
                if (AddToLetterLDistance) str.Append("\t" + "∆L");
                if (AddToLetterWDistance) str.Append("\t" + "∆W");
                if (AddToLetterVDistance) str.Append("\t" + "∆V");
                if (AddToLetterCDistance) str.Append("\t" + "∆C");
                str.AppendLine();
            }


            if (
                 AddToWordWNumber ||
                 AddToWordVNumber ||
                 AddToWordCNumber ||
                 AddToWordWDistance ||
                 AddToWordVDistance ||
                 AddToWordCDistance
               )
            {
                str.Append("Add to each word value");
                if (AddToWordWNumber) str.Append("\t" + "W");
                if (AddToWordVNumber) str.Append("\t" + "V");
                if (AddToWordCNumber) str.Append("\t" + "C");
                if (AddToWordWDistance) str.Append("\t" + "∆W");
                if (AddToWordVDistance) str.Append("\t" + "∆V");
                if (AddToWordCDistance) str.Append("\t" + "∆C");
                str.AppendLine();
            }


            if (
                 AddToVerseVNumber ||
                 AddToVerseCNumber ||
                 AddToVerseVDistance ||
                 AddToVerseCDistance
               )
            {
                str.Append("Add to each verse value");
                if (AddToVerseVNumber) str.Append("\t" + "V");
                if (AddToVerseCNumber) str.Append("\t" + "C");
                if (AddToVerseVDistance) str.Append("\t" + "∆V");
                if (AddToVerseCDistance) str.Append("\t" + "∆C");
                str.AppendLine();
            }

            if (
                 AddToChapterCNumber
               )
            {
                str.Append("Add to each chapter value");
                if (AddToChapterCNumber) str.Append("\t" + "C");
                str.AppendLine();
            }

            str.AppendLine("----------------------------------------");
            str.AppendLine("Letter" + "\t" + "Value");
            str.AppendLine("----------------------------------------");
            foreach (char letter in LetterValues.Keys)
            {
                str.AppendLine(letter.ToString() + "\t" + LetterValues[letter].ToString());
            }
            str.AppendLine("----------------------------------------");

            return str.ToString();
        }
        public string ToTabbedString()
        {
            return (TextMode +
                    "\t" + LetterOrder +
                    "\t" + LetterValue +
                    "\t" + Scope.ToString() +
                    "\t" + (AddToLetterLNumber ? "L" : "") +
                    "\t" + (AddToLetterWNumber ? "W" : "") +
                    "\t" + (AddToLetterVNumber ? "V" : "") +
                    "\t" + (AddToLetterCNumber ? "C" : "") +
                    "\t" + (AddToLetterLDistance ? "∆L" : "") +
                    "\t" + (AddToLetterWDistance ? "∆W" : "") +
                    "\t" + (AddToLetterVDistance ? "∆V" : "") +
                    "\t" + (AddToLetterCDistance ? "∆C" : "") +
                    "\t" + (AddToWordWNumber ? "W" : "") +
                    "\t" + (AddToWordVNumber ? "V" : "") +
                    "\t" + (AddToWordCNumber ? "C" : "") +
                    "\t" + (AddToWordWDistance ? "∆W" : "") +
                    "\t" + (AddToWordVDistance ? "∆V" : "") +
                    "\t" + (AddToWordCDistance ? "∆C" : "") +
                    "\t" + (AddToVerseVNumber ? "V" : "") +
                    "\t" + (AddToVerseCNumber ? "C" : "") +
                    "\t" + (AddToVerseVDistance ? "∆V" : "") +
                    "\t" + (AddToVerseCDistance ? "∆C" : "") +
                    "\t" + (AddToChapterCNumber ? "C" : "")
           );
        }

        public NumerologySystem()
            : this(FAVORITE_NUMERORLOGY_SYSTEM)
        {
        }
        public NumerologySystem(string name)
        {
            // Name will update TextMode, LetterOrder, LetterValue too
            Name = name;
        }
        public NumerologySystem(string name, Dictionary<char, long> letter_values)
            : this(name)
        {
            this.letter_values = letter_values;
        }
        // copy constructors
        public NumerologySystem(NumerologySystem numerology_system)
        {
            if (numerology_system != null)
            {
                Name = numerology_system.Name;
                Scope = numerology_system.Scope;
                LetterValues.Clear();
                foreach (char key in numerology_system.Keys)
                {
                    LetterValues.Add(key, numerology_system[key]);
                }
                AddToLetterLNumber = numerology_system.AddToLetterLNumber;
                AddToLetterWNumber = numerology_system.AddToLetterWNumber;
                AddToLetterVNumber = numerology_system.AddToLetterVNumber;
                AddToLetterCNumber = numerology_system.AddToLetterCNumber;
                AddToLetterLDistance = numerology_system.AddToLetterLDistance;
                AddToLetterWDistance = numerology_system.AddToLetterWDistance;
                AddToLetterVDistance = numerology_system.AddToLetterVDistance;
                AddToLetterCDistance = numerology_system.AddToLetterCDistance;
                AddToWordWNumber = numerology_system.AddToWordWNumber;
                AddToWordVNumber = numerology_system.AddToWordVNumber;
                AddToWordCNumber = numerology_system.AddToWordCNumber;
                AddToWordWDistance = numerology_system.AddToWordWDistance;
                AddToWordVDistance = numerology_system.AddToWordVDistance;
                AddToWordCDistance = numerology_system.AddToWordCDistance;
                AddToVerseVNumber = numerology_system.AddToVerseVNumber;
                AddToVerseCNumber = numerology_system.AddToVerseCNumber;
                AddToVerseVDistance = numerology_system.AddToVerseVDistance;
                AddToVerseCDistance = numerology_system.AddToVerseCDistance;
                AddToChapterCNumber = numerology_system.AddToChapterCNumber;
            }
        }
        // copy constructor with new name
        public NumerologySystem(NumerologySystem numerology_system, string numerology_system_name)
            : this(numerology_system)
        {
            if (numerology_system != null)
            {
                Name = numerology_system_name;
            }
        }
    }
}
