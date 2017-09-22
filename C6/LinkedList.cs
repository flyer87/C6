﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

using C6.Collections;
using C6.Contracts;

using static System.Diagnostics.Contracts.Contract;

using static C6.Collections.ExceptionMessages;
using static C6.Contracts.ContractMessage;
using static C6.EventTypes;
using static C6.Speed;

using SC = System.Collections;
using SCG = System.Collections.Generic;


namespace C6
{
    public class LinkedList<T> : ICollection<T>
    {
        #region Fields

        private Node _starSentinel, _endSentinel;

        private WeakViewList<LinkedList<T>> _views;
        private LinkedList<T> _underlying; // always null for a proper list
        private WeakViewList<LinkedList<T>>.Node _myWeakReference; // always null for a proper list

        private int _version, _sequencedHashCodeVersion = -1, _unsequencedHashCodeVersion = -1;
        private int _sequencedHashCode, _unsequencedHashCode;

        private event EventHandler _collectionChanged;
        private event EventHandler<ClearedEventArgs> _collectionCleared;
        private event EventHandler<ItemAtEventArgs<T>> _itemInserted, _itemRemovedAt;
        private event EventHandler<ItemCountEventArgs<T>> _itemsAdded, _itemsRemoved;

        #endregion

        #region Constructors

        public LinkedList(SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
        {
            IsValid = true;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default;
            AllowsNull = allowsNull;

            _starSentinel = new Node(default(T));
            _endSentinel = new Node(default(T));
            _starSentinel.Next = _endSentinel;
            _endSentinel.Prev = _starSentinel;
        }

        public LinkedList(SCG.IEnumerable<T> items, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            : this(equalityComparer, allowsNull)
        {
            #region Code Contracts

            // ReSharper disable InvocationIsSkipped
            // Argument must be non-null
            Requires(items != null, ArgumentMustBeNonNull);

            // not the same instance
            // Ensures(!ReferenceEquals(_items, items)); ???
            // ReSharper enable InvocationIsSkipped

            #endregion

            AddRange(items); // ??? do we need it
        }

        #endregion

        #region Properties

        #region ICollectionValue

        public bool IsValid { get; private set; }
        public bool AllowsNull { get; private set; }
        public int Count { get; private set; }
        public Speed CountSpeed => Constant;
        public bool IsEmpty => Count == 0;

        #endregion

        #region IListenable

        public EventTypes ActiveEvents { get; private set; }
        public EventTypes ListenableEvents => All;

        #endregion

        #region IExtensible

        public virtual bool AllowsDuplicates => true;
        public virtual bool DuplicatesByCounting => false;
        public virtual SCG.IEqualityComparer<T> EqualityComparer { get; }
        public virtual bool IsFixedSize => false;
        public virtual bool IsReadOnly => false;

        #endregion

        #region ICollection

        public Speed ContainsSpeed => Linear;

        #endregion

        #region IList

        public LinkedList<T> Underlying => _underlying; // ??? do it IList<T>

        #endregion

        private int UnderlyingCount => (Underlying ?? this).Count;

        #endregion

        #region Public Methods

        #region ICollectionValue

        public virtual T Choose() => First;

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
                array[arrayIndex++] = item;
        }

        public virtual T[] ToArray()
        {
            var array = new T[Count];
            CopyTo(array, 0);
            return array;
        }

        public virtual bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider)
            => Showing.Show(this, stringBuilder, ref rest, formatProvider);

        public virtual string ToString(string format, IFormatProvider formatProvider)
            => Showing.ShowString(this, format, formatProvider);

        public override string ToString() => ToString(null, null);

        #endregion

        #region IExtensible

        public virtual bool Add(T item)
        {
            #region Code Contracts            

            // The version is updated            
            Ensures(_version != OldValue(_version));

            #endregion

            InsertPrivate(Count, _endSentinel, item);
            (_underlying ?? this).RaiseForAdd(item);
            return true;
        }

