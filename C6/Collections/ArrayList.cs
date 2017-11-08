// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

using C6.Contracts;

using static System.Diagnostics.Contracts.Contract;

using static C6.Collections.ExceptionMessages;
using static C6.Contracts.ContractMessage;
using static C6.EventTypes;
using static C6.Speed;

using SC = System.Collections;
using SCG = System.Collections.Generic;

namespace C6.Collections
{
    /// <summary>
    ///     Represents a generic list whose items that can be accessed efficiently by index.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the items in the collection.
    /// </typeparam>
    /// <remarks>
    ///     <para>
    ///         <see cref="ArrayList{T}"/> uses an internal array whose size is dynamically increased as required. Item access
    ///         by index takes constant time. Items are added to the end of the list in amortized constant time, but insertion
    ///         of one or more items takes time proportional to the number of items that must be moved to make room for the new
    ///         item(s). The collection allows duplicates and stores them explicitly.
    ///     </para>
    ///     <para>
    ///         Changing the state of an item while it is stored in an <see cref="ArrayList{T}"/> does not affect the
    ///         <see cref="ArrayList{T}"/>. It might, however, affect any accessible <see cref="ICollectionValue{T}"/> returned
    ///         from the collection, if that <see cref="ICollectionValue{T}"/> relies on the state of the item, e.g. when the
    ///         change affects the item's hash code while unique items are enumerated.
    ///     </para>
    /// </remarks>
    [Serializable]
    [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
    public class ArrayList<T> : CollectionValueBase<T>, IList<T>, IStack<T>
    {        
        #region Fields

        public static readonly T[] EmptyArray = new T[0];

        private const int MinArrayLength = 0x00000004;
        private const int MaxArrayLength = 0x7FEFFFFF;

        private T[] _items;
        // new 
        private ArrayList<T> _underlying;
        private WeakViewList<ArrayList<T>> _views;
        private WeakViewList<CollectionValueBase<T>> _collValues = new WeakViewList<CollectionValueBase<T>>(); // Why CollectionValueBase, but not ICollectionValue
        private WeakViewList<ArrayList<T>>.Node _myWeakReference;
        private int _offsetField;
        private int _size;
        // -new         

        private int _version, _sequencedHashCodeVersion = -1, _unsequencedHashCodeVersion = -1;
        private int _sequencedHashCode, _unsequencedHashCode;

        private event EventHandler _collectionChanged;
        private event EventHandler<ClearedEventArgs> _collectionCleared;
        private event EventHandler<ItemAtEventArgs<T>> _itemInserted, _itemRemovedAt;
        private event EventHandler<ItemCountEventArgs<T>> _itemsAdded, _itemsRemoved;

        #endregion

        #region Code Contracts

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            // ReSharper disable InvocationIsSkipped

            // Array is non-null
            Invariant(_items != null);

            // Count is not bigger than the capacity
            Invariant(Count <= Capacity);

            // All items must be non-null if collection disallows null values
            Invariant(AllowsNull || ForAll(this, item => item != null));

            // The unused part of the array contains default values
            Invariant(ForAll(Count, Capacity, i => Equals(_items[i], default(T))));

            // Equality comparer is non-null
            Invariant(EqualityComparer != null);

            // Empty array is always empty
            Invariant(EmptyArray.IsEmpty());

            #region View invarints 

            Invariant(UnderlyingCount <= Capacity);

            Invariant(Offset + Count <= UnderlyingCount);

            // Offset is non-negative
            Invariant(Offset >= 0);

            // TODO: If there are views all should the same underlying(???) _items             
            Invariant((_underlying ?? this)._views == null || ForAll((_underlying ?? this)._views, view => view._items == (_underlying ?? this)._items));

            #endregion

            // ReSharper restore InvocationIsSkipped
        }

        #endregion

        #region Constructors        

        //public ArrayList()
        //{

        //}

        public ArrayList(SCG.IEnumerable<T> items, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false) 
            : base(allowsNull)
        {
            #region Code Contracts

            // ReSharper disable InvocationIsSkipped

            // Argument must be non-null
            Requires(items != null, ArgumentMustBeNonNull);
                                    
            // All items must be non-null if collection disallows null values
            Requires(allowsNull || ForAll(items, item => item != null), ItemsMustBeNonNull);

            // Value types cannot be null
            Requires(!typeof(T).IsValueType || !allowsNull, AllowsNullMustBeFalseForValueTypes);

            // The specified enumerable is not equal to the array saved
            Ensures(!ReferenceEquals(items, _items));

            // ReSharper restore InvocationIsSkipped

            #endregion
            
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default;

            var collectionValue = items as ICollectionValue<T>;
            var collection = items as SCG.ICollection<T>;

            // Use ToArray() for ICollectionValue<T>
            if (collectionValue != null) 
            {
                _items = collectionValue.IsEmpty ? EmptyArray : collectionValue.ToArray();
                Count = Capacity;
            }
            // Use CopyTo() for ICollection<T>
            else if (collection != null)
            {
                Count = collection.Count;
                _items = Count == 0 ? EmptyArray : new T[Count];
                collection.CopyTo(_items, 0);
            }
            else
            {
                _items = EmptyArray;
                AddRange(items);
            }
        }

        public ArrayList(int capacity = 0, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false) 
            : base(allowsNull)            
        {
            #region Code Contracts

            // ReSharper disable InvocationIsSkipped

            // Argument must be non-negative
            Requires(0 <= capacity, ArgumentMustBeNonNegative);

            // Value types cannot be null
            Requires(!typeof(T).IsValueType || !allowsNull, AllowsNullMustBeFalseForValueTypes);

            // ReSharper restore InvocationIsSkipped

            #endregion
            
            Capacity = capacity;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default;
        }

        #endregion

        #region Properties        

        public virtual EventTypes ActiveEvents { get; private set; }

        public virtual bool AllowsDuplicates => true;

        /// <summary>
        ///     Gets or sets the total number of items the internal data structure can hold without resizing.
        /// </summary>
        /// <value>
        ///     The number of items that the <see cref="ArrayList{T}"/> can contain before resizing is required.
        /// </value>
        /// <remarks>
        ///     <para>
        ///         <see cref="Capacity"/> is the number of items that the <see cref="ArrayList{T}"/> can store before resizing is
        ///         required, whereas <see cref="ICollectionValue{T}.Count"/> is the number of items that are actually in the
        ///         <see cref="ArrayList{T}"/>.
        ///     </para>
        ///     <para>
        ///         If the capacity is significantly larger than the count and you want to reduce the memory used by the
        ///         <see cref="ArrayList{T}"/>, you can decrease capacity by calling the <see cref="TrimExcess"/> method or by
        ///         setting the <see cref="Capacity"/> property explicitly to a lower value. When the value of
        ///         <see cref="Capacity"/> is set explicitly, the internal data structure is also reallocated to accommodate the
        ///         specified capacity, and all the items are copied.
        ///     </para>
        /// </remarks>
        public int Capacity
        {
            get { return _items.Length; }
            set
            {
                #region Code Contracts

                // Capacity must be at least as big as the number of items
                Requires(value >= Count);

                // Capacity is at least as big as the number of items
                Ensures(value >= Count);

                Ensures(Capacity == value);

                #endregion

                if (value > 0)
                {
                    if (value == _items.Length)
                    {
                        return;
                    }

                    Array.Resize(ref (_underlying ?? this)._items, value);
                    if (_views == null)
                    {
                        return;
                    }
                    foreach (var v in _views)
                        v._items = (_underlying ?? this)._items;
                }
                else
                {
                    _items = EmptyArray;
                }
            }
        }

        public virtual Speed ContainsSpeed => Linear;

        public override Speed CountSpeed => Constant;

        public virtual EnumerationDirection Direction => EnumerationDirection.Forwards;
           
        public virtual bool DuplicatesByCounting => false;

        public virtual SCG.IEqualityComparer<T> EqualityComparer { get; }        

        public virtual T First => _items[_offsetField];

        public virtual Speed IndexingSpeed => Constant;

        public virtual bool IsFixedSize => false;

        public virtual bool IsReadOnly => false;

        public virtual T Last => _items[ _offsetField + Count - 1];

        public virtual EventTypes ListenableEvents => All;

        public virtual T this[int index]
        {
            get { return _items[_offsetField + index]; }
            set {
                #region Code Contracts

                // The version is updated
                Ensures(_version != OldValue(_version));

                #endregion

                UpdateVersion();

                index += _offsetField;
                var oldItem = _items[index];
                _items[index] = value;

                (_underlying ?? this).RaiseForIndexSetter(oldItem, value, index);
            }
        }
        
        public virtual int Offset => _offsetField;        

        /// <summary>
        /// Null if this list is not a view.
        /// </summary>
        /// <value>Underlying list for view.</value>        
        public virtual IList<T> Underlying => _underlying;
                
        #endregion

        private int UnderlyingCount => (Underlying ?? this).Count;

        #region Public Methods

        public override T[] ToArray()
        {
            var array = new T[Count];
            CopyTo(array, 0);
            return array;
        }

        public virtual bool Add(T item)
        {
            #region Code Contracts            

            // The version is updated            
            Ensures(_version != OldValue(_version));            
            #endregion

            InsertPrivate(Count, item);            
            (_underlying ?? this).RaiseForAdd(item); //RaiseForAdd(item); // we raise the event on the proper list, ya?!
            // InvalidateCollectionValuesPrivate();
            return true;
        }

        private void InvalidateCollectionValuesPrivate()
        {
            if (_collValues == null) return;            

            foreach (var value in _collValues)
            {
                value.IsValid = false;                
            }

            _collValues.Clear(); // new!!!

            // Should we dispose value by value? Coz Views are disposed!
            // Should we implement IDisposable            
        }

        public virtual bool AddRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts            
            // If collection changes, the version is updated
            // !@ Ensures(this.IsSameSequenceAs(OldValue((_underlying ?? this).ToArray())) || _version != OldValue(_version));

            #endregion

            // TODO: Handle ICollectionValue<T> and ICollection<T>

            // TODO: Avoid creating an array? Requires a lot of extra code, since we need to properly handle items already added from a bad enumerable
            // A bad enumerator will throw an exception here            
            var array = items.ToArray();
            if (array.IsEmpty())
            {
                return false;
            }

            InsertRangePrivate(Count, array);

            (_underlying ?? this).RaiseForAddRange(array);
            return true;
        }

