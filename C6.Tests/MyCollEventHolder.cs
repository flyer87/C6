using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C6.Tests.Helpers;


namespace C6.Tests
{
    public class MyCollEventHolder<T>
    {
        private readonly CollectionEvent<T>[] _events;

        public MyCollEventHolder(CollectionEvent<T>[] ce)
        {
            _events = ce;
        }

        public CollectionEventConstraint<T> For(IListenable<T> collection)
        {
            return new CollectionEventConstraint<T>(collection, _events);
        }
    }
}
