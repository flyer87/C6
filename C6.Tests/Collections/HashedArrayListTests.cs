using System.Collections.Generic;

using static C6.EventTypes;
using static C6.Speed;

using C6.Collections;
using C6.Tests.Helpers;
using NUnit.Framework;


namespace C6.Tests.Collections
{
    [TestFixture]
    public class HashedArrayListTests :TestBase
    {
    }

    [TestFixture]
    public class HashedArrayListGeneralViewTests : GeneralViewTest
    {
        protected override IList<T> GetEmptyList<T>(IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedArrayList<T>(equalityComparer);

        protected override IList<T> GetList<T>(IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedArrayList<T>(enumerable, equalityComparer);
    }

    [TestFixture]
    public class HashedArrayListListTests : IListTests
    {
        protected override EventTypes ListenableEvents => All; // Why All? -Up to us, could be changed to non-All for some tests

        protected override bool AllowsNull => false;
        protected override bool AllowsDuplicates => false;
        protected override bool DuplicatesByCounting => true;
        protected override bool IsFixedSize => false;
        protected override bool IsReadOnly => false;
        protected override Speed ContainsSpeed => Constant;
        protected override Speed IndexingSpeed => Constant;

        protected override IList<T> GetEmptyList<T>(IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedArrayList<T>(equalityComparer);

        protected override IList<T> GetList<T>(IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedArrayList<T>(enumerable, equalityComparer);
    }
}
