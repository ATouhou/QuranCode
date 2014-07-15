using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace InitialLetters
{
    // new type shortcut
    public class Sentence : List<string>
    {
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            if (this.Count > 1)
            {
                foreach (string word in this)
                {
                    str.Append(word + " ");
                }
                if (str.Length > 1)
                {
                    str.Remove(str.Length - 1, 1); // remove last space
                }
            }
            return str.ToString();
        }
    }

    // callback functions to indicate progress
    public delegate void LoopEndedDelegate();
    public delegate void PruningDoneDelegate(uint recursion_level, List<Entry> pruned_entries);
    public delegate void SentenceFoundDelegate(Sentence words);

    // Entry for a list of letters and their possible words.
    public class Entry : IComparable
    {
        public Letters letters;
        public Sentence words;

        public Entry(Letters letters, Sentence words)
        {
            this.letters = letters;
            this.words = words;
        }

        public int CompareTo(object obj)
        {
            return this.letters.CompareTo(((Entry)obj).letters);
        }
    }

    public class SentenceGenerator
    {
        // combine a list of words and a list of sentences to make new sentences.
        private static List<Sentence> Combine(Sentence words, List<Sentence> sentences)
        {
            List<Sentence> result = new List<Sentence>();
            foreach (Sentence sentence in sentences)
            {
                foreach (string word in words)
                {
                    Sentence longest_sentence = new Sentence();
                    longest_sentence.InsertRange(0, sentence);
                    longest_sentence.Add(word);
                    result.Add(longest_sentence);
                }
            }
            return result;
        }

        // return a list that is entries but contains only those items which can be made from the given letters.
        private static List<Entry> Prune(Letters letters,
            List<Entry> dictionary,
            uint recursion_level,
            PruningDoneDelegate PruningDoneCallback)
        {
            List<Entry> result = new List<Entry>();
            foreach (Entry entry in dictionary)
            {
                Letters entry_letters = entry.letters;
                if (letters.Subtract(entry_letters) != null)
                {
                    result.Add(entry);
                }
            }
            PruningDoneCallback(recursion_level, result);
            return result;
        }

        public static bool Cancel = false;
        public static List<Sentence> Run(Letters letters,
            List<Entry> dictionary,
            uint recursion_level,
            LoopEndedDelegate LoopEndedCallback,
            PruningDoneDelegate PruningDoneCallback,
            SentenceFoundDelegate SentenceFoundCallback)
        {
            List<Sentence> sentences = new List<Sentence>();

            List<Entry> pruned_sentences = Prune(letters, dictionary, recursion_level, PruningDoneCallback);

            int pruned_initial_size = pruned_sentences.Count;

            while (pruned_sentences.Count > 0)
            {
                if (Cancel) break;

                Entry entry = pruned_sentences[0];
                Letters this_bag = entry.letters;
                Letters diff = letters.Subtract(this_bag);
                if (diff != null)
                {
                    if (diff.Empty())
                    {
                        foreach (string word in entry.words)
                        {
                            Sentence loner = new Sentence();
                            loner.Add(word);
                            sentences.Add(loner);
                            if (recursion_level == 0)
                                SentenceFoundCallback(loner);
                        }
                    }
                    else
                    {
                        List<Sentence> from_smaller = Run(diff, pruned_sentences, recursion_level + 1,
                            LoopEndedCallback,
                            PruningDoneCallback,
                            SentenceFoundCallback);
                        List<Sentence> combined_sentences = Combine(entry.words, from_smaller);
                        foreach (Sentence sentence in combined_sentences)
                        {
                            sentences.Add(sentence);
                            if (recursion_level == 0)
                                SentenceFoundCallback(sentence);
                        }
                    }
                }
                pruned_sentences.RemoveAt(0);
                if (recursion_level == 0)
                    LoopEndedCallback();

                Application.DoEvents();
            }
            return sentences;
        }
    }
}
