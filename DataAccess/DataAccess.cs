using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Model;

public static class DataAccess
{
    // quran text from http://tanzil.net
    // load book with Verse.MAX_NUMBER lines and simplify it on the fly
    public static string[] LoadVerseTexts(string title)
    {
        string[] result = new string[Verse.MAX_NUMBER];
        if (!String.IsNullOrEmpty(title))
        {
            string filename = Globals.DATA_FOLDER + "/" + "quran-uthmani.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    int i = 0;
                    while (!reader.EndOfStream)
                    {
                        // skip # comment lines (tanzil copyrights, other meta info, ...)
                        string line = reader.ReadLine();
                        if ((line.Length > 0) && (!line.StartsWith("#")))
                        {
                            result[i] = line;
                        }
                        i++;
                    }
                }
            }
        }
        return result;
    }
    // (c)2014 Hadi Al-Thehabi to decorate the end of each verse with appropraite stopmark
    public static string[] LoadVerseStopmarks()
    {
        string[] result = new string[Verse.MAX_NUMBER];
        string filename = Globals.DATA_FOLDER + "/" + "verse_stopmarks.txt";
        if (File.Exists(filename))
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                int i = 0;
                while (!reader.EndOfStream)
                {
                    result[i] = reader.ReadLine();
                    i++;
                }
            }
        }
        return result;
    }
    //public static Book LoadBook(string title)
    //{
    //    Book book = null;
    //    if (!String.IsNullOrEmpty(title))
    //    {
    //        string verse_ends_filename = Globals.DATA_FOLDER + "/" + "verse_ends.txt";
    //        List<bool> verse_ends = new List<bool>(Verse.MAX_NUMBER);
    //        if (File.Exists(verse_ends_filename))
    //        {
    //            using (StreamReader reader = File.OpenText(verse_ends_filename))
    //            {
    //                while (!reader.EndOfStream)
    //                {
    //                    string line = reader.ReadLine();
    //                    verse_ends.Add(line == "1");
    //                }
    //            }
    //        }

    //        string filename = Globals.DATA_FOLDER + "/" + "quran-uthmani.txt";
    //        if (File.Exists(filename))
    //        {
    //            using (StreamReader reader = File.OpenText(filename))
    //            {
    //                string content = reader.ReadToEnd();
    //                content = content.Replace("\r\n", "\n");
    //                string[] lines = content.Split('\n');

    //                List<Verse> verses = new List<Verse>();
    //                for (int i = 0; i < lines.Length; i++)
    //                {
    //                    // skip # comment lines (tanzil copyrights, other meta info, ...)
    //                    if ((lines[i].Length > 0) && (!lines[i].StartsWith("#")))
    //                    {
    //                        Verse verse = new Verse(i + 1, lines[i], verse_ends[i]);
    //                        verses.Add(verse);
    //                    }
    //                }

    //                book = new Book(title, verses);
    //            }
    //        }
    //    }
    //    return book;
    //}
    //public static void SaveBook(Book book, string filename)
    //{
    //    if (book != null)
    //    {
    //        StringBuilder str = new StringBuilder();
    //        foreach (Verse verse in book.Verses)
    //        {
    //            str.AppendLine(verse.Text);
    //        }
    //        SaveFile(filename, str.ToString());
    //    }
    //}

    // recitation infos from http://www.everyayah.com
    public static void LoadRecitationInfos(Book book)
    {
        if (book != null)
        {
            book.RecitationInfos = new Dictionary<string, RecitationInfo>();
            string filename = Globals.AUDIO_FOLDER + "/" + "metadata.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    string line = reader.ReadLine(); // skip header row
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length >= 4)
                        {
                            RecitationInfo recitation = new RecitationInfo();
                            recitation.Url = parts[0];
                            recitation.Folder = parts[0];
                            recitation.Language = parts[1];
                            recitation.Reciter = parts[2];
                            int.TryParse(parts[3], out recitation.Quality);
                            recitation.Name = recitation.Language + " - " + recitation.Reciter;
                            book.RecitationInfos.Add(parts[0], recitation);
                        }
                    }
                }
            }
        }
    }

    // translations info from http://tanzil.net
    public static void LoadTranslationInfos(Book book)
    {
        if (book != null)
        {
            book.TranslationInfos = new Dictionary<string, TranslationInfo>();
            string filename = Globals.TRANSLATIONS_FOLDER + "/" + "Offline" + "/" + "metadata.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    string line = reader.ReadLine(); // skip header row
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length >= 4)
                        {
                            TranslationInfo translation = new TranslationInfo();
                            translation.Url = "?transID=" + parts[0] + "&type=" + TranslationInfo.FileType;
                            translation.Flag = parts[1];
                            translation.Language = parts[2];
                            translation.Translator = parts[3];
                            translation.Name = parts[2] + " - " + parts[3];
                            book.TranslationInfos.Add(parts[0], translation);
                        }
                    }
                }
            }
        }
    }

    // translation books from http://tanzil.net
    public static void LoadTranslations(Book book)
    {
        if (book != null)
        {
            try
            {
                string[] filenames = Directory.GetFiles(Globals.TRANSLATIONS_FOLDER + "/");
                foreach (string filename in filenames)
                {
                    List<string> translated_lines = DataAccess.LoadLines(filename);
                    if (translated_lines != null)
                    {
                        string translation = filename.Substring((Globals.TRANSLATIONS_FOLDER.Length + 1), filename.Length - (Globals.TRANSLATIONS_FOLDER.Length + 1) - 4);
                        if (translation == "metadata.txt") continue;
                        LoadTranslation(book, translation);
                    }
                }
            }
            catch
            {
                // ignore error
            }
        }
    }
    public static void LoadTranslation(Book book, string translation)
    {
        if (book != null)
        {
            try
            {
                string[] filenames = Directory.GetFiles(Globals.TRANSLATIONS_FOLDER + "/");
                bool title_is_installed = false;
                foreach (string filename in filenames)
                {
                    if (filename.Contains(translation))
                    {
                        title_is_installed = true;
                        break;
                    }
                }
                if (!title_is_installed)
                {
                    File.Copy(Globals.TRANSLATIONS_FOLDER + "/" + "Offline" + "/" + translation + ".txt", Globals.TRANSLATIONS_FOLDER + "/" + translation + ".txt", true);
                }

                filenames = Directory.GetFiles(Globals.TRANSLATIONS_FOLDER + "/");
                foreach (string filename in filenames)
                {
                    if (filename.Contains(translation))
                    {
                        List<string> translated_lines = DataAccess.LoadLines(filename);
                        if (translated_lines != null)
                        {
                            if (book.TranslationInfos != null)
                            {
                                if (book.TranslationInfos.ContainsKey(translation))
                                {
                                    for (int i = 0; i < Verse.MAX_NUMBER; i++)
                                    {
                                        book.Verses[i].Translations[translation] = translated_lines[i];
                                    }

                                    // add bismAllah translation to the first verse of each chapter except chapters 1 and 9
                                    foreach (Chapter chapter in book.Chapters)
                                    {
                                        if ((chapter.Number != 1) && (chapter.Number != 9))
                                        {
                                            if ((translation != "ar.emlaaei") && (translation != "en.transliteration") && (translation != "en.wordbyword"))
                                            {
                                                if (!chapter.Verses[0].Translations[translation].StartsWith(book.Verses[0].Translations[translation]))
                                                {
                                                    chapter.Verses[0].Translations[translation] = book.Verses[0].Translations[translation] + " " + chapter.Verses[0].Translations[translation];
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
            catch
            {
                // ignore error
            }
        }
    }
    public static void UnloadTranslation(Book book, string translation)
    {
        if (book != null)
        {
            try
            {
                string[] filenames = Directory.GetFiles(Globals.TRANSLATIONS_FOLDER + "/");
                foreach (string filename in filenames)
                {
                    if (filename.Contains(translation))
                    {
                        if (book.TranslationInfos != null)
                        {
                            if (book.TranslationInfos.ContainsKey(translation))
                            {
                                for (int i = 0; i < Verse.MAX_NUMBER; i++)
                                {
                                    book.Verses[i].Translations.Remove(translation);
                                }
                                book.TranslationInfos.Remove(translation);
                            }
                            break;
                        }
                    }
                }
            }
            catch
            {
                // ignore error
            }
        }
    }
    // useful for correcting translations later in sha Allah
    public static void SaveTranslation(Book book, string translation)
    {
        if (book != null)
        {
            StringBuilder str = new StringBuilder();
            foreach (Verse verse in book.Verses)
            {
                str.AppendLine(verse.Translations[translation]);
            }
            string filename = Globals.TRANSLATIONS_FOLDER + "/" + translation + ".txt";
            SaveFile(filename, str.ToString());
        }
    }

    // word meanings from http://qurandev.appspot.com - modified by Ali Adams
    public static void LoadWordMeanings(Book book)
    {
        if (book != null)
        {
            string filename = Globals.TRANSLATIONS_FOLDER + "/" + "Offline" + "/" + "en.wordbyword.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    while (!reader.EndOfStream)
                    {
                        try
                        {
                            foreach (Verse verse in book.Verses)
                            {
                                string line = reader.ReadLine();
                                string[] parts = line.Split('\t');

                                int word_count = 0;
                                foreach (Word word in verse.Words)
                                {
                                    if (word.Text != "و") // Simplified29Waw
                                    {
                                        word_count++;
                                    }
                                }
                                if (parts.Length != word_count)
                                {
                                    throw new Exception("File format error.");
                                }

                                int i = 0;
                                foreach (Word word in verse.Words)
                                {
                                    if (word.Text == "و") // Simplified29Waw
                                    {
                                        word.Meaning = "and";
                                    }
                                    else
                                    {
                                        word.Meaning = parts[i];
                                        i++;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("LoadWordMeanings: " + ex.Message);
                        }
                    }
                }
            }
        }
    }

    // uthmani roots by Ali Adams from emlaaei version of http://www.noorsoft.org version 0.9.1
    public static void LoadRootWords(Book book)
    {
        if (book != null)
        {
            string filename = Globals.DATA_FOLDER + "/" + "root-words.txt";
            if (File.Exists(filename))
            {
                book.RootWords = new Dictionary<string, List<Word>>();
                using (StreamReader reader = File.OpenText(filename))
                {
                    while (!reader.EndOfStream)
                    {
                        try
                        {
                            string line = reader.ReadLine();
                            string[] parts = line.Split('\t');
                            if (parts.Length == 3)
                            {
                                string root = parts[0];
                                int count = int.Parse(parts[1]);
                                string[] addresses = parts[2].Split(';');

                                List<Word> root_words = new List<Word>();
                                foreach (string address in addresses)
                                {
                                    string[] segments = address.Split(':');
                                    if (segments.Length == 2)
                                    {
                                        string[] segment_parts = segments[1].Split(',');
                                        foreach (string segment_part in segment_parts)
                                        {
                                            int verse_index = int.Parse(segments[0]);
                                            Verse verse = book.Verses[verse_index];
                                            if (verse != null)
                                            {
                                                if (verse.Words != null)
                                                {
                                                    if (verse.Words.Count > 0)
                                                    {
                                                        int word_number = int.Parse(segment_part);
                                                        Word root_word = null;
                                                        // THIS software now loads an already-uthmani word root list
                                                        // generated by the SaveRootWordsDictionary() below

                                                        //// update word_number from emlaaei to uthmani
                                                        //try
                                                        //{
                                                        //    // for all words after merged words decrement word_number
                                                        //    for (int i = 0; i < word_number - 1; i++)
                                                        //    {
                                                        //        // if previous words starts with يَٰ or وَيَٰ   
                                                        //        if (
                                                        //            (verse.Words[i].Text.StartsWith("يَٰ"))
                                                        //            ||
                                                        //            (verse.Words[i].Text.StartsWith("وَيَٰ"))
                                                        //            )
                                                        //        {
                                                        //            word_number--;
                                                        //        }

                                                        //        // also, if previous words equals to these exceptions
                                                        //        if (verse.Words[i].Text == "يَبْنَؤُمَّ") // 3 words in imlaaei
                                                        //        {
                                                        //            word_number--;
                                                        //            word_number--;
                                                        //        }
                                                        //        else if (verse.Words[i].Text == "فَإِلَّمْ") // 2 words in imlaaei
                                                        //        {
                                                        //            word_number--;
                                                        //        }
                                                        //        else if (verse.Words[i].Text == "هَٰٓأَنتُمْ") // 2 words in imlaaei
                                                        //        {
                                                        //            word_number--;
                                                        //        }
                                                        //        else if (verse.Words[i].Text == "وَأَلَّوِ") // 2 words in imlaaei
                                                        //        {
                                                        //            word_number--;
                                                        //        }
                                                        //        // "بَعْدَ" and "مَا" to become "بَعْدَمَا"    // 2 words in imlaaei 
                                                        //        else if (verse.Words[i].Text == "بَعْدَ")
                                                        //        {
                                                        //            if (verse.Words.Count > (i + 1))
                                                        //            {
                                                        //                if (verse.Words[i + 1].Text == "مَا")
                                                        //                {
                                                        //                    word_number--;
                                                        //                }
                                                        //            }
                                                        //        }
                                                        //        else if ((root == "ما") && (verse.Words[word_number - 1].Text == "بَعْدَ"))
                                                        //        {
                                                        //            word_number--;
                                                        //        }
                                                        //    }
                                                        //}
                                                        //catch
                                                        //{
                                                        //    throw new Exception("Root word location cannot be corrected automatically.");
                                                        //}
                                                        if ((word_number > 0) && (word_number <= verse.Words.Count))
                                                        {
                                                            root_word = verse.Words[word_number - 1];
                                                            if (root_word != null)
                                                            {
                                                                root_words.Add(root_word);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!book.RootWords.ContainsKey(root))
                                {
                                    book.RootWords.Add(root, root_words);
                                }
                            }
                            else
                            {
                                // skip reading copyright notice;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("LoadRootWords: " + ex.Message);
                        }
                    }
                }
            }
        }
    }

    // word and its roots reverse-extracted from above by Ali Adams
    public static void LoadWordRoots(Book book)
    {
        if (book != null)
        {
            // Id	Chapter	Verse	Word	Text	Roots
            string filename = Globals.DATA_FOLDER + "/" + "word-roots.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    while (!reader.EndOfStream)
                    {
                        try
                        {
                            foreach (Verse verse in book.Verses)
                            {
                                foreach (Word word in verse.Words)
                                {
                                    if (word.Text == "و") // Simplified29Waw
                                    {
                                        word.Roots = new List<string>() { "و" };
                                    }
                                    else
                                    {
                                        string line = reader.ReadLine();
                                        string[] parts = line.Split('\t');

                                        if (parts.Length == 6)
                                        {
                                            string text = parts[4];
                                            string[] subparts = parts[5].Split('|');
                                            word.Roots = new List<string>(subparts);
                                        }
                                        else
                                        {
                                            throw new Exception("Invalid file format.");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("LoadWordRoots: " + ex.Message);
                        }
                    }
                }
            }
        }
    }

    // corpus word parts from http://corpus.quran.com version 0.4 - modified by Ali Adams
    public static void LoadWordParts(Book book)
    {
        if (book != null)
        {
            string filename = Globals.DATA_FOLDER + "/" + "word-parts.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    int waw_count = 0;
                    int previous_verse_number = 0;
                    while (!reader.EndOfStream)
                    {
                        try
                        {
                            string line = reader.ReadLine();
                            if ((line.Length == 0) || line.StartsWith("#") || line.StartsWith("LOCATION") || line.StartsWith("ADDRESS"))
                            {
                                continue; // skip header info
                            }
                            else
                            {
                                string[] parts = line.Split('\t');
                                if (parts.Length >= 4)
                                {
                                    string address = parts[0];
                                    if (address.StartsWith("(") && address.EndsWith(")"))
                                    {
                                        address = parts[0].Substring(1, parts[0].Length - 2);
                                    }
                                    string[] address_parts = address.Split(':');
                                    if (address_parts.Length == 4)
                                    {
                                        int chapter_number = int.Parse(address_parts[0]);
                                        int verse_number = int.Parse(address_parts[1]);
                                        if (previous_verse_number != verse_number)
                                        {
                                            waw_count = 0;
                                            previous_verse_number = verse_number;
                                        }
                                        int word_number = int.Parse(address_parts[2]) + waw_count;
                                        int word_part_number = int.Parse(address_parts[3]);

                                        string buckwalter = parts[1];
                                        string tag = parts[2];

                                        Chapter chapter = book.Chapters[chapter_number - 1];
                                        if (chapter != null)
                                        {
                                            Verse verse = chapter.Verses[verse_number - 1];
                                            if (verse != null)
                                            {
                                                // add bismAllah manually to each chapter except 1 and 9
                                                if (
                                                    ((chapter_number != 1) && (chapter_number != 9))
                                                    &&
                                                    ((verse_number == 1) && (word_number == 1) && (word_part_number == 1))
                                                   )
                                                {
                                                    Verse bismAllah_verse = book.Verses[0];

                                                    // if there is no bismAllah, add one
                                                    if (parts[1] != bismAllah_verse.Words[0].Parts[0].Buckwalter)
                                                    {
                                                        // insert 4 new words
                                                        verse.Words.InsertRange(0, new List<Word>(4));

                                                        //(1:1:1:1)	bi	PP	PREFIX|bi+
                                                        WordPart word_part = new WordPart(verse.Words[0],
                                                              bismAllah_verse.Words[0].Parts[0].NumberInWord,
                                                              bismAllah_verse.Words[0].Parts[0].Buckwalter,
                                                              bismAllah_verse.Words[0].Parts[0].Tag,
                                                              new WordPartGrammar(bismAllah_verse.Words[0].Parts[0].Grammar)
                                                        );
                                                        if ((chapter_number == 95) || (chapter_number == 97))
                                                        {
                                                            // add shadda  { '~', 'ّ' } on B or bism
                                                            word_part.Buckwalter = word_part.Buckwalter.Insert(1, "~");
                                                        }

                                                        //(1:1:1:2)	somi	N	STEM|POS:N|LEM:{som|ROOT:smw|M|GEN
                                                        new WordPart(verse.Words[0],
                                                          bismAllah_verse.Words[0].Parts[1].NumberInWord,
                                                          bismAllah_verse.Words[0].Parts[1].Buckwalter,
                                                          bismAllah_verse.Words[0].Parts[1].Tag,
                                                          new WordPartGrammar(bismAllah_verse.Words[0].Parts[1].Grammar)
                                                        );

                                                        //(1:1:2:1)	{ll~ahi	PN	STEM|POS:PN|LEM:{ll~ah|ROOT:Alh|GEN
                                                        new WordPart(verse.Words[1],
                                                          bismAllah_verse.Words[1].Parts[0].NumberInWord,
                                                          bismAllah_verse.Words[1].Parts[0].Buckwalter,
                                                          bismAllah_verse.Words[1].Parts[0].Tag,
                                                          new WordPartGrammar(bismAllah_verse.Words[1].Parts[0].Grammar)
                                                        );

                                                        //(1:1:3:1)	{l	DET	PREFIX|Al+
                                                        new WordPart(verse.Words[2],
                                                          bismAllah_verse.Words[2].Parts[0].NumberInWord,
                                                          bismAllah_verse.Words[2].Parts[0].Buckwalter,
                                                          bismAllah_verse.Words[2].Parts[0].Tag,
                                                          new WordPartGrammar(bismAllah_verse.Words[2].Parts[0].Grammar)
                                                        );

                                                        //(1:1:3:2)	r~aHoma`ni	ADJ	STEM|POS:ADJ|LEM:r~aHoma`n|ROOT:rHm|MS|GEN
                                                        new WordPart(verse.Words[2],
                                                          bismAllah_verse.Words[2].Parts[1].NumberInWord,
                                                          bismAllah_verse.Words[2].Parts[1].Buckwalter,
                                                          bismAllah_verse.Words[2].Parts[1].Tag,
                                                          new WordPartGrammar(bismAllah_verse.Words[2].Parts[1].Grammar)
                                                        );

                                                        //(1:1:4:1)	{l	DET	PREFIX|Al+
                                                        new WordPart(verse.Words[3],
                                                          bismAllah_verse.Words[3].Parts[0].NumberInWord,
                                                          bismAllah_verse.Words[3].Parts[0].Buckwalter,
                                                          bismAllah_verse.Words[3].Parts[0].Tag,
                                                          new WordPartGrammar(bismAllah_verse.Words[3].Parts[0].Grammar)
                                                        );

                                                        //(1:1:4:2)	r~aHiymi	ADJ	STEM|POS:ADJ|LEM:r~aHiym|ROOT:rHm|MS|GEN
                                                        new WordPart(verse.Words[3],
                                                          bismAllah_verse.Words[3].Parts[1].NumberInWord,
                                                          bismAllah_verse.Words[3].Parts[1].Buckwalter,
                                                          bismAllah_verse.Words[3].Parts[1].Tag,
                                                          new WordPartGrammar(bismAllah_verse.Words[3].Parts[1].Grammar)
                                                        );
                                                    }
                                                }
                                                // correct word_number (if needed) for all subsequenct chapter word_parts
                                                if (
                                                    ((chapter_number != 1) && (chapter_number != 9)) && (verse_number == 1)
                                                   )
                                                {
                                                    word_number += 4;
                                                }

                                                Word word = verse.Words[word_number - 1];
                                                if (word != null)
                                                {
                                                    List<string> grammar = new List<string>(parts[3].Split('|'));
                                                    if (grammar.Count > 0)
                                                    {
                                                        //(1:5:3:1)	wa	CONJ	PREFIX|w_CONJ+
                                                        //(1:5:3:2)	<iy~aAka	PRON	STEM|POS:PRON|LEM:<iy~aA|2MS
                                                        if (word.Text == "و")
                                                        {
                                                            waw_count++;
                                                        }
                                                        new WordPart(word, word_part_number, buckwalter, tag, grammar);
                                                    }
                                                    else
                                                    {
                                                        throw new Exception("Grammar field is missing.\r\n" + filename);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Invalid Location Format.\r\n" + filename);
                                    }
                                }
                                else
                                {
                                    throw new Exception("Invalid File Format.\r\n" + filename);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("LoadWordParts: " + ex.Message);
                        }
                    }
                }
            }
        }
    }

    // help methods
    public static List<string> LoadLines(string filename)
    {
        List<string> result = new List<string>();
        if (File.Exists(filename))
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    result.Add(line);
                }
            }
        }
        return result;
    }
    public static string LoadFile(string filename)
    {
        StringBuilder str = new StringBuilder();
        if (File.Exists(filename))
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    str.AppendLine(line);
                }
            }
        }
        return str.ToString();
    }
    public static void SaveFile(string filename, string content)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
            {
                writer.Write(content);
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }
    }

    // download page in the range 001-604 from
    // http://www.searchtruth.org/quran/images1/###.jpg
}
