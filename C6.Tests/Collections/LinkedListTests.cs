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
    public class LinkedListListTests : IExtensibleTests
    {
        protected override bool AllowsNull => true;
        protected override EventTypes ListenableEvents => All;

        protected override bool AllowsDuplicates => true;
        protected override bool DuplicatesByCounting => false;
        protected override bool IsFixedSize => false;
        protected override bool IsReadOnly => false;

        protected override IExtensible<T> GetEmptyExtensible<T>(IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(equalityComparer, allowsNull);

        protected override IExtensible<T> GetExtensible<T>(IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(enumerable, equalityComparer, allowsNull);
    }
}
