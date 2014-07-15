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
using System.Windows.Forms;
using System.Text;
using System.IO;
using Model;

public class MyScript : IScriptRunner
{
    private bool OnlyKeyLetters(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;
        List<Verse> verses = client.Selection.Verses;

        StringBuilder str = new StringBuilder();
        foreach (Verse verse in verses)
        {
            string verse_text = verse.Text;
            verse_text = verse_text.Replace("ف", "");
            verse_text = verse_text.Replace("ف", "");
            verse_text = verse_text.Replace("ز", "");
            verse_text = verse_text.Replace("ء", "");
            verse_text = verse_text.Replace("خ", "");
            verse_text = verse_text.Replace("ش", "");
            verse_text = verse_text.Replace("ظ", "");
            verse_text = verse_text.Replace("ج", "");
            verse_text = verse_text.Replace("ث", "");
            str.AppendLine(verse_text);
        }
        string filename = client.NumerologySystem.Name + "_" + "OnlyKeyLetters" + Globals.OUTPUT_FILE_EXT;
        ScriptRunner.SaveText(filename, str.ToString());
        ScriptRunner.DisplayFile(filename);
        return false; // so not to close Script window
    }
    private bool OnlyInitialLetters(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;
        List<Verse> verses = client.Selection.Verses;

        //الم
        //الم
        //المص
        //الر
        //الر
        //المر
        //الر
        //الر
        //الر
        //كهيعص
        //طه
        //طسم
        //طس
        //طسم
        //الم
        //الم
        //الم
        //الم
        //يس
        //ص
        //حم
        //حم
        //حم
        //حم
        //حم
        //حم
        //حم
        //ق
        //ن
        //-----------------
        //ا
        //ل
        //م
        //ص
        //ر
        //ك
        //ه
        //ي
        //ع
        //ط
        //س
        //ح
        //ق
        //ن
        //-----------------
        //ا	13
        //ل	13
        //م	17
        //ص	3
        //ر	6
        //ك	1
        //ه	2
        //ي	2
        //ع	1
        //ط	4
        //س	4
        //ح	7
        //ق	1
        //ن	1
        try
        {
            StringBuilder str = new StringBuilder();
            foreach (Verse verse in verses)
            {
                str.AppendLine(verse.Text);
            }
            str.Replace("ب", "");
            str.Replace("د", "");
            str.Replace("و", "");
            str.Replace("ت", "");
            str.Replace("ذ", "");
            str.Replace("غ", "");
            str.Replace("ض", "");
            str.Replace("ف", "");
            str.Replace("ز", "");
            str.Replace("ء", "");
            str.Replace("خ", "");
            str.Replace("ش", "");
            str.Replace("ظ", "");
            str.Replace("ج", "");
            str.Replace("ث", "");

            string filename = client.NumerologySystem.Name + "_" + "OnlyInitialLetters" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveText(filename, str.ToString());
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }

    private bool JumpLettersByX(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoJumpLettersByX(client, verses_text, extra);

            string filename = client.NumerologySystem.Name + "_" + "JumpLettersByX" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool JumpLettersByValues(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoJumpLettersByValues(client, verses_text, extra);

            string filename = client.NumerologySystem.Name + "_" + "JumpLettersByValues" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool JumpLettersByPrimeNumbers(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoJumpLettersByPrimeNumbers(client, verses_text, extra);

            string filename = client.NumerologySystem.Name + "_" + "JumpLettersByPrimeNumbers" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool JumpLettersByFibonacciNumbers(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        // 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, ...
        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoJumpLettersByFibonacciNumbers(client, verses_text, extra);

            string filename = client.NumerologySystem.Name + "_" + "JumpLettersByFibonacciNumbers" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool JumpLettersByPiDigits(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        // 3.14159265358979323846264338327950288419716939937510582097494459230781640628620899862803482534211706798214808651328230664709384460955058223172535940812848111745028410270193852110555964462294895493038196442881097566593344612847564823378678316527120190914564856692346034861045432664821339360726024914127372458700660631558817488152092096282925409171536436789259036001133053054882046652138414695194151160943305727036575959195309218611738193261179310511854807446237996274956735188575272489122793818301194912983367336244065664308602139494639522473719070217986094370277053921717629317675238467481846766940513200056812714526356082778577134275778960917363717872146844090122495343014654958537105079227968925892354201995611212902196086403441815981362977477130996051870721134999999837297804995105973173281609631859502445945534690830264252230825334468503526193118817101000313783875288658753320838142061717766914730359825349042875546873115956286388235378759375195778185778053217122680661300192787661119590921642019893809525720106548586327...
        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoJumpLettersByPiDigits(client, verses_text, "");

            string filename = client.NumerologySystem.Name + "_" + "JumpLettersByPiDigits" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool JumpLettersByEulerDigits(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        // 2.71828182845904523536028747135266249775724709369995957496696762772407663035354759457138217852516642742746639193200305992181741359662904357290033429526059563073813232862794349076323382988075319525101901157383418793070215408914993488416750924476146066808226480016847741185374234544243710753907774499206955170276183860626133138458300075204493382656029760673711320070932870912744374704723069697720931014169283681902551510865746377211125238978442505695369677078544996996794686445490598793163688923009879312773617821542499922957635148220826989519366803318252886939849646510582093923982948879332036250944311730123819706841614039701983767932068328237646480429531180232878250981945581530175671736133206981125099618188159304169035159888851934580727386673858942287922849989208680582574927961048419844436346324496848756023362482704197862320900216099023530436994184914631409343173814364054625315209618369088870701676839642437814059271456354906130310720851038375051011574770417189861068739696552126715468895703503540212340784981933432106...
        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoJumpLettersByEulerDigits(client, verses_text, "");

            string filename = client.NumerologySystem.Name + "_" + "JumpLettersByEulerDigits" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool JumpLettersByGoldenRatioDigits(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        // 1.61803398874989484820458683436563811772030917980576286213544862270526046281890244970720720418939113748475408807538689175212663386222353693179318006076672635443338908659593958290563832266131992829026788067520876689250171169620703222104321626954862629631361443814975870122034080588795445474924618569536486444924104432077134494704956584678850987433944221254487706647809158846074998871240076521705751797883416625624940758906970400028121042762177111777805315317141011704666599146697987317613560067087480710131795236894275219484353056783002287856997829778347845878228911097625003026961561700250464338243776486102838312683303724292675263116533924731671112115881863851331620384005222165791286675294654906811317159934323597349498509040947621322298101726107059611645629909816290555208524790352406020172799747175342777592778625619432082750513121815628551222480939471234145170223735805772786160086883829523045926478780178899219902707769038953219681986151437803149974110692608867429622675756052317277752035361393621076738937645560606059...
        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoJumpLettersByGoldenRatioDigits(client, verses_text, "");

            string filename = client.NumerologySystem.Name + "_" + "JumpLettersByGoldenRatioDigits" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private char[] DoJumpLettersByX(Client client, string verses_text, string extra)
    {
        int count = verses_text.Length;
        char[] result = new char[count];
        //text = verses_text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int new_count = verses_text.Length;
        int i = 0;
        int r = 0;

        int step;
        try
        {
            step = int.Parse(extra);
        }
        catch
        {
            step = 1;
        }
        if (step > 0)
        {
            while (i < new_count)
            {
                if (verses_text[i] == '_')
                {
                    result[r++] = '\r';
                    result[r++] = '\n';
                }
                else
                {
                    result[r++] = verses_text[i];
                }
                i += step;
            }
        }
        return result;
    }
    private char[] DoJumpLettersByValues(Client client, string verses_text, string extra)
    {
        int count = verses_text.Length;
        char[] result = new char[count];
        //text = verses_text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int new_count = verses_text.Length;
        int i = 0;
        int r = 0;
        while (i < new_count)
        {
            if (verses_text[i] == '_')
            {
                result[r++] = '\r';
                result[r++] = '\n';
                i++; // skip '_'
            }
            else
            {
                result[r++] = verses_text[i];
                try
                {
                    i += (int)client.CalculateValue(verses_text[i].ToString());
                }
                catch
                {
                    // skip exceptions (in Abjad)
                    i += 1;
                }
            }
        }
        return result;
    }
    private char[] DoJumpLettersByPrimeNumbers(Client client, string verses_text, string extra)
    {
        //text = verses_text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int l = 0; // letter index
        for (int i = 0; i < count - 1; i++) // count - 1 to ignore last newline
        {
            if (Numbers.IsPrime(l + 1))
            {
                if (verses_text[i] == '_')
                {
                    result[r++] = '\r';
                    result[r++] = '\n';
                    i++; // skip '_'
                }
                result[r++] = verses_text[i];
            }
            l++;
        }
        return result;
    }
    private char[] DoJumpLettersByFibonacciNumbers(Client client, string verses_text, string extra)
    {
        //text = text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int l = 0; // letter index
        int N1 = 0;
        int N2 = 1;
        int Fn = N1 + N2;
        result[r++] = verses_text[0]; // add first 1 of Fibonacci numbers (1, 1, 2, 3, 5, 8, ...)
        for (int i = 0; i < count - 1; i++) // count - 1 to ignore last newline
        {
            if (l == (Fn - 1))
            {
                if (verses_text[i] == '_')
                {
                    result[r++] = '\r';
                    result[r++] = '\n';
                    i++; // skip '_'
                }
                result[r++] = verses_text[i];

                // next fibonacci number
                N1 = N2;
                N2 = Fn;
                Fn = N1 + N2;
                if (Fn >= count)
                {
                    break;
                }
            }
            l++;
        }
        return result;
    }
    private char[] DoJumpLettersByPiDigits(Client client, string verses_text, string extra)
    {
        //text = text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int d = 0; // digit index
        for (int i = 0; i < count; ) // i++) // advance inside loop
        {
            // advance pi_digit amount ignoring white spaces
            int j_count = Numbers.PiDigits[d++];
            for (int j = 0; j < j_count; j++)
            {
                if (i < count)
                {
                    if (verses_text[i] == '_')
                    {
                        result[r++] = '\r';
                        result[r++] = '\n';
                    }
                }

                // in all cases, advance to next letter
                i++;
            }

            // add the 0th-based letter to result
            if ((i - 1) < count)
            {
                result[r++] = verses_text[i - 1];
            }
        }
        string result_str = new String(result);
        result = result_str.ToCharArray();
        return result;
    }
    private char[] DoJumpLettersByEulerDigits(Client client, string verses_text, string extra)
    {
        //text = text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int d = 0; // digit index
        for (int i = 0; i < count; ) // i++) // advance inside loop
        {
            // advance e_digit amount ignoring white spaces
            int j_count = Numbers.EDigits[d++];
            for (int j = 0; j < j_count; j++)
            {
                if (i < count)
                {
                    if (verses_text[i] == '_')
                    {
                        result[r++] = '\r';
                        result[r++] = '\n';
                    }
                }

                // in all cases, advance to next letter
                i++;
            }

            // add the 0th-based letter to result
            if ((i - 1) < count)
            {
                result[r++] = verses_text[i - 1];
            }
        }
        string result_str = new String(result);
        result = result_str.ToCharArray();
        return result;
    }
    private char[] DoJumpLettersByGoldenRatioDigits(Client client, string verses_text, string extra)
    {
        //text = text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int d = 0; // digit index
        for (int i = 0; i < count; ) // i++) // advance inside loop
        {
            // advance phi_digit amount ignoring white spaces
            int j_count = Numbers.PhiDigits[d++];
            for (int j = 0; j < j_count; j++)
            {
                if (i < count)
                {
                    if (verses_text[i] == '_')
                    {
                        result[r++] = '\r';
                        result[r++] = '\n';
                    }
                }

                // in all cases, advance to next letter
                i++;
            }

            // add the 0th-based letter to result
            if ((i - 1) < count)
            {
                result[r++] = verses_text[i - 1];
            }
        }
        string result_str = new String(result);
        result = result_str.ToCharArray();
        return result;
    }

    private bool PrimeLetters(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoPrimeLetters(client, verses_text, extra);

            string filename = client.NumerologySystem.Name + "_" + "PrimeLetters" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool AdditivePrimeLetters(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoAdditivePrimeLetters(client, verses_text, extra);

            string filename = client.NumerologySystem.Name + "_" + "AdditivePrimeLetters" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool PurePrimeLetters(Client client, string extra)
    {
        if (client == null) return false;
        if (client.Selection == null) return false;

        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoPurePrimeLetters(client, verses_text, extra);

            string filename = client.NumerologySystem.Name + "_" + "PurePrimeLetters" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private bool FibonacciLetters(Client client, string extra)
    {
        // 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, ...
        if (client == null) return false;
        if (client.Selection == null) return false;

        try
        {
            string verses_text = client.Selection.Text;
            char[] result = DoFibonacciLetters(client, verses_text, extra);

            string filename = client.NumerologySystem.Name + "_" + "FibonacciLetters" + Globals.OUTPUT_FILE_EXT;
            ScriptRunner.SaveLetters(filename, result);
            ScriptRunner.DisplayFile(filename);
            return false; // so not to close Script window
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return false; // to stay in the Script window
        }
    }
    private char[] DoPrimeLetters(Client client, string verses_text, string extra)
    {
        //text = verses_text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int l = 0; // letter index
        for (int i = 0; i < count - 1; i++) // count - 1 to ignore last newline
        {
            if (Numbers.IsPrime(l + 1))
            {
                if (verses_text[i] == '_')
                {
                    result[r++] = '\r';
                    result[r++] = '\n';
                    i++; // skip '_'
                }
                result[r++] = verses_text[i];
            }
            l++;
        }
        return result;
    }
    private char[] DoAdditivePrimeLetters(Client client, string verses_text, string extra)
    {
        //text = text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int l = 0; // letter index
        for (int i = 0; i < count - 1; i++) // count - 1 to ignore last newline
        {
            if (Numbers.IsAdditivePrime(l + 1))
            {
                if (verses_text[i] == '_')
                {
                    result[r++] = '\r';
                    result[r++] = '\n';
                    i++; // skip '_'
                }
                result[r++] = verses_text[i];
            }
            l++;
        }
        return result;
    }
    private char[] DoPurePrimeLetters(Client client, string verses_text, string extra)
    {
        //text = text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int l = 0; // letter index
        for (int i = 0; i < count - 1; i++) // count - 1 to ignore last newline
        {
            if (Numbers.IsPurePrime(l + 1))
            {
                if (verses_text[i] == '_')
                {
                    result[r++] = '\r';
                    result[r++] = '\n';
                    i++; // skip '_'
                }
                result[r++] = verses_text[i];
            }
            l++;
        }
        return result;
    }
    private char[] DoFibonacciLetters(Client client, string verses_text, string extra)
    {
        //text = text.Replace("\r\n", "_"); // will miss 15 \n without \r before them
        verses_text = verses_text.Replace("\r", "");
        verses_text = verses_text.Replace("\n", "_");
        int count = verses_text.Length;
        char[] result = new char[count];
        int r = 0; // result index
        int l = 0; // letter index
        for (int i = 0; i < count - 1; i++) // count - 1 to ignore last newline
        {
            if (Numbers.IsFibonacci(l + 1))
            {
                if (verses_text[i] == '_')
                {
                    result[r++] = '\r';
                    result[r++] = '\n';
                    i++; // skip '_'
                }
                result[r++] = verses_text[i];
            }
            l++;
        }
        return result;
    }

    /// <summary>
    /// Run implements IScriptRunner interface to be invoked by QuranCode application
    /// </summary>
    /// <param name="args">any number and type of arguments</param>
    /// <returns>return any type</returns>
    public object Run(object[] args)
    {
        try
        {
            if (args.Length == 2)   // ScriptMethod(Client client, string extra)
            {
                Client client = args[0] as Client;
                string extra = args[1].ToString();

                if (client != null)
                {
                    return JumpLettersByPrimeNumbers(client, extra);
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Application.ProductName);
            return null;
        }
    }
}
