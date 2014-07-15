public enum Edition { Standard, Grammar, Lite, Research }
//"Standard Edition\tStart normally for original and simplified text." + "\r\n" +
//"Grammar Edition\tStart with Ctrl for word-by-word grammar." + "\r\n" +
//"Lite Edition\tStart with Shift for essential features only." + "\r\n" +
//"Research Edition\tStart with Ctrl+Shift for research methods." + "\r\n" +

public static class Globals
{
    // Software Version
    public static string VERSION = "5.0.6"; // updated by Version.bat (with AssemblyInfo.cs of all assemblies)
    public static Edition EDITION = Edition.Grammar;
    public static string SHORT_VERSION
    {
        get
        {
            if (EDITION == Edition.Standard)
            {
                return ("v" + VERSION + "");
            }
            else if (EDITION == Edition.Grammar)
            {
                return ("v" + VERSION + "G");
            }
            else if (EDITION == Edition.Lite)
            {
                return ("v" + VERSION + "L");
            }
            else if (EDITION == Edition.Research)
            {
                return ("v" + VERSION + "R");
            }
            else
            {
                return ("v" + VERSION + "!"); // Invalid Edition
            }
        }
    }
    public static string LONG_VERSION
    {
        get
        {
            return (VERSION + " - " + EDITION.ToString() + " Edition");
        }
    }

    // Global Variables
    public static string OUTPUT_FILE_EXT = ".csv"; // to open in Excel
    public static string DELIMITER = "\t";
    public static string SUB_DELIMITER = "|";
    public static string DATE_FORMAT = "yyyy-MM-dd";
    public static string TIME_FORMAT = "HH:mm:ss";
    public static string DATETIME_FORMAT = DATE_FORMAT + " " + TIME_FORMAT;
    public static string NUMBER_FORMAT = "000";
    public static int DEFAULT_FREQUENCY = 313; // Hz for WAVMaker

    // Global Folders
    public static string FONTS_FOLDER = "Fonts";
    public static string IMAGES_FOLDER = "Images";
    public static string DATA_FOLDER = "Data";
    public static string AUDIO_FOLDER = "Audio";
    public static string TRANSLATIONS_FOLDER = "Translations";
    public static string TAFSEERS_FOLDER = "Tafseers";
    public static string RULES_FOLDER = "Rules";
    public static string VALUES_FOLDER = "Values";
    public static string STATISTICS_FOLDER = "Statistics";
    public static string RESEARCH_FOLDER = "Research";
    public static string DRAWINGS_FOLDER = "Drawings";
    public static string BOOKMARKS_FOLDER = "Bookmarks";
    public static string HISTORY_FOLDER = "History";
    public static string HELP_FOLDER = "Help";
}
