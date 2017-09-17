using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class HashedArrayListListTests : IExtensibleTests
    {
        protected override EventTypes ListenableEvents => All; // Why All? -Up to us, could be changed to non-All for some tests

        protected override bool AllowsNull => false;
        protected override bool AllowsDuplicates => false;
        protected override bool DuplicatesByCounting => true;
        protected override bool IsFixedSize => false;
        protected override bool IsReadOnly => false;
        //protected override Speed ContainsSpeed => Constant;
        //protected override Speed IndexingSpeed => Constant;

        protected override IExtensible<T> GetEmptyExtensible<T>(IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedArrayList<T>(equalityComparer);

        protected override IExtensible<T> GetExtensible<T>(IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new HashedArrayList<T>(enumerable, equalityComparer);
    }
}
