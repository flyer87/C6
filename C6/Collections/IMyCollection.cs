using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCG = System.Collections.Generic;

namespace C6.Collections
{
    class MyCollection<T> : IIndexed<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            throw new NotImplementedException();
        }

        public bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider)
        {
            throw new NotImplementedException();
        }

        public bool AllowsNull { get; }
        public ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities()
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            return Remove(item);
        }

        public bool Remove(T item, out T removedItem)
        {
            throw new NotImplementedException();
        }

        public bool RemoveDuplicates(T item)
        {
            throw new NotImplementedException();
        }

        public bool RemoveRange(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public bool RetainRange(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public ICollectionValue<T> UniqueItems()
        {
            throw new NotImplementedException();
        }

        public bool UnsequencedEquals(ICollection<T> otherCollection)
        {
            throw new NotImplementedException();
        }

        public bool Update(T item)
        {
            throw new NotImplementedException();
        }

        public bool Update(T item, out T oldItem)
        {
            throw new NotImplementedException();
        }

        public bool UpdateOrAdd(T item)
        {
            throw new NotImplementedException();
        }

        public bool UpdateOrAdd(T item, out T oldItem)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public Speed ContainsSpeed { get; }
        public int Count { get; }
        public Speed CountSpeed { get; }
        public bool IsEmpty { get; }
        public T Choose()
        {
            throw new NotImplementedException();
        }

        void SCG.ICollection<T>.Add(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            return Contains(item);
        }

        public bool ContainsRange(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        void SCG.ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int CountDuplicates(T item)
        {
            throw new NotImplementedException();
        }

        public bool Find(ref T item)
        {
            throw new NotImplementedException();
        }

        public ICollectionValue<T> FindDuplicates(T item)
        {
            throw new NotImplementedException();
        }

        public bool FindOrAdd(ref T item)
        {
            throw new NotImplementedException();
        }

        public int GetUnsequencedHashCode()
        {
            throw new NotImplementedException();
        }

        public T[] ToArray()
        {
            throw new NotImplementedException();
        }

        public bool IsValid { get; }
        public EventTypes ActiveEvents { get; }
        public EventTypes ListenableEvents { get; }
        public event EventHandler CollectionChanged;
        public event EventHandler<ClearedEventArgs> CollectionCleared;
        public event EventHandler<ItemAtEventArgs<T>> ItemInserted;
        public event EventHandler<ItemAtEventArgs<T>> ItemRemovedAt;
        public event EventHandler<ItemCountEventArgs<T>> ItemsAdded;
        public event EventHandler<ItemCountEventArgs<T>> ItemsRemoved;
        public bool AllowsDuplicates { get; }
        public bool DuplicatesByCounting { get; }
        public IEqualityComparer<T> EqualityComparer { get; }
        public bool IsFixedSize { get; }
        public bool IsReadOnly { get; }
        public bool Add(T item)
        {
            throw new NotImplementedException();
        }

        public bool AddRange(IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public EnumerationDirection Direction { get; }
        public IDirectedCollectionValue<T> Backwards()
        {
            throw new NotImplementedException();
        }

        public int GetSequencedHashCode()
        {
            throw new NotImplementedException();
        }

        public bool SequencedEquals(ISequenced<T> otherCollection)
        {
            throw new NotImplementedException();
        }

        public Speed IndexingSpeed { get; }

        public T this[int index]
        {
            get { throw new NotImplementedException(); }
        }

        public IDirectedCollectionValue<T> GetIndexRange(int startIndex, int count)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public int LastIndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public T RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void RemoveIndexRange(int startIndex, int count)
        {
            throw new NotImplementedException();
        }
    }
}
