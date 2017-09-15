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
using System.Collections.ObjectModel;
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
    public class HashedArrayList<T> : ICollection<T>
    {
        #region Fields

        public static readonly T[] EmptyArray = new T[0];
        private const int MinArrayLength = 0x00000004;
        private const int MaxArrayLength = 0x7FEFFFFF;


        private T[] _items;

        //private SCG.HashSet<KeyValuePair<T, int>> _itemIndex;
        private SCG.Dictionary<T, int> _itemIndex;

        private event EventHandler _collectionChanged;
        private event EventHandler<ClearedEventArgs> _collectionCleared;
        private event EventHandler<ItemAtEventArgs<T>> _itemInsertedAt, _itemRemovedAt;
        private event EventHandler<ItemCountEventArgs<T>> _itemsAdded, _itemsRemoved;

        private int _version, _sequencedHashCodeVersion = -1, _unsequencedHashCodeVersion = -1;
        private int _sequencedHashCode, _unsequencedHashCode;


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

            //var collectionValues = items as ICollectionValue<T>;
            //var collection = items as SCG.ICollection<T>;

            //if (collectionValues != null) 
            //{
            //    _items = collectionValues.IsEmpty ? EmptyArray : collectionValues.ToArray();
            //    _itemIndex = 
            //    Count = collectionValues.Count;
            //}
            //else if (collection != null)
            //{
            //    Count = collection.Count;
            //    _items = Count == 0 ? EmptyArray : new T[Count];
            //    _itemIndex = null;
            //    collection.CopyTo(_items, 0);
            //}
            //else
            //{
            _items = EmptyArray;
            _itemIndex = new SCG.Dictionary<T, int>(EqualityComparer); // EqualityComparer as paramter ??
            AddRange(items); // ??? do we need it
            //}
        }

        public HashedArrayList(int capacity = 0, SCG.IEqualityComparer<T> equalityComparer = null) // why 0 ???
        {
            #region Code Contracts            

            Requires(capacity >= 0, ArgumentMustBeNonNegative);

            #endregion

            IsValid = true;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default; // !!! should be before Capacity = capacity
            Capacity = capacity;
        }

        public HashedArrayList(SCG.IEqualityComparer<T> equalityComparer = null)
            : this(0, equalityComparer) { }

        #endregion

        #region Explicit implementations

        SC.IEnumerator SC.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        public int Capacity
        {
            get { return _items.Length; }
            set {
                #region Code Contracts

                //Capacity should at least as big the number of items
                Requires(value >= Count);

                // Capacity is at least as big as the number of items
                Ensures(Capacity >= Count);

                Ensures(Capacity == value);

                #endregion

                if (value > 0) {
                    if (value == _items.Length) {
                        return;
                    }

                    Array.Resize(ref _items, value);
                }
                else {
                    _items = EmptyArray;
                    _itemIndex = new SCG.Dictionary<T, int>(EqualityComparer); // EqualityComparer !!!
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

        #region IListenable

        public virtual EventTypes ActiveEvents { get; private set; }
        public virtual EventTypes ListenableEvents => All;

        #endregion

        #region IExtensible

        public bool AllowsDuplicates => false;

        public bool DuplicatesByCounting => true;

        public bool IsFixedSize => false; // can add and remove 

        public bool IsReadOnly => false;

        public virtual SCG.IEqualityComparer<T> EqualityComparer { get; } // ??? virtual

        #endregion

        #region ICollection

        public Speed ContainsSpeed => Constant;

        #endregion

        #region ISequenced
        public EnumerationDirection Direction { get; }


        #endregion

        #endregion

        #region Public methods

        #region ICollectionValue

        public SCG.IEnumerator<T> GetEnumerator()
        {
            #region Code Contracts

            Ensures(_version == OldValue(_version));

            #endregion

            var version = _version;
            //yield return default(T); ???
            for (int i = 0; CheckVersion(version) && i < Count; i++) {
                yield return _items[i];
            }
        }



        public override string ToString() => ToString(null, null); // to_base(override, the Object's)

        public string ToString(string format, IFormatProvider formatProvider) // to_base, here: no
            => Showing.ShowString(this, format, formatProvider);

        public bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider)
            => Showing.Show(this, stringBuilder, ref rest, formatProvider); // to_base(virtual), here: no       


        public T[] ToArray() // to_base(virtual)
        {
            var array = new T[Count];
            CopyTo(array, 0);
            return array;
        }

        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_items, 0, array, arrayIndex, Count); //to_base

        #endregion

        #region IExtensible

        public bool Add(T item)
        {
            #region Code Contracts            

            #endregion

            if (FindOrAddToHashPrivate(item, Count)) {
                // ? Does it work
                return false;
            }

            UpdateVersion();

            InsertPrivate(Count, item);
            // !!! reindex(size + offsetField);
            /*View: underl.*/
            RaiseForAdd(item); //View: for underlying;
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
            // View: underl.Count
            // =========
            EnsureCapacity(Count + countToAdd);
            var index = Count; // make it better
            // make space - irrelevant for Add(: from the end)
            if (index < Count) {
                Array.Copy(_items, index, _items, index + countToAdd, Count - index);
            }

            // copy the relevants
            var oldIndex = index;
            var countAdded = 0;
            foreach (var item in items) {
                if (FindOrAddToHashPrivate(item, index)) {
                    continue;
                }
                _items[index++] = item;
                countAdded++;
            }

            // shrink the space if too much space is allocated
            if (countAdded < countToAdd) {
                Array.Copy(_items, oldIndex + countToAdd, _items, index, Count - oldIndex);
                Array.Clear(_items, Count + countAdded, countToAdd - countAdded); //#to_delete: kolkoto sa ostanali ne zapalnati
            }
            if (countAdded <= 0) {
                return false;
            }

            UpdateVersion();

            Count += countAdded; // Views: under_count
            ReindexPrivate(index);
            //View: fix views
            /*under.*/
            RaiseForAddRange(array);

            return true;
        }

        #endregion

        #region ICollection

        public virtual int IndexOf(T item)
        {
            int index;
            if (_itemIndex.TryGetValue(item, out index)) {
                return index;
            }
            return ~Count;
        }

        public ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities()
        {
            // ???
            throw new NotImplementedException();
        }

        public bool RetainRange(SCG.IEnumerable<T> items)
        {
            // View:
            throw new NotImplementedException();
        }

        public ICollectionValue<T> UniqueItems() => new ItemSet(this);

        public bool UnsequencedEquals(ICollection<T> otherCollection) => this.UnsequencedEquals(otherCollection, EqualityComparer);

        public bool Update(T item)
        {
            #region Code Contracts          

            #endregion

            T oldItem;
            return Update(item, out oldItem);
        }

        public bool Update(T item, out T oldItem)
        {
            #region Code Contracts            

            #endregion

            int index;
            if ((index = IndexOf(item)) < 0) {
                oldItem = default(T);
                return false;
            }

            UpdateVersion();

            oldItem = _items[index]; // View: + offsetField
            _items[index] = item; // View: + offsetField            
            _itemIndex[item] = index; // View: offsetField

            /*(underlying ?? this). */
            RaiseForUpdate(item, oldItem);
            return true;
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

        public virtual bool Remove(T item)
        {
            T removedItem;
            return Remove(item, out removedItem);
        }

        public bool Remove(T item, out T removedItem)
        {
            #region Code Contracts            

            #endregion

            // ??? No duplicates => LastIndexOf(item) - What?!

            // Remove (last) instance of item, since this moves the fewest items
            var index = IndexOf(item);

            if (index >= 0) {
                removedItem = RemoveAtPrivate(index);
                RaiseForRemove(removedItem);
                return true;
            }

            removedItem = default(T);
            return false;
        }

        public virtual bool RemoveDuplicates(T item) => Remove(item);

        public bool RemoveRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts            

            #endregion

            if (IsEmpty) {
                return false;
            }

            if (items.IsEmpty()) {
                return true;
            }

            // TODO: Complete it with View

            throw new NotImplementedException();
        }

        void SCG.ICollection<T>.Add(T item) => Add(item);

        public bool ContainsRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts           

            // Ensures()

            #endregion
            if (items.IsEmpty())
            {
                return true;
            }

            if (IsEmpty) {
                return false;
            }

            // TODO: Private method
            // TODO: ???Replace ArrayList<T> with more efficient data structure like HashBag<T>
            //var itemsToContain = new ArrayList<T>(items, EqualityComparer);
            bool containsRange = true;
            foreach (var item in items) {
                containsRange = containsRange && _itemIndex.ContainsKey(item); // ??? &= 
            }

            return containsRange;
        }

        public virtual void Clear()
        {
            #region Code Contracts

            // Ensures()            

            #endregion

            if (IsEmpty) {
                return;
            }

            UpdateVersion();

            var oldCount = Count;
            //View: fixViewsBeforeRemove(0, size);            
            ClearPrivate();
            RaiseForClear(oldCount);
        }

        public virtual bool Contains(T item) => IndexOf(item) >= 0;

        public int CountDuplicates(T item) => IndexOf(item) >= 0 ? 1 : 0;

        public bool Find(ref T item)
        {
            #region Code Contracts          

            #endregion

            int index;
            if ((index = IndexOf(item)) < 0) {
                return false;
            }

            item = _items[index]; // View: offset
            return true;
        }

        public virtual ICollectionValue<T> FindDuplicates(T item)
        {
            #region Code Contract

            //RequireValidity();

            #endregion

            var duplicates = new Duplicates(this, item);
            //_collValues.Add(duplicates);

            return duplicates;
        }

        public bool FindOrAdd(ref T item)
        {
            #region Code Contracts          

            #endregion

            if (Find(ref item)) {
                return true;
            }

            Add(item);
            return false;
        }

        public int GetUnsequencedHashCode()
        {
            if (_unsequencedHashCodeVersion != _version) {
                _unsequencedHashCodeVersion = _version;
                _unsequencedHashCode = this.GetUnsequencedHashCode(EqualityComparer);
            }

            return _unsequencedHashCode;
        }

        #endregion

        #endregion

        #region Events        

        public event EventHandler CollectionChanged
        {
            add {
                _collectionChanged += value;
                ActiveEvents |= Changed;
            }
            remove {
                _collectionChanged -= value;
                if (_collectionChanged == null) {
                    ActiveEvents &= ~Changed;
                }
            }
        }

        public event EventHandler<ClearedEventArgs> CollectionCleared
        {
            add {
                _collectionCleared += value;
                ActiveEvents |= Cleared;
            }
            remove {
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
            add {
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
            add {
                _itemsRemoved += value;
                ActiveEvents |= Removed;
            }
            remove {
                _itemsRemoved -= value;
                if (_itemsRemoved == null) {
                    ActiveEvents &= ~Removed;
                }
            }
        }

        #endregion

        #region Private

        private bool CheckVersion(int version)
        {
            if (_version == version) {
                return true;
            }

            throw new InvalidOperationException(CollectionWasModified);
        }

        private void ReindexPrivate(int index)
        {
            ReindexPrivate(index, Count);
        }

        private void ReindexPrivate(int index, int end)
        {
            for (var i = index; i < end; i++) {
                _itemIndex[_items[i]] = i;
            }
        }

        private bool FindOrAddToHashPrivate(T item, int index)
        {
            #region Code Contract            

            #endregion

            if (_itemIndex.ContainsKey(item)) {
                return true;
            }
            _itemIndex[item] = index;
            return false;
        }

        private void InsertPrivate(int index, T item)
        {
            #region Code Contracts 

            // Argument must within bounds
            Requires(index >= 0, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);

            // no need: Requires(item != null, ArgumentMustBeNonNull);

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
            //Views: Count++, und.Count++
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
            if ((uint) newCapacity > MaxArrayLength) {
                // why uint ???
                newCapacity = MaxArrayLength;
            }
            else if (newCapacity < MinArrayLength) {
                newCapacity = MinArrayLength;
            }

            if (newCapacity < requiredCapacity) {
                newCapacity = requiredCapacity;
            }

            Capacity = newCapacity;
        }

        [Pure]
        private bool Equals(T x, T y) => EqualityComparer.Equals(x, y);

        private void ClearPrivate()
        {
            _items = EmptyArray;
            _itemIndex.Clear();
            Count = 0;
        }

        private T RemoveAtPrivate(int index)
        {
            UpdateVersion();
            // new
            //View: index += _offsetField;
            //View: FixViewsBeforeSingleRemovePrivate(index);

            Count--;
            // VIew:
            //if (_underlying != null)
            //{
            //    _underlying.Count--;
            //}
            // -new

            var item = _items[index];
            var underlyingCount = /*View: (_underlying ?? this).*/ Count;
            if (index < underlyingCount) // if (--Count > index)
            {
                Array.Copy(_items, index + 1, _items, index, underlyingCount - index);
            }
            _items[underlyingCount] = default(T);

            _itemIndex.Remove(item);
            //??? reindex()
            return item;
        }

        private void UpdateVersion() => _version++;

        private void RaiseForAdd(T item)
        {
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForAddRange(T[] array)
        {
            if (ActiveEvents.HasFlag(Added)) {
                foreach (var item in array) {
                    OnItemsAdded(item, 1);
                }
            }

            OnCollectionChanged();
        }

        private void RaiseForClear(int count)
        {
            OnCollectionCleared(true, count);
            OnCollectionChanged();
        }

        private void RaiseForRemove(T removedItem)
        {
            OnItemsRemoved(removedItem, 1);
            OnCollectionChanged();
        }

        private void RaiseForUpdate(T item, T olditem)
        {
            OnItemsRemoved(olditem, 1);
            OnItemsAdded(item, 1);            
            OnCollectionChanged();
        }

        #region InvokingMethods

        private void OnItemsAdded(T item, int count) => _itemsAdded?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnCollectionChanged() => _collectionChanged?.Invoke(this, EventArgs.Empty);

        private void OnCollectionCleared(bool full, int count, int? start = null) => _collectionCleared?.Invoke(this, new ClearedEventArgs(full, count, start));

        private void OnItemsRemoved(T item, int count) => _itemsRemoved?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        #endregion

        #endregion

        #region Nested types        

        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class Duplicates : CollectionValueBase<T>, ICollectionValue<T>
        {
            #region Fields

            private readonly HashedArrayList<T> _base;
            private readonly int _version;
            private readonly T _item;
            private HashedArrayList<T> _list;

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
            public Duplicates(HashedArrayList<T> list, T item)
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
                get { return base.IsValid; }

                protected internal set { base.IsValid = value; }
            }

            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get {
                    CheckVersion();

                    return List.Count;
                }
            }

            public override Speed CountSpeed
            {
                get {
                    CheckVersion();
                    // TODO: Always use Linear?
                    //return _list == null ? Linear : Constant;
                    return Constant;
                }
            }

            public override bool IsEmpty => /*CheckVersion() &*/ List.IsEmpty;

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

                CheckVersion();
                int index;
                if ((index = _base.IndexOf(_item)) >= 0)
                {
                    yield return _base._items[index];
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

            private ICollectionValue<T> List
            {
                get {
                    if (_list != null) {
                        return _list;
                    }

                    var index = _base.IndexOf(_item);
                    return index < 0 ?
                        new HashedArrayList<T>() :
                        new HashedArrayList<T> { _base._items[index] };
                }
            }

            #endregion
        }

        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class ItemSet : CollectionValueBase<T>, ICollectionValue<T> // ??? CollectionValueBase
        {
            #region Fields

            private readonly HashedArrayList<T> _base;
            private readonly int _version;
            // TODO: Replace with HashedArrayList<T>
            //private SCG.HashSet<T> _set;
            private HashedArrayList<T> _set;

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
            public ItemSet(HashedArrayList<T> list)
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

            // ? Why do we need it? Isn't that enough to overrire GetEnumerator()?
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
                    //var set = new SCG.HashSet<T>(_base.EqualityComparer);
                    var set = new HashedArrayList<T>(_base.EqualityComparer);

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
            private ICollectionValue<T> Set => _set ?? (_set = new HashedArrayList<T>(_base, _base.EqualityComparer));

            #endregion
        }


        #endregion
        

    }
}