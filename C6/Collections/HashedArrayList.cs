using System;
using System.Linq;
using System.Text;
using SCG = System.Collections.Generic;
using SC = System.Collections;

namespace C6.Collections
{
    public class HashedArrayList<T> : SCG.IEnumerable<T>
    {
        #region Fields

        public static readonly T[] EmptyArray = new T[0];

        private T[] _items;
        private SCG.HashSet<KeyValuePair<T, int>> _itemIndex;
        #endregion

        #region Constructors

        public HashedArrayList()
            : this(0) { }

        public HashedArrayList(SCG.IEnumerable<T> items, SCG.IEqualityComparer<T> equalityComparer = null)
        {
            // allowsNull = false - by default !
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default; // ??? Default

            var collection = items as SCG.ICollection<T>;
            var collectionValues = items as ICollectionValue<T>;

            if (collectionValues != null) // ??? what is the idea with this check. It might not cast or what
            {
                _items = collectionValues.IsEmpty ? EmptyArray : collectionValues.ToArray();
                Count = collectionValues.Count;
            }
            else if (collection != null)
            {
                Count = collection.Count;
                _items = Count == 0 ? EmptyArray : new T[Count];
                collection.CopyTo(_items, 0);
            }
            else
            {
                _items = EmptyArray;
                // AddRange(items) ??? do we need it
            }
        }

        public HashedArrayList(int capacity = 0, SCG.IEqualityComparer<T> equalityComparer = null) // why 0 ???
        {
            Capacity = capacity;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default;
        }

        public HashedArrayList(SCG.IEqualityComparer<T> equalityComparer = null) 
            : this(0, equalityComparer) { }
                    
        #endregion

        public SCG.IEnumerator<T> GetEnumerator() // overriden one; base class CollectionValueBase
        {
            for (int i = 0; i < _items.Length; i++)
            {
                yield return _items[i];
            }            
        }

        SC.IEnumerator SC.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region Explicit implementations

        // already implemented?
        //SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Properties
        public int Capacity { get; private set; }

        public virtual int Count { get; protected set; } // to_base

        public virtual SCG.IEqualityComparer<T> EqualityComparer { get; } // ??? virtual
        #endregion
    }
}
