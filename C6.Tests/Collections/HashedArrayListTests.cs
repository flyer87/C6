using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class HashedArrayListListTests : ICollectionValueTests {

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

        protected override ICollectionValue<T> GetEmptyCollectionValue<T>(bool allowsNull = false) => new HashedArrayList<T>();
        
        protected override ICollectionValue<T> GetCollectionValue<T>(IEnumerable<T> enumerable, bool allowsNull = false) => new HashedArrayList<T>(enumerable);
    }

    
}
