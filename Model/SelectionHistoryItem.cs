using System;
using System.Collections.Generic;

namespace Model
{
    public class SelectionHistoryItem : Selection
    {
        public SelectionHistoryItem(Book book, SelectionScope scope, List<int> indexes)
            : base(book, scope, indexes)
        {
        }
    }
}
