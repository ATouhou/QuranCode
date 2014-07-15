#region QuranCode Object Model
//----------------------------
// Book
// Book.Verses
// Book.Chapters.Verses
// Book.Stations.Verses
// Book.Parts.Verses
// Book.Groups.Verses
// Book.Quarters.Verses
// Book.Bowings.Verses
// Book.Pages.Verses
// Verse.Words
// Word.Letters
// Client.Bookmarks
// Client.Selection         // readonly, current selection (chapter, station, part, ... , verse, word, letter)
// Client.LetterStatistics  // readonly, statistics for current selection or highlighted text
//----------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Model;

// private methods are for Research Edition only
public static partial class Research
{
    static Research()
    {
        if (!Directory.Exists(Globals.RESEARCH_FOLDER))
        {
            Directory.CreateDirectory(Globals.RESEARCH_FOLDER);
        }
    }

    private static void NewResearchMethod(Client client, string extra)
    {
        // ResearchMethodsRunButton will display ScriptTextBox for user to write scripts
    }

    private static void _______________________________(Client client, string extra)
    {
    }
    public static void AllahWords(Client client, string extra)
    {
        if (client == null) return;
        if (client.Book == null) return;
        List<Verse> verses = client.Book.Verses;

        string filename = "AllahWords" + Globals.OUTPUT_FILE_EXT;
        string result = DoAllahWords(client, verses);
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    public static void NotAllahWords(Client client, string extra)
    {
        if (client == null) return;
        if (client.Book == null) return;
        List<Verse> verses = client.Book.Verses;

        string filename = "NotAllahWords" + Globals.OUTPUT_FILE_EXT;
        string result = DoNotAllahWords(client, verses);
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static string DoAllahWords(Client client, List<Verse> verses)
    {
        StringBuilder str = new StringBuilder();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                try
                {
                    str.AppendLine
                        (
                            "#" + "\t" +
                            "Address" + "\t" +
                            "Text" + "\t" +
                            "Verse" + "\t" +
                            "Word"
                          );

                    int count = 0;
                    foreach (Verse verse in verses)
                    {
                        foreach (Word word in verse.Words)
                        {
                            // always simplify29 for Allah word comparison
                            string simplified_text = word.Text.Simplify29();

                            if (
                                (simplified_text == "الله") ||
                                (simplified_text == "ءالله") ||
                                (simplified_text == "ابالله") ||
                                (simplified_text == "اللهم") ||
                                (simplified_text == "بالله") ||
                                (simplified_text == "تالله") ||
                                (simplified_text == "فالله") ||
                                (simplified_text == "والله") ||
                                (simplified_text == "وتالله") ||
                                (simplified_text == "لله") ||
                                (simplified_text == "فلله") ||
                                (simplified_text == "ولله")
                              )
                            {
                                count++;
                                str.AppendLine(
                                    count + "\t" +
                                    word.Address + "\t" +
                                    word.Text + "\t" +
                                    verse.Address + "\t" +
                                    word.NumberInVerse
                                  );
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
        return str.ToString();
    }
    private static string DoNotAllahWords(Client client, List<Verse> verses)
    {
        StringBuilder str = new StringBuilder();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                try
                {
                    str.AppendLine
                        (
                            "#" + "\t" +
                            "Address" + "\t" +
                            "Text" + "\t" +
                            "Verse" + "\t" +
                            "Word"
                          );

                    int count = 0;
                    foreach (Verse verse in verses)
                    {
                        foreach (Word word in verse.Words)
                        {
                            // always simplify29 for Allah word comparison
                            string simplified_text = word.Text.Simplify29();

                            if (
                                (simplified_text == "الضلله") ||
                                (simplified_text == "الكلله") ||
                                (simplified_text == "خلله") ||
                                (simplified_text == "خللها") ||
                                (simplified_text == "خللهما") ||
                                (simplified_text == "سلله") ||
                                (simplified_text == "ضلله") ||
                                (simplified_text == "ظلله") ||
                                (simplified_text == "ظللها") ||
                                (simplified_text == "كلله") ||
                                (simplified_text == "للهدي") ||
                                (simplified_text == "وظللهم") ||
                                (simplified_text == "يضلله") ||
                                (simplified_text == "اللهب") ||
                                (simplified_text == "اللهو")
                              )
                            {
                                count++;
                                str.AppendLine(
                                    count + "\t" +
                                    word.Address + "\t" +
                                    word.Text + "\t" +
                                    verse.Address + "\t" +
                                    word.NumberInVerse
                                  );
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
        return str.ToString();
    }

    private static void ________________________________(Client client, string extra)
    {
    }
    private static void JumpWordsByX(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoJumpWordsByX(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "JumpWordsByX" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void JumpWordsByPrimeNumbers(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoJumpWordsByPrimeNumbers(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "JumpWordsByPrimeNumbers" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void JumpWordsByAdditivePrimeNumbers(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoJumpWordsByAdditivePrimeNumbers(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "JumpWordsByAdditivePrimeNumbers" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void JumpWordsByPurePrimeNumbers(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoJumpWordsByPurePrimeNumbers(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "JumpWordsByPurePrimeNumbers" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void JumpWordsByFibonacciNumbers(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoJumpWordsByFibonacciNumbers(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "JumpWordsByFibonacciNumbers" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void JumpWordsByPiDigits(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoJumpWordsByPiDigits(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "JumpWordsByPiDigits" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void JumpWordsByEulerDigits(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoJumpWordsByEulerDigits(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "JumpWordsByEulerDigits" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void JumpWordsByGoldenRatioDigits(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoJumpWordsByGoldenRatioDigits(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "JumpWordsByGoldenRatioDigits" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static List<string> DoJumpWordsByX(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        int step;
        try
        {
            step = int.Parse(extra);
        }
        catch
        {
            step = 1;
        }

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if ((i % step) == 0)
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoJumpWordsByPrimeNumbers(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if (Numbers.Primes.Contains(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoJumpWordsByAdditivePrimeNumbers(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if (Numbers.AdditivePrimes.Contains(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoJumpWordsByPurePrimeNumbers(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if (Numbers.PurePrimes.Contains(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoJumpWordsByFibonacciNumbers(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if (Numbers.Fibonaccis.Contains(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoJumpWordsByPiDigits(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }


            //if (Numbers.PiDigits.Contains(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoJumpWordsByEulerDigits(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        return result;
    }
    private static List<string> DoJumpWordsByGoldenRatioDigits(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        return result;
    }

    private static void _________________________________(Client client, string extra)
    {
    }
    private static void PrimeWords(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoPrimeWords(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "PrimeWords" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void AdditivePrimeWords(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoAdditivePrimeWords(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "AdditivePrimeWords" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void PurePrimeWords(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoPurePrimeWords(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "PurePrimeWords" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void FibonacciWords(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<string> result = DoFibonacciWords(client, verses, extra);

        string filename = client.NumerologySystem.Name + "_" + "FibonacciWords" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveWords(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static List<string> DoPrimeWords(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if (Numbers.IsPrime(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoAdditivePrimeWords(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if (Numbers.IsAdditivePrime(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoPurePrimeWords(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if (Numbers.IsPurePrime(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }
    private static List<string> DoFibonacciWords(Client client, List<Verse> verses, string extra)
    {
        List<string> result = new List<string>();

        List<Word> words = new List<Word>();
        foreach (Verse verse in verses)
        {
            words.AddRange(verse.Words);
            words.Add(null); // to mark end of verse
        }
        words.RemoveAt(words.Count - 1);

        for (int i = 0; i < words.Count; i++)
        {
            if (words[i] == null)
            {
                result.Add("\r\n");
                words.RemoveAt(i);
            }

            if (Numbers.IsFibonacci(i + 1))
            {
                result.Add(words[i].Text + " ");
            }
        }

        return result;
    }

    public static void __________________________________(Client client, string extra)
    {
    }
    public static void ChapterVersesSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<Chapter> chapters = client.Book.GetChapters(verses);

        List<long> values = new List<long>();
        foreach (Chapter chapter in chapters)
        {
            values.Add(chapter.Verses.Count);
        }

        string filename = client.NumerologySystem.Name + "_" + "ChapterVerses" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void ChapterWordsSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<Chapter> chapters = client.Book.GetChapters(verses);

        List<long> values = new List<long>();
        foreach (Chapter chapter in chapters)
        {
            values.Add(chapter.WordCount);
        }

        string filename = client.NumerologySystem.Name + "_" + "ChapterWords" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void ChapterLettersSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<Chapter> chapters = client.Book.GetChapters(verses);

        List<long> values = new List<long>();
        foreach (Chapter chapter in chapters)
        {
            values.Add(chapter.LetterCount);
        }

        string filename = client.NumerologySystem.Name + "_" + "ChapterLetters" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void VerseWordsSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            values.Add(verse.Words.Count);
        }

        string filename = client.NumerologySystem.Name + "_" + "VerseWords" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void VerseLettersSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            values.Add(verse.LetterCount);
        }

        string filename = client.NumerologySystem.Name + "_" + "VerseLetters" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void WordLettersSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                values.Add(word.Letters.Count);
            }
        }

        string filename = client.NumerologySystem.Name + "_" + "WordLetters" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void ChapterValuesSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<Chapter> chapters = client.Book.GetChapters(verses);

        List<long> values = new List<long>();
        foreach (Chapter chapter in chapters)
        {
            values.Add(client.CalculateValue(chapter));
        }

        string filename = client.NumerologySystem.Name + "_" + "ChapterValues" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void VerseValuesSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            values.Add(client.CalculateValue(verse));
        }

        string filename = client.NumerologySystem.Name + "_" + "VerseValues" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void WordValuesSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                values.Add(client.CalculateValue(word));
            }
        }

        string filename = client.NumerologySystem.Name + "_" + "WordValues" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    public static void LetterValuesSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                foreach (Letter letter in word.Letters)
                {
                    values.Add(client.CalculateValue(letter));
                }
            }
        }

        string filename = client.NumerologySystem.Name + "_" + "LetterValues" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    private static void ChapterValueDiffsSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;
        List<Chapter> chapters = client.Book.GetChapters(verses);

        List<long> values = new List<long>();
        foreach (Chapter chapter in chapters)
        {
            values.Add(client.CalculateValue(chapter));
        }

        List<long> value_diffs = new List<long>();
        for (int i = 0; i < values.Count - 1; i++)
        {
            long value_diff = values[i + 1] - values[i];
            value_diffs.Add(value_diff);
        }

        string filename = client.NumerologySystem.Name + "_" + "ChapterValueDiffs" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    private static void VerseValueDiffsSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            values.Add(client.CalculateValue(verse));
        }

        List<long> value_diffs = new List<long>();
        for (int i = 0; i < values.Count - 1; i++)
        {
            long value_diff = values[i + 1] - values[i];
            value_diffs.Add(value_diff);
        }

        string filename = client.NumerologySystem.Name + "_" + "VerseValueDiffs" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    private static void WordValueDiffsSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                values.Add(client.CalculateValue(word));
            }
        }

        List<long> value_diffs = new List<long>();
        for (int i = 0; i < values.Count - 1; i++)
        {
            long value_diff = values[i + 1] - values[i];
            value_diffs.Add(value_diff);
        }

        string filename = client.NumerologySystem.Name + "_" + "WordValueDiffs" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    private static void LetterValueDiffsSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                foreach (Letter letter in word.Letters)
                {
                    values.Add(client.CalculateValue(letter));
                }
            }
        }

        List<long> value_diffs = new List<long>();
        for (int i = 0; i < values.Count - 1; i++)
        {
            long value_diff = values[i + 1] - values[i];
            value_diffs.Add(value_diff);
        }

        string filename = client.NumerologySystem.Name + "_" + "LetterValueDiffs" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    private static void SameVerseDistancesSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            values.Add(verse.DistanceToPrevious.dV);
        }

        string filename = client.NumerologySystem.Name + "_" + "SameVerseDistances" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    private static void SameWordDistancesSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                values.Add(word.DistanceToPrevious.dW);
            }
        }

        string filename = client.NumerologySystem.Name + "_" + "SameWordDistances" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    private static void SameLetterDistancesSound(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        List<long> values = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                foreach (Letter letter in word.Letters)
                {
                    values.Add(letter.DistanceToPrevious.dL);
                }
            }
        }

        string filename = client.NumerologySystem.Name + "_" + "SameLetterDistances" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        DoSaveAndPlayWAVFile(path, values, extra);
    }
    private static void DoSaveAndPlayWAVFile(string path, List<long> values, string extra)
    {
        PublicStorage.SaveValues(path, values);
        PublicStorage.DisplayFile(path); // *.csv

        int frequency = 0;
        if (extra.Length == 0)
        {
            // update ref path.csv to .wav
            WAVFile.GenerateWAVFile(ref path, values, Globals.DEFAULT_FREQUENCY);
        }
        else if (int.TryParse(extra, out frequency))
        {
            // update ref path.csv to .wav
            WAVFile.GenerateWAVFile(ref path, values, frequency);
        }
        else
        {
            throw new Exception("Invalid frequency value = " + extra);
        }
        // play .wav file
        WAVFile.PlayWAVFile(path);
    }

    public static void ______________________________________(Client client, string extra)
    {
    }
    public static void ChapterInformation(Client client, string extra)
    {
        if (client == null) return;
        //if (client.Selection == null) return;
        //List<Verse> verses = client.Selection.Verses;
        if (client.Book == null) return;
        List<Verse> verses = client.Book.Verses;
        List<Chapter> chapters = client.Book.GetChapters(verses);
        string result = DoChapterInformation(client, chapters);

        string filename = client.NumerologySystem.Name + "_" + "ChapterStatistics" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    public static void VerseInformation(Client client, string extra)
    {
        if (client == null) return;
        //if (client.Selection == null) return;
        //List<Verse> verses = client.Selection.Verses;
        if (client.Book == null) return;
        List<Verse> verses = client.Book.Verses;
        string result = DoVerseInformation(client, verses);

        string filename = client.NumerologySystem.Name + "_" + "VerseStatistics" + Globals.OUTPUT_FILE_EXT;
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    public static void WordInformation(Client client, string extra)
    {
        if (client == null) return;
        //if (client.Selection == null) return;
        //List<Verse> verses = client.Selection.Verses;
        if (client.Book == null) return;
        List<Verse> verses = client.Book.Verses;

        string filename = "WordInformation" + Globals.OUTPUT_FILE_EXT;
        string result = DoWordInformation(client, verses);
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    public static void WordPartInformation(Client client, string extra)
    {
        if (client == null) return;
        //if (client.Selection == null) return;
        //List<Verse> verses = client.Selection.Verses;
        if (client.Book == null) return;
        List<Verse> verses = client.Book.Verses;

        string filename = "WordPartInformation" + Globals.OUTPUT_FILE_EXT;
        string result = DoWordPartInformation(client, verses);
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static string DoChapterInformation(Client client, List<Chapter> chapters)
    {
        if (client == null) return null;

        StringBuilder str = new StringBuilder();

        str.Append("Name" + "\t" + "Transliteration" + "\t" + "English" + "\t" + "Place" + "\t" + "Order" + "\t" + "Page" + "\t" + "Chapter" + "\t" + "Verses" + "\t" + "Words" + "\t" + "Letters" + "\t" + "Unique" + "\t" + "Value" + "\t" + "Factors" + "\t" + "P" + "\t" + "AP" + "\t" + "PP" + "\t" + "C" + "\t" + "AC" + "\t" + "PC" + "\t");

        NumerologySystem numerology_system = client.NumerologySystem;
        if (numerology_system != null)
        {
            if (numerology_system.LetterValues.Keys.Count > 0)
            {
                foreach (char key in numerology_system.LetterValues.Keys)
                {
                    str.Append(key.ToString() + "\t");
                }
                if (str.Length > 1)
                {
                    str.Remove(str.Length - 1, 1); // \t
                }
                str.Append("\r\n");
            }

            foreach (Chapter chapter in chapters)
            {
                str.Append(chapter.Name + "\t");
                str.Append(chapter.TransliteratedName + "\t");
                str.Append(chapter.EnglishName + "\t");
                str.Append(chapter.RevelationPlace.ToString() + "\t");
                str.Append(chapter.RevelationOrder.ToString() + "\t");
                str.Append(chapter.Verses[0].Page.Number.ToString() + "\t");
                str.Append(chapter.Number.ToString() + "\t");
                str.Append(chapter.Verses.Count.ToString() + "\t");
                str.Append(chapter.WordCount.ToString() + "\t");
                str.Append(chapter.LetterCount.ToString() + "\t");
                str.Append(chapter.UniqueLetters.Count.ToString() + "\t");

                long value = client.CalculateValue(chapter);
                str.Append(value.ToString() + "\t");
                str.Append(Numbers.FactorizeToString(value) + "\t");

                int p = Numbers.IndexOfPrime(value);
                int ap = Numbers.IndexOfAdditivePrime(value);
                int pp = Numbers.IndexOfPurePrime(value);
                int c = Numbers.IndexOfComposite(value);
                int ac = Numbers.IndexOfAdditiveComposite(value);
                int pc = Numbers.IndexOfPureComposite(value);
                str.Append((p == -1 ? "-" : p.ToString()) + "\t"
                               + (ap == -1 ? "-" : ap.ToString()) + "\t"
                               + (pp == -1 ? "-" : pp.ToString()) + "\t"
                               + (c == -1 ? "-" : c.ToString()) + "\t"
                               + (ac == -1 ? "-" : ac.ToString()) + "\t"
                               + (pc == -1 ? "-" : pc.ToString())
                             );
                str.Append("\t");

                if (numerology_system.LetterValues.Keys.Count > 0)
                {
                    foreach (char key in numerology_system.LetterValues.Keys)
                    {
                        str.Append(chapter.GetLetterFrequency(key) + "\t");
                    }
                    if (str.Length > 1)
                    {
                        str.Remove(str.Length - 1, 1); // \t
                    }
                    str.Append("\r\n");
                }
            }
        }
        return str.ToString();
    }
    private static string DoVerseInformation(Client client, List<Verse> verses)
    {
        if (client == null) return null;

        StringBuilder str = new StringBuilder();

        str.Append("#" + "\t" + "Page" + "\t" + "Chapter" + "\t" + "Verse" + "\t" + "Words" + "\t" + "Letters" + "\t" + "Unique" + "\t" + "Value" + "\t" + "Factors" + "\t" + "P" + "\t" + "AP" + "\t" + "PP" + "\t" + "C" + "\t" + "AC" + "\t" + "PC" + "\t");

        NumerologySystem numerology_system = client.NumerologySystem;
        if (numerology_system != null)
        {
            foreach (char key in numerology_system.LetterValues.Keys)
            {
                str.Append(key.ToString() + "\t");
            }
            str.Append("Text");
            str.Append("\r\n");

            int count = 0;
            foreach (Verse verse in verses)
            {
                count++;
                str.Append(verse.Number.ToString() + "\t");
                str.Append(verse.Page.Number.ToString() + "\t");
                str.Append(verse.Chapter.Number.ToString() + "\t");
                str.Append(verse.NumberInChapter.ToString() + "\t");
                str.Append(verse.Words.Count.ToString() + "\t");
                str.Append(verse.LetterCount.ToString() + "\t");
                str.Append(verse.UniqueLetters.Count.ToString() + "\t");

                long value = client.CalculateValue(verse);
                str.Append(value.ToString() + "\t");
                str.Append(Numbers.FactorizeToString(value) + "\t");

                int p = Numbers.IndexOfPrime(value);
                int ap = Numbers.IndexOfAdditivePrime(value);
                int pp = Numbers.IndexOfPurePrime(value);
                int c = Numbers.IndexOfComposite(value);
                int ac = Numbers.IndexOfAdditiveComposite(value);
                int pc = Numbers.IndexOfPureComposite(value);
                str.Append((p == -1 ? "-" : p.ToString()) + "\t"
                               + (ap == -1 ? "-" : ap.ToString()) + "\t"
                               + (pp == -1 ? "-" : pp.ToString()) + "\t"
                               + (c == -1 ? "-" : c.ToString()) + "\t"
                               + (ac == -1 ? "-" : ac.ToString()) + "\t"
                               + (pc == -1 ? "-" : pc.ToString())
                             );
                str.Append("\t");

                foreach (char character in numerology_system.LetterValues.Keys)
                {
                    if (Constants.INDIAN_DIGITS.Contains(character)) continue;
                    if (Constants.STOPMARKS.Contains(character)) continue;
                    if (Constants.QURANMARKS.Contains(character)) continue;
                    if (character == '{') continue;
                    if (character == '}') continue;
                    str.Append(verse.GetLetterFrequency(character).ToString() + "\t");
                }

                str.Append(verse.Text);

                str.Append("\r\n");
            }
        }
        return str.ToString();
    }
    private static string DoWordInformation(Client client, List<Verse> verses)
    {
        if (client == null) return null;

        StringBuilder str = new StringBuilder();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                str.AppendLine
                (
                    "Address" + "\t" +
                    "Chapter" + "\t" +
                    "Verse" + "\t" +
                    "Word" + "\t" +
                    "Text" + "\t" +
                    "Transliteration" + "\t" +
                    "Roots" + "\t" +
                    "Meaning" + "\t" +
                    "Occurrence" + "\t" +
                    "Frequency"
                );

                Dictionary<string, int> word_occurrences = new Dictionary<string, int>();
                Dictionary<string, int> word_frequencies = new Dictionary<string, int>();
                DoCalculateWordOccurrencesAndFrequencies(verses, ref word_occurrences, ref word_frequencies);

                foreach (Verse verse in verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        List<string> roots = word.Roots;
                        StringBuilder roots_str = new StringBuilder();
                        if (roots.Count > 0)
                        {
                            foreach (string root in roots)
                            {
                                roots_str.Append(root + "|");
                            }
                            roots_str.Remove(roots_str.Length - 1, 1);
                        }

                        str.AppendLine
                        (
                            word.Address + "\t" +
                            verse.Chapter.Number.ToString() + "\t" +
                            verse.NumberInChapter.ToString() + "\t" +
                            word.NumberInVerse.ToString() + "\t" +
                            word.Text + "\t" +
                            word.Transliteration + "\t" +
                            roots_str.ToString() + "\t" +
                            word.Meaning + "\t" +
                            word_occurrences[word.Address].ToString() + "\t" +
                            word_frequencies[word.Address].ToString()
                        );
                    }
                }
            }
        }
        return str.ToString();
    }
    private static string DoWordPartInformation(Client client, List<Verse> verses)
    {
        if (client == null) return null;

        StringBuilder str = new StringBuilder();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                str.AppendLine
                (
                    "Address" + "\t" +
                    "Chapter" + "\t" +
                    "Verse" + "\t" +
                    "Word" + "\t" +
                    "Part" + "\t" +
                    "Text" + "\t" +
                    "Buckwalter" + "\t" +
                    "Tag" + "\t" +
                    "Type" + "\t" +
                    "Position" + "\t" +
                    "Attribute" + "\t" +
                    "Qualifier" + "\t" +
                    "PersonDegree" + "\t" +
                    "PersonGender" + "\t" +
                    "PersonNumber" + "\t" +
                    "Mood" + "\t" +
                    "Lemma" + "\t" +
                    "Root" + "\t" +
                    "SpecialGroup" + "\t" +
                    "WordAddress"
                );

                foreach (Verse verse in verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        if (word.Parts != null)
                        {
                            foreach (WordPart part in word.Parts)
                            {
                                str.AppendLine
                                (
                                    part.Address + "\t" +
                                    part.Word.Verse.Chapter.Number.ToString() + "\t" +
                                    part.Word.Verse.NumberInChapter.ToString() + "\t" +
                                    part.Word.NumberInVerse.ToString() + "\t" +
                                    part.NumberInWord.ToString() + "\t" +
                                    part.Text + "\t" +
                                    part.Buckwalter + "\t" +
                                    part.Tag + "\t" +
                                    part.Grammar.ToTable() + "\t" +
                                    part.Word.Address
                                );
                            }
                        }
                    }
                }
            }
        }
        return str.ToString();
    }
    private static void DoCalculateWordOccurrencesAndFrequencies(List<Verse> verses, ref Dictionary<string, int> word_occurrences, ref Dictionary<string, int> word_frequencies)
    {
        Dictionary<string, int> text_frequencies = new Dictionary<string, int>();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                foreach (Verse verse in verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        if (text_frequencies.ContainsKey(word.Text))
                        {
                            text_frequencies[word.Text]++;
                            word_occurrences[word.Address] = text_frequencies[word.Text];
                        }
                        else
                        {
                            text_frequencies.Add(word.Text, 1);
                            word_occurrences.Add(word.Address, 1);
                        }
                    }
                }

                // sum up all word_text frequencies for all word_addresses with the same word_text
                foreach (Verse verse in verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        foreach (string key in text_frequencies.Keys)
                        {
                            if (key == word.Text)
                            {
                                if (word_frequencies.ContainsKey(word.Address))
                                {
                                    word_frequencies[word.Address] += text_frequencies[word.Text];
                                }
                                else
                                {
                                    word_frequencies.Add(word.Address, text_frequencies[word.Text]);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static void _______________________________________(Client client, string extra)
    {
    }
    public static void WordRelatedWords(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        string filename = "WordRelatedWords" + Globals.OUTPUT_FILE_EXT;
        string result = DoWordRelatedWords(client, verses);
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    public static void WordRelatedWordMeanings(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        string filename = "WordRelatedWordMeanings" + Globals.OUTPUT_FILE_EXT;
        string result = DoWordRelatedWordMeanings(client, verses);
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    public static void WordRelatedVerses(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        string filename = "WordRelatedVerses" + Globals.OUTPUT_FILE_EXT;
        string result = DoWordRelatedVerses(client, verses);
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    public static void WordRelatedVerseMeanings(Client client, string extra)
    {
        if (client == null) return;
        if (client.Selection == null) return;
        List<Verse> verses = client.Selection.Verses;

        string filename = "WordRelatedVerseMeanings" + Globals.OUTPUT_FILE_EXT;
        string result = DoWordRelatedVerseMeanings(client, verses);
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static string DoWordRelatedWords(Client client, List<Verse> verses)
    {
        if (client == null) return null;
        if (client.Book == null) return null;

        StringBuilder str = new StringBuilder();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                str.AppendLine
                (
                    "Address" + "\t" +
                    "Text" + "\t" +
                    "Transliteration" + "\t" +
                    "Meaning" + "\t" +
                    "RelatedWords"
                );

                foreach (Verse verse in verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        List<Word> related_words = client.Book.GetRelatedWords(word, false);
                        related_words = related_words.RemoveDuplicates();

                        StringBuilder related_words_str = new StringBuilder();
                        if (related_words.Count > 0)
                        {
                            foreach (Word related_word in related_words)
                            {
                                related_words_str.Append(related_word.Text + "\t");
                            }
                            related_words_str.Remove(related_words_str.Length - 1, 1);
                        }

                        str.AppendLine
                        (
                            word.Address + "\t" +
                            word.Text + "\t" +
                            word.Transliteration + "\t" +
                            word.Meaning + "\t" +
                            related_words_str
                        );
                    }
                }
            }
        }
        return str.ToString();
    }
    private static string DoWordRelatedWordMeanings(Client client, List<Verse> verses)
    {
        if (client == null) return null;
        if (client.Book == null) return null;

        StringBuilder str = new StringBuilder();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                str.AppendLine
                (
                    "Address" + "\t" +
                    "Text" + "\t" +
                    "Transliteration" + "\t" +
                    "RelatedWordMeanings"
                );

                foreach (Verse verse in verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        List<Word> related_words = client.Book.GetRelatedWords(word, false);
                        List<string> related_word_meanings = new List<string>();
                        foreach (Word related_word in related_words)
                        {
                            related_word_meanings.Add(related_word.Meaning);
                        }
                        related_word_meanings = related_word_meanings.RemoveDuplicates();

                        StringBuilder related_word_meanings_str = new StringBuilder();
                        if (related_word_meanings.Count > 0)
                        {
                            foreach (string related_word_meaning in related_word_meanings)
                            {
                                related_word_meanings_str.Append(related_word_meaning + "\t");
                            }
                            related_word_meanings_str.Remove(related_word_meanings_str.Length - 1, 1);
                        }

                        str.AppendLine
                        (
                            word.Address + "\t" +
                            word.Text + "\t" +
                            word.Transliteration + "\t" +
                            related_word_meanings_str
                        );
                    }
                }
            }
        }
        return str.ToString();
    }
    private static string DoWordRelatedVerses(Client client, List<Verse> verses)
    {
        if (client == null) return null;
        if (client.Book == null) return null;

        StringBuilder str = new StringBuilder();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                str.AppendLine
                (
                    "Address" + "\t" +
                    "Text" + "\t" +
                    "Transliteration" + "\t" +
                    "Meaning" + "\t" +
                    "RelatedWords"
                );

                foreach (Verse verse in verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        List<Verse> related_verses = client.Book.GetRelatedVerses(word, false);
                        related_verses = related_verses.RemoveDuplicates();

                        StringBuilder related_verses_str = new StringBuilder();
                        if (related_verses.Count > 0)
                        {
                            foreach (Verse related_verse in related_verses)
                            {
                                related_verses_str.Append(related_verse.Text + "\t");
                            }
                            related_verses_str.Remove(related_verses_str.Length - 1, 1);
                        }

                        str.AppendLine
                        (
                            word.Address + "\t" +
                            word.Text + "\t" +
                            word.Transliteration + "\t" +
                            word.Meaning + "\t" +
                            related_verses_str
                        );
                    }
                }
            }
        }
        return str.ToString();
    }
    private static string DoWordRelatedVerseMeanings(Client client, List<Verse> verses)
    {
        if (client == null) return null;
        if (client.Book == null) return null;

        StringBuilder str = new StringBuilder();
        if (verses != null)
        {
            if (verses.Count > 0)
            {
                str.AppendLine
                (
                    "Address" + "\t" +
                    "Text" + "\t" +
                    "Transliteration" + "\t" +
                    "Meaning" + "\t" +
                    "RelatedVerseMeanings"
                );

                foreach (Verse verse in verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        List<Verse> related_verses = client.Book.GetRelatedVerses(word, false);
                        related_verses = related_verses.RemoveDuplicates();

                        StringBuilder related_verse_meanings_str = new StringBuilder();
                        if (related_verses.Count > 0)
                        {
                            foreach (Verse related_verse in related_verses)
                            {
                                if (related_verse.Words.Count > 0)
                                {
                                    foreach (Word related_verse_word in related_verse.Words)
                                    {
                                        related_verse_meanings_str.Append(related_verse_word.Meaning + "    ");
                                    }
                                    related_verse_meanings_str.Remove(related_verse_meanings_str.Length - 4, 4); // "    "
                                }
                                related_verse_meanings_str.Append("\t");
                            }
                            related_verse_meanings_str.Remove(related_verse_meanings_str.Length - 1, 1);
                        }

                        str.AppendLine
                        (
                            word.Address + "\t" +
                            word.Text + "\t" +
                            word.Transliteration + "\t" +
                            word.Meaning + "\t" +
                            related_verse_meanings_str
                        );
                    }
                }
            }
        }
        return str.ToString();
    }

    private static void ________________________________________(Client client, string extra)
    {
    }
    private static void FindVersesWithXValueDigitSum(Client client, string extra)
    {
        if (client == null) return;
        if (client.NumerologySystem == null) return;

        string result = DoFindVersesWithXValueDigitSum(client, extra, NumberType.Any);
        string filename = client.NumerologySystem.Name + "_" + "FindVersesWithXValueDigitSum" + "_" + extra + "_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void FindVersesWithPValueAndXDigitSum(Client client, string extra)
    {
        if (client == null) return;
        if (client.NumerologySystem == null) return;

        string result = DoFindVersesWithXValueDigitSum(client, extra, NumberType.Prime);
        string filename = client.NumerologySystem.Name + "_" + "FindVersesWithPValueAndXDigitSum" + "_" + extra + "_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void FindVersesWithAPValueAndXDigitSum(Client client, string extra)
    {
        if (client == null) return;
        if (client.NumerologySystem == null) return;

        string result = DoFindVersesWithXValueDigitSum(client, extra, NumberType.AdditivePrime);
        string filename = client.NumerologySystem.Name + "_" + "FindVersesWithAPValueAndXDigitSum" + "_" + extra + "_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void FindVersesWithPPValueAndXDigitSum(Client client, string extra)
    {
        if (client == null) return;
        if (client.NumerologySystem == null) return;

        string result = DoFindVersesWithXValueDigitSum(client, extra, NumberType.PurePrime);
        string filename = client.NumerologySystem.Name + "_" + "FindVersesWithPPValueAndXDigitSum" + "_" + extra + "_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void FindVersesWithCValueAndXDigitSum(Client client, string extra)
    {
        if (client == null) return;
        if (client.NumerologySystem == null) return;

        string result = DoFindVersesWithXValueDigitSum(client, extra, NumberType.Composite);
        string filename = client.NumerologySystem.Name + "_" + "FindVersesWithCValueAndXDigitSum" + "_" + extra + "_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void FindVersesWithACValueAndXDigitSum(Client client, string extra)
    {
        if (client == null) return;
        if (client.NumerologySystem == null) return;

        string result = DoFindVersesWithXValueDigitSum(client, extra, NumberType.AdditiveComposite);
        string filename = client.NumerologySystem.Name + "_" + "FindVersesWithACValueAndXDigitSum" + "_" + extra + "_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static void FindVersesWithPCValueAndXDigitSum(Client client, string extra)
    {
        if (client == null) return;
        if (client.NumerologySystem == null) return;

        string result = DoFindVersesWithXValueDigitSum(client, extra, NumberType.PureComposite);
        string filename = client.NumerologySystem.Name + "_" + "FindVersesWithPCValueAndXDigitSum" + "_" + extra + "_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        string path = Globals.RESEARCH_FOLDER + "/" + filename;
        PublicStorage.SaveText(path, result);
        PublicStorage.DisplayFile(path);
    }
    private static string DoFindVersesWithXValueDigitSum(Client client, string extra, NumberType number_type)
    {
        if (client == null) return null;
        if (client.Book == null) return null;
        if (client.Book.Verses == null) return null;

        StringBuilder str = new StringBuilder();
        str.Append("#" + "\t" + "Verse" + "\t" + "Address" + "\t" + "Words" + "\t" + "Letters" + "\t" + "Unique" + "\t" + "Value" + "\t" + "DigitSum" + "\t" + "Text");
        str.Append("\r\n");

        int count = 0;
        foreach (Verse verse in client.Book.Verses)
        {
            long value = client.CalculateValue(verse);

            bool extra_condition = false;
            if (extra == "") // target == any digit sum
            {
                extra_condition = true;
            }
            else if (extra.ToUpper() == "P") // target == prime digit sum
            {
                extra_condition = Numbers.IsPrime(Numbers.DigitSum(value));
            }
            else if (extra.ToUpper() == "AP") // target == additive prime digit sum
            {
                extra_condition = Numbers.IsAdditivePrime(Numbers.DigitSum(value));
            }
            else if (extra.ToUpper() == "PP") // target == pure prime digit sum
            {
                extra_condition = Numbers.IsPurePrime(Numbers.DigitSum(value));
            }
            else if (extra.ToUpper() == "C") // target == composite digit sum
            {
                extra_condition = Numbers.IsComposite(Numbers.DigitSum(value));
            }
            else if (extra.ToUpper() == "AC") // target == additive composite digit sum
            {
                extra_condition = Numbers.IsAdditiveComposite(Numbers.DigitSum(value));
            }
            else if (extra.ToUpper() == "PC") // target == pure composite digit sum
            {
                extra_condition = Numbers.IsPureComposite(Numbers.DigitSum(value));
            }
            else
            {
                int target;
                if (int.TryParse(extra, out target))
                {
                    if (target == 0) // target == any digit sum
                    {
                        extra_condition = true;
                    }
                    else
                    {
                        extra_condition = (Numbers.DigitSum(value) == target);
                    }
                }
                else
                {
                    return null;  // invalid extra data
                }
            }

            if (
                 (
                    (number_type == NumberType.Any)
                    ||
                    (Numbers.IsNumberType(value, number_type))
                 )
                 &&
                 extra_condition
               )
            {
                count++;
                str.Append(count.ToString() + "\t");
                str.Append(verse.Number.ToString() + "\t");
                str.Append(verse.Address.ToString() + "\t");
                str.Append(verse.Words.Count.ToString() + "\t");
                str.Append(verse.LetterCount.ToString() + "\t");
                str.Append(verse.UniqueLetters.Count.ToString() + "\t");
                str.Append(value.ToString() + "\t");
                str.Append(Numbers.DigitSum(value).ToString() + "\t");
                str.Append(verse.Text + "\t");
                str.Append("\r\n");
            }
        }
        return str.ToString();
    }

    private static void __________________________________________(Client client, string extra)
    {
    }
    public class ZeroDifferenceNumerologySystem
    {
        public NumberType NumberType;
        public NumerologySystem NumerologySystem;

        // these two need to be equal
        public long BismAllahValue = -1L;
        public int AlFatihaValueIndex = -1; // PrimeIndex | AdditivePrimeIndex | PurePrimeIndex

        // these two need to be equal
        public long AlFatihaValue = -1L;
        public int BookValueIndex = -1; // PrimeIndex | AdditivePrimeIndex | PurePrimeIndex
    }
    private static void FindBismAllahEqualsAlFatihaIndex(Client client, string extra)
    {
        if (client == null) return;
        if (client.Book == null) return;
        if (client.NumerologySystem == null) return;

        NumerologySystem backup_numerology_system = new NumerologySystem(client.NumerologySystem);

        long target_difference;
        try
        {
            target_difference = long.Parse(extra);
        }
        catch
        {
            target_difference = 0L;
        }

        // zero difference between Value(BismAllah) and ValueIndex(Al-Fatiha)
        List<ZeroDifferenceNumerologySystem> good_numerology_systems = new List<ZeroDifferenceNumerologySystem>();

        NumberType[] number_types = (NumberType[])Enum.GetValues(typeof(NumberType));
        foreach (NumberType number_type in number_types)
        {
            if (
                (number_type == NumberType.Prime) ||
                (number_type == NumberType.AdditivePrime) ||
                (number_type == NumberType.PurePrime) ||
                (number_type == NumberType.Composite) ||
                (number_type == NumberType.AdditiveComposite) ||
                (number_type == NumberType.PureComposite)
               )
            {
                NumerologySystemScope[] numerology_system_scopes = (NumerologySystemScope[])Enum.GetValues(typeof(NumerologySystemScope));
                foreach (NumerologySystemScope numerology_system_scope in numerology_system_scopes)
                {
                    switch (numerology_system_scope)
                    {
                        case NumerologySystemScope.Book:
                            {
                                client.BuildNumerologySystem(client.Book.Text);
                            }
                            break;
                        case NumerologySystemScope.Selection:
                            {
                                client.BuildNumerologySystem(client.Book.Chapters[0].Text);
                            }
                            break;
                        case NumerologySystemScope.HighlightedText:
                            {
                                client.BuildNumerologySystem(client.Book.Verses[0].Text);
                            }
                            break;
                        default:
                            break;
                    }

                    // Quran 74:30 "Over It Nineteen."
                    int PERMUTATIONS = 524288; // 2^19
                    for (int i = 0; i < PERMUTATIONS; i++)
                    {
                        client.NumerologySystem.AddToLetterLNumber = ((i & 262144) != 0);
                        client.NumerologySystem.AddToLetterWNumber = ((i & 131072) != 0);
                        client.NumerologySystem.AddToLetterVNumber = ((i & 65536) != 0);
                        client.NumerologySystem.AddToLetterCNumber = ((i & 32768) != 0);
                        client.NumerologySystem.AddToLetterLDistance = ((i & 16384) != 0);
                        client.NumerologySystem.AddToLetterWDistance = ((i & 8192) != 0);
                        client.NumerologySystem.AddToLetterVDistance = ((i & 4096) != 0);
                        client.NumerologySystem.AddToLetterCDistance = ((i & 2048) != 0);
                        client.NumerologySystem.AddToWordWNumber = ((i & 1024) != 0);
                        client.NumerologySystem.AddToWordVNumber = ((i & 512) != 0);
                        client.NumerologySystem.AddToWordCNumber = ((i & 256) != 0);
                        client.NumerologySystem.AddToWordWDistance = ((i & 128) != 0);
                        client.NumerologySystem.AddToWordVDistance = ((i & 64) != 0);
                        client.NumerologySystem.AddToWordCDistance = ((i & 32) != 0);
                        client.NumerologySystem.AddToVerseVNumber = ((i & 16) != 0);
                        client.NumerologySystem.AddToVerseCNumber = ((i & 8) != 0);
                        client.NumerologySystem.AddToVerseVDistance = ((i & 4) != 0);
                        client.NumerologySystem.AddToVerseCDistance = ((i & 2) != 0);
                        client.NumerologySystem.AddToChapterCNumber = ((i & 1) != 0);

                        long alfatiha_value = client.CalculateValue(client.Book.Chapters[0]);
                        int alfatiha_value_index = -1;
                        switch (number_type)
                        {
                            case NumberType.Prime:
                                {
                                    alfatiha_value_index = Numbers.IndexOfPrime(alfatiha_value);
                                }
                                break;
                            case NumberType.AdditivePrime:
                                {
                                    alfatiha_value_index = Numbers.IndexOfAdditivePrime(alfatiha_value);
                                }
                                break;
                            case NumberType.PurePrime:
                                {
                                    alfatiha_value_index = Numbers.IndexOfPurePrime(alfatiha_value);
                                }
                                break;
                            case NumberType.Composite:
                                {
                                    alfatiha_value_index = Numbers.IndexOfComposite(alfatiha_value);
                                }
                                break;
                            case NumberType.AdditiveComposite:
                                {
                                    alfatiha_value_index = Numbers.IndexOfAdditiveComposite(alfatiha_value);
                                }
                                break;
                            case NumberType.PureComposite:
                                {
                                    alfatiha_value_index = Numbers.IndexOfPureComposite(alfatiha_value);
                                }
                                break;
                            default:
                                break;
                        }

                        if (alfatiha_value_index != -1)
                        {
                            long bismAllah_value = client.CalculateValue(client.Book.Chapters[0].Verses[0]);

                            long difference = bismAllah_value - (long)alfatiha_value_index;
                            if (Math.Abs(difference) <= target_difference)
                            {
                                ZeroDifferenceNumerologySystem good_numerology_system = new ZeroDifferenceNumerologySystem();
                                good_numerology_system.NumerologySystem = new NumerologySystem(client.NumerologySystem);
                                good_numerology_system.NumberType = number_type;
                                good_numerology_system.BismAllahValue = bismAllah_value;
                                good_numerology_system.AlFatihaValue = alfatiha_value;
                                good_numerology_system.AlFatihaValueIndex = alfatiha_value_index;
                                good_numerology_systems.Add(good_numerology_system);
                            }
                        }

                    } // next PERMUTATION

                } // next NumerologySystemScope

                string filename = "FindBismAllahEqualsAlFatiha" + number_type.ToString() + "IndexSystem" + Globals.OUTPUT_FILE_EXT;
                string path = Globals.RESEARCH_FOLDER + "/" + filename;

                StringBuilder str = new StringBuilder();
                str.AppendLine("TextMode" +
                        "\t" + "LetterOrder" +
                        "\t" + "LetterValues" +
                        "\t" + "Scope" +
                        "\t" + "AddToLetterLNumber" +
                        "\t" + "AddToLetterWNumber" +
                        "\t" + "AddToLetterVNumber" +
                        "\t" + "AddToLetterCNumber" +
                        "\t" + "AddToLetterLDistance" +
                        "\t" + "AddToLetterWDistance" +
                        "\t" + "AddToLetterVDistance" +
                        "\t" + "AddToLetterCDistance" +
                        "\t" + "AddToWordWNumber" +
                        "\t" + "AddToWordVNumber" +
                        "\t" + "AddToWordCNumber" +
                        "\t" + "AddToWordWDistance" +
                        "\t" + "AddToWordVDistance" +
                        "\t" + "AddToWordCDistance" +
                        "\t" + "AddToVerseVNumber" +
                        "\t" + "AddToVerseCNumber" +
                        "\t" + "AddToVerseVDistance" +
                        "\t" + "AddToVerseCDistance" +
                        "\t" + "AddToChapterCNumber" +
                        "\t" + "BismAllahValue" +
                        "\t" + "AlFatihaIndex"
                    );
                foreach (ZeroDifferenceNumerologySystem good_numerology_system in good_numerology_systems)
                {
                    str.Append(good_numerology_system.NumerologySystem.ToTabbedString());
                    str.Append("\t" + good_numerology_system.BismAllahValue.ToString());
                    str.Append("\t" + good_numerology_system.AlFatihaValueIndex.ToString());
                    str.AppendLine();
                }
                PublicStorage.SaveText(path, str.ToString());
                PublicStorage.DisplayFile(path);

                // clear for next NumberType
                good_numerology_systems.Clear();

            } // if NumberType

        } // next NumberType

        client.NumerologySystem = backup_numerology_system;
    }
    private static void FindAlFatihaEqualsBookIndex(Client client, string extra)
    {
        if (client == null) return;
        if (client.Book == null) return;
        if (client.NumerologySystem == null) return;

        NumerologySystem backup_numerology_system = new NumerologySystem(client.NumerologySystem);

        long target_difference;
        try
        {
            target_difference = long.Parse(extra);
        }
        catch
        {
            target_difference = 0L;
        }

        // zero difference between Value(BismAllah) and ValueIndex(Al-Fatiha)
        List<ZeroDifferenceNumerologySystem> good_numerology_systems = new List<ZeroDifferenceNumerologySystem>();

        // zero difference between Value(Al-Fatiha) and ValueIndex(Book)
        List<ZeroDifferenceNumerologySystem> best_numerology_systems = new List<ZeroDifferenceNumerologySystem>();

        NumberType[] number_types = (NumberType[])Enum.GetValues(typeof(NumberType));
        foreach (NumberType number_type in number_types)
        {
            if (
                (number_type == NumberType.Prime) ||
                (number_type == NumberType.AdditivePrime) ||
                (number_type == NumberType.PurePrime) ||
                (number_type == NumberType.Composite) ||
                (number_type == NumberType.AdditiveComposite) ||
                (number_type == NumberType.PureComposite)
               )
            {
                NumerologySystemScope[] numerology_system_scopes = (NumerologySystemScope[])Enum.GetValues(typeof(NumerologySystemScope));
                foreach (NumerologySystemScope numerology_system_scope in numerology_system_scopes)
                {
                    switch (numerology_system_scope)
                    {
                        case NumerologySystemScope.Book:
                            {
                                client.BuildNumerologySystem(client.Book.Text);
                            }
                            break;
                        case NumerologySystemScope.Selection:
                            {
                                client.BuildNumerologySystem(client.Book.Chapters[0].Text);
                            }
                            break;
                        case NumerologySystemScope.HighlightedText:
                            {
                                client.BuildNumerologySystem(client.Book.Verses[0].Text);
                            }
                            break;
                        default:
                            break;
                    }

                    // Quran 74:30 "Over It Nineteen."
                    int PERMUTATIONS = 524288; // 2^19
                    for (int i = 0; i < PERMUTATIONS; i++)
                    {
                        client.NumerologySystem.AddToLetterLNumber = ((i & 262144) != 0);
                        client.NumerologySystem.AddToLetterWNumber = ((i & 131072) != 0);
                        client.NumerologySystem.AddToLetterVNumber = ((i & 65536) != 0);
                        client.NumerologySystem.AddToLetterCNumber = ((i & 32768) != 0);
                        client.NumerologySystem.AddToLetterLDistance = ((i & 16384) != 0);
                        client.NumerologySystem.AddToLetterWDistance = ((i & 8192) != 0);
                        client.NumerologySystem.AddToLetterVDistance = ((i & 4096) != 0);
                        client.NumerologySystem.AddToLetterCDistance = ((i & 2048) != 0);
                        client.NumerologySystem.AddToWordWNumber = ((i & 1024) != 0);
                        client.NumerologySystem.AddToWordVNumber = ((i & 512) != 0);
                        client.NumerologySystem.AddToWordCNumber = ((i & 256) != 0);
                        client.NumerologySystem.AddToWordWDistance = ((i & 128) != 0);
                        client.NumerologySystem.AddToWordVDistance = ((i & 64) != 0);
                        client.NumerologySystem.AddToWordCDistance = ((i & 32) != 0);
                        client.NumerologySystem.AddToVerseVNumber = ((i & 16) != 0);
                        client.NumerologySystem.AddToVerseCNumber = ((i & 8) != 0);
                        client.NumerologySystem.AddToVerseVDistance = ((i & 4) != 0);
                        client.NumerologySystem.AddToVerseCDistance = ((i & 2) != 0);
                        client.NumerologySystem.AddToChapterCNumber = ((i & 1) != 0);

                        long alfatiha_value = client.CalculateValue(client.Book.Chapters[0]);
                        int alfatiha_value_index = -1;
                        switch (number_type)
                        {
                            case NumberType.Prime:
                                {
                                    alfatiha_value_index = Numbers.IndexOfPrime(alfatiha_value);
                                }
                                break;
                            case NumberType.AdditivePrime:
                                {
                                    alfatiha_value_index = Numbers.IndexOfAdditivePrime(alfatiha_value);
                                }
                                break;
                            case NumberType.PurePrime:
                                {
                                    alfatiha_value_index = Numbers.IndexOfPurePrime(alfatiha_value);
                                }
                                break;
                            case NumberType.Composite:
                                {
                                    alfatiha_value_index = Numbers.IndexOfComposite(alfatiha_value);
                                }
                                break;
                            case NumberType.AdditiveComposite:
                                {
                                    alfatiha_value_index = Numbers.IndexOfAdditiveComposite(alfatiha_value);
                                }
                                break;
                            case NumberType.PureComposite:
                                {
                                    alfatiha_value_index = Numbers.IndexOfPureComposite(alfatiha_value);
                                }
                                break;
                            default:
                                break;
                        }

                        if (alfatiha_value_index != -1)
                        {
                            long bismAllah_value = client.CalculateValue(client.Book.Chapters[0].Verses[0]);

                            long difference = bismAllah_value - (long)alfatiha_value_index;
                            if (difference == 0L) // not (Math.Abs(difference) <= target_difference) // use target_difference for best systems only (not good systems)
                            {
                                ZeroDifferenceNumerologySystem good_numerology_system = new ZeroDifferenceNumerologySystem();
                                good_numerology_system.NumerologySystem = new NumerologySystem(client.NumerologySystem);
                                good_numerology_system.NumberType = number_type;
                                good_numerology_system.BismAllahValue = bismAllah_value;
                                good_numerology_system.AlFatihaValue = alfatiha_value;
                                good_numerology_system.AlFatihaValueIndex = alfatiha_value_index;
                                good_numerology_systems.Add(good_numerology_system);

                                // is  Value(Book) == ValueIndex(Al-Faiha)
                                long book_value = client.CalculateValue(client.Book.Verses);
                                int book_value_index = -1;
                                switch (good_numerology_system.NumberType)
                                {
                                    case NumberType.Prime:
                                        {
                                            book_value_index = Numbers.IndexOfPrime(book_value);
                                        }
                                        break;
                                    case NumberType.AdditivePrime:
                                        {
                                            book_value_index = Numbers.IndexOfAdditivePrime(book_value);
                                        }
                                        break;
                                    case NumberType.PurePrime:
                                        {
                                            book_value_index = Numbers.IndexOfPurePrime(book_value);
                                        }
                                        break;
                                    case NumberType.Composite:
                                        {
                                            book_value_index = Numbers.IndexOfComposite(book_value);
                                        }
                                        break;
                                    case NumberType.AdditiveComposite:
                                        {
                                            book_value_index = Numbers.IndexOfAdditiveComposite(book_value);
                                        }
                                        break;
                                    case NumberType.PureComposite:
                                        {
                                            book_value_index = Numbers.IndexOfPureComposite(book_value);
                                        }
                                        break;
                                    default:
                                        break;
                                }

                                if (book_value_index != -1)
                                {
                                    difference = alfatiha_value - (long)book_value_index;
                                    if (Math.Abs(difference) <= target_difference)
                                    {
                                        ZeroDifferenceNumerologySystem best_numerology_system = good_numerology_system;

                                        // collect all matching systems to print out at the end
                                        best_numerology_systems.Add(best_numerology_system);

                                        // prinet out the current matching system now
                                        string i_filename = "FindAlFatihaEqualsBook" + number_type.ToString() + "IndexSystem" + Globals.OUTPUT_FILE_EXT;
                                        string i_path = Globals.RESEARCH_FOLDER + "/" + i_filename;

                                        StringBuilder i_str = new StringBuilder();
                                        i_str.AppendLine("TextMode" +
                                                "\t" + "LetterOrder" +
                                                "\t" + "LetterValues" +
                                                "\t" + "Scope" +
                                                "\t" + "AddToLetterLNumber" +
                                                "\t" + "AddToLetterWNumber" +
                                                "\t" + "AddToLetterVNumber" +
                                                "\t" + "AddToLetterCNumber" +
                                                "\t" + "AddToLetterLDistance" +
                                                "\t" + "AddToLetterWDistance" +
                                                "\t" + "AddToLetterVDistance" +
                                                "\t" + "AddToLetterCDistance" +
                                                "\t" + "AddToWordWNumber" +
                                                "\t" + "AddToWordVNumber" +
                                                "\t" + "AddToWordCNumber" +
                                                "\t" + "AddToWordWDistance" +
                                                "\t" + "AddToWordVDistance" +
                                                "\t" + "AddToWordCDistance" +
                                                "\t" + "AddToVerseVNumber" +
                                                "\t" + "AddToVerseCNumber" +
                                                "\t" + "AddToVerseVDistance" +
                                                "\t" + "AddToVerseCDistance" +
                                                "\t" + "AddToChapterCNumber" +
                                                "\t" + "BismAllahValue" +
                                                "\t" + "AlFatihaIndex" +
                                                "\t" + "AlFatihaValue" +
                                                "\t" + "BookValueIndex"
                                            );

                                        i_str.Append(best_numerology_system.NumerologySystem.ToTabbedString());
                                        i_str.Append("\t" + best_numerology_system.BismAllahValue.ToString());
                                        i_str.Append("\t" + best_numerology_system.AlFatihaValueIndex.ToString());
                                        i_str.Append("\t" + best_numerology_system.AlFatihaValue.ToString());
                                        i_str.Append("\t" + best_numerology_system.BookValueIndex.ToString());
                                        i_str.AppendLine();

                                        PublicStorage.SaveText(i_path, i_str.ToString());
                                        PublicStorage.DisplayFile(i_path);

                                        // wait for file to be written correctly to prevent cross-thread problem
                                        // if another match was found shortly after this one
                                        Thread.Sleep(3000);
                                    }
                                }
                            }
                        }

                    } // next PERMUTATION

                } // next NumerologySystemScope

                string filename = "FindAlFatihaEqualsBook" + number_type.ToString() + "IndexSystem" + Globals.OUTPUT_FILE_EXT;
                string path = Globals.RESEARCH_FOLDER + "/" + filename;

                StringBuilder str = new StringBuilder();
                str.AppendLine("TextMode" +
                        "\t" + "LetterOrder" +
                        "\t" + "LetterValues" +
                        "\t" + "Scope" +
                        "\t" + "AddToLetterLNumber" +
                        "\t" + "AddToLetterWNumber" +
                        "\t" + "AddToLetterVNumber" +
                        "\t" + "AddToLetterCNumber" +
                        "\t" + "AddToLetterLDistance" +
                        "\t" + "AddToLetterWDistance" +
                        "\t" + "AddToLetterVDistance" +
                        "\t" + "AddToLetterCDistance" +
                        "\t" + "AddToWordWNumber" +
                        "\t" + "AddToWordVNumber" +
                        "\t" + "AddToWordCNumber" +
                        "\t" + "AddToWordWDistance" +
                        "\t" + "AddToWordVDistance" +
                        "\t" + "AddToWordCDistance" +
                        "\t" + "AddToVerseVNumber" +
                        "\t" + "AddToVerseCNumber" +
                        "\t" + "AddToVerseVDistance" +
                        "\t" + "AddToVerseCDistance" +
                        "\t" + "AddToChapterCNumber" +
                        "\t" + "BismAllahValue" +
                        "\t" + "AlFatihaIndex" +
                        "\t" + "AlFatihaValue" +
                        "\t" + "BookValueIndex"
                    );
                foreach (ZeroDifferenceNumerologySystem best_numerology_system in best_numerology_systems)
                {
                    str.Append(best_numerology_system.NumerologySystem.ToTabbedString());
                    str.Append("\t" + best_numerology_system.BismAllahValue.ToString());
                    str.Append("\t" + best_numerology_system.AlFatihaValueIndex.ToString());
                    str.Append("\t" + best_numerology_system.AlFatihaValue.ToString());
                    str.Append("\t" + best_numerology_system.BookValueIndex.ToString());
                    str.AppendLine();
                }
                PublicStorage.SaveText(path, str.ToString());
                PublicStorage.DisplayFile(path);

                // clear for next NumberType
                good_numerology_systems.Clear();
                best_numerology_systems.Clear();

            } // if NumberType

        } // next NumberType

        client.NumerologySystem = backup_numerology_system;
    }
}
