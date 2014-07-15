using System;
using System.Collections.Generic;

namespace Model
{
    public struct NumberQuery
    {
        /// <summary>
        /// Word/verse/chapter number or Sum of word/verse/chapter numbers in a range query (W.., V.., C..)
        /// </summary>
        public int Number;
        public int ChapterCount;
        public int VerseCount;
        public int WordCount;
        public int LetterCount;
        public int UniqueLetterCount;
        public long Value;
        public int ValueDigitSum;
        public int ValueDigitalRoot;
        public NumberType NumberNumberType;
        public NumberType ChapterCountNumberType;
        public NumberType VerseCountNumberType;
        public NumberType WordCountNumberType;
        public NumberType LetterCountNumberType;
        public NumberType UniqueLetterCountNumberType;
        public NumberType ValueNumberType;
        public NumberType ValueDigitSumNumberType;
        public NumberType ValueDigitalRootNumberType;
        public ComparisonOperator NumberComparisonOperator;
        public ComparisonOperator ChapterCountComparisonOperator;
        public ComparisonOperator VerseCountComparisonOperator;
        public ComparisonOperator WordCountComparisonOperator;
        public ComparisonOperator LetterCountComparisonOperator;
        public ComparisonOperator UniqueLetterCountComparisonOperator;
        public ComparisonOperator ValueComparisonOperator;
        public ComparisonOperator ValueDigitSumComparisonOperator;
        public ComparisonOperator ValueDigitalRootComparisonOperator;

        public bool IsEmpty(NumberSearchType numbers_result_type)
        {
            bool is_range_empty = true;
            if (numbers_result_type == NumberSearchType.WordRanges)
            {
                is_range_empty = (WordCount < 2);
            }
            else if (numbers_result_type == NumberSearchType.VerseRanges)
            {
                is_range_empty = (VerseCount < 2);
            }
            else if (numbers_result_type == NumberSearchType.ChapterRanges)
            {
                is_range_empty = (ChapterCount < 2);
            }

            return
            (
                (Number == 0) &&
                is_range_empty &&
                (WordCount == 0) &&
                (LetterCount == 0) &&
                (UniqueLetterCount == 0) &&
                (Value == 0) &&
                (ValueDigitSum == 0) &&
                (ValueDigitalRoot == 0) &&
                (NumberNumberType == NumberType.None) &&
                (ChapterCountNumberType == NumberType.None) &&
                (VerseCountNumberType == NumberType.None) &&
                (WordCountNumberType == NumberType.None) &&
                (LetterCountNumberType == NumberType.None) &&
                (UniqueLetterCountNumberType == NumberType.None) &&
                (ValueNumberType == NumberType.None) &&
                (ValueDigitSumNumberType == NumberType.None) &&
                (ValueDigitalRootNumberType == NumberType.None)
           );
        }
    }
}
