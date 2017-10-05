using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C6.Collections;
using static C6.EventTypes;
using static C6.Speed;


namespace C6.Tests.Collections
{
    public class HashedLinkedListTests : IIndexedTests
    {
        protected override bool AllowsNull => false;
        protected override EventTypes ListenableEvents => All;
        protected override bool AllowsDuplicates => false;
        protected override bool DuplicatesByCounting => true;
        protected override bool IsFixedSize => false;
        protected override bool IsReadOnly => false;
        protected override Speed ContainsSpeed => Constant;
        protected override Speed IndexingSpeed => Linear;

        protected override IIndexed<T> GetEmptyIndexed<T>(IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedLinkedList<T>(equalityComparer);

        protected override IIndexed<T> GetIndexed<T>(IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedLinkedList<T>(enumerable, equalityComparer);        
     }
}