        public virtual bool AddRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            // !@ Ensures(this.IsSameSequenceAs(OldValue((_underlying ?? this).ToArray())) || _version != OldValue(_version));           

            #endregion

            // TODO: Handle ICollectionValue<T> and ICollection<T>
            // TODO: Avoid creating an array? Requires a lot of extra code, since we need to properly handle items already added from a bad enumerable
            var array = items.ToArray();
            if (array.IsEmpty())
            {
                return false;
            }

            InsertRangePrivate(Count, array);
            RaiseForAddRange(array);
            return true;
        }

        #endregion

        #region ICollection

        public virtual void Clear()
        {
            if (IsEmpty)
            {
                return;
            }

            //View: for view RemoveIndexRange()

            var oldCount = Count;
            ClearPrivate();
            (_underlying ?? this).RaiseForClear(oldCount); // View(Offset, oldCount) ???
        }

        public bool Contains(T item) => IndexOf(item) >= 0;

        public bool ContainsRange(SCG.IEnumerable<T> items)
        {
            if (items.IsEmpty())
                return true;

            if (IsEmpty)
                return false;

            var itemsToContain = new LinkedList<T>(items, EqualityComparer, AllowsNull);

            if (itemsToContain.Count > Count)
                return false;

            return this.Any(item => itemsToContain.Remove(item) && itemsToContain.IsEmpty);
        }

        public int CountDuplicates(T item)
        {            
            return item == null ? this.Count(x => x == null) : this.Count(x => Equals(x, item));
        }

        public bool Find(ref T item)
        {
            var node = _starSentinel.Next;
            var index = 0;
            if (!FindNodePrivate(item, ref node, ref index))
            {
                return false;
            }

            item = node.item;
            return true;
        }

        public ICollectionValue<T> FindDuplicates(T item)
            => new Duplicates(this, item);

        public bool FindOrAdd(ref T item) => Find(ref item) ? true : !Add(item);

        // TODO: Update hash code when items are added, if the hash code version is not equal to -1
        public virtual int GetUnsequencedHashCode()
        {
            if (_unsequencedHashCodeVersion == _version)
            {
                return _unsequencedHashCode;
            }

            _unsequencedHashCodeVersion = _version;
            _unsequencedHashCode = this.GetUnsequencedHashCode(EqualityComparer);
            return _unsequencedHashCode;
        }

        public ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities()
        {
            throw new NotImplementedException();
        }

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
            var index = 0;
            var node = _starSentinel.Next;
            removedItem = default(T);

            if (!FindNodePrivate(item, ref node, ref index))
                return false;

