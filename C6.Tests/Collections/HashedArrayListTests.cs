using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static C6.EventTypes;

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
    public class HashedArrayListListTests : IListenableTests {

        [Ignore("Not relevant")]
        public override void AllowsNull_AllowsNull_True()
        {
            base.AllowsNull_AllowsNull_True();
        }

        [Ignore("Not relevant")]
        public override void AllowsNull_EmptyCollectionAllowsNull_True()
        {
            base.AllowsNull_EmptyCollectionAllowsNull_True();
        }

        protected override EventTypes ListenableEvents => None; // Why All? -Up to us, could be changed to non-All for some tests
        protected override IListenable<T> GetEmptyListenable<T>(bool allowsNull = false) => new HashedArrayList<T>();
        protected override IListenable<T> GetListenable<T>(IEnumerable<T> enumerable, bool allowsNull = false) => new HashedArrayList<T>(enumerable);        
    }

    
}
