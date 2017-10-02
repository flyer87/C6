using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C6.Collections;
using static C6.EventTypes;


namespace C6.Tests.Collections
{
    public class HashedLinkedListTests : IExtensibleTests
    {
        protected override bool AllowsNull => false;
        protected override EventTypes ListenableEvents => All;
        protected override bool AllowsDuplicates => false;
        protected override bool DuplicatesByCounting => true;
        protected override bool IsFixedSize => false;
        protected override bool IsReadOnly => false;

        protected override IExtensible<T> GetEmptyExtensible<T>(IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedLinkedList<T>(equalityComparer);

        protected override IExtensible<T> GetExtensible<T>(IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedLinkedList<T>(enumerable, equalityComparer);
    }
}