        // Only creates one Range instead of two as with GetIndexRange(0, Count).Backwards()
        public virtual IDirectedCollectionValue<T> Backwards()
        {            
            return new Range(this, Count - 1, Count, EnumerationDirection.Backwards);
        }

        public override T Choose() => Last;
        
        public virtual void Clear()
        {
            #region Code Contracts            
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));
            #endregion

            if (IsEmpty)
            {
                return;
            }

            // if it is called on a view
            if (_underlying != null)
            {
                RemoveIndexRange(0, Count);
                return;
            }

            // Only update version if the collection is actually cleared
            UpdateVersion();

            var oldCount = Count;
            FixViewsBeforeRemovePrivate(0, Count); // this is time-consuming, no need?
            ClearPrivate();
            (_underlying ?? this).RaiseForClear(oldCount);
        }

        public virtual bool Contains(T item) => IndexOf(item) >= 0;

        public virtual bool ContainsRange(SCG.IEnumerable<T> items)
        {            
            // ToDo: .toArray()
            if (items.IsEmpty())
            {
                return true;
            }

            if (IsEmpty)
            {
                return false;
            }

            // TODO: Replace ArrayList<T> with more efficient data structure like HashBag<T>
            var itemsToContain = new ArrayList<T>(items, EqualityComparer, AllowsNull);

            if (itemsToContain.Count > Count)
            {
                return false;
            }

            return this.Any(item => itemsToContain.Remove(item) && itemsToContain.IsEmpty);
        }
        
        public override void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, _offsetField, array, arrayIndex, Count);
        }

        // Explicitly check against null to avoid using the (slower) equality comparer
        public virtual int CountDuplicates(T item)
        {            
            return item == null ? this.Count(x => x == null) : this.Count(x => Equals(x, item));
        }

        public virtual bool Find(ref T item)
        {            
            var index = IndexOf(item);

            if (index < 0)
            {
                return false;
            }

            index += _offsetField;
            item = _items[index];
            return true;
        }

        public virtual ICollectionValue<T> FindDuplicates(T item)
        {            
            var duplicates = new Duplicates(this, item);            
            _collValues.Add(duplicates); // ???  so far, no. Intsead of UpdateVersi() _collValues could be used, but ...

            return duplicates;
        }

        public virtual bool FindOrAdd(ref T item)
        {
            #region Code Contracts            
            // If collection changes, the version is updated            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));
            #endregion

            if (Find(ref item))
            {
                return true;
            }

            // Let Add handle version update and events
            Add(item);

            return false;
        }

        public override SCG.IEnumerator<T> GetEnumerator()
        {
            #region Code Contracts                      

            // The version is not updated
            Ensures(_version == OldValue(_version));            

            #endregion

            var version = _version;
            // Check version at each call to MoveNext() to ensure an exception is thrown even when the enumerator was really finished
            // for (var i = 0; CheckVersion(version) & i < Count; i++)
            for (var i = Offset; CheckVersion(version) && i < Offset + Count; i++)
            {
                yield return _items[i];
            }
        }

        public virtual IDirectedCollectionValue<T> GetIndexRange(int startIndex, int count)
        {            
            return new Range(this, startIndex, count, EnumerationDirection.Forwards);
        }

        // TODO: Update hash code when items are added, if the hash code version is not equal to -1
        public virtual int GetSequencedHashCode()
        {
            if (_sequencedHashCodeVersion != _version)
            {
                _sequencedHashCodeVersion = _version;
                _sequencedHashCode = this.GetSequencedHashCode(EqualityComparer);
            }

            return _sequencedHashCode;
        }

        // TODO: Update hash code when items are added, if the hash code version is not equal to -1
        public virtual int GetUnsequencedHashCode()
        {
            if (_unsequencedHashCodeVersion != _version)
            {
                _unsequencedHashCodeVersion = _version;
                _unsequencedHashCode = this.GetUnsequencedHashCode(EqualityComparer);
            }

            return _unsequencedHashCode;
        }

        [Pure]
        public virtual int IndexOf(T item)
        {
            #region Code Contracts                        

            // TODO: Add contract to IList<T>.IndexOf
            // Result is a valid index
            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : ~Result<int>() == Count);

            // Item at index is the first equal to item                        
            Ensures(Result<int>() < 0 || !this.Take(Result<int>()).Contains(item, EqualityComparer) && EqualityComparer.Equals(item, this.ElementAt(Result<int>())));

            #endregion

            if (item == null)
            {
                for (var i = 0; i < Count; i++)
                {
                    // Explicitly check against null to avoid using the (slower) equality comparer
                    if (_items[_offsetField + i] == null)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (var i = 0; i < Count; i++)
                {
                    if (Equals(item, _items[_offsetField + i]))
                    {
                        return i;
                    }
                }
            }

            return ~Count;
        }

        public virtual void Insert(int index, T item)
        {
            #region Code Contracts            

            // If collection changes, the version is updated
            //Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            InsertPrivate(index, item);
            (_underlying ?? this).RaiseForInsert(Offset + index, item);
        }

        public virtual void InsertFirst(T item) => Insert(0, item); // Offset ? No!

        public virtual void InsertLast(T item) => Insert(Count, item); // Offset ? No!

        public virtual void InsertRange(int index, SCG.IEnumerable<T> items)
        {
            #region Code Contracts                       

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            // TODO: Handle ICollectionValue<T> and ICollection<T>

            // TODO: Avoid creating an array? Requires a lot of extra code, since we need to properly handle items already added from a bad enumerable
            // A bad enumerator will throw an exception here
            var array = items.ToArray();

            if (array.IsEmpty())
            {
                return;
            }

            InsertRangePrivate(index, array);
            (_underlying ?? this).RaiseForInsertRange(index, array);
        }

        // !!! 
        public virtual bool IsSorted() => IsSorted(SCG.Comparer<T>.Default.Compare);

        public virtual bool IsSorted(Comparison<T> comparison)
        {            
            if (Count <= 1)
            {
                return true;
            }

            // TODO: Can we check that comparison doesn't alter the collection?
            for (var i = _offsetField + 1; i < Count + _offsetField; i++)
            {
                if (comparison(_items[i - 1], _items[i]) > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public virtual bool IsSorted(SCG.IComparer<T> comparer) => IsSorted((comparer ?? SCG.Comparer<T>.Default).Compare);

        // TODO: Defer execution
        public virtual ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities()
        {
            throw new NotImplementedException(); // ???
        }

        public virtual int LastIndexOf(T item)
        {
            #region Code Contracts

            // TODO: Add contract to IList<T>.LastIndexOf
            // Result is a valid index                                    
            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : ~Result<int>() == Count);

            // Item at index is the first equal to item
            // !@ Ensures(Result<int>() < 0 || !this.Skip(Result<int>() + 1).Contains(item, EqualityComparer) && EqualityComparer.Equals(item, this.ElementAt(Result<int>())));

            #endregion

            if (item == null)
            {
                for (var i = Count - 1; i >= 0; i--)
                {
                    // Explicitly check against null to avoid using the (slower) equality comparer
                    if (_items[_offsetField + i] == null)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = Count - 1; i >= 0; i--)
                {
                    if (Equals(item, _items[_offsetField + i]))
                    {
                        return i;
                    }
                }
            }

            return ~Count;
        }

        public virtual T Pop() => RemoveLast();

        public virtual void Push(T item) => InsertLast(item);

        public virtual bool Remove(T item)
        {
            #region Code Contracts                        

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            T removedItem;
            return Remove(item, out removedItem);
        }

        public virtual bool Remove(T item, out T removedItem)
        {
            #region Code Contracts            
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            removedItem = default(T);
            if (Count <= 0)
            {
                return false;
            }

            // Remove last instance of item, since this moves the fewest items
            int index;
            if ((index = LastIndexOf(item)) < 0)
            {
                return false;
            }

            removedItem = RemoveAtPrivate(index);
            (_underlying ?? this).RaiseForRemove(removedItem);
            return true;
        }

        public virtual T RemoveAt(int index)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            //var old = OldValue(this.ToArray());            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            var item = RemoveAtPrivate(index);
            (_underlying ?? this).RaiseForRemovedAt(item, Offset + index);
            return item;
        }

        public void DoSomething() {}

        // Explicitly check against null to avoid using the (slower) equality comparer
        public virtual bool RemoveDuplicates(T item) => item == null ? RemoveAllWhere(x => x == null) : RemoveAllWhere(x => Equals(item, x));

        public virtual T RemoveFirst() => RemoveAt(0); 

        public virtual void RemoveIndexRange(int startIndex, int count)
        {
            #region Code Contracts
                       
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (count == 0)
            {
                return;
            }

            // Only update version if item is actually removed
            UpdateVersion();
            
            startIndex += _offsetField;
            FixViewsBeforeRemovePrivate(startIndex, count);            
            
            if ((UnderlyingCount - count) > startIndex)
            {
                Array.Copy(_items, startIndex + count, _items, startIndex, UnderlyingCount - count - startIndex);
            }

            // new
            Count -= count;
            if (_underlying != null)
            {
                _underlying.Count -= count;
            }
                        
            Array.Clear(_items, UnderlyingCount, count);

            (_underlying ?? this).RaiseForRemoveIndexRange(startIndex, count);
        }

        public virtual T RemoveLast() => RemoveAt(Count - 1);

        public virtual bool RemoveRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts            
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            // toArray ???
            if (IsEmpty || items.IsEmpty())
            {
                return false;
            }

            // TODO: Replace ArrayList<T> with more efficient data structure like HashBag<T>
            var itemsToRemove = new ArrayList<T>(items, EqualityComparer, AllowsNull);
            return RemoveAllWhere(item => itemsToRemove.Remove(item));
        }

        public virtual bool RetainRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts            
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (IsEmpty)
            {
                return false;
            }

            if (items.IsEmpty())
            {
                // Optimize call, if no items should be retained
                UpdateVersion();
                var itemsRemoved = this; // for views ??? Use "this"
                ClearPrivate();
                RaiseForRemoveAllWhere(itemsRemoved);
                return true;
            }

            // TODO: Replace ArrayList<T> with more efficient data structure like HashBag<T>
            var itemsToRemove = new ArrayList<T>(items, EqualityComparer, AllowsNull);
            return RemoveAllWhere(item => !itemsToRemove.Remove(item));
        }

        public virtual void Reverse()
        {
            #region Code Contracts            
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Count <= 1)
            {
                return;
            }

            // Only update version if the collection is actually reversed
            UpdateVersion();

            Array.Reverse(_items, _offsetField, Count);
            //TODO: be more forgiving wrt. disposing ???
            DisposeOverlappingViewsPrivate(true);
            (_underlying ?? this).RaiseForReverse();
        }

        public virtual bool SequencedEquals(ISequenced<T> otherCollection)
        {            
            return this.SequencedEquals(otherCollection, EqualityComparer);
        }

        public virtual void Shuffle() => Shuffle(new Random());

        public virtual void Shuffle(Random random)
        {
            #region Code Contracts            
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Count <= 1)
            {
                return;
            }

            // Only update version if the collection is shuffled
            UpdateVersion();

            _items.Shuffle(_offsetField, Count, random);
            DisposeOverlappingViewsPrivate(false);
            (_underlying ?? this).RaiseForShuffle();
        }

        public virtual void Sort() => Sort((SCG.IComparer<T>)null);

        // TODO: It seems that Array.Sort(T[], Comparison<T>) is the only method that takes an Comparison<T>, not allowing us to set bounds on the sorting
        public virtual void Sort(Comparison<T> comparison) => Sort(comparison.ToComparer());

        public virtual void Sort(SCG.IComparer<T> comparer)
        {
            #region Code Contracts
            
            // If collection changes, the version is updated            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (comparer == null)
            {
                comparer = SCG.Comparer<T>.Default;
            }

            if (IsSorted(comparer))
            {
                return;
            }

            // Only update version if the collection is actually sorted
            UpdateVersion();
            Array.Sort(_items, _offsetField, Count, comparer);
            DisposeOverlappingViewsPrivate(false);
            (_underlying ?? this).RaiseForSort();
        }

        public override string ToString()
        {
            #region Code Contracts
            Requires(IsValid, ListOrViewMustBeValid);
            #endregion            

            return ToString(null, null);
        }
        
        /// <summary>
        ///     Sets the capacity to the actual number of items in the <see cref="ArrayList{T}"/>, if that number is less than a
        ///     threshold value.
        /// </summary>
        /// <remarks>
        ///     This method can be used to minimize a collection's memory overhead if no new items will be added to the collection.
        ///     The cost of reallocating and copying a large <see cref="ArrayList{T}"/>
        ///     can be considerable, however, so the <see cref="TrimExcess"/> method does nothing if the list is at more than 90
        ///     percent of capacity. This avoids incurring a large reallocation cost for a relatively small gain. The current
        ///     threshold of 90 percent might change in future releases.
        /// </remarks>
        public virtual void TrimExcess()
        {
            // local method. ??? IsValid            

            if (Capacity * 0.9 <= Count)
            {
                return;
            }
            Capacity = Count;
        }

        public virtual ICollectionValue<T> UniqueItems()
        {            
            return new ItemSet(this);
        }

        public virtual bool UnsequencedEquals(ICollection<T> otherCollection)
        {            
            return this.UnsequencedEquals(otherCollection, EqualityComparer);
        }

        public virtual bool Update(T item)
        {
            #region Code Contracts                        

            // If collection changes, the version is updated            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            T oldItem;
            return Update(item, out oldItem);
        }

        public virtual bool Update(T item, out T oldItem)
        {
            #region Code Contracts                        

            // If collection changes, the version is updated            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));
            #endregion

            var index = IndexOf(item);

            if (index >= 0)
            {
                // Only update version if item is actually updated
                UpdateVersion();

                oldItem = _items[_offsetField + index];
                _items[_offsetField + index] = item;

                (_underlying ?? this).RaiseForUpdate(item, oldItem);

                return true;
            }

            oldItem = default(T);
            return false;
        }

        public virtual bool UpdateOrAdd(T item)
        {
            #region Code Contracts

            // The version is updated
            Ensures(_version != OldValue(_version));

            #endregion

            T oldItem;
            return UpdateOrAdd(item, out oldItem);
        }

        public virtual bool UpdateOrAdd(T item, out T oldItem)
        {
            #region Code Contracts                                  
            // The version is updated
            Ensures(_version != OldValue(_version));            
            #endregion

            if (Update(item, out oldItem))
            {
                return true;
            }

            Add(item);
            return false;
        }

        public virtual IList<T> View(int index, int count)
        {            
            if (_views == null)
                _views = new WeakViewList<ArrayList<T>>();

            var view = (ArrayList<T>)MemberwiseClone();
            view._underlying = _underlying ?? this;
            view._offsetField = _offsetField + index;
            view.Count = count;            
                        
            view._myWeakReference = _views.Add(view);
            return view;
        }

        /// <summary>
        /// Create a list view on this list containing the (first) occurrence of a particular item.
        /// <para>Returns <code>null</code> if the item is not in this list.</para>
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The new list view.</returns>
        public virtual IList<T> ViewOf(T item)
        {                                    
            var index = IndexOf(item); // ??? IndexOf or indexOf
            return index < 0 ? null : View(index, 1);
        }

        /// <summary>
        /// Create a list view on this list containing the last occurrence of a particular item. 
        /// <para>Returns <code>null</code> if the item is not in this list.</para>
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The new list view.</returns>
        public virtual IList<T> LastViewOf(T item)
        {
            //var index = lastIndexOf(item); // ??? calling private method
            var index = LastIndexOf(item); 
            return index < 0 ? null : View(index, 1);
        }

        public virtual IList<T> Slide(int offset)
        {
            TrySlide(offset, Count);
            return this;
        }
               
        public virtual IList<T> Slide(int offset, int count)
        {
            TrySlide(offset, count);
            return this;
        }
               
        public virtual bool TrySlide(int offset)
        {
            return TrySlide(offset, Count);
        }
               
        public virtual bool TrySlide(int offset, int count)
        {
            // check the indices
            var newOffset = Offset + offset;                        
            if (newOffset < 0 || count < 0 || newOffset + count > Underlying.Count ) 
            {
                return false;
            }

            UpdateVersion();
            
            _offsetField = newOffset;
            Count = count;

            return true;
        }

        public virtual IList<T> Span(IList<T> other)
        {
            if (other.Offset + other.Count - Offset < 0) return null;


            return (_underlying ?? this).View(Offset, other.Offset + other.Count - Offset);
        }

        public virtual void Dispose()
        {
            Dispose(false);
        }

        public string Print()
        {
            //if (!IsValid)
            //{
            //    throw new NotSupportedException();
            //}
            //else
            //{
            //    throw new NotImplementedException();
            //}
            Requires(IsValid);

            var s = "";
            for (var i = _offsetField; i < Count; i++)
            {
                s += _items[i] + ", ";
            }
            return s;
        }

        #endregion

        #region Events

        public virtual event EventHandler CollectionChanged
        {
            add
            {
                _collectionChanged += value;
                ActiveEvents |= Changed;
            }
            remove
            {
                _collectionChanged -= value;
                if (_collectionChanged == null)
                {
                    ActiveEvents &= ~Changed;
                }
            }
        }

        public virtual event EventHandler<ClearedEventArgs> CollectionCleared
        {
            add
            {
                _collectionCleared += value;
                ActiveEvents |= Cleared;
            }
            remove
            {
                _collectionCleared -= value;
                if (_collectionCleared == null)
                {
                    ActiveEvents &= ~Cleared;
                }
            }
        }

        public virtual event EventHandler<ItemAtEventArgs<T>> ItemInserted
        {
            add
            {
                _itemInserted += value;
                ActiveEvents |= Inserted;
            }
            remove
            {
                _itemInserted -= value;
                if (_itemInserted == null)
                {
                    ActiveEvents &= ~Inserted;
                }
            }
        }

        public virtual event EventHandler<ItemAtEventArgs<T>> ItemRemovedAt
        {
            add
            {
                _itemRemovedAt += value;
                ActiveEvents |= RemovedAt;
            }
            remove
            {
                _itemRemovedAt -= value;
                if (_itemRemovedAt == null)
                {
                    ActiveEvents &= ~RemovedAt;
                }
            }
        }

        public virtual event EventHandler<ItemCountEventArgs<T>> ItemsAdded
        {
            add
            {
                _itemsAdded += value;
                ActiveEvents |= Added;
            }
            remove
            {
                _itemsAdded -= value;
                if (_itemsAdded == null)
                {
                    ActiveEvents &= ~Added;
                }
            }
        }

        public virtual event EventHandler<ItemCountEventArgs<T>> ItemsRemoved
        {
            add
            {
                _itemsRemoved += value;
                ActiveEvents |= Removed;
            }
            remove
            {
                _itemsRemoved -= value;
                if (_itemsRemoved == null)
                {
                    ActiveEvents &= ~Removed;
                }
            }
        }

        #endregion

        #region Explicit Implementations

        bool SC.ICollection.IsSynchronized => false;

        object SC.ICollection.SyncRoot { get; } = new object();

        object SC.IList.this[int index]
        {
            get { return this[index]; }
            set
            {
                try
                {
                    this[index] = (T)value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
                }
            }
        }

        int SC.IList.Add(object value)
        {
            try
            {
                return Add((T)value) ? Count - 1 : -1;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
            }
        }

        void SCG.ICollection<T>.Add(T item) => Add(item);

        bool SC.IList.Contains(object value) => IsCompatibleObject(value) && Contains((T)value);

        void SC.ICollection.CopyTo(Array array, int index)
        {
            try
            {
                Array.Copy(_items, 0, array, index, Count);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Target array type is not compatible with the type of items in the collection.");
            }
        }

        SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        int SC.IList.IndexOf(object value) => IsCompatibleObject(value) ? Math.Max(-1, IndexOf((T)value)) : -1;

        // Explicit implemDentation is needed, since C6.IList<T>.IndexOf(T) breaks SCG.IList<T>.IndexOf(T)'s precondition: Result<T>() >= -1
        int SCG.IList<T>.IndexOf(T item) => Math.Max(-1, IndexOf(item));

        void SC.IList.Insert(int index, object value)
        {
            try
            {
                Insert(index, (T)value);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
            }
        }

        void SC.IList.Remove(object value)
        {
            if (IsCompatibleObject(value))
            {
                Remove((T)value);
            }
        }

        void SC.IList.RemoveAt(int index) => RemoveAt(index);

        void SCG.IList<T>.RemoveAt(int index) => RemoveAt(index);

        #endregion

        #region Private Members

        private void Dispose(bool disposingUnderlying)
        {
            if (!IsValid)
            {
                return;
            }

            if (_underlying != null) // view
            {
                IsValid = false;
                if (!disposingUnderlying && _views != null) // the purpose of disposingUnderlying
                    _views.Remove(_myWeakReference);
                _underlying = null;
                _views = null; // shared ref. for _view! Does this set other views to null ??? No!
                // only the current view's field (_view) starts to point to null.
                _myWeakReference = null;
            }
            else // proper list
            {
                //isValid = false;
                if (_views != null)
                    foreach (var view in _views)
                        view.Dispose(true); // How can we assure that the nodes are deleted?
                Clear();
            }
        }

        /*private bool RequireValidity()
        {
            Requires(IsValid, ListOrViewMustBeValid);
            return true;
        }*/

        private bool CheckVersion(int version)
        {
            if (version == _version)
            {
                return true;
            }

            // See https://msdn.microsoft.com/library/system.collections.ienumerator.movenext.aspx
            throw new InvalidOperationException(CollectionWasModified);
        }

        private int indexOf(T item) //new!
        {
            // ??? CodeContract here - NO. item could be null, no matter of AllowsNull's value            
            if (item == null)
            {
                for (var i = 0; i < Count; i++)
                {
                    // Explicitly check against null to avoid using the (slower) equality comparer
                    if (_items[_offsetField + i] == null)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (var i = 0; i < Count; i++)
                {
                    if (Equals(item, _items[_offsetField + i]))
                    {
                        return i;
                    }
                }
            }

            return ~Count;

            //for (int i = 0; i < _size; i++)
            //    if (equals(item, array[offsetField + i]))
            //        return i;
            //return ~_size;
        }

        private int lastIndexOf(T item) //new! ??? Do we need this. There is already a public method.
        {
            // CodeContract here ???
            if (item == null)
            {
                for (var i = Count - 1; i >= 0; i--)
                {
                    // Explicitly check against null to avoid using the (slower) equality comparer
                    if (_items[_offsetField + i] == null)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (var i = Count - 1; i >= 0; i--)
                {
                    if (Equals(item, _items[_offsetField + i]))
                    {
                        return i;
                    }
                }
            }

            return ~Count;
        }

        //protected void checkRange(int start, int count)
        //{
        //    if (start < 0 || count < 0 || start + count > size)
        //        throw new ArgumentOutOfRangeException();
        //}

        private void ClearPrivate()
        {
            _items = EmptyArray; //
            Count = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="otherOffset"></param>
        /// <param name="otherCount"></param>
        /// <returns>The position of View(otherOffset, otherSize) with respect to this view</returns>
        MutualViewPosition viewPosition(int otherOffset, int otherCount)
        {
            int end = _offsetField + Count, otherEnd = otherOffset + otherCount;
            if (otherOffset >= end || otherEnd <= _offsetField)
                return MutualViewPosition.NonOverlapping;
            if (Count == 0 || (otherOffset <= _offsetField && end <= otherEnd))
                return MutualViewPosition.Contains;
            if (otherCount == 0 || (_offsetField <= otherOffset && otherEnd <= end))
                return MutualViewPosition.ContainedIn;
            return MutualViewPosition.Overlapping;
        }

        private void DisposeOverlappingViewsPrivate(bool reverse)
        {
            if (_views != null)
                foreach (ArrayList<T> view in _views)
                {
                    if (view != this)
                    {
                        switch (viewPosition(view._offsetField, view.Count))
                        {
                            case MutualViewPosition.ContainedIn:
                                if (reverse)
                                    view._offsetField = 2 * _offsetField + Count - view.Count - view._offsetField;
                                else
                                    view.Dispose();
                                break;
                            case MutualViewPosition.Overlapping:
                                view.Dispose();
                                break;
                            case MutualViewPosition.Contains:
                            case MutualViewPosition.NonOverlapping:
                                break;
                        }
                    }
                }
        }

        private void EnsureCapacity(int requiredCapacity)
        {
            #region Code Contracts

            Requires(requiredCapacity >= 0);

            Requires(requiredCapacity >= Count);

            Ensures(Capacity >= requiredCapacity);

            Ensures(MinArrayLength <= Capacity && Capacity <= MaxArrayLength);

            #endregion

            if (Capacity >= requiredCapacity)
            {
                return;
            }

            var capacity = Capacity * 2;

            if ((uint)capacity > MaxArrayLength)
            {
                capacity = MaxArrayLength;
            }
            else if (capacity < MinArrayLength)
            {
                capacity = MinArrayLength;
            }

            if (capacity < requiredCapacity)
            {
                capacity = requiredCapacity;
            }

            Capacity = capacity;
        }

        [Pure]
        private bool Equals(T x, T y) => EqualityComparer.Equals(x, y);

        [Pure]
        private int GetHashCode(T x) => EqualityComparer.GetHashCode(x); // ??? Not used

        private void InsertPrivate(int index, T item)
        {
            #region Code Contracts

            // Argument must be within bounds
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);

            #endregion

            // Only update version if items are actually added
            UpdateVersion();            

            // TODO: Check if Count == Capacity?
            EnsureCapacity(UnderlyingCount + 1);
            
            index += _offsetField;                        
            // Move items one to the right
            if (index < UnderlyingCount)
            {
                Array.Copy(_items, index, _items, index + 1, UnderlyingCount - index);
            }
            _items[index] = item;

            Count++;
            if (_underlying != null)
            {
                _underlying.Count++;
            }

            FixViewsAfterInsertPrivate(1, index);
        }

        private void FixViewsBeforeSingleRemovePrivate(int realRemovalIndex)
        {
            if (_views == null) return;

            foreach (var view in _views)
            {
                if (view == this) continue;

                if (view._offsetField <= realRemovalIndex && view._offsetField + view.Count > realRemovalIndex)
                    view.Count--;
                if (view._offsetField > realRemovalIndex)
                    view._offsetField--;
            }
        }

        private void FixViewsBeforeRemovePrivate(int start, int count)
        {
            int clearend = start + count - 1;
            if (_views != null)
                foreach (ArrayList<T> view in _views)
                {
                    if (view == this)
                        continue;
                    int viewoffset = view._offsetField, viewend = viewoffset + view.Count - 1;
                    if (start < viewoffset)
                    {
                        if (clearend < viewoffset)
                            view._offsetField = viewoffset - count;
                        else
                        {
                            view._offsetField = start;
                            view.Count = clearend < viewend ? viewend - clearend : 0;
                        }
                    }
                    else if (start <= viewend)
                        view.Count = clearend <= viewend ? view.Count - count : start - viewoffset;
                }
        }

        private void FixViewsAfterInsertPrivate(int added, int realInsertionIndex)
        {            
            if (_views != null)
                foreach (ArrayList<T> view in _views)
                {
                    if (view != this)
                    {
                        // in the middle
                        if (view._offsetField < realInsertionIndex && realInsertionIndex < view._offsetField + view.Count )
                            view.Count += added;
                        // before the beginning
                        if (view._offsetField > realInsertionIndex || (view._offsetField == realInsertionIndex && view.Count > 0))
                            view._offsetField += added;
                    }
                }
        }

        private void InsertRangePrivate(int index, T[] items)
        {
            #region Code Contracts

            // Argument must be within bounds
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || ForAll(items, item => item != null), ItemsMustBeNonNull);

            #endregion

            // Only update version if items are actually added
            UpdateVersion();
                        
            var count = items.Length;                               

            (_underlying ?? this).EnsureCapacity(UnderlyingCount + count); // ??? old:EnsureCapacity(Count + count);
            
            index += _offsetField;            
            if (index < UnderlyingCount)
            {
                Array.Copy(_items, index, _items, index + count, UnderlyingCount - index);
            }

            // C5
            //var underlyingsize = (_underlying ?? this).Count;
            //int added = index - Count - _offsetField;
            //if (added < count)
            //{
            //    Array.Copy(_items, Count + _offsetField + count, _items, index, underlyingsize - Count - _offsetField);
            //    // Array.Copy(array, size + offsetField + toadd, array, i, underlyingsize - size - offsetField);
            //    Array.Clear(_items, underlyingsize + added, count - added);
            //}
            //if (added > 0)
            //{
            //    Count += count;
            //    FixViewsAfterInsert(count, index);
            //    // raise                 
            //}

            Array.Copy(items, 0, _items, index, count);  // View:          

            Count += count;                
            if (_underlying != null)
                _underlying.Count += count;            
            FixViewsAfterInsertPrivate(count, index);            
        }

        #region Position, PositionComparer and ViewHandler nested types
        [Serializable]
        class PositionComparer : SCG.IComparer<Position>
        {
            public int Compare(Position a, Position b)
            {
                return a.index.CompareTo(b.index);
            }
        }
        /// <summary>
        /// During RemoveAll, we need to cache the original endpoint indices of views (??? also for ArrayList?)
        /// </summary>
        struct Position
        {
            public readonly ArrayList<T> view;
            public readonly int index;
            public Position(ArrayList<T> view, bool left)
            {
                this.view = view;
                index = left ? view._offsetField : view._offsetField + view.Count - 1;
            }
            public Position(int index) { this.index = index; view = null; }
        }

        /// <summary>
        /// Handle the update of (other) views during a multi-remove operation.
        /// </summary>
        struct ViewHandler
        {
            ArrayList<Position> leftEnds;
            ArrayList<Position> rightEnds;
            int leftEndIndex, rightEndIndex;
            internal readonly int viewCount;
            internal ViewHandler(ArrayList<T> list)
            {
                leftEndIndex = rightEndIndex = viewCount = 0;
                leftEnds = rightEnds = null;
                if (list._views != null)
                    foreach (ArrayList<T> v in list._views)
                        if (v != list)
                        {
                            if (leftEnds == null)
                            {
                                leftEnds = new ArrayList<Position>();
                                rightEnds = new ArrayList<Position>();
                            }
                            leftEnds.Add(new Position(v, true));
                            rightEnds.Add(new Position(v, false));
                        }
                if (leftEnds == null)
                    return;
                viewCount = leftEnds.Count;
                leftEnds.Sort(new PositionComparer());
                rightEnds.Sort(new PositionComparer());
            }
            /// <summary>
            /// This is to be called with realindex pointing to the first node to be removed after a (stretch of) node that was not removed
            /// </summary>
            /// <param name="removed"></param>
            /// <param name="realindex"></param>
            internal void skipEndpoints(int removed, int realindex)
            {
                if (viewCount > 0)
                {
                    Position endpoint;
                    while (leftEndIndex < viewCount && (endpoint = leftEnds[leftEndIndex]).index <= realindex)
                    {
                        var view = endpoint.view;
                        view._offsetField = view._offsetField - removed;
                        view.Count += removed;
                        leftEndIndex++;
                    }
                    while (rightEndIndex < viewCount && (endpoint = rightEnds[rightEndIndex]).index < realindex)
                    {
                        endpoint.view.Count -= removed;
                        rightEndIndex++;
                    }
                }
            }
            internal void updateViewSizesAndCounts(int removed, int realindex)
            {
                if (viewCount > 0)
                {
                    Position endpoint;
                    while (leftEndIndex < viewCount && (endpoint = leftEnds[leftEndIndex]).index <= realindex)
                    {
                        ArrayList<T> view = endpoint.view;
                        view._offsetField = view.Offset - removed;
                        view.Count += removed;
                        leftEndIndex++;
                    }
                    while (rightEndIndex < viewCount && (endpoint = rightEnds[rightEndIndex]).index < realindex)
                    {
                        endpoint.view.Count -= removed;
                        rightEndIndex++;
                    }
                }
            }
        }
        #endregion

        private static bool IsCompatibleObject(object value) => value is T || value == null && default(T) == null;

        private bool RemoveAllWhere(Func<T, bool> predicate)
        {
            // If result is false, the collection remains unchanged
            #region Code Contract            
            Ensures(Result<bool>() || this.IsSameSequenceAs(OldValue(ToArray())));
            #endregion

            // updatecheck() ???

            if (IsEmpty)
            {
                return false;
            }

            var shouldRememberItems = ActiveEvents.HasFlag(Removed); // ??? 
            IExtensible<T> itemsRemoved = null;
            var cntRemoved = 0;
            var viewHandler = new ViewHandler(this); 

            // TODO: Use bulk moves - consider using predicate(item) ^ something ???
            var j = _offsetField;
            for (var i = _offsetField; i < _offsetField + Count; i++)
            {
                var item = _items[i];

                if (predicate(item))
                {                    
                    if (shouldRememberItems)
                    {
                        (itemsRemoved ?? (itemsRemoved = new ArrayList<T>(allowsNull: AllowsNull))).Add(item); // TODO: Test allows null
                    }
                    cntRemoved++;
                    viewHandler.updateViewSizesAndCounts(cntRemoved, i+1);
                }
                else
                {
                    // Avoid overriding an item with itself
                    if (j != i)
                    {
                        _items[j] = item;
                    }
                    j++;
                    viewHandler.skipEndpoints(cntRemoved, i+1); // not effective ???
                }
            }

            // No items were removed
            if (cntRemoved == 0)// (Count == j) 
            {
                Assert(itemsRemoved == null);
                return false;
            }
            
            viewHandler.updateViewSizesAndCounts(cntRemoved, UnderlyingCount);

            Array.Copy(_items, _offsetField + Count, _items, j, UnderlyingCount - _offsetField - Count);            
            Count -= cntRemoved;
            if (_underlying != null)
            {
                _underlying.Count -= cntRemoved;
            }

            
            UpdateVersion(); // Only update version if items are actually removed

            // Clean up            
            Array.Clear(_items, UnderlyingCount, cntRemoved); // underlyingCount != j !!!
            //Count = j;

            (_underlying ?? this).RaiseForRemoveAllWhere(itemsRemoved);

            return true;
        }

        private T RemoveAtPrivate(int index)
        {
            UpdateVersion();
                        
            index += _offsetField;
            FixViewsBeforeSingleRemovePrivate(index);

            Count--;
            if (_underlying != null)
            {
                _underlying.Count--;
            }

            var item = _items[index];            
            if (index < UnderlyingCount) // if (--Count > index)
            {
                Array.Copy(_items, index + 1, _items, index, UnderlyingCount - index);
            }

            _items[UnderlyingCount] = default(T);
            return item;
        }

        private void UpdateVersion() => _version++;

        #region Event Helpers

        #region Invoking Methods

        private void OnCollectionChanged()
            => _collectionChanged?.Invoke(this, EventArgs.Empty);

        private void OnCollectionCleared(bool full, int count, int? start = null)
            => _collectionCleared?.Invoke(this, new ClearedEventArgs(full, count, start));

        private void OnItemsAdded(T item, int count)
            => _itemsAdded?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnItemsRemoved(T item, int count)
            => _itemsRemoved?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnItemInserted(T item, int index)
            => _itemInserted?.Invoke(this, new ItemAtEventArgs<T>(item, index));

        private void OnItemRemovedAt(T item, int index)
            => _itemRemovedAt?.Invoke(this, new ItemAtEventArgs<T>(item, index));

        #endregion

        #region Method-Specific Helpers

        private void RaiseForAdd(T item)
        {
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForAddRange(SCG.IEnumerable<T> items)
        {
            Requires(items != null);

            if (ActiveEvents.HasFlag(Added))
            {
                foreach (var item in items)
                {
                    OnItemsAdded(item, 1);
                }
            }
            OnCollectionChanged();
        }

        private void RaiseForClear(int count)
        {
            Requires(count >= 1);

            OnCollectionCleared(true, count);
            OnCollectionChanged();
        }

        private void RaiseForIndexSetter(T oldItem, T newItem, int index)
        {
            if (ActiveEvents == None)
            {
                return;
            }

            OnItemRemovedAt(oldItem, index);
            OnItemsRemoved(oldItem, 1);
            OnItemInserted(newItem, index);
            OnItemsAdded(newItem, 1);
            OnCollectionChanged();
        }

        private void RaiseForInsert(int index, T item)
        {
            OnItemInserted(item, index);
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForInsertRange(int index, T[] array)
        {
            if (ActiveEvents.HasFlag(Inserted | Added))
            {
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    OnItemInserted(item, index + i); // View: ???
                    OnItemsAdded(item, 1);
                }
            }
            OnCollectionChanged();
        }

        private void RaiseForRemove(T item)
        {
            OnItemsRemoved(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForRemovedAt(T item, int index)
        {
            OnItemRemovedAt(item, index);
            OnItemsRemoved(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForRemoveIndexRange(int startIndex, int count)
        {
            OnCollectionCleared(false, count, startIndex);
            OnCollectionChanged();
        }

        private void RaiseForRemoveAllWhere(SCG.IEnumerable<T> items)
        {
            if (ActiveEvents.HasFlag(Removed))
            {
                foreach (var item in items)
                {
                    OnItemsRemoved(item, 1);
                }
            }
            OnCollectionChanged();
        }

        private void RaiseForReverse() => OnCollectionChanged();

        private void RaiseForShuffle() => OnCollectionChanged();

        private void RaiseForSort() => OnCollectionChanged();

        private void RaiseForUpdate(T item, T oldItem)
        {
            Requires(Equals(item, oldItem));

            OnItemsRemoved(oldItem, 1);
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        #endregion

        #endregion

        #endregion

        #region Nested Types

        // TODO: Explicitly check against null to avoid using the (slower) equality comparer
        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class Duplicates : CollectionValueBase<T>, ICollectionValue<T>
        {
            #region Fields

            private readonly ArrayList<T> _base;
            private readonly int _version;
            private readonly T _item;
            private ArrayList<T> _list;

            #endregion

            #region Code Contracts

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                // ReSharper disable InvocationIsSkipped

                // All items in the list are equal to the item
                Invariant(_list == null || ForAll(_list, x => _base.EqualityComparer.Equals(x, _item)));

                // All items in the list are equal to the item
                Invariant(_list == null || _list.Count == _base.CountDuplicates(_item));

                // ReSharper restore InvocationIsSkipped
            }

            #endregion

            #region Constructors

            // TODO: Document
            public Duplicates(ArrayList<T> list, T item)
            {
                #region Code Contracts

                // Argument must be non-null
                Requires(list != null, ArgumentMustBeNonNull);

                #endregion

                _base = list;
                _version = _base._version;
                _item = item;
            }

            #endregion

            #region Properties

            public override bool IsValid
            {
                get
                {
                    return base.IsValid;
                }

                protected internal set
                {
                    base.IsValid = value;
                }
            }

            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get
                {
                    CheckVersion();
                    
                    return List.Count;
                }
            }

            public override Speed CountSpeed
            {
                get
                {
                    CheckVersion();
                    // TODO: Always use Linear?
                    return _list == null ? Linear : Constant;
                }
            }

            public override bool IsEmpty => CheckVersion() & List.IsEmpty;

            #endregion

            #region Public Methods

            public override T Choose()
            {
                CheckVersion();
                return _base.Choose(); // TODO: Is this necessarily an item in the collection value?!
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                List.CopyTo(array, arrayIndex);
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            public override SCG.IEnumerator<T> GetEnumerator()
            {
                #region Code contracts
                Requires(IsValid);
                #endregion

                // If a list already exists, enumerate that
                if (_list != null)
                {
                    var enumerator = _list.GetEnumerator();
                    while (CheckVersion() & enumerator.MoveNext())                    
                    {
                        yield return enumerator.Current;
                    }
                }
                // Otherwise, evaluate lazily
                else
                {
                    var list = new ArrayList<T>(allowsNull: AllowsNull);
                    Func<T, T, bool> equals = _base.Equals;

                    var enumerator = _base.GetEnumerator();


                    T item;
                    while (/*CheckVersion() &*/ enumerator.MoveNext())
                    {
                        // Only return duplicate items
                        if (equals(item = enumerator.Current, _item))
                        {
                            list.Add(item);
                            yield return item;
                        }
                    }

                    // Save list for later (re)user
                    _list = list;
                }
            }

            public override int GetHashCode()
            {
                CheckVersion();
                return base.GetHashCode();
            }

            public override T[] ToArray()
            {
                CheckVersion();
                return List.ToArray();
            }

            #endregion

            #region Private Members

            private string DebuggerDisplay => _version == _base._version ? ToString() : "Expired collection value; original collection was modified since range was created.";

            private bool CheckVersion() => _base.CheckVersion(_version);

            private ArrayList<T> List => _list != null ? _list : (_list = new ArrayList<T>(_base.Where(x => _base.Equals(x, _item)), allowsNull: AllowsNull));

            #endregion
        }

        // TODO: Introduce base class?
        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class ItemSet : CollectionValueBase<T>, ICollectionValue<T>
        {
            #region Fields

            private readonly ArrayList<T> _base;
            private readonly int _version;
            // TODO: Replace with HashedArrayList<T>
            private SCG.HashSet<T> _set;

            #endregion

            #region Code Contracts

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                // ReSharper disable InvocationIsSkipped

                // Base list is never null
                Invariant(_base != null);

                // Either the set has not been created, or it contains the same as the base list's distinct items
                Invariant(_set == null || _set.UnsequenceEqual(_base.Distinct(_base.EqualityComparer), _base.EqualityComparer));

                // ReSharper restore InvocationIsSkipped
            }

            #endregion

            #region Constructors

            // TODO: Document
            public ItemSet(ArrayList<T> list)
            {
                #region Code Contracts

                // Argument must be non-null
                Requires(list != null, ArgumentMustBeNonNull);

                #endregion

                _base = list;
                _version = _base._version;
            }

            #endregion

            #region Properties

            // Where is that from?
            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get
                {
                    CheckVersion();
                    return Set.Count;
                }
            }

            public override Speed CountSpeed
            {
                get
                {
                    CheckVersion();
                    // TODO: Always use Linear?
                    return _set == null ? Linear : Constant;
                }
            }

            public override bool IsEmpty => CheckVersion() & _base.IsEmpty;

            #endregion

            #region Public Methods

            public override T Choose()
            {
                CheckVersion();
                return _base.Choose(); // TODO: Is this necessarily an item in the collection value?!
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                Set.CopyTo(array, arrayIndex);
            }

            // ???? Equals comes from where. Ok - from Object class
            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            // ??? Why do we need it? Isn't that enough to overrire GetEnumerator()?
            public override SCG.IEnumerator<T> GetEnumerator()
            {
                // If a set already exists, enumerate that
                if (_set != null)
                {
                    var enumerator = Set.GetEnumerator();
                    while (CheckVersion() & enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                }
                // Otherwise, evaluate lazily
                else
                {
                    var set = new SCG.HashSet<T>(_base.EqualityComparer);

                    var enumerator = _base.GetEnumerator();
                    while (CheckVersion() & enumerator.MoveNext())
                    {
                        // Only return new items
                        if (set.Add(enumerator.Current))
                        {
                            yield return enumerator.Current;
                        }
                    }

                    // Save set for later (re)user
                    _set = set;
                }
            }

            // Where is that from?
            public override int GetHashCode()
            {
                CheckVersion();
                return base.GetHashCode();
            }

            public override T[] ToArray()
            {
                CheckVersion();
                return Set.ToArray();
            }

            #endregion

            #region Private Members

            private string DebuggerDisplay => _version == _base._version ? ToString() : "Expired collection value; original collection was modified since range was created.";

            private bool CheckVersion() => _base.CheckVersion(_version);

            // TODO: Replace with HashedArrayList<T>!
            private SCG.ISet<T> Set => _set ?? (_set = new SCG.HashSet<T>(_base, _base.EqualityComparer));

            #endregion
        }

        /// <summary>
        ///     Represents a range of an <see cref="ArrayList{T}"/>.
        /// </summary>
        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class Range : CollectionValueBase<T>, IDirectedCollectionValue<T>
        {
            #region Fields

            private readonly ArrayList<T> _base;
            private readonly int _version, _startIndex, _count, _sign;
            private readonly EnumerationDirection _direction;

            #endregion
            
            #region Constructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="Range"/> class that starts at the specified index and spans the next
            ///     <paramref name="count"/> items in the specified direction.
            /// </summary>
            /// <param name="list">
            ///     The underlying <see cref="ArrayList{T}"/>.
            /// </param>
            /// <param name="startIndex">
            ///     The zero-based <see cref="ArrayList{T}"/> index at which the range starts.
            /// </param>
            /// <param name="count">
            ///     The number of items in the range.
            /// </param>
            /// <param name="direction">
            ///     The direction of the range.
            /// </param>
            public Range(ArrayList<T> list, int startIndex, int count, EnumerationDirection direction)
            {
                #region Code Contracts
                
                // Argument must be non-null
                Requires(list != null, ArgumentMustBeNonNull);

                // Argument must be within bounds
                Requires(-1 <= startIndex, ArgumentMustBeWithinBounds);
                Requires(startIndex < list.Count || startIndex == 0 && count == 0, ArgumentMustBeWithinBounds);

                // Argument must be within bounds
                Requires(0 <= count, ArgumentMustBeWithinBounds);
                Requires(direction.IsForward() ? startIndex + count <= list.Count : count <= startIndex + 1, ArgumentMustBeWithinBounds);

                // Argument must be valid enum constant
                Requires(Enum.IsDefined(typeof(EnumerationDirection), direction), EnumMustBeDefined);


                Ensures(_base != null);
                Ensures(_version == _base._version);
                Ensures(_sign == (direction.IsForward() ? 1 : -1));
                Ensures(-1 <= _startIndex);
                Ensures(_startIndex < _base.Count || _startIndex == 0 && _base.Count == 0);
                Ensures(-1 <= _startIndex + _sign * _count);
                Ensures(_startIndex + _sign * _count <= _base.Count);
                
                #endregion

                _base = list;
                _version = list._version;

                _sign = (int)direction;
                _startIndex = startIndex;
                _count = count;
                _direction = direction;                
            }

            #endregion

            #region Properties

            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get
                {
                    CheckVersion();
                    return _count;
                }
            }

            public override Speed CountSpeed
            {
                get
                {
                    CheckVersion();
                    return Constant;
                }
            }

            public EnumerationDirection Direction
            {
                get
                {
                    CheckVersion();
                    return _direction;
                }
            }

            #endregion

            #region Public Methods

            public IDirectedCollectionValue<T> Backwards()
            {
                CheckVersion();
                var startIndex = _startIndex + (_count - 1) * _sign;
                var direction = Direction.Opposite();
                return new Range(_base, startIndex, _count, direction);
            }

            public override T Choose()
            {
                CheckVersion();
                // Select the highest index in the range
                var index = _direction.IsForward() ? _startIndex + _count - 1 : _startIndex;
                return _base._items[index];
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                if (_direction.IsForward()) // depending on _direction choose the most efficient one
                {
                    // Copy array directly
                    Array.Copy(_base._items, _base.Offset + _startIndex, array, arrayIndex, _count); // Offset!!
                }
                else
                {
                    // Use enumerator instead of copying and then reversing
                    base.CopyTo(array, arrayIndex);
                }
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            public override SCG.IEnumerator<T> GetEnumerator()
            {
                var items = _base._items;                
                for (var i = 0; i < Count; i++)
                {
                    yield return items[_base.Offset + _startIndex + _sign * i];
                }
            }

            public override int GetHashCode()
            {
                CheckVersion();
                return base.GetHashCode();
            }

            public override T[] ToArray()
            {
                CheckVersion();
                return base.ToArray();                
            }

            #endregion

            #region Private Members

            private string DebuggerDisplay => _version == _base._version ? ToString() : "Expired collection value; original collection was modified since range was created.";

            private bool CheckVersion() => _base.CheckVersion(_version);

            #endregion
        }

        // tags 
        // Code contract
        private sealed class WeakViewList<V> : SCG.IEnumerable<V> where V : class
        {
            Node start;

            [Serializable]
            internal class Node
            {
                internal WeakReference weakview;
                internal Node prev, next;
                internal Node(V view) { weakview = new WeakReference(view); }
            }

            internal Node Add(V view)
            {
                Node newNode = new Node(view);
                if (start != null) { start.prev = newNode; newNode.next = start; }
                start = newNode;
                return newNode;
            }

            internal void Remove(Node n)
            {
                if (n == start) { start = start.next; if (start != null) start.prev = null; }
                else { n.prev.next = n.next; if (n.next != null) n.next.prev = n.prev; }
            }

            internal void Clear()
            {
                start = null;
            }

            /// <summary>
            /// Note that it is safe to call views.Remove(view.myWeakReference) if view
            /// is the currently yielded object
            /// </summary>
            /// <returns></returns>
            public SCG.IEnumerator<V> GetEnumerator()
            {
                Node n = start;
                while (n != null)
                {
                    //V view = n.weakview.Target as V; //This provokes a bug in the beta1 verifyer
                    object o = n.weakview.Target;
                    V view = o is V ? (V)o : null;
                    if (view == null)
                        Remove(n);
                    else
                        yield return view;
                    n = n.next;
                }
            }

            SC.IEnumerator SC.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion
    }
}