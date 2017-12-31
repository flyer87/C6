using System;
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

namespace C6.Collections
{
    public class LinkedList<T> : IList<T>, IStack<T>, IQueue<T>
    {
        #region Fields

        private Node _startSentinel, _endSentinel;

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

        #region Code Contracts

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            // ReSharper disable InvocationIsSkipped            

            // Start sentil is not null
            Invariant(_startSentinel != null);

            // End sentil is not null
            Invariant(_endSentinel != null);

            // All items must be non-null if collection disallows null values
            Invariant(AllowsNull || ForAll(this, item => item != null));

            // Equality comparer is non-null
            Invariant(EqualityComparer != null);

            // If Count is 0 then _startSentinel.Next points to _endSentinel
            Invariant(!(Count == 0 && _startSentinel != null) || _startSentinel.Next == _endSentinel);

            // If Count is 0 then _endSentinel.Prev points to _startSentinel
            Invariant(!(Count == 0 && _endSentinel != null) || _endSentinel.Prev == _startSentinel);

            // Each .Prev points to the correct node
            Node node = _startSentinel.Next, prev = _startSentinel;
            Invariant(ForAll(0, Count, i => {
                var result = node.Prev == prev;
                prev = node;
                node = node.Next;
                return result;
            }));

            // Next pointers are not null
            node = _startSentinel.Next;
            prev = _startSentinel;
            Invariant(ForAll(0, Count, i => {
                var result = node != null;

                prev = node;
                node = node.Next;
                return result;
            }));

            #region Views

            // TODO: Find better way for doing that 
            /* var nodes = new Node[UnderlyingCount];  
            var index = 0;            
            var cursor = _startSentinel.Next;
            while (cursor != _endSentinel)
            {
                nodes[index++] = cursor;                
                cursor = cursor.Next;
            }

            Invariant((_underlying ?? this)._views == null ||
                      ForAll((_underlying ?? this)._views, v => true
                          // v._startSentinel == nodes[v.Offset] &&                              
                          // (v._endSentinel == nodes[v.Offset + v.Count + 1]) &&                           

                      )); 
            */

            // Offset and Count of each view is within bounds
            Invariant((_underlying ?? this)._views == null ||
                      ForAll((_underlying ?? this)._views, v =>
                          v.Offset >= 0 &&
                          // v.IsValid &&                                                                                    
                          v.Offset + v.Count >= 0 &&
                          v.Offset + v.Count <= UnderlyingCount
                      ));

            // Each view points to the same _views as the underlying; 
            // Each view have the correct underlying list
            Invariant((_underlying ?? this)._views == null ||
                      ForAll((_underlying ?? this)._views, v =>
                          // v.IsValid &&                                                                                
                              v._views == (_underlying ?? this)._views &&
                              v._underlying == (_underlying ?? this))
            );            

            #endregion

            // ReSharper restore InvocationIsSkipped            
        }

        #endregion

        #region Constructors

        public LinkedList(SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
        {
            #region Code Contracts

            // ReSharper disable InvocationIsSkipped

            // Value types cannot be null
            Requires(!typeof(T).IsValueType || !allowsNull, AllowsNullMustBeFalseForValueTypes);

            // ReSharper restore InvocationIsSkipped

            #endregion

            IsValid = true;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default;
            AllowsNull = allowsNull;

            _startSentinel = new Node(default(T));
            _endSentinel = new Node(default(T));
            _startSentinel.Next = _endSentinel;
            _endSentinel.Prev = _startSentinel;
        }

        public LinkedList(SCG.IEnumerable<T> items, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            : this(equalityComparer, allowsNull)
        {
            #region Code Contracts
            // ReSharper disable InvocationIsSkipped
            // Argument must be non-null
            Requires(items != null, ArgumentMustBeNonNull);

            // All items must be non-null if collection disallows null values
            Requires(allowsNull || ForAll(items, item => item != null), ItemsMustBeNonNull);

            // Value types cannot be null
            Requires(!typeof(T).IsValueType || !allowsNull, AllowsNullMustBeFalseForValueTypes);

            // ReSharper restore InvocationIsSkipped
            #endregion

            AddRange(items);
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

        #region IDirectedCollectionValues

        public virtual EnumerationDirection Direction => EnumerationDirection.Forwards;

        #endregion

        #region IIndexed

        public Speed IndexingSpeed => Linear;

        #endregion

        #region IList

        public int Offset { get; private set; }

        public IList<T> Underlying => _underlying;

        public virtual T First => _startSentinel.Next.item;

        public T Last => _endSentinel.Prev.item;

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
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            // TODO: Handle ICollectionValue<T> and ICollection<T>
            // TODO: Avoid creating an array? Requires a lot of extra code, since we need to properly handle items already added from a bad enumerable
            var array = items.ToArray();
            if (array.IsEmpty()) {
                return false;
            }

            InsertRangePrivate(Count, array);
            (_underlying ?? this).RaiseForAddRange(array);
            return true;
        }

        #endregion

        #region ICollection

        public virtual void Clear()
        {
            #region Code Contracts            

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (IsEmpty) {
                return;
            }

            var oldCount = Count;
            ClearPrivate();
            //(_underlying ?? this).RaiseForClear(oldCount);
            (_underlying ?? this).RaiseForRemoveIndexRange(Offset, oldCount);
        }

        public virtual bool Contains(T item) => IndexOf(item) >= 0;

        public virtual bool ContainsRange(SCG.IEnumerable<T> items)
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

        public virtual int CountDuplicates(T item)        
            => item == null ? this.Count(x => x == null) : this.Count(x => Equals(x, item));        

        public virtual bool Find(ref T item)
        {
            var node = _startSentinel.Next;
            var index = 0;
            if (!FindNodePrivate(item, ref node, ref index, EnumerationDirection.Forwards)) {
                return false;
            }

            item = node.item;
            return true;
        }

        public virtual ICollectionValue<T> FindDuplicates(T item) => new Duplicates(this, item);

        public virtual bool FindOrAdd(ref T item) => Find(ref item) || !Add(item);

        // TODO: Update hash code when items are added, if the hash code version is not equal to -1
        public virtual int GetUnsequencedHashCode()
        {
            if (_unsequencedHashCodeVersion == _version) {
                return _unsequencedHashCode;
            }

            _unsequencedHashCodeVersion = _version;
            _unsequencedHashCode = this.GetUnsequencedHashCode(EqualityComparer);
            return _unsequencedHashCode;
        }

        public virtual ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities()
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
            #region Code Contracts            
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            var index = 0;
            var node = _startSentinel.Next;
            removedItem = default(T);

            if (!FindNodePrivate(item, ref node, ref index, EnumerationDirection.Forwards))
                return false;

            removedItem = RemoveAtPrivate(node, index);
            (_underlying ?? this).RaiseForRemove(removedItem);
            return true;
        }

        public virtual bool RemoveDuplicates(T item)
            => item == null ? RemoveAllWhere(x => x == null) : RemoveAllWhere(x => Equals(x, item));

        public virtual bool RemoveRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts            
            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (IsEmpty || items.IsEmpty()) {
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

            if (IsEmpty) {
                return false;
            }

            if (items.IsEmpty()) {
                var itemsRemoved = new T[Count];
                CopyTo(itemsRemoved, 0);
                if (_underlying == null) // proper list     
                {
                    ClearPrivate(); 
                }
                else // view
                {
                    RemoveIndexRangePrivate(0, Count);
                }

                (_underlying ?? this).RaiseForRemoveAllWhere(itemsRemoved);
                return true;
            }

            var itemsToRemove = new ArrayList<T>(items, EqualityComparer, AllowsNull);
            return RemoveAllWhere(item => !itemsToRemove.Remove(item));
        }

        public virtual ICollectionValue<T> UniqueItems() => new ItemSet(this);

        public virtual bool UnsequencedEquals(ICollection<T> otherCollection)
            => this.UnsequencedEquals(otherCollection, EqualityComparer);

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

            var node = _startSentinel.Next;
            var index = 0;
            if (!FindNodePrivate(item, ref node, ref index, EnumerationDirection.Forwards)) {
                oldItem = default(T);
                return false;
            }

            UpdateVersion();

            oldItem = node.item;
            node.item = item;

            (_underlying ?? this).RaiseForUpdate(item, oldItem);
            return true;
        }

