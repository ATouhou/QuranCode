using System;

namespace Model
{
    public enum LanguageType { Arabic, Translation };

    public enum SelectionScope { Book, Chapter, Page, Station, Part, Group, Quarter, Bowing, Verse, Word, Letter };

    public enum FindScope { Book, Selection, Result };
    public enum FindType { Text, Similarity, Numbers, FrequencySum, Prostration, Revelation };

    public enum TextSearchType { Exact, Proximity, Root };
    public enum TextLocation { Anywhere, AtStart, AtMiddle, AtEnd, AllWords, AnyWord };
    public enum TextWordness { WholeWord, PartOfWord, Any };

    public enum SimilarityMethod { SimilarText, SimilarFirstHalf, SimilarLastHalf, SimilarWords, SimilarFirstWord, SimilarLastWord };
    public enum SimilaritySource { Verse, Book };

    public enum NumberSearchType { Words, Sentences, Verses, Chapters, WordRanges, VerseRanges, ChapterRanges };
    public enum FrequencySearchType { Words, Sentences, Verses };

    public enum RevelationPlace { None, Makkah, Medina, Both };

    public enum ProstrationType { None, Recommended, Obligatory, Both };

    public enum FrequencySumType { NoDuplicates, Duplicates };

    //                             1 + 0.618  0.618 + 1
    public enum GoldenRatioOrder { LongShort, ShortLong };
    public enum GoldenRatioScope { None, Letter, Word, Sentence };

    /// <summary>
    /// Source of letters used in building a numerology system dynamically
    /// </summary>
    public enum NumerologySystemScope { Book, Selection, HighlightedText };

    /// <summary>
    /// <para>    : None</para>
    /// <para>Laaa: MustContinue</para>
    /// <para>Sala: ShouldContinue</para>
    /// <para>Jeem: CanStop</para>
    /// <para>Dots: CanStopAtOneOnly</para>
    /// <para>Qala: ShouldStop</para>
    /// <para>Seen: MustPause</para>
    /// <para>Meem: MustStop</para>
    /// </summary>
    //                     None,   Stop0%,       Stop25%,      Stop50%, Stop50%AtEither,  Stop75%,    Pause100%, Stop100%
    public enum Stopmark { None, MustContinue, ShouldContinue, CanStop, CanStopAtOneOnly, ShouldStop, MustPause, MustStop };
    //MustPause Occurrences:
    //Stop  	١٨_١	بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ ٱلْحَمْدُ لِلَّهِ ٱلَّذِىٓ أَنزَلَ عَلَىٰ عَبْدِهِ ٱلْكِتَٰبَ وَلَمْ يَجْعَل لَّهُۥ عِوَجَا ۜ
    //Stop  	٣٦_٥٢	قَالُوا۟ يَٰوَيْلَنَا مَنۢ بَعَثَنَا مِن مَّرْقَدِنَا ۜ ۗ هَٰذَا مَا وَعَدَ ٱلرَّحْمَٰنُ وَصَدَقَ ٱلْمُرْسَلُونَ
    //Stop  	٦٩_٢٨	مَآ أَغْنَىٰ عَنِّى مَالِيَهْ ۜ
    //Continue 	٧٥_٢٧	وَقِيلَ مَنْ ۜ رَاقٍۢ
    //Continue 	٨٣_١٤	كَلَّا ۖ بَلْ ۜ رَانَ عَلَىٰ قُلُوبِهِم مَّا كَانُوا۟ يَكْسِبُونَ
}