            removedItem = RemoveAtPrivate(node, index);
            (_underlying ?? this).RaiseForRemove(removedItem);
            return true;
        }

        public bool RemoveDuplicates(T item)
        {
            throw new NotImplementedException();
        }

        public bool RemoveRange(SCG.IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public bool RetainRange(SCG.IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public ICollectionValue<T> UniqueItems()              
            => new ItemSet(this);
        
        public bool UnsequencedEquals(ICollection<T> otherCollection)        
            => this.UnsequencedEquals(otherCollection, EqualityComparer);
        
        public bool Update(T item)
        {
            T oldItem;
            return Update(item, out oldItem);
        }

        public bool Update(T item, out T oldItem)
        {
            var node = _starSentinel.Next;
            var index = 0;            
            if (!FindNodePrivate(item, ref node, ref index))
            {
                oldItem = default(T);
                return false;
            }

            UpdaVersion();

            oldItem = node.item;
            node.item = item;

            (_underlying ?? this).RiaseForUpdate(item, oldItem);
            return true;
        }

        public bool UpdateOrAdd(T item)
        {
            T olditem;
            return UpdateOrAdd(item, out olditem);
        }

        public bool UpdateOrAdd(T item, out T oldItem) => Update(item, out oldItem) ? true : !Add(item);
                
        #endregion

        #region IIndexed

        public virtual int IndexOf(T item)
        {
            var node = _starSentinel.Next;
            var index = 0;
            FindNodePrivate(item, ref node, ref index); 
            return index;
        }

        #endregion

        #region IList

        public virtual T First => _starSentinel.Next.item;


        #endregion

        #endregion

        #region Events

        public virtual event EventHandler CollectionChanged
        {
            add {
                _collectionChanged += value;
                ActiveEvents |= Changed;
            }
            remove {
                _collectionChanged -= value;
                if (_collectionChanged == null)
                {
                    ActiveEvents &= ~Changed;
                }
            }
        }

        public virtual event EventHandler<ClearedEventArgs> CollectionCleared
        {
            add {
                _collectionCleared += value;
                ActiveEvents |= Cleared;
            }
            remove {
                _collectionCleared -= value;
                if (_collectionCleared == null)
                {
                    ActiveEvents &= ~Cleared;
                }
            }
        }

        public virtual event EventHandler<ItemAtEventArgs<T>> ItemInserted
        {
            add {
                _itemInserted += value;
                ActiveEvents |= Inserted;
            }
            remove {
                _itemInserted -= value;
                if (_itemInserted == null)
                {
                    ActiveEvents &= ~Inserted;
                }
            }
        }

        public virtual event EventHandler<ItemAtEventArgs<T>> ItemRemovedAt
        {
            add {
                _itemRemovedAt += value;
                ActiveEvents |= RemovedAt;
            }
            remove {
                _itemRemovedAt -= value;
                if (_itemRemovedAt == null)
                {
                    ActiveEvents &= ~RemovedAt;
                }
            }
        }

        public virtual event EventHandler<ItemCountEventArgs<T>> ItemsAdded
        {
            add {
                _itemsAdded += value;
                ActiveEvents |= Added;
            }
            remove {
                _itemsAdded -= value;
                if (_itemsAdded == null)
                {
                    ActiveEvents &= ~Added;
                }
            }
        }

        public virtual event EventHandler<ItemCountEventArgs<T>> ItemsRemoved
        {
            add {
                _itemsRemoved += value;
                ActiveEvents |= Removed;
            }
            remove {
                _itemsRemoved -= value;
                if (_itemsRemoved == null)
                {
                    ActiveEvents &= ~Removed;
                }
            }
        }

        #endregion

        #region Explicit implemntations

        SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        public SCG.IEnumerator<T> GetEnumerator() // overrides valuebase 
        {
            var version = _underlying?._version ?? _version; // ??? underlying

            var cursor = _starSentinel.Next;
            while (cursor != _endSentinel && CheckVersion(version))
            {
                yield return cursor.item;
                cursor = cursor.Next;
            }
        }

        #region ICollection

        void SCG.ICollection<T>.Add(T item) => Add(item);

        #endregion

        #endregion

        #region Private Methods

        [Pure]
        private bool Equals(T x, T y) => EqualityComparer.Equals(x, y);

        private void InsertPrivate(int index, Node succ, T item)
        {
            #region Code Contracts

            // Argument must be within bounds
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);

            #endregion

            UpdaVersion();

            var node = new Node(item, succ.Prev, succ);
            succ.Prev.Next = node;
            succ.Prev = node;

            Count++;
            if (_underlying != null)
            {
                _underlying.Count++;
            }
            //View: fixViewsAfterInsert(succ, newnode.prev, 1, Offset + index);
        }

        private void InsertRangePrivate(int index, SCG.IEnumerable<T> items)
        {
            #region Code Contracts

            // Argument must be within bounds
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || ForAll(items, item => item != null), ItemsMustBeNonNull);

            #endregion

            UpdaVersion();

            Node cursor;
            var succ = index == Count ? _endSentinel : GetNode(index);
            var pred = cursor = succ.Prev;

            var count = 0;
            foreach (var item in items)
            {
                var tmp = new Node(item, cursor, null);
                cursor.Next = tmp; // == pred.Next := first temporary
                count++;
                cursor = tmp;
            }

            if (count == 0) // no need ??? The same reason as down!
                return;

            succ.Prev = cursor;
            cursor.Next = succ;

            Count += count;
            if (_underlying != null)
                _underlying.Count += count;

            if (count > 0) // no need! We have array.IsEmpty() check in the public methods ???
            {
                //fixViewsAfterInsert(succ, pred, count, offset + i);                
            }
        }

        private void ClearPrivate()
        {
            UpdaVersion();

            //View: FixViewsBeforeRemovePrivate(0, Count);            
            _starSentinel.Next = _endSentinel; //_starSentinel.Prev = null;
            _endSentinel.Prev = _starSentinel; //_endSentinel.Next = null;

            if(_underlying != null)
            {
                _underlying.Count -= Count;
            }
            Count = 0;
        }

        private void UpdaVersion() => _version++;

        private bool CheckVersion(int version)
        {
            if (_version == version)
            {
                return true;
            }

            // See https://msdn.microsoft.com/library/system.collections.ienumerator.movenext.aspx
            throw new InvalidOperationException(CollectionWasModified);
        }

        private Node GetNode(int index)
        {
            #region Code Contracts                        

            // ReSharper disable InvocationIsSkipped
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index < Count, ArgumentMustBeWithinBounds);
            // ReSharper enable InvocationIsSkipped

            #endregion

            if (index < Count / 2)
            {
                // Closer to front
                var node = _starSentinel;
                for (var i = 0; i <= index; i++)
                    node = node.Next;

                return node;
            }
            else
            {
                // Closer to end
                var node = _endSentinel;
                for (var i = Count; i > index; i--)
                    node = node.Prev;

                return node;
            }
        }

        private bool FindNodePrivate(T item, ref Node node, ref int index) // FIFO style
        {                        
            while (node != _endSentinel)
            {
                if (Equals(item, node.item))
                {
                    return true;
                }

                index++;
                node = node.Next;
            }

            index = ~Count;
            return false;
        }

        private T RemoveAtPrivate(Node node, int index)
        {
            UpdaVersion();

            //View: fixViewsBeforeSingleRemove(node, Offset + index);
            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;

            Count--;
            if (_underlying != null)
                _underlying.Count--;

            return node.item;
        }




        private void RaiseForAdd(T item)
        {
            if (!ActiveEvents.HasFlag(Added))
                return;

            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForAddRange(SCG.IEnumerable<T> items)
        {
            if (!ActiveEvents.HasFlag(Added))
            {
                return;
            }

            foreach (var item in items)
            {
                OnItemsAdded(item, 1);
            }

            OnCollectionChanged();
        }

        private void RaiseForClear(int count)
        {
            if (ActiveEvents == None)
            {
                return;
            }

            OnCollectionCleared(true, count);
            OnCollectionChanged();
        }

        private void RaiseForRemove(T item)
        {
            if (!ActiveEvents.HasFlag(Removed))
                return;

            OnItemsRemoved(item, 1);
            OnCollectionChanged();
        }

        private void RiaseForUpdate(T item, T oldItem)
        {
            Requires(Equals(item, oldItem));
            // ActiveEvents check ???

            OnItemsRemoved(oldItem, 1);
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        #region Invoking methods

        private void OnCollectionChanged()
            => _collectionChanged?.Invoke(this, EventArgs.Empty);

        private void OnCollectionCleared(bool full, int count, int? start = null)
            => _collectionCleared?.Invoke(this, new ClearedEventArgs(true, count, start));

        private void OnItemsAdded(T item, int count)
            =>
                _itemsAdded?.Invoke(this, new ItemCountEventArgs<T>(item, count));


        private void OnItemsRemoved(T item, int count)
            => _itemsRemoved?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        #endregion

        #endregion

        #region Nested types

        /// <summary>
        /// Something ???
        /// </summary>
        /// <typeparam name="V"></typeparam>
        private sealed class Node // Why not Node<T> ??
        {
            public Node Next; // why public ???
            public Node Prev; // why public ???
            public T item;

            internal Node(T item) // Why internal ???; else :
            {
                this.item = item;
            }

            internal Node(T item, Node prev, Node next)
            {
                this.item = item;
                this.Prev = prev;
                this.Next = next;
            }

            public override string ToString()
            {
                return $"Node(item={item})";
            }
        }

        private sealed class WeakViewList<V> where V : class
        {
            Node start;


            [Serializable]
            internal class Node
            {
                internal WeakReference weakview;
                internal Node prev, next;

                internal Node(V view)
                {
                    weakview = new WeakReference(view);
                }
            }


            internal Node Add(V view)
            {
                Node newNode = new Node(view);
                if (start != null)
                {
                    start.prev = newNode;
                    newNode.next = start;
                }
                start = newNode;
                return newNode;
            }

            internal void Remove(Node n)
            {
                if (n == start)
                {
                    start = start.next;
                    if (start != null)
                        start.prev = null;
                }
                else
                {
                    n.prev.next = n.next;
                    if (n.next != null)
                        n.next.prev = n.prev;
                }
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
                    V view = o is V ? (V) o : null;
                    if (view == null)
                        Remove(n);
                    else
                        yield return view;
                    n = n.next;
                }
            }
        }

        /*
    ActiveEvents 
    All 		
    Apply 		
    Choose - ok		
    CopyTo - ok 
    Count - ok		
    CountSpeed - ok
    Exists 		
    Filter 		
    Find 
    IsEmpty - ok
    ListenableEvents 
    ToArray - ok         
*/
        // TODO: Introduce base class?
        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class ItemSet : CollectionValueBase<T>, ICollectionValue<T> // ??? CollectionValues base
        {
            #region Fields

            private readonly LinkedList<T> _base;
            private readonly int _version;
            // TODO: Replace with C6.HashSet<T> in future
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
            public ItemSet(LinkedList<T> list)
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

            // TODO: Replace with C6.HashSet<T>!
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

            private readonly LinkedList<T> _base;
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
            public Range(LinkedList<T> list, int startIndex, int count, EnumerationDirection direction)
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
                //var index = _direction.IsForward() ? _startIndex + _count - 1 : _startIndex;
                var node = _direction.IsForward() ? _base._starSentinel.Next : _base._endSentinel.Prev;
                return node.item;
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                if (_direction.IsForward())
                {
                    // Copy array directly
                    //Array.Copy(_base._items, _base.Offset + _startIndex, array, arrayIndex, _count); // Offset!!
                    throw new NotImplementedException();
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
                throw new NotImplementedException();

                //var items = _base._items;
                //for (var i = 0; i < Count; i++)
                //{
                //    yield return items[_base.Offset + _startIndex + _sign * i];
                //}
            }

            public override int GetHashCode()
            {
                CheckVersion();
                return base.GetHashCode();
            }

            public override T[] ToArray()// ??? only this.ToArray(). Reimplement?
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

        // TODO: Explicitly check against null to avoid using the (slower) equality comparer
        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class Duplicates : CollectionValueBase<T>, ICollectionValue<T>
        {
            #region Fields

            private readonly LinkedList<T> _base;
            private readonly int _version;
            private readonly T _item;
            private LinkedList<T> _list;

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
            public Duplicates(LinkedList<T> list, T item)
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
                    var list = new LinkedList<T>(allowsNull: AllowsNull);
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

            private LinkedList<T> List => _list != null ? _list : (_list = new LinkedList<T>(_base.Where(x => _base.Equals(x, _item)), allowsNull: AllowsNull));

            #endregion
        }

        #endregion
    }
}