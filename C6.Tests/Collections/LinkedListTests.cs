using System.Collections.Generic;

using static C6.EventTypes;
using static C6.Speed;

using C6.Collections;
using C6.Tests.Helpers;
using NUnit.Framework;


namespace C6.Tests.Collections
{
    [TestFixture]
    public class LinkedListTests : TestBase
    {
    }


    [TestFixture]
    public class LinkedListViewTests : GeneralViewTest
    {
        protected override IList<T> GetEmptyList<T>(IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(equalityComparer, allowsNull);

        protected override IList<T> GetList<T>(IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new ArrayList<T>(enumerable, equalityComparer, allowsNull);
    }


    [TestFixture]
    public class LinkedListListTests : IListTests
    {
        protected override bool AllowsNull => true;
        protected override EventTypes ListenableEvents => All;

        protected override bool AllowsDuplicates => true;
        protected override bool DuplicatesByCounting => false;
        protected override bool IsFixedSize => false;
        protected override bool IsReadOnly => false;
        protected override Speed ContainsSpeed => Linear;
        protected override Speed IndexingSpeed => Linear;

        protected override IList<T> GetEmptyList<T>(IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(equalityComparer, allowsNull);

        protected override IList<T> GetList<T>(IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(enumerable, equalityComparer, allowsNull);        
    }
}
