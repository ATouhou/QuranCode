using System;
using System.Collections.Generic;
using System.Text;

namespace InitialLetters
{
    public class Letters : IComparable
    {
        private string characters;

        public Letters(string text)
        {
            Char[] chars = text.ToLower().ToCharArray();
            Array.Sort(chars);
            Char[] letters = Array.FindAll<char>(chars, Char.IsLetter);
            StringBuilder str = new StringBuilder();
            str.Insert(0, letters);
            characters = str.ToString();
        }
        public bool Empty()
        {
            return (characters.Length == 0);
        }
        public Letters Subtract(Letters second)
        {
            string letters1 = characters;
            string letters2 = second.characters;
            string different_letters = "";

            while (true)
            {
                if (letters2.Length == 0) return new Letters(different_letters + letters1);
                if (letters1.Length == 0) return null;
                {
                    char letter2 = letters2[0];
                    char letter1 = letters1[0];
                    if (letter1 > letter2) return null;

                    if (letter1 < letter2)
                    {
                        letters1 = letters1.Substring(1);
                        different_letters += letter1;
                        continue;
                    }

                    //if (letter1 == letter2) throw new Exception("Error: letter1 == letter2");

                    letters1 = letters1.Substring(1);
                    letters2 = letters2.Substring(1);
                }
            }
        }
        private static string Subtract(string first, string sencod)
        {
            Letters letters1 = new Letters(first);
            Letters letters2 = new Letters(sencod);
            Letters different_letters = letters1.Subtract(letters2);
            if (different_letters == null) return null;
            return different_letters.ToString();
        }

        public override string ToString()
        {
            return characters;
        }
        public override int GetHashCode()
        {
            return characters.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return characters.Equals(((Letters)obj).characters);
        }
        public int CompareTo(object obj)
        {
            Letters other = obj as Letters;
            if (other != null)
            {
                if (this.characters.Length > other.characters.Length)
                    return -1;
                else if (this.characters.Length < other.characters.Length)
                    return 1;
                else
                    return 0;
            }
            throw new InvalidCastException();
        }

        private static void TestSubtraction(string first, string second, string expected_difference)
        {
            string actual_difference = Subtract(first, second);
            System.Diagnostics.Trace.Assert(actual_difference == expected_difference,
                                             "Test failure: "
                                             + "Subtracting `" + second
                                             + "' from `" + first
                                             + "' yielded `" + actual_difference
                                             + "', but should have yielded `" + expected_difference + "'.");
        }
        public static void Test()
        {
            TestSubtraction("tile", "lite", "");
            TestSubtraction("tiles", "lite", "s");
            TestSubtraction("a", "b", null);
            Console.WriteLine("All tests have passed.");
        }
    }
}
