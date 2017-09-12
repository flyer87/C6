//using System;
//using System.Linq;
//using System.Text;

//using C6.Collections;

//using SCG = System.Collections.Generic;
//using SC = System.Collections;

//using C6.Contracts;

//using static System.Diagnostics.Contracts.Contract;
//using static C6.Contracts.ContractMessage;
//using static C6.Speed;

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

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
    public class HashedArrayList<T> : IExtensible<T>
    {
        #region Fields

        public static readonly T[] EmptyArray = new T[0];
        private const int MinArrayLength = 0x00000004;
        private const int MaxArrayLength = 0x7FEFFFFF;


        private T[] _items;
        private SCG.HashSet<KeyValuePair<T, int>> _itemIndex;

        private event EventHandler _collectionChanged;
        private event EventHandler<ClearedEventArgs> _collectionCleared;
        private event EventHandler<ItemAtEventArgs<T>> _itemInsertedAt, _itemRemovedAt;
        private event EventHandler<ItemCountEventArgs<T>> _itemsAdded, _itemsRemoved;

        private int _version;


        #endregion Fields

        #region Constructors

        public HashedArrayList()
            : this(0) { }

        public HashedArrayList(SCG.IEnumerable<T> items, SCG.IEqualityComparer<T> equalityComparer = null)
        {
            #region Code Contracts

            // ReSharper disable InvocationIsSkipped
            // Argument must be non-null
            Requires(items != null, ArgumentMustBeNonNull);

            // not the same instance
            Ensures(!ReferenceEquals(_items, items));
            // ReSharper enable InvocationIsSkipped

            #endregion

            // allowsNull = false - by default !
            IsValid = true;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default; // ??? Default

            var collectionValues = items as ICollectionValue<T>;
            var collection = items as SCG.ICollection<T>;
            
            if (collectionValues != null) 
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
            #region Code Contracts            

            Requires(capacity >= 0, ArgumentMustBeNonNegative);

            #endregion

            IsValid = true;
            Capacity = capacity;                    
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default;
        }

        public HashedArrayList(SCG.IEqualityComparer<T> equalityComparer = null)
            : this(0, equalityComparer) { }

        #endregion

        #region Explicit implementations

        SC.IEnumerator SC.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // already implemented?
        //SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Properties

        public int Capacity
        {
            get { return _items.Length; }
            set
            {
                #region Code Contracts
                //Capacity should at least as big the number of items
                Requires(value >= Count);

                // Capacity is at least as big as the number of items
                Ensures(Capacity >= Count);

                Ensures(Capacity == value); 
                #endregion

                if (value > 0)
                {
                    if (value == _items.Length)
                    {
                        return;
                    }

                    Array.Resize(ref _items, value);
                }
                else
                {
                    _items = EmptyArray;
                }
            }
        } //from HAL

        #region ICollectionValue
        public bool IsValid { get; }

        public int Count { get; protected set; } // to_base

        public bool AllowsNull => false; // by defintion!

        public Speed CountSpeed => Constant; // to_base: abstract        

        public bool IsEmpty => Count == 0; // to base:virtual

        public T Choose() => _items[0]; //to_base: virtual // Count - 1
        #endregion

        public virtual SCG.IEqualityComparer<T> EqualityComparer { get; } // ??? virtual
        
        #region IListenable
        public virtual EventTypes ActiveEvents { get; private set; }
        public virtual EventTypes ListenableEvents => All;
        #endregion

        #region IExtensible
        public bool AllowsDuplicates => false;

        public bool DuplicatesByCounting => true;

        public bool IsFixedSize => false; // can add and remove 

        public bool IsReadOnly => false;
        #endregion

        #endregion

        #region Public methods

        public SCG.IEnumerator<T> GetEnumerator()
        {
            //yield return default(T);
            for (int i = 0; i < Count; i++)
            {
                yield return _items[i];
            }
        }

        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_items, 0, array, arrayIndex, Count); //to_base

        public T[] ToArray() // to_base(virtual)
        {
            var array = new T[Count];
            CopyTo(array, 0);
            return array;
        }

        public override string ToString() => this.ToString(null, null); // to_base(override, the Object's)

        public string ToString(string format, IFormatProvider formatProvider) // to_base, here: no
            => Showing.ShowString(this, format, formatProvider);

        public bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider)
            => Showing.Show(this, stringBuilder, ref rest, formatProvider); // to_base(virtual), here: no       

        #region IExtensible
        public bool Add(T item)
        {
            #region Code Contracts            
            #endregion

            if (FindOrAddToHashPrivate(item)) { // ? Does it work
                return false;
            }
             
            InsertPrivate(Count, item);
            // !!! reindex(size + offsetField);
            /*View: underl.*/RaiseForAdd(item); //View: for underlying;
            return true;
        }
       
        public bool AddRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts
            #endregion

            // TODO: Handle ICollectionValue<T> and ICollection<T>
            var array = items.ToArray();
            if (array.IsEmpty()) {
                return false;
            }
            
            var countToAdd = array.Length;
            // View: under. Count
            // =========
            EnsureCapacity(Count + countToAdd);
            var index = Count; // make it better
            // make space - irrelevant for Add(: from the end)
            if (index < Count)
            {
                Array.Copy(_items, index, _items, index + countToAdd, Count - index);
            }
            
            // copy the relevants
            var oldIndex = index;
            var countAdded = 0;
            foreach (var item in items) {
                if (FindOrAddToHashPrivate(item))
                {
                    continue;
                }
                _items[index++] = item;
                countAdded++;
            }

            // shrink the space if too much space is made
            if (countAdded < countToAdd)
            {
                Array.Copy(_items, oldIndex + countToAdd, _items, index, Count - oldIndex);
                Array.Clear(_items, Count + countAdded, countToAdd - countAdded ); //#to_delete: kolkoto sa ostanali ne zapalnati
            }
            if (countAdded > 0) {
                Count += countAdded; // Views: under_count
                ReindexPrivate(index);
                //View: fix views
                /*under.*/RaiseForAddRange(array);
            }
                        
            return true;
        }        

        #endregion

        #endregion

        #region Events        
        public event EventHandler CollectionChanged
        {
            add
            {
                _collectionChanged += value;
                ActiveEvents |= Changed;
            }
            remove
            {
                _collectionChanged -= value;
                if (_collectionChanged == null) {
                    ActiveEvents &= ~Changed;
                }
            }
        }
        public event EventHandler<ClearedEventArgs> CollectionCleared
        {
            add
            {
                _collectionCleared += value;
                ActiveEvents |= Cleared;
            }
            remove
            {
                _collectionCleared -= value;
                if (_collectionCleared == null) {                    
                    ActiveEvents &= ~Cleared;
                }
            }
        }
        public event EventHandler<ItemAtEventArgs<T>> ItemInserted
        {
            add {
                _itemInsertedAt += value;
                ActiveEvents |= Inserted;
            }
            remove {
                _itemInsertedAt -= value;
                if (_itemInsertedAt == null) {
                    ActiveEvents &= ~Inserted;
                }
            }
        }
        public event EventHandler<ItemAtEventArgs<T>> ItemRemovedAt
        {
            add {
                _itemRemovedAt += value;
                ActiveEvents |= RemovedAt;
            }
            remove {
                _itemRemovedAt -= value;
                if (_itemRemovedAt == null) {
                    ActiveEvents &= ~RemovedAt;
                }                
            }
        }
        public event EventHandler<ItemCountEventArgs<T>> ItemsAdded
        {
            add
            {
                _itemsAdded += value;
                ActiveEvents |= Added;
            }
            remove {
                _itemsAdded -= value;
                if (_itemsAdded == null) {
                    ActiveEvents &= ~Added;
                }
            }
        }
        public event EventHandler<ItemCountEventArgs<T>> ItemsRemoved
        {
            add
            {
                _itemsRemoved += value;
                ActiveEvents |= Removed;
            }
            remove
            {
                _itemsRemoved -= value;
                if (_itemsRemoved == null) {
                    ActiveEvents &= ~Removed;
                }
            }
        }
        #endregion

        #region Private

        private void ReindexPrivate(int i)
        {
            
        }

        private bool FindOrAddToHashPrivate(T item)
        {
            // TODO Something else?            
            KeyValuePair<T, int> p = new KeyValuePair<T, int>(item, Count); // View: cnt+offset
            if (_itemIndex.Contains(p))
            {
                return true;
            }
            _itemIndex.Add(p);
            return false;
        }

        private void InsertPrivate(int index, T item)
        {
            #region Code Contracts            
            #endregion

            UpdateVersion();

            EnsureCapacity(Count + 1);

            //Views: update the index to underindex here

            // Moves items one to the right
            if (index < Count) {
                Array.Copy(_items, index, _items, index + 1, Count - index);
            }
            _items[index] = item;
            Count++;
            //Views: underlying ? und.Count++
            //Views: FixViewsAfterInsertPrivate(1, index);
        }

        private void InsertRangePrivate(T[] array, int index)
        {
            #region Code Contract

            #endregion
            UpdateVersion();

            var count = array.Length;
            // View: under. Count
            EnsureCapacity(Count + count);

            if (index < Count) {
                Array.Copy(_items, index, _items, index + count, Count - index);
            }

            Array.Copy(array, 0, _items, index, count);
            Count += count; // View:
            // FixViewsAfterInsertPrivate(count, index);
        }

        private void EnsureCapacity(int requiredCapacity)
        {
            #region Code Contracts            
            #endregion

            if (Capacity >= requiredCapacity) {
                return;
            }

            var newCapacity = Capacity * 2;
            if ((uint) newCapacity > MaxArrayLength) { // why uint ???
                newCapacity = MaxArrayLength;
            }
            else if (newCapacity < MinArrayLength) {
                newCapacity = MinArrayLength;
            }

            if (newCapacity < requiredCapacity)
            {
                newCapacity = requiredCapacity;
            }

            Capacity = newCapacity;
        }

        private void UpdateVersion() => _version++;

        private void RaiseForAdd(T item)
        {
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForAddRange(T[] array)
        {
            if (ActiveEvents.HasFlag(Added))
            {
                foreach (var item in array)
                {
                    OnItemsAdded(item, 1);
                }
            }

            OnCollectionChanged();
        }

        #region InvokingMethods

        private void OnItemsAdded(T item, int count) => _itemsAdded?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnCollectionChanged() => _collectionChanged?.Invoke(this, EventArgs.Empty);


        #endregion

        #endregion

    }
}