        public virtual bool UpdateOrAdd(T item)
        {
            T olditem;
            return UpdateOrAdd(item, out olditem);
        }

        public virtual bool UpdateOrAdd(T item, out T oldItem) => Update(item, out oldItem) || !Add(item);

        #endregion

        #region ISequenced

        public virtual int GetSequencedHashCode()
        {
            if (_sequencedHashCodeVersion != _version) {
                _sequencedHashCodeVersion = _version;
                _sequencedHashCode = this.GetSequencedHashCode(EqualityComparer);
            }

            return _sequencedHashCode;
        }

        public virtual bool SequencedEquals(ISequenced<T> otherCollection)
        {
            return this.SequencedEquals(otherCollection, EqualityComparer);
        }

        #endregion

        #region IDirectedCollectionValue

        public virtual IDirectedCollectionValue<T> Backwards()
        {
            return new Range(this, Count - 1, Count, EnumerationDirection.Backwards);
        }

        #endregion

        #region IIndexed

        public virtual IDirectedCollectionValue<T> GetIndexRange(int startIndex, int count)
            => new Range(this, startIndex, count, EnumerationDirection.Forwards);

        public virtual T this[int index]
        {
            get { return GetNodeAtPrivate(index).item; }
            set {
                #region Code Contracts

                // The version is updated
                Ensures(_version != OldValue(_version));

                #endregion

                UpdateVersion();

                var node = GetNodeAtPrivate(index);
                var oldItem = node.item;
                node.item = value;

                (_underlying ?? this).RaiseForIndexSetter(oldItem, value, Offset + index);
            }
        }

        public virtual int IndexOf(T item)
        {
            #region Code Contracts                                    
            // Result is a valid index
            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : ~Result<int>() == Count);

            // Item at index is the first equal to item                        
            Ensures(Result<int>() < 0 || !this.Take(Result<int>()).Contains(item, EqualityComparer) && EqualityComparer.Equals(item, this.ElementAt(Result<int>())));

            #endregion

