using System;
using System.Linq;
using System.Text;
using SCG = System.Collections.Generic;

namespace C6.Collections
{
    public abstract class CollectionValueBaseH<T>
    {
        protected CollectionValueBaseH(int capacity, SCG.IEqualityComparer<T> itemEqualityComparer)
        {
            
        }
    }
}