            var node = _startSentinel.Next;
            var index = 0;
            FindNodePrivate(item, ref node, ref index, EnumerationDirection.Forwards);
            return index;
        }

        public virtual int LastIndexOf(T item)
        {
            #region Code Contracts                        
            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : ~Result<int>() == Count);

            // Item at index is the first equal to item
            Ensures(Result<int>() < 0 || !this.Skip(Result<int>() + 1).Contains(item, EqualityComparer) && EqualityComparer.Equals(item, this.ElementAt(Result<int>())));

            #endregion

            var node = _endSentinel.Prev;
            var index = Count - 1;
            FindNodePrivate(item, ref node, ref index, EnumerationDirection.Backwards);
            return index;
        }

        public virtual T RemoveAt(int index)
        {
            #region Code Contracts
            // If collection changes, the version is updated                      
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            var node = GetNodeAtPrivate(index);
            var item = RemoveAtPrivate(node, index);
            (_underlying ?? this).RaiseForRemoveAt(item, Offset + index);
            return item;
        }

        public virtual void RemoveIndexRange(int startIndex, int count)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (IsEmpty || count == 0) {
                return;
            }

            RemoveIndexRangePrivate(startIndex, count);
            // Commented, becase .Clear() in RemoveIndexRangePrivate reaiss the events
            //(_underlying ?? this).RaiseForRemoveIndexRange(Offset + startIndex, count);
        }

        #endregion

        #region IList

        public virtual void InsertFirst(T item) => Insert(0, item);

        public virtual void InsertLast(T item) => Insert(Count, item);

        public virtual void Insert(int index, T item)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            InsertPrivate(index, index == Count ? _endSentinel : GetNodeAtPrivate(index), item);
            (_underlying ?? this).RaiseForInsert(Offset + index, item);
        }

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

            if (array.IsEmpty()) {
                return;
            }

            InsertRangePrivate(index, array); 
            (_underlying ?? this).RaiseForInsertRange(Offset + index, array);
        }

        public virtual bool IsSorted(Comparison<T> comparison)
        {
            if (Count <= 1) {
                return true;
            }

            var prevNode = _startSentinel.Next;
            var node = prevNode.Next;

            while (node != _endSentinel) {
                if (comparison(prevNode.item, node.item) > 0) {
                    return false;
                }

                prevNode = prevNode.Next;
                node = node.Next;
            }

            return true;
        }

        public virtual bool IsSorted(SCG.IComparer<T> comparer) =>
            IsSorted((comparer ?? SCG.Comparer<T>.Default).Compare);

        public virtual bool IsSorted() => IsSorted(SCG.Comparer<T>.Default.Compare);

        public virtual T RemoveFirst() => RemoveAt(0);

        public virtual T RemoveLast() => RemoveAt(Count - 1);

        public virtual void Reverse()
        {
            #region Code Contracts            

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Count <= 1) {
                return;
            }

            UpdateVersion();

            Position[] positions = null;
            int poslow = 0, poshigh = 0;
            if (_views != null) {
                // TODO: SCG.Queue<T> should be replaced with C6.CircularQueue<T> when implemented
                SCG.Queue<Position> positionsQueue = null;
                foreach (var view in _views) {
                    if (view == this) {
                        continue;
                    }

                    switch (ViewPosition(view)) {
                        case MutualViewPosition.ContainedIn:
                            (positionsQueue ?? (positionsQueue = new SCG.Queue<Position>())).Enqueue(new Position(view, true));
                            positionsQueue.Enqueue(new Position(view, false));
                            break;
                        case MutualViewPosition.Overlapping:
                            view.Dispose();
                            break;
                        case MutualViewPosition.Contains:
                        case MutualViewPosition.NonOverlapping:
                            break;
                    }
                }

                if (positionsQueue != null) {
                    positions = positionsQueue.ToArray();
                    Array.Sort(positions, PositionComparer.Default);

                    poshigh = positions.Length - 1;
                }
            }

            Node a = GetNodeAtPrivate(0), b = GetNodeAtPrivate(Count - 1);
            for (var i = 0; i < Count / 2; i++) {
                var swap = a.item;
                a.item = b.item;
                b.item = swap;

                if (positions != null)
                    MirrorViewSentinelsForReversePrivate(positions, ref poslow, ref poshigh, a, b, i);
                a = a.Next;
                b = b.Prev;
            }

            if (positions != null && Count % 2 != 0)
                MirrorViewSentinelsForReversePrivate(positions, ref poslow, ref poshigh, a, b, Count / 2);

            (_underlying ?? this).RaiseForReverse();
        }

        public virtual void Shuffle(Random random)
        {
            #region Code Contracts            

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Count <= 1) {
                return;
            }

            UpdateVersion();

            DisposeOverlappingViewsPrivate(false);

            // add to ArrayList) & shuffle
            var list = new ArrayList<T>(this);
            list.Shuffle(random);

            // put them back to the llist
            var index = 0;
            var cursor = _startSentinel.Next;
            while (cursor != _endSentinel) {
                cursor.item = list[index++];
                cursor = cursor.Next;
            }

            (_underlying ?? this).RaiseForShuffle();
        }

        public virtual void Shuffle() => Shuffle(new Random());

        public virtual IList<T> Slide(int offset) => Slide(offset, Count);

        public virtual IList<T> Slide(int offset, int count)
        {
            TrySlide(offset, count);
            return this;
        }

        public virtual bool TrySlide(int offset, int count)
        {
            var newOffset = Offset + offset;
            if (newOffset < 0 || count < 0 || newOffset + count > UnderlyingCount) {
                return false;
            }

            var oldOffset = Offset;
            GetPairPrivate(offset - 1, offset + count, out _startSentinel, out _endSentinel,
                new[] { -oldOffset - 1, -1, Count, UnderlyingCount - oldOffset },
                new[] { _underlying._startSentinel, _startSentinel, _endSentinel, _underlying._endSentinel });

            UpdateVersion();

            Count = count;
            Offset += offset;
            return true;
        }

        public virtual bool TrySlide(int offset) => TrySlide(offset, Count);

        public virtual IList<T> Span(IList<T> other)
        {
            if (other.Offset + other.Count - Offset < 0)
                return null;

            return (_underlying ?? this).View(Offset, other.Offset + other.Count - Offset);
        }

        public virtual void Sort(SCG.IComparer<T> comparer)
        {
            #region Code Contracts            

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (comparer == null) {
                comparer = SCG.Comparer<T>.Default;
            }

            if (Count <= 1) {
                return;
            }

            if (IsSorted(comparer)) {
                return;
            }

            UpdateVersion();

            DisposeOverlappingViews(false);

            // Build a linked list of non-empty runs.
            // The prev field in first node of a run points to next run's first node
            var runTail = _startSentinel.Next;
            var prevNode = _startSentinel.Next;

            _endSentinel.Prev.Next = null;
            while (prevNode != null) {
                var node = prevNode.Next;

                while (node != null && comparer.Compare(prevNode.item, node.item) <= 0) {
                    prevNode = node;
                    node = prevNode.Next;
                }

                // Completed a run; prevNode is the last node of that run
                prevNode.Next = null; // Finish the run
                runTail.Prev = node; // Link it into the chain of runs
                runTail = node;
                if (comparer.Compare(_endSentinel.Prev.item, prevNode.item) <= 0)
                    _endSentinel.Prev = prevNode; // Update last pointer to point to largest

                prevNode = node; // Start a new run
            }

            // Repeatedly merge runs two and two, until only one run remains
            while (_startSentinel.Next.Prev != null) {
                var run = _startSentinel.Next;
                Node newRunTail = null;

                while (run != null && run.Prev != null) {
                    // At least two runs, merge
                    var nextRun = run.Prev.Prev;
                    var newrun = MergeRuns(run, run.Prev, comparer);

                    if (newRunTail != null)
                        newRunTail.Prev = newrun;
                    else
                        _startSentinel.Next = newrun;

                    newRunTail = newrun;
                    run = nextRun;
                }

                if (run != null) // Add the last run, if any
                    newRunTail.Prev = run;
            }

            _endSentinel.Prev.Next = _endSentinel;
            _startSentinel.Next.Prev = _startSentinel;

            //assert invariant();
            //assert isSorted();

            (_underlying ?? this).RaiseForSort();
        }

        public virtual void Sort() => Sort((SCG.IComparer<T>) null);

        public virtual void Sort(Comparison<T> comparison) => Sort(comparison.ToComparer());

        public virtual IList<T> View(int index, int count)
        {
            if (_views == null)
                _views = new WeakViewList<LinkedList<T>>();

            var view = (LinkedList<T>) MemberwiseClone();
            view._underlying = _underlying ?? this;
            view.Offset = Offset + index;
            view.Count = count;

            GetPairPrivate(index - 1, index + count, out view._startSentinel, out view._endSentinel,
                new[] { -1, Count }, new Node[] { _startSentinel, _endSentinel });
            
            view._myWeakReference = _views.Add(view);
            return view;
        }

        public virtual IList<T> ViewOf(T item)
        {
            var node = _startSentinel.Next;
            var index = 0;
            return !FindNodePrivate(item, ref node, ref index, EnumerationDirection.Forwards) ? null : View(index, 1);
        }

        public virtual IList<T> LastViewOf(T item)
        {
            var node = _endSentinel.Prev;
            var index = Count - 1;
            return !FindNodePrivate(item, ref node, ref index, EnumerationDirection.Backwards) ? null : View(index, 1);
        }

        #endregion

        #region IStack<T>

        public T Pop() => RemoveLast();

        public void Push(T item) => InsertLast(item);

        #endregion

        #region IQueue<T>

        public virtual T Dequeue() => RemoveFirst();

        public virtual void Enqueue(T item) => InsertLast(item);

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
                if (_collectionChanged == null) {
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
                if (_collectionCleared == null) {
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
                if (_itemInserted == null) {
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
                if (_itemRemovedAt == null) {
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
                if (_itemsAdded == null) {
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
                if (_itemsRemoved == null) {
                    ActiveEvents &= ~Removed;
                }
            }
        }

        #endregion

        #region Explicit implementations

        SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        public SCG.IEnumerator<T> GetEnumerator() // overrides valuebase 
        {
            #region Code Contracts   
            // ReSharper disable InvocationIsSkipped

            // Must be valid
            Requires(IsValid, MustBeValid);

            // The version is not updated
            Ensures(_version == OldValue(_version));

            // ReSharper enable InvocationIsSkipped
            #endregion

            var version = _version; //_underlying?._version ?? _version; HERE!

            var cursor = _startSentinel.Next;
            while (cursor != _endSentinel && CheckVersion(version)) {
                yield return cursor.item;
                cursor = cursor.Next;
            }
        }

        #region ICollection<T>

        void SCG.ICollection<T>.Add(T item) => Add(item);

        #endregion

        #region System.Collections.ICollection Members

        bool SC.ICollection.IsSynchronized => false;

        object SC.ICollection.SyncRoot
            => ((SC.ICollection) _underlying)?.SyncRoot ?? _startSentinel;

        void SC.ICollection.CopyTo(Array array, int index)
        {
            #region Code Contracts

            Requires(index >= 0, ArgumentMustBeWithinBounds);
            Requires(index + Count <= array.Length, ArgumentMustBeWithinBounds);

            #endregion

            try 
            {
                foreach (var item in this) {
                    array.SetValue(item, index++);
                }
            }
            catch (InvalidCastException) //catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Target array type is not compatible with the type of items in the collection.");
            }
        }

        #endregion

        #region System.Collections.IList Members

        int SC.IList.Add(object value)
        {
            try {
                return Add((T) value) ? Count - 1 : -1;
            }
            catch (InvalidCastException) {
                throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
            }
        }

        bool SC.IList.Contains(object value) => IsCompatibleObject(value) && Contains((T) value);

        int SC.IList.IndexOf(object value) => IsCompatibleObject(value) ? Math.Max(-1, IndexOf((T) value)) : -1;

        void SC.IList.Insert(int index, object value)
        {
            try {
                Insert(index, (T) value);
            }
            catch (InvalidCastException) {
                throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
            }
        }

        object SC.IList.this[int index]
        {
            get { return this[index]; }
            set {
                try {
                    this[index] = (T) value;
                }
                catch (InvalidCastException) {
                    throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
                }
            }
        }

        void SC.IList.Remove(object value)
        {
            if (IsCompatibleObject(value)) {
                Remove((T) value);
            }
        }

        void SC.IList.RemoveAt(int index) => RemoveAt(index);

        #endregion

        #region System.Collections.Generic.IList<T> Members

        // Explicit implementation is needed, since C6.IList<T>.IndexOf(T) breaks SCG.IList<T>.IndexOf(T)'s precondition: Result<T>() >= -1
        int SCG.IList<T>.IndexOf(T item) => Math.Max(-1, IndexOf(item));

        void SCG.IList<T>.RemoveAt(int index) => RemoveAt(index);

        #endregion

        #region IDisposable

        public virtual void Dispose() => DisposePrivate(false);

        #endregion


        #endregion

        #region Private Methods

        #region fixView utilities

        /// <summary>
        /// 
        /// </summary>
        /// <param name="added">The actual number of inserted nodes</param>
        /// <param name="pred">The predecessor of the inserted nodes</param>
        /// <param name="succ">The successor of the added nodes</param>
        /// <param name="realInsertionIndex"></param>
        private void FixViewsAfterInsertPrivate(Node succ, Node pred, int added, int realInsertionIndex)
        {
            if (_views == null) {
                return;
            }

            foreach (var view in _views) {
                if (view == this) {
                    continue;
                }

                if (view.Offset == realInsertionIndex && view.Count > 0)
                    view._startSentinel = succ.Prev;
                if (view.Offset + view.Count == realInsertionIndex)
                    view._endSentinel = pred.Next;
                if (view.Offset < realInsertionIndex && view.Offset + view.Count > realInsertionIndex)
                    view.Count += added;
                if (view.Offset > realInsertionIndex || (view.Offset == realInsertionIndex && view.Count > 0))
                    view.Offset += added;
            }
        }

        private void FixViewsBeforeSingleRemovePrivate(Node node, int realRemovalIndex)
        {
            if (_views == null) {
                return;
            }

            foreach (var view in _views) {
                if (view == this) {
                    continue;
                }

                if (view.Offset - 1 == realRemovalIndex)
                    view._startSentinel = node.Prev;
                if (view.Offset + view.Count == realRemovalIndex)
                    view._endSentinel = node.Next;
                if (view.Offset <= realRemovalIndex && view.Offset + view.Count > realRemovalIndex)
                    view.Count--;
                if (view.Offset > realRemovalIndex)
                    view.Offset--;
            }
        }

        private void FixViewsBeforeRemovePrivate(int start, int count, Node first, Node last)
        {
            if (_views == null) {
                return;
            }

            var clearend = start + count - 1;
            foreach (var view in _views) {
                if (view == this) {
                    continue;
                }

                int viewoffset = view.Offset, viewend = viewoffset + view.Count - 1;
                //sentinels
                if (start < viewoffset && viewoffset - 1 <= clearend)
                    view._startSentinel = first.Prev;
                if (start <= viewend + 1 && viewend < clearend)
                    view._endSentinel = last.Next;
                //offsets and sizes
                if (start < viewoffset) {
                    if (clearend < viewoffset)
                        view.Offset = viewoffset - count;
                    else {
                        view.Offset = start;
                        view.Count = clearend < viewend ? viewend - clearend : 0;
                    }
                }
                else if (start <= viewend)
                    view.Count = clearend <= viewend ? view.Count - count : start - viewoffset;
            }
        }

        private void DisposeOverlappingViewsPrivate(bool reverse)
        {
            if (_views == null)
                return;

            foreach (var view in _views) {
                if (view == this)
                    continue;

                switch (ViewPosition(view)) {
                    case MutualViewPosition.ContainedIn:
                        if (reverse) { }
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

        #endregion

        private void RemoveIndexRangePrivate(int startIndex, int count)
        {                      
            if (count == 0 || IsEmpty)
            {
                return;
            }

            UpdateVersion();
            
            View(startIndex, count).Clear();           
        }

        private void DisposePrivate(bool disposingUnderlying)
        {
            if (!IsValid)
                return;

            if (_underlying != null) {
                IsValid = false;
                if (!disposingUnderlying && _views != null)
                    _views.Remove(_myWeakReference);

                _endSentinel = null;
                _startSentinel = null;
                _underlying = null;
                _views = null;
                _myWeakReference = null;
            }
            else {
                //isValid = false;
                //endsentinel = null;
                //startsentinel = null;
                if (_views != null)
                    foreach (var view in _views)
                        view.DisposePrivate(true);
                //views = null;
                Clear();
            }
        }

        private bool RemoveAllWhere(Func<T, bool> predicate)
        {
            if (IsEmpty) {
                return false;
            }

            IExtensible<T> itemsRemoved = null;
            var shouldRememberItems = ActiveEvents.HasFlag(Removed);
            var viewHandler = new ViewHandler(this);

            int index = 0, countRemoved = 0, myoffset = Offset;
            var node = _startSentinel.Next;

            while (node != _endSentinel) {
                var predicateResult = predicate(node.item);
                if (!predicateResult) {
                    //updatecheck();
                    node = node.Next;
                    index++;
                }

                //updatecheck();
                viewHandler.skipEndpoints(countRemoved, myoffset + index);
                var localend = node.Prev; //Latest node not to be removed

                if (predicateResult) {
                    if (shouldRememberItems) {
                        (itemsRemoved ?? (itemsRemoved = new ArrayList<T>(allowsNull: AllowsNull))).Add(node.item);
                    }

                    countRemoved++;
                    node = node.Next;
                    index++;
                    viewHandler.updateViewSizesAndCounts(countRemoved, myoffset + index);
                }

                //updatecheck();
                viewHandler.updateSentinels(myoffset + index, localend, node);
                localend.Next = node;
                node.Prev = localend;
            }

            index = _underlying?.Count + 1 - myoffset ?? Count + 1 - myoffset;
            viewHandler.updateViewSizesAndCounts(countRemoved, myoffset + index);

            if (countRemoved == 0) {
                Assert(itemsRemoved == null);
                return false;
            }

            // Only update version if items are actually removed
            UpdateVersion();

            Count -= countRemoved;
            if (_underlying != null)
                _underlying.Count -= countRemoved;

            (_underlying ?? this).RaiseForRemoveAllWhere(itemsRemoved);
            return true;
        }

        private void MirrorViewSentinelsForReversePrivate(Position[] positions, ref int poslow, ref int poshigh, Node a, Node b, int i)
        {

            int aindex = Offset + i, bindex = Offset + Count - 1 - i;
            Position pos;

            while (poslow <= poshigh && (pos = positions[poslow]).Index == aindex) {
                //TODO: Note: in the case og hashed linked list, if this.offset == null, but pos.View.offset!=null                
                if (pos.Left)
                    pos.View._endSentinel = b.Next;
                else {
                    pos.View._startSentinel = b.Prev;
                    pos.View.Offset = bindex;
                }
                poslow++;
            }

            while (poslow < poshigh && (pos = positions[poshigh]).Index == bindex) {
                if (pos.Left)
                    pos.View._endSentinel = a.Next;
                else {
                    pos.View._startSentinel = a.Prev;
                    pos.View.Offset = aindex;
                }
                poshigh--;
            }
        }

        private void GetPairPrivate(int p1, int p2, out Node n1, out Node n2, int[] positions, Node[] nodes)
        {
            int nearest1, nearest2;
            int delta1 = CalcDistancePrivate(p1, out nearest1, positions), d1 = delta1 < 0 ? -delta1 : delta1;
            int delta2 = CalcDistancePrivate(p2, out nearest2, positions), d2 = delta2 < 0 ? -delta2 : delta2;

            if (d1 < d2) {
                n1 = GetNodePrivate(p1, positions, nodes);
                n2 = GetNodePrivate(p2, new[] { positions[nearest2], p1 }, new[] { nodes[nearest2], n1 });
            }
            else {
                n2 = GetNodePrivate(p2, positions, nodes);
                n1 = GetNodePrivate(p1, new[] { positions[nearest1], p2 }, new[] { nodes[nearest1], n2 });
            }
        }

        private Node GetNodePrivate(int pos, int[] positions, Node[] nodes) // Get a node; make the search shorter
        {
            int nearest;
            var delta = CalcDistancePrivate(pos, out nearest, positions); // Get the delta and nearest. Start searching from nearest!
            var node = nodes[nearest];
            if (delta > 0)
                for (var i = 0; i < delta; i++)
                    node = node.Prev;
            else
                for (var i = 0; i > delta; i--)
                    node = node.Next;
            return node;
        }

        private int CalcDistancePrivate(int pos, out int nearest, int[] positions)
        {
            nearest = -1;
            var bestdist = int.MaxValue;
            var signeddist = bestdist;
            for (int i = 0; i < positions.Length; i++) {
                int thisdist = positions[i] - pos;
                if (thisdist >= 0 && thisdist < bestdist) {
                    nearest = i;
                    bestdist = thisdist;
                    signeddist = thisdist;
                }

                if (thisdist < 0 && -thisdist < bestdist) {
                    nearest = i;
                    bestdist = -thisdist;
                    signeddist = thisdist;
                }
            }
            return signeddist;
        }

        private static Node MergeRuns(Node run1, Node run2, SCG.IComparer<T> comparer)
        {
            //P: assert run1 != null && run2 != null;
            Node prev;
            bool prev1; //P: is prev from run1?

            if (comparer.Compare(run1.item, run2.item) <= 0) {
                prev = run1;
                prev1 = true;
                run1 = run1.Next;
            }
            else {
                prev = run2;
                prev1 = false;
                run2 = run2.Next;
            }

            Node start = prev;

            //P: assert start != null;
            start.Prev = null;
            while (run1 != null && run2 != null) {
                if (prev1) {
                    //P: assert prev.next == run1;
                    //P: Comparable run2item = (Comparable)run2.item;
                    while (run1 != null && comparer.Compare(run2.item, run1.item) >= 0) {
                        prev = run1;
                        run1 = prev.Next;
                    }

                    if (run1 != null) {
                        //P: prev.item <= run2.item < run1.item; insert run2
                        prev.Next = run2;
                        run2.Prev = prev;
                        prev = run2;
                        run2 = prev.Next;
                        prev1 = false;
                    }
                }
                else {
                    //P: assert prev.next == run2;
                    //P: Comparable run1item = (Comparable)run1.item;
                    while (run2 != null && comparer.Compare(run1.item, run2.item) > 0) {
                        prev = run2;
                        run2 = prev.Next;
                    }

                    if (run2 != null) {
                        //P:  prev.item < run1.item <= run2.item; insert run1
                        prev.Next = run1;
                        run1.Prev = prev;
                        prev = run1;
                        run1 = prev.Next;
                        prev1 = true;
                    }
                }
            }

            //P: assert !(run1 != null && prev1) && !(run2 != null && !prev1);
            if (run1 != null) {
                //P:  last run2 < all of run1; attach run1 at end
                prev.Next = run1;
                run1.Prev = prev;
            }
            else if (run2 != null) {
                //P:  last run1 
                prev.Next = run2;
                run2.Prev = prev;
            }

            return start;
        }

        private void DisposeOverlappingViews(bool reverse)
        {
            if (_views == null) {
                return;
            }

            foreach (var view in _views) {
                if (view == this) {
                    continue;
                }

                switch (ViewPosition(view)) {
                    case MutualViewPosition.ContainedIn:
                        if (reverse) { }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="otherView"></param>
        /// <returns>The position of View(otherOffset, otherSize) wrt. this view</returns>
        private MutualViewPosition ViewPosition(LinkedList<T> otherView)
        {
            int end = Offset + Count, otherOffset = otherView.Offset, otherSize = otherView.Count, otherEnd = otherOffset + otherSize;
            if (otherOffset >= end || otherEnd <= Offset)
                return MutualViewPosition.NonOverlapping;
            if (Count == 0 || (otherOffset <= Offset && end <= otherEnd))
                return MutualViewPosition.Contains;
            if (otherSize == 0 || (Offset <= otherOffset && otherEnd <= end))
                return MutualViewPosition.ContainedIn;
            return MutualViewPosition.Overlapping;
        }

        private bool IsCompatibleObject(object value) => value is T || value == null && default(T) == null;

        private bool Equals(T x, T y) => EqualityComparer.Equals(x, y);

        private void InsertPrivate(int index, Node succ, T item)
        {
            #region Code Contracts

            // Argument must be within bounds
            Requires(index >= 0, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);

            #endregion

            UpdateVersion();

            var node = new Node(item, succ.Prev, succ);
            succ.Prev.Next = node;
            succ.Prev = node;

            Count++;
            if (_underlying != null) {
                _underlying.Count++;
            }

            FixViewsAfterInsertPrivate(succ, node.Prev, 1, Offset + index);
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

            UpdateVersion();

            Node cursor;
            var succ = index == Count ? _endSentinel : GetNodeAtPrivate(index);
            var pred = cursor = succ.Prev;

            var count = 0;
            foreach (var item in items) {
                var tmp = new Node(item, cursor, null);
                cursor.Next = tmp; // == pred.Next := first temporary
                count++;
                cursor = tmp;
            }

            if (count == 0) // no need 
                return;

            succ.Prev = cursor;
            cursor.Next = succ;

            Count += count;
            if (_underlying != null)
                _underlying.Count += count;

            if (count > 0) // no need!
            {
                FixViewsAfterInsertPrivate(succ, pred, count, Offset + index);
            }
        }

        private void ClearPrivate()
        {
            UpdateVersion();

            FixViewsBeforeRemovePrivate(Offset, Count, _startSentinel.Next, _endSentinel.Prev);

            _startSentinel.Next = _endSentinel; //_startSentinel.Prev = null;
            _endSentinel.Prev = _startSentinel; //_endSentinel.Next = null;

            if (_underlying != null) {
                _underlying.Count -= Count;
            }
            Count = 0;
        }

        private void UpdateVersion()
        {
            _version++;
            if (_underlying != null)
            {
                _underlying._version++;
            }
        }

        private bool CheckVersion(int version)
        {
            if (_version == version) {
                return true;
            }

            // See https://msdn.microsoft.com/library/system.collections.ienumerator.movenext.aspx
            throw new InvalidOperationException(CollectionWasModified);
        }

        private Node GetNodeAtPrivate(int index)
        {
            #region Code Contracts                        

            // ReSharper disable InvocationIsSkipped
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index < Count, ArgumentMustBeWithinBounds);
            // ReSharper enable InvocationIsSkipped

            #endregion

            if (index < Count / 2) {
                // Closer to front
                var node = _startSentinel;
                for (var i = 0; i <= index; i++)
                    node = node.Next;

                return node;
            }
            else {
                // Closer to end
                var node = _endSentinel;
                for (var i = Count; i > index; i--)
                    node = node.Prev;

                return node;
            }
        }

        private bool FindNodePrivate(T item, ref Node node, ref int index, EnumerationDirection direction) // FIFO style
        {
            var endNode = direction.IsForward() ? _endSentinel : _startSentinel;
            while (node != endNode) {
                if (item == null) {
                    if (node.item == null) {
                        return true;
                    }
                }
                else {
                    if (Equals(item, node.item)) {
                        return true;
                    }
                }

                index = direction.IsForward() ? index + 1 : index - 1;
                node = direction.IsForward() ? node.Next : node.Prev;
            }

            index = ~Count;
            return false;
        }

        private T RemoveAtPrivate(Node node, int index)
        {
            UpdateVersion();

            FixViewsBeforeSingleRemovePrivate(node, Offset + index);

            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;

            Count--;
            if (_underlying != null) {
                _underlying.Count--;
            }

            return node.item;
        }

        private void RaiseForRemoveIndexRange(int startIndex, int count)
        {
            if (ActiveEvents.HasFlag(Cleared)) {
                OnCollectionCleared(false, count, startIndex);
            }
                            
            OnCollectionChanged();
        }

        private void RaiseForRemoveAllWhere(SCG.IEnumerable<T> items)
        {            
            if (ActiveEvents.HasFlag(Removed)) {
                foreach (var item in items) {
                    OnItemsRemoved(item, 1);
                }
            }
            OnCollectionChanged();
        }

        private void RaiseForReverse() => OnCollectionChanged();

        private void RaiseForSort() => OnCollectionChanged();

        private void RaiseForIndexSetter(T oldItem, T newItem, int index)
        {
            if (ActiveEvents == None) {
                return;
            }

            OnItemRemovedAt(oldItem, index);
            OnItemsRemoved(oldItem, 1);
            OnItemInserted(newItem, index);
            OnItemsAdded(newItem, 1);
            OnCollectionChanged();
        }

        private void RaiseForShuffle() => OnCollectionChanged();

        private void RaiseForInsertRange(int index, SCG.IEnumerable<T> items) 
        {
            Requires(items != null);

            if (ActiveEvents.HasFlag(Inserted | Added)) {
                foreach (var item in items) {
                    OnItemInserted(item, index++);
                    OnItemsAdded(item, 1);
                }
            }
            OnCollectionChanged();
        }

        private void RaiseForAdd(T item)
        {
            if (ActiveEvents.HasFlag(Added)) {
                OnItemsAdded(item, 1);
            }
            OnCollectionChanged();
        }

        private void RaiseForAddRange(SCG.IEnumerable<T> items)
        {
            Requires(items != null);

            if (ActiveEvents.HasFlag(Added)) {
                foreach (var item in items) {
                    OnItemsAdded(item, 1);
                }
            }

            OnCollectionChanged();
        }

        private void RaiseForClear(int count)
        {
            if (ActiveEvents.HasFlag(Cleared)) {
                OnCollectionCleared(true, count);
            }

            OnCollectionChanged();
        }

        private void RaiseForRemove(T item)
        {
            if (ActiveEvents.HasFlag(Removed)) {
                OnItemsRemoved(item, 1);
            }
                            
            OnCollectionChanged();
        }

        private void RaiseForUpdate(T item, T oldItem)
        {
            Requires(Equals(item, oldItem));

            if (ActiveEvents.HasFlag(Removed | Added)) {
                OnItemsRemoved(oldItem, 1);
                OnItemsAdded(item, 1);
            }
            OnCollectionChanged();
        }

        private void RaiseForRemoveAt(T item, int index)
        {
            if (ActiveEvents.HasFlag(Removed | RemovedAt)) {
                OnItemRemovedAt(item, index);
                OnItemsRemoved(item, 1);
            }
            OnCollectionChanged();
        }

        private void RaiseForInsert(int index, T item)
        {
            if (ActiveEvents.HasFlag(Inserted | Added)) {
                OnItemInserted(item, index);
                OnItemsAdded(item, 1);
            }

            OnCollectionChanged();
        }

        #region Invoking methods

        private void OnCollectionChanged()
            => _collectionChanged?.Invoke(this, EventArgs.Empty);

        private void OnCollectionCleared(bool full, int count, int? start = null)
            => _collectionCleared?.Invoke(this, new ClearedEventArgs(full, count, start));

        private void OnItemsAdded(T item, int count)
            => _itemsAdded?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnItemsRemoved(T item, int count)
            => _itemsRemoved?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnItemRemovedAt(T item, int index)
            => _itemRemovedAt?.Invoke(this, new ItemAtEventArgs<T>(item, index));

        private void OnItemInserted(T item, int index)
            => _itemInserted?.Invoke(this, new ItemAtEventArgs<T>(item, index));

        #endregion

        #endregion

        #region Nested types

        
        private sealed class Node 
        {
            public Node Next; 
            public Node Prev; 
            public T item;

            internal Node(T item)
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


        private sealed class WeakViewList<V> : SCG.IEnumerable<V> where V : class
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
                if (start != null) {
                    start.prev = newNode;
                    newNode.next = start;
                }
                start = newNode;
                return newNode;
            }

            internal void Remove(Node n)
            {
                if (n == start) {
                    start = start.next;
                    if (start != null)
                        start.prev = null;
                }
                else {
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
                while (n != null) {
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

            SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();
        }
        
        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class ItemSet : CollectionValueBase<T>, ICollectionValue<T>
        {
            #region Fields

            private readonly LinkedList<T> _base;

            private readonly int _version;

            // TODO: Replace with C5.HashTable<T> in future
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
            
            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get {
                    CheckVersion();
                    return Set.Count;
                }
            }

            public override Speed CountSpeed
            {
                get {
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
                return _base.Choose();
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                Set.CopyTo(array, arrayIndex);
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);
            
            public override SCG.IEnumerator<T> GetEnumerator()
            {
                // If a set already exists, enumerate that
                if (_set != null) {
                    var enumerator = Set.GetEnumerator();
                    while (CheckVersion() & enumerator.MoveNext()) {
                        yield return enumerator.Current;
                    }
                }
                // Otherwise, evaluate lazily
                else {
                    var set = new SCG.HashSet<T>(_base.EqualityComparer);

                    var enumerator = _base.GetEnumerator();
                    while (CheckVersion() & enumerator.MoveNext()) {
                        // Only return new items
                        if (set.Add(enumerator.Current)) {
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

            // TODO: Replace with C5.HashTable<T> when implemented
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
            private Node _startNode;

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
                _version = list._underlying?._version ?? list._version;

                _startIndex = startIndex;
                _count = count;
                _sign = (int) direction;
                _direction = direction;
                if (count > 0)
                    _startNode = list.GetNodeAtPrivate(startIndex);
            }

            #endregion

            #region Properties

            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get {
                    CheckVersion();
                    return _count;
                }
            }

            public override Speed CountSpeed
            {
                get {
                    CheckVersion();
                    return Constant;
                }
            }

            public EnumerationDirection Direction
            {
                get {
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
                //--var index = _direction.IsForward() ? _startIndex + _count - 1 : _startIndex;
                return _startNode.item;
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();

                base.CopyTo(array, arrayIndex); 
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            public override SCG.IEnumerator<T> GetEnumerator()
            {
                var count = Count;
                var cursor = _startNode;
                if (cursor == null) {
                    yield break;
                }

                CheckVersion();
                yield return cursor.item;
                while (--count > 0) {
                    CheckVersion();
                    cursor = _direction.IsForward() ? cursor.Next : cursor.Prev;
                    yield return cursor.item;
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
                get { return base.IsValid; }

                protected set { base.IsValid = value; }
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
                    return _list == null ? Linear : Constant;
                }
            }

            public override bool IsEmpty => CheckVersion() & List.IsEmpty;

            #endregion

            #region Public Methods

            public override T Choose()
            {
                CheckVersion();
                return _base.Choose();
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
                if (_list != null) {
                    var enumerator = _list.GetEnumerator();
                    while (CheckVersion() & enumerator.MoveNext()) {
                        yield return enumerator.Current;
                    }
                }
                // Otherwise, evaluate lazily
                else {
                    var list = new LinkedList<T>(allowsNull: AllowsNull);
                    Func<T, T, bool> equals = _base.Equals;

                    var enumerator = _base.GetEnumerator();


                    T item;
                    while ( /*CheckVersion() &*/ enumerator.MoveNext()) {
                        // Only return duplicate items
                        if (equals(item = enumerator.Current, _item)) {
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


        /// <summary>
        /// 
        /// </summary>
        private struct Position
        {
            public readonly LinkedList<T> View;
            public bool Left;

            public readonly int Index;

            public Position(LinkedList<T> view, bool left)
            {
                View = view;
                Left = left;

                Index = left ? view.Offset : view.Offset + view.Count - 1;
            }

            public Position(int index)
            {
                Index = index;
                View = null;
                Left = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        private class PositionComparer : SCG.IComparer<Position>
        {
            private static PositionComparer _default;
            //public PositionComparer() { }

            public static PositionComparer Default => _default ?? (_default = new PositionComparer());

            public int Compare(Position a, Position b) => a.Index.CompareTo(b.Index);
        }

        //TODO: merge the two implementations using Position values as arguments
        /// <summary>
        /// Handle the update of (other) views during a multi-remove operation.
        /// </summary>
        struct ViewHandler
        {
            ArrayList<Position> leftEnds;
            ArrayList<Position> rightEnds;
            int leftEndIndex, rightEndIndex, leftEndIndex2, rightEndIndex2;
            internal readonly int viewCount;

            internal ViewHandler(LinkedList<T> list)
            {
                leftEndIndex = rightEndIndex = leftEndIndex2 = rightEndIndex2 = viewCount = 0;
                leftEnds = rightEnds = null;
                if (list._views != null)
                    foreach (var v in list._views)
                        if (v != list) {
                            if (leftEnds == null) {
                                leftEnds = new ArrayList<Position>();
                                rightEnds = new ArrayList<Position>();
                            }
                            leftEnds.Add(new Position(v, true));
                            rightEnds.Add(new Position(v, false));
                        }
                if (leftEnds == null)
                    return;
                viewCount = leftEnds.Count;
                leftEnds.Sort(PositionComparer.Default);
                rightEnds.Sort(PositionComparer.Default);
            }

            /// <summary>
            /// This is to be called with realindex pointing to the first node to be removed after a (stretch of) node that was not removed
            /// </summary>
            /// <param name="removed"></param>
            /// <param name="realindex"></param>
            internal void skipEndpoints(int removed, int realindex)
            {
                if (viewCount > 0) {
                    Position endpoint;
                    while (leftEndIndex < viewCount && (endpoint = leftEnds[leftEndIndex]).Index <= realindex) {
                        LinkedList<T> view = endpoint.View;
                        view.Offset = view.Offset - removed;
                        view.Count += removed;
                        leftEndIndex++;
                    }
                    while (rightEndIndex < viewCount && (endpoint = rightEnds[rightEndIndex]).Index < realindex) {
                        LinkedList<T> view = endpoint.View;
                        view.Count -= removed;
                        rightEndIndex++;
                    }
                }
                if (viewCount > 0) {
                    Position endpoint;
                    while (leftEndIndex2 < viewCount && (endpoint = leftEnds[leftEndIndex2]).Index <= realindex)
                        leftEndIndex2++;
                    while (rightEndIndex2 < viewCount && (endpoint = rightEnds[rightEndIndex2]).Index < realindex - 1)
                        rightEndIndex2++;
                }
            }

            internal void updateViewSizesAndCounts(int removed, int realindex)
            {
                if (viewCount > 0) {
                    Position endpoint;
                    while (leftEndIndex < viewCount && (endpoint = leftEnds[leftEndIndex]).Index <= realindex) {
                        LinkedList<T> view = endpoint.View;
                        view.Offset = view.Offset - removed;
                        view.Count += removed;
                        leftEndIndex++;
                    }
                    while (rightEndIndex < viewCount && (endpoint = rightEnds[rightEndIndex]).Index < realindex) {
                        LinkedList<T> view = endpoint.View;
                        view.Count -= removed;
                        rightEndIndex++;
                    }
                }
            }

            internal void updateSentinels(int realindex, Node newstart, Node newend)
            {
                if (viewCount > 0) {
                    Position endpoint;
                    while (leftEndIndex2 < viewCount && (endpoint = leftEnds[leftEndIndex2]).Index <= realindex) {
                        LinkedList<T> view = endpoint.View;
                        view._startSentinel = newstart;
                        leftEndIndex2++;
                    }
                    while (rightEndIndex2 < viewCount && (endpoint = rightEnds[rightEndIndex2]).Index < realindex - 1) {
                        LinkedList<T> view = endpoint.View;
                        view._endSentinel = newend;
                        rightEndIndex2++;
                    }
                }
            }

        }

        #endregion
    }
}