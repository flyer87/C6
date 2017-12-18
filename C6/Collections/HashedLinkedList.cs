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

using SCG = System.Collections.Generic;
using SC = System.Collections;


namespace C6.Collections
{
    public class HashedLinkedList<T> : IList<T>, IStack<T>, IQueue<T>
    {
        #region Fields

        private Node _startSentinel, _endSentinel;
        private SCG.IDictionary<T, Node> _itemNode; // ??? initialize here or somewhere else

        private HashedLinkedList<T> _underlying; // null for proper list
        private WeakViewList<HashedLinkedList<T>> _views;
        private WeakViewList<HashedLinkedList<T>>.Node _myWeakReference; // null for proper list

        private int _version, _sequencedHashCodeVersion = -1, _unsequencedHashCodeVersion = -1;
        private int _sequencedHashCode, _unsequencedHashCode;

        private event EventHandler _collectionChanged;
        private event EventHandler<ClearedEventArgs> _collectionCleared;
        private event EventHandler<ItemAtEventArgs<T>> _itemInserted, _itemRemovedAt;
        private event EventHandler<ItemCountEventArgs<T>> _itemsAdded, _itemsRemoved;

        private int _taggroups;

        #endregion

        #region Code Contracts

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            // Equality comparer is non-null
            Invariant(EqualityComparer != null);

            // startsentil is not null
            Invariant(_startSentinel != null);

            // end sentil is not null
            Invariant(_endSentinel != null);

            // if (underlying == null ??? corectly transfered

            // If Count is 0 then _startSentinel.Next points to _endSentinel
            Invariant(Count != 0 || _startSentinel.Next == _endSentinel);

            // If Count is 0 then _endSentinel.Prev points to _startSentinel
            Invariant(Count != 0 || _endSentinel.Prev == _startSentinel);

            // The list and the dictioonary have the same number of items
            Invariant(Underlying != null || Count == _itemNode.Count);

            // Each list item is in the dictionary; the nodes are the same
            Node node = _startSentinel.Next, nodeOut;
            Invariant(Underlying != null || ForAll(0, Count, i => {
                var res = _itemNode.TryGetValue(node.item, out nodeOut) && nodeOut == node;
                node = node.Next;
                return res;
            }));

            #region View

            // Offset and Count of each view is within bounds
            Invariant((_underlying ?? this)._views == null ||
                      ForAll((_underlying ?? this)._views, v =>
                          v.Offset >= 0 &&
                          // ??? v.IsValid &&                                                                                    
                          v.Offset + v.Count >= 0 &&
                          v.Offset + v.Count <= UnderlyingCount
                      ));

            // Each view points to the same _views as the underlying; 
            // Each view have the correct underlying list
            Invariant((_underlying ?? this)._views == null ||
                      ForAll((_underlying ?? this)._views, v =>
                          // ??? v.IsValid &&                                                                                
                              v._views == (_underlying ?? this)._views &&
                              v._underlying == (_underlying ?? this)
                      ));

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

            #endregion
        }

        #endregion

        #region Constructors

        public HashedLinkedList(SCG.IEqualityComparer<T> equalityComparer = null)
        {
            IsValid = true;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default;
            _startSentinel = new Node(default(T));
            _endSentinel = new Node(default(T));

            _startSentinel.Next = _endSentinel;
            _endSentinel.Prev = _startSentinel;

            _startSentinel.taggroup = new TagGroup {
                Tag = int.MinValue,
                Count = 0
            };

            _endSentinel.taggroup = new TagGroup {
                Tag = int.MaxValue,
                Count = 0
            };

            _itemNode = new SCG.Dictionary<T, Node>(EqualityComparer);
        }

        public HashedLinkedList(SCG.IEnumerable<T> items, SCG.IEqualityComparer<T> equalityComparer = null) :
            this(equalityComparer)
        {
            AddRange(items);
        }

        #endregion

        #region Properties

        private int UnderlyingCount => (_underlying ?? this).Count;

        public int Taggroups // ??? private 
        {
            get { return _underlying == null ? _taggroups : _underlying._taggroups; }
            set {
                if (_underlying == null)
                    _taggroups = value;
                else
                    _underlying._taggroups = value;
            }
        }

        #region ICollectionValue

        public virtual int Count { get; private set; }
        public bool AllowsNull => false;
        public Speed CountSpeed => Constant;
        public bool IsEmpty => Count == 0;
        public bool IsValid { get; private set; }

        #endregion

        #region IListenable

        public EventTypes ActiveEvents { get; private set; }
        public EventTypes ListenableEvents => All;

        #endregion

        #region IExtensible

        public SCG.IEqualityComparer<T> EqualityComparer { get; }
        public virtual bool AllowsDuplicates => false;
        public bool DuplicatesByCounting => true;
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;

        #endregion

        #region ICollection

        public Speed ContainsSpeed { get; }

        #endregion

        #region IDirectedCollectionValue

        public virtual EnumerationDirection Direction => EnumerationDirection.Forwards;

        #endregion

        #region IIndexed

        public virtual Speed IndexingSpeed => Linear;

        #endregion

        #region IList

        public virtual IList<T> Underlying => _underlying;

        public virtual int Offset { get; private set; }

        public T Last => _endSentinel.Prev.item;

        public virtual T First => _startSentinel.Next.item;

        #endregion

        #endregion

        #region Public methods

        #region ICollectionValue

        public T Choose() => First;

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this) {
                array[arrayIndex++] = item;
            }
        }

        public virtual T[] ToArray()
        {
            var array = new T[Count];
            CopyTo(array, 0);
            return array;
        }

        public string ToString(string format, IFormatProvider formatProvider)
            => Showing.ShowString(this, format, formatProvider);

        public bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider)
            => Showing.Show(this, stringBuilder, ref rest, formatProvider);

        public override string ToString() => ToString(null, null);

        #endregion

        #region IExtensible

        public virtual bool Add(T item)
        {
            var node = new Node(item);
            if (FindOrAddToHashPrivate(item, node)) {
                return false;
            }

            InsertNodeBeforePrivate(true, _endSentinel, node); // why true ???
            (_underlying ?? this).RaiseForAdd(item);
            return true;
        }

        public virtual bool AddRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts            

            #endregion

            // TODO: Handle ICollectionValue<T> and ICollection<T>
            var array = items.ToArray();
            if (array.IsEmpty()) {
                return false;
            }

            // ??? C6.LinkedList: All this below is in a private method InsertRangePrivate
            var countAdded = 0;
            var pred = _endSentinel.Prev;
            foreach (var item in array) // or items???
            {
                var node = new Node(item);
                if (FindOrAddToHashPrivate(item, node)) {
                    continue;
                }

                InsertNodeBeforePrivate(false, _endSentinel, node); // why false ???
                countAdded++;
            }

            if (countAdded <= 0) {
                return false;
            }

            UpdateVersion();

            FixViewsAfterInsertPrivate(_endSentinel, pred, countAdded, 0);
            (_underlying ?? this).RaiseForAddRange(array); // wrong! Not array, only the added ones ???
            return true;
        }

        #endregion

        #region ICollection

        public virtual void Clear()
        {
            if (Count <= 0) {
                return;
            }

            // Clear dict
            var oldCount = Count;
            if (_underlying == null) {
                _itemNode.Clear();
            }
            else {
                foreach (var item in this) {
                    _itemNode.Remove(item);
                }
            }

            //Clear linkedlist
            ClearPrivate();

            //(_underlying ?? this).RaiseForClear(oldCount);
            (_underlying ?? this).RaiseForRemoveIndexRange(Offset, oldCount);
        }

        public virtual bool Contains(T item) => IndexOf(item) >= 0;

        public virtual bool ContainsRange(SCG.IEnumerable<T> items)
        {
            var array = items.ToArray(); // ??? to array(). Why ???
            if (array.IsEmpty()) {
                return true;
            }

            if (IsEmpty) {
                return false;
            }

            return items.All(item => _itemNode.ContainsKey(item));
        }

        public virtual int CountDuplicates(T item) => IndexOf(item) >= 0 ? 1 : 0;

        public virtual bool Find(ref T item)
        {
            #region Code Contracts            

            #endregion

            // try find in hash
            Node node;
            if (!ContainsItemPrivate(item, out node)) {
                return false;
            }

            item = node.item;
            return true;
        }

        public virtual ICollectionValue<T> FindDuplicates(T item) => new Duplicates(this, item);

        public virtual bool FindOrAdd(ref T item) => Find(ref item) || !Add(item);

        public virtual int GetUnsequencedHashCode()
        {
            if (_unsequencedHashCodeVersion == _version) {
                return _unsequencedHashCode;
            }

            _unsequencedHashCode = this.GetUnsequencedHashCode(EqualityComparer);
            _unsequencedHashCodeVersion = _version;
            return _unsequencedHashCode;
        }

        public virtual ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities()
        {
            throw new NotImplementedException();
        }

        public virtual bool Remove(T item, out T removedItem)
        {
            #region Code Contracts

            #endregion

            removedItem = default(T);
            if (Count <= 0) // ??? maybe remove it
            {
                return false;
            }

            Node node;
            var index = 0; // ??? Not changed at all            
            if (!TryRemoveFromHash(item, out node)) // try to remove from hash
            {
                return false;
            }

            removedItem = node.item;
            RemoveNodePrivate(node, index); // remove from linked list

            (_underlying ?? this).RaiseForRemove(removedItem, 1);
            return true;
        }

        public virtual bool Remove(T item)
        {
            #region Code Contracts

            #endregion

            T removedItem;
            return Remove(item, out removedItem);
        }

        public virtual bool RemoveDuplicates(T item) => RemoveAllWherePrivate(x => Equals(x, item));

        public virtual bool RemoveRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts            

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            var list = new HashedLinkedList<T>(items, EqualityComparer);
            return RemoveAllWherePrivate(x => !list.IsEmpty && list.Remove(x));
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

            // Optimize call, if no items should be retained
            if (items.IsEmpty()) {
                var itemsRemoved = this;
                // clear dict
                if (_underlying == null) {
                    _itemNode.Clear();
                }
                else {
                    foreach (var item in this) {
                        _itemNode.Remove(item);
                    }
                }
                // clear linkedlist
                ClearPrivate();
                RaiseForRemoveAllWhere(itemsRemoved);
                return true;
            }

            //using (var list = new HashedLinkedList<T>(items, EqualityComparer))
            {
                var list = new HashedLinkedList<T>(items, EqualityComparer);
                return RemoveAllWherePrivate(x => !list.Remove(x));
            }
        }

        public virtual ICollectionValue<T> UniqueItems() => new ItemSet(this);

        public virtual bool UnsequencedEquals(ICollection<T> otherCollection) // ??? version check
            => this.UnsequencedEquals(otherCollection, EqualityComparer);

        public virtual bool Update(T item)
        {
            #region Code Contracts

            #endregion

            T oldItem;
            return Update(item, out oldItem);
        }

        public virtual bool Update(T item, out T oldItem)
        {
            #region Code Contracts            

            #endregion

            Node node;
            if (!ContainsItemPrivate(item, out node)) {
                oldItem = default(T);
                return false;
            }

            UpdateVersion();

            oldItem = node.item;
            node.item = item;
            (_underlying ?? this).RaiseForUpdate(item, oldItem);

            return true;
        }

        public virtual bool UpdateOrAdd(T item) => Update(item) || !Add(item);

        public virtual bool UpdateOrAdd(T item, out T oldItem) => Update(item, out oldItem) || !Add(item);

        #endregion

        #region IDirectedCollectionValue

        public virtual IDirectedCollectionValue<T> Backwards()
            => new Range(this, Count - 1, Count, EnumerationDirection.Backwards);

        #endregion

        #region ISequenced

        public virtual int GetSequencedHashCode()
        {
            if (_sequencedHashCodeVersion == _version) {
                return _sequencedHashCode;
            }

            _sequencedHashCodeVersion = _version;
            _sequencedHashCode = this.GetSequencedHashCode(EqualityComparer);
            return _sequencedHashCode;
        }

        public virtual bool SequencedEquals(ISequenced<T> otherCollection)
            => this.SequencedEquals(otherCollection, EqualityComparer);


        #endregion

        #region IIndexed

        public virtual int IndexOf(T item)
        {
            #region Code Contracts

            // TODO: Add contract to IList<T>.LastIndexOf

            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : ~Result<int>() == Count);

            // Item at index is the first equal to item
            Ensures(Result<int>() < 0 || !this.Skip(Result<int>() + 1).Contains(item, EqualityComparer) && EqualityComparer.Equals(item, this.ElementAt(Result<int>())));

            #endregion

            // is it in the dictionary
            if (!_itemNode.ContainsKey(item)) {
                return ~Count;
            }

            var node = _itemNode[item];
            if (!IsInsideViewPrivate(node)) {
                return ~Count;
            }

            node = _startSentinel.Next;
            var index = 0;
            // find it in the list
            FindNodeAndIndexByItemPrivate(item, ref node, ref index, EnumerationDirection.Forwards);
            return index;
        }

        public virtual IDirectedCollectionValue<T> GetIndexRange(int startIndex, int count)
            => new Range(this, startIndex, count, EnumerationDirection.Forwards);

        public virtual T this[int index]
        {
            get { return GetNodeByIndexPrivate(index).item; }
            set {
                #region Code Contracts

                // The version is updated
                Ensures(_version != OldValue(_version));

                #endregion

                var node = GetNodeByIndexPrivate(index);

                var oldItem = node.item;
                if (Equals(oldItem, value)) {
                    node.item = value;
                    _itemNode[value] = node; // does C5 update the key  as well ?
                }
                else {
                    node.item = value;
                    _itemNode[value] = node;
                    _itemNode.Remove(oldItem);
                }
                // else { } Exception or ?

                UpdateVersion();

                (_underlying ?? this).RaiseForIndexSetter(oldItem, value, Offset + index);
            }
        }

        public virtual int LastIndexOf(T item) => IndexOf(item);

        public virtual T RemoveAt(int index)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            //var old = OldValue(this.ToArray());            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            var node = GetNodeByIndexPrivate(index);
            _itemNode.Remove(node.item);
            var item = RemoveNodePrivate(node, index);

            (_underlying ?? this).RaiseForRemoveAt(item, Offset + index);
            return item;
        }

        public virtual void RemoveIndexRange(int startIndex, int count)
        {
            #region Code Contracts            

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (count == 0 || IsEmpty) {
                return;
            }

            UpdateVersion();

            // Version with view: 
            View(startIndex, count).Clear();            

            return;

            

            // =====================================
            // Version with NO view:
            var startNode = GetNodeByIndexPrivate(startIndex);
            var endNode = GetNodeByIndexPrivate(startIndex + count - 1);

            // clean the _itemNode
            var cursor = startNode;
            for (var i = 0; i < count; i++) {
                _itemNode.Remove(cursor.item);
                cursor = cursor.Next;
            }
            
            // clean the list; ??? lines down can be replaced with RemoveFromListPrivate()
            startNode.Prev.Next = endNode.Next;
            endNode.Next.Prev = startNode.Prev;

            Count -= count;
            if (_underlying != null) {
                _underlying.Count -= count;
            }

            (_underlying ?? this).RaiseForRemoveIndexRange(Offset + startIndex, count);
        }

        #endregion

        #region IDisposable

        public virtual void Dispose() => DisposePrivate(false);

        #endregion

        #region IList       

        public virtual void Insert(int index, T item)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            var succ = index == Count ? _endSentinel : GetNodeByIndexPrivate(index);
            var node = new Node(item);
            if (FindOrAddToHashPrivate(item, node)) // ??? return, Update or throw and excpetion 
            {
                return;
            }
            InsertNodeBeforePrivate(true, succ, node); // why true ???

            (_underlying ?? this).RaiseForInsert(item, Offset + index);
        }

        public virtual void InsertFirst(T item) => Insert(0, item);

        public virtual void InsertLast(T item) => Insert(Count, item);

        public virtual void InsertRange(int index, SCG.IEnumerable<T> items)
        {
            //TODO: some toArray() operations ???

            var array = items.ToArray(); // if it is enumerable we can't check with a method that it is Empty ???

            if (array.IsEmpty()) {
                return;
            }

            InsertRangePrivate(index, array);
            (_underlying ?? this).RaiseForInsertRange(Offset + index, array);
            //  not corect should raise only for really inserted
            // !!! it is correct, since we have a precondition checking that all the items are not in the list
        }

        public virtual bool IsSorted(Comparison<T> comparison)
        {
            if (Count <= 1) {
                return true;
            }

            var cursor = _startSentinel.Next;
            var prevItem = cursor.item;
            cursor = cursor.Next;
            while (cursor != _endSentinel) {
                if (comparison(prevItem, cursor.item) > 0) {
                    return false;
                }

                prevItem = cursor.item;
                cursor = cursor.Next;
            }

            return true;
        }

        public virtual bool IsSorted(SCG.IComparer<T> comparer) => IsSorted((comparer ?? SCG.Comparer<T>.Default).Compare);

        public virtual bool IsSorted() => IsSorted(SCG.Comparer<T>.Default.Compare);

        public virtual IList<T> LastViewOf(T item) => ViewOf(item); // ??? wrong 

        public virtual T RemoveFirst() => RemoveAt(0);

        public virtual T RemoveLast() => RemoveAt(Count - 1);

        public virtual void Reverse()
        {
            if (Count <= 1)
                return;

            UpdateVersion(); // ??? here or 

            Position[] positions = null;
            int poslow = 0, poshigh = 0;
            if (_views != null) // the code down - in a private method ???
            {
                LinkedList<Position> positionsInner = null;
                foreach (var view in _views) {
                    if (view == this)
                        continue;

                    switch (ViewPositionPrivate(view)) {
                        case MutualViewPosition.ContainedIn:
                            (positionsInner ?? (positionsInner = new LinkedList<Position>())).Add(new Position(view, true));
                            positionsInner.Add(new Position(view, false)); // ??? in the row up: true, and now false;
                            break;
                        case MutualViewPosition.Overlapping:
                            view.Dispose();
                            break;
                        case MutualViewPosition.Contains:
                        case MutualViewPosition.NonOverlapping:
                            break;
                    }
                }

                if (positionsInner != null) {
                    positions = positionsInner.ToArray();
                    //Sorting.IntroSort<Position>(positions, 0, positions.Length, PositionComparer.Default);
                    Array.Sort(positions, 0, positions.Length, PositionComparer.Default);
                    poshigh = positions.Length - 1;
                }
            }

            // ??? the real reversing. Yes - through swapping
            Node a = GetNodeByIndexPrivate(0), b = GetNodeByIndexPrivate(Count - 1);
            for (var i = 0; i < Count / 2; i++) {
                var swap = a.item;
                a.item = b.item;
                b.item = swap;
                _itemNode[a.item] = a;
                _itemNode[b.item] = b;

                if (positions != null)
                    MirrorViewSentinelsForReverse(positions, ref poslow, ref poshigh, a, b, i);
                a = a.Next;
                b = b.Prev;
            }

            if (positions != null && Count % 2 != 0)
                MirrorViewSentinelsForReverse(positions, ref poslow, ref poshigh, a, b, Count / 2);

            (_underlying ?? this).RaiseForReversed();
        }

        public virtual void Shuffle() => Shuffle(new Random());

        public virtual void Shuffle(Random random)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Count <= 1) {
                return;
            }

            DisposeOverlappingViewsPrivate(false); // View:
            ShufflePrivate(random);

            (_underlying ?? this).RaiseForShuffle();
        }

        public virtual IList<T> Slide(int offset) => Slide(offset, Count);

        public virtual IList<T> Slide(int offset, int count)
        {
            TrySlide(offset, count);
            return this;
        }

        public virtual IList<T> Span(IList<T> other)
        {
            if (other.Offset + other.Count - Offset < 0)
                return null;

            return (_underlying ?? this).View(Offset, other.Offset + other.Count - Offset);
        }

        public virtual void Sort(SCG.IComparer<T> comparer)
        {
            if (Count <= 1)
                return;

            if (comparer == null) {
                comparer = SCG.Comparer<T>.Default;
            }

            if (IsSorted(comparer))
                return;

            DisposeOverlappingViewsPrivate(false); // View:
            if (_underlying != null) {
                var cursor = _startSentinel.Next;
                while (cursor != _endSentinel) {
                    cursor.taggroup.Count--;
                    cursor = cursor.Next;
                }
            }

            // Build a linked list of non-empty runs.
            // The prev field in first node of a run points to next run's first node
            Node runTail = _startSentinel.Next;
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
                    Node nextRun = run.Prev.Prev;
                    Node newrun = MergeRuns(run, run.Prev, comparer);

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
            {
                Node cursor = _startSentinel.Next, end = _endSentinel;
                int tag, taglimit;
                var t = GetTagGroup(_startSentinel, _endSentinel, out tag, out taglimit);
                var tagdelta = taglimit / (Count + 1) - tag / (Count + 1);
                tagdelta = tagdelta == 0 ? 1 : tagdelta;
                if (_underlying == null)
                    _taggroups = 1;
                while (cursor != end) {
                    tag = tag + tagdelta > taglimit ? taglimit : tag + tagdelta;
                    cursor.tag = tag;
                    t.Count++;
                    cursor.taggroup = t;
                    cursor = cursor.Next;
                }
                if (t != _startSentinel.taggroup)
                    t.First = _startSentinel.Next;
                if (t != _endSentinel.taggroup)
                    t.Last = _endSentinel.Prev;
                if (tag == taglimit)
                    SplitTagGroupPrivate(t);
            }

            UpdateVersion();

            (_underlying ?? this).RaiseForSort();
        }

        public virtual void Sort() => Sort((SCG.Comparer<T>) null);

        public virtual void Sort(Comparison<T> comparison) => Sort(comparison.ToComparer());

        public virtual bool TrySlide(int offset) => TrySlide(offset, Count);

        public virtual bool TrySlide(int offset, int count)
        {
            var newOffset = Offset + offset;
            if (newOffset < 0 || count < 0 || newOffset + count > UnderlyingCount) {
                return false;
            }
            // if (this.offset == null) in C5 offset is nullable ??? So what ?

            UpdateVersion();

            var oldOffset = Offset;
            GetPairPrivate(offset - 1, offset + count, out _startSentinel, out _endSentinel,
                new[] { -oldOffset - 1, -1, Count, UnderlyingCount - oldOffset },
                new[] { _underlying._startSentinel, _startSentinel, _endSentinel, _underlying._endSentinel });

            Count = count;
            Offset += offset;

            return true;
        }

        public virtual IList<T> View(int index, int count)
        {
            _views = _views ?? (_views = new WeakViewList<HashedLinkedList<T>>());
            var view = (HashedLinkedList<T>) MemberwiseClone();
            view._underlying = _underlying ?? this;
            view.Offset = Offset + index;
            view.Count = count;

            GetPairPrivate(index - 1, index + count, out view._startSentinel, out view._endSentinel,
                new[] { -1, Count }, new[] { _startSentinel, _endSentinel });
            view._myWeakReference = _views.Add(view);

            return view;
        }

        public virtual IList<T> ViewOf(T item)
        {
            var node = _startSentinel.Next;
            var index = 0;
            return FindNodeAndIndexByItemPrivate(item, ref node, ref index, EnumerationDirection.Forwards)
                ? View(index, 1) : null;
        }

        #endregion

        #region IStack<T>
        public T Pop() => RemoveLast();

        public void Push(T item) => InsertLast(item);

        #endregion

        #region IQueue<T>
        public virtual T Dequeue() => RemoveLast();

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
            // var version = (_underlying ?? this)._version; // ??? underlying !!!
            var version = _version;

            var cursor = _startSentinel.Next;
            while (cursor != _endSentinel && CheckVersion(version)) {
                yield return cursor.item;
                cursor = cursor.Next;
            }
        }

        void SCG.ICollection<T>.Add(T item) => Add(item);

        void SC.ICollection.CopyTo(Array array, int index)
        {
            #region Code Contracts

            Requires(index >= 0, ArgumentMustBeWithinBounds);
            Requires(index + Count <= array.Length, ArgumentMustBeWithinBounds);

            #endregion

            try // why try ???
            {
                foreach (var item in this) {
                    array.SetValue(item, index++);
                }
            }
            catch (InvalidCastException) //catch (ArrayTypeMismatchException) ??? ArrayTypeMismatchException not correct or 
            {
                throw new ArgumentException("Target array type is not compatible with the type of items in the collection.");
            }
        }

        bool SC.ICollection.IsSynchronized => false;

        object SC.ICollection.SyncRoot => new object();

        #region IList

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

        int SC.IList.IndexOf(object value) => IsCompatibleObject(value) ? Math.Max(IndexOf((T) value), -1) : -1;

        void SC.IList.Insert(int index, object value)
        {
            try {
                Insert(index, (T) value);
            }
            catch (InvalidCastException) {
                throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
            }
        }

        // Explicit implementation is needed, since C6.IList<T>.IndexOf(T) breaks SCG.IList<T>.IndexOf(T)'s precondition: Result<T>() >= -1
        int SCG.IList<T>.IndexOf(T item) => Math.Max(-1, IndexOf(item));

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

        void SCG.IList<T>.RemoveAt(int index) => RemoveAt(index);

        #endregion

        #endregion

        #region Private methods

        private bool RemoveAllWherePrivate(Func<T, bool> predicate)
        {
            if (Count <= 0)
                return false;

            IExtensible<T> itemsRemoved = null;
            var shouldRememberItems = ActiveEvents.HasFlag(Removed);

            var countRemoved = 0;
            var node = _startSentinel.Next;
            while (node != _endSentinel) {
                var canRemove = predicate(node.item); //updatecheck(); ??? replace with somthing
                if (canRemove) {
                    _itemNode.Remove(node.item);
                    RemoveFromListPrivate(node, 119); // ??? 119
                    if (shouldRememberItems)
                        (itemsRemoved ?? (itemsRemoved = new LinkedList<T>(allowsNull: AllowsNull)))
                            .Add(node.item);
                    countRemoved++;
                }

                node = node.Next;
            }

            if (countRemoved == 0) {
                Assert(itemsRemoved == null); // why Assert ???
                return false;
            }

            UpdateVersion(); // once or countRemoved times in the while ???

            if (shouldRememberItems) {
                (_underlying ?? this).RaiseForRemoveAllWhere(itemsRemoved);
            }

            return true;
        }

        private void MirrorViewSentinelsForReverse(Position[] positions, ref int poslow, ref int poshigh, Node a, Node b, int i)
        {
            int? aindex = Offset + i, bindex = Offset + Count - 1 - i; // ??? int? aindex. Non of the vaues is nullable here

            Position pos;

            while (poslow <= poshigh && (pos = positions[poslow]).Endpoint == a) {
                //TODO: Note: in the case og hashed linked list, if this.offset == null, but pos.View.offset!=null
                //we may at this point compute this.offset and non-null values of aindex and bindex
                if (pos.Left)
                    pos.View._endSentinel = b.Next;
                else {
                    pos.View._startSentinel = b.Prev;
                    pos.View.Offset = bindex.GetValueOrDefault(); // ??? would this brake the logic of thr code?
                }
                poslow++;
            }
            while (poslow < poshigh && (pos = positions[poshigh]).Endpoint == b) {
                if (pos.Left)
                    pos.View._endSentinel = a.Next;
                else {
                    pos.View._startSentinel = a.Prev;
                    pos.View.Offset = aindex.GetValueOrDefault(); // ??? would this brake the logic of thr code
                }
                poshigh--;
            }
        }

        private T RemoveFromListPrivate(Node node, int index)
        {
            FixViewsBeforeSingleRemovePrivate(node, Offset + index); //View:

            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;

            Count--;
            if (_underlying != null) {
                _underlying.Count--;
            }

            RemoveFromTagGroupPrivate(node);

            return node.item;
        }

        private void RaiseForRemoveAllWhere(SCG.IEnumerable<T> items)
        {
            foreach (var item in items) {
                OnItemsRemoved(item, 1);
            }
            OnCollectionChanged();
        }

        private void ShufflePrivate(Random random)
        {
            UpdateVersion();

            var array = new ArrayList<T>();
            array.AddRange(this);
            array.Shuffle(random);

            var index = 0;
            var cursor = _startSentinel.Next;
            while (cursor != _endSentinel) {
                cursor.item = array[index++];
                _itemNode[cursor.item] = cursor;

                cursor = cursor.Next;
            }
        }

        private void InsertRangePrivate(int index, SCG.IEnumerable<T> items)
        {
            Node node;
            var count = 0;
            var succ = index == Count ? _endSentinel : GetNodeByIndexPrivate(index);
            var pred = node = succ.Prev;
            TagGroup taggroup = null;
            int taglimit = 0, thetag = 0;
            taggroup = GetTagGroup(node, succ, out thetag, out taglimit);

            try {
                foreach (var item in items) {
                    var tmp = new Node(item, node, null);

                    if (_itemNode.ContainsKey(item)) // No! Contains, but should we update the found key [with curr. item]
                    {
                        continue;
                    }

                    _itemNode[item] = tmp;

                    tmp.tag = thetag < taglimit ? ++thetag : thetag;
                    tmp.taggroup = taggroup;
                    node.Next = tmp;
                    count++;
                    node = tmp;
                }
            }
            finally {
                if (count != 0) {
                    UpdateVersion();

                    taggroup.Count += count;
                    if (taggroup != pred.taggroup)
                        taggroup.First = pred.Next;

                    if (taggroup != succ.taggroup)
                        taggroup.Last = node;

                    succ.Prev = node;
                    node.Next = succ;
                    if (node.tag == node.Prev.tag)
                        SplitTagGroupPrivate(taggroup);

                    Count += count;
                    if (_underlying != null)
                        _underlying.Count += count;

                    FixViewsAfterInsertPrivate(succ, pred, count, 0); //View:
                    // maybe no ?! raiseForInsertAll(pred, i, count, insertion); it is moved up !
                }
            }

        }

        private void DisposePrivate(bool disposingUnderlying)
        {
            if (!IsValid)
                return;

            if (_underlying != null) {
                IsValid = false;
                if (!disposingUnderlying)
                    _views?.Remove(_myWeakReference);
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

        private static bool IsCompatibleObject(object value) => value is T || value == null && default(T) == null;

        private bool FindNodeAndIndexByItemPrivate(T item, ref Node node, ref int index, EnumerationDirection direction) // FIFO style ???
        {
            var endNode = direction.IsForward() ? _endSentinel : _startSentinel;
            while (node != endNode) {
                if (Equals(item, node.item)) {
                    return true;
                }

                index = direction.IsForward() ? index + 1 : index - 1;
                node = direction.IsForward() ? node.Next : node.Prev;
            }

            index = ~Count;
            return false;
        }

        private Node GetNodeByIndexPrivate(int index)
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

        /// <summary>
        /// Find the distance from pos to the set given by positions. Return the
        /// signed distance as return value and as an out parameter, the
        /// array index of the nearest position. This is used for up to length 5 of
        /// positions, and we do not assume it is sorted. 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="positions"></param>
        /// <param name="nearest"></param>
        /// <returns></returns>
        int CalcDistancePrivate(int pos, out int nearest, int[] positions)
        {
            nearest = -1;
            int bestdist = int.MaxValue;
            int signeddist = bestdist;
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

        /// <summary>
        /// Find the node at position pos, given known positions of several nodes.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="positions"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        Node GetNodePrivate(int pos, int[] positions, Node[] nodes)
        {
            int nearest;
            var delta = CalcDistancePrivate(pos, out nearest, positions);
            var node = nodes[nearest];
            if (delta > 0)
                for (var i = 0; i < delta; i++)
                    node = node.Prev;
            else
                for (var i = 0; i > delta; i--)
                    node = node.Next;
            return node;
        }

        /// <summary>
        /// Get nodes at positions p1 and p2, given nodes at several positions.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="positions"></param>
        /// <param name="nodes"></param>
        void GetPairPrivate(int p1, int p2, out Node n1, out Node n2, int[] positions, Node[] nodes)
        {
            int nearest1, nearest2;
            int delta1 = CalcDistancePrivate(p1, out nearest1, positions), d1 = delta1 < 0 ? -delta1 : delta1;
            int delta2 = CalcDistancePrivate(p2, out nearest2, positions), d2 = delta2 < 0 ? -delta2 : delta2;

            if (d1 < d2) {
                n1 = GetNodePrivate(p1, positions, nodes);
                n2 = GetNodePrivate(p2, new int[] { positions[nearest2], p1 }, new Node[] { nodes[nearest2], n1 });
            }
            else {
                n2 = GetNodePrivate(p2, positions, nodes);
                n1 = GetNodePrivate(p1, new int[] { positions[nearest1], p2 }, new Node[] { nodes[nearest1], n2 });
            }
        }


        [Pure]
        private bool Equals(T x, T y) => EqualityComparer.Equals(x, y);

        private T RemoveNodePrivate(Node node, int index)
        {
            UpdateVersion();

            FixViewsBeforeSingleRemovePrivate(node, Offset + index); //View: 
            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;

            Count--;
            if (_underlying != null)
                _underlying.Count--;
            RemoveFromTagGroupPrivate(node);

            return node.item;
        }

        private bool TryRemoveFromHash(T item, out Node node) // ??? from Hash or Dict
        {
            if (_underlying == null) {
                return _itemNode.TryGetValue(item, out node) && _itemNode.Remove(item);
            }
            else {
                if (!ContainsItemPrivate(item, out node)) {
                    return false;
                }

                _itemNode.TryGetValue(item, out node);
                _itemNode.Remove(item);
                return true;
            }
        }

        private bool ContainsItemPrivate(T item, out Node node) // ??? remove: out Node node                 
            => _itemNode.TryGetValue(item, out node) && IsInsideViewPrivate(node);

        private bool IsInsideViewPrivate(Node node)
        {
            if (_underlying == null)
                return true;
            return _startSentinel.Precedes(node) && node.Precedes(_endSentinel);
        }

        private void ClearPrivate()
        {
            // ??? Create a method for the first part ??? like FixView ...
            // view part ?
            //TODO: mix with tag maintenance to only run through list once?
            var viewHandler = new ViewHandler(this);
            if (viewHandler.viewCount > 0) {
                var removed = 0;
                var node = _startSentinel.Next;
                viewHandler.skipEndpoints(0, node);
                while (node != _endSentinel) {
                    removed++;
                    node = node.Next;
                    viewHandler.updateViewSizesAndCounts(removed, node);
                }

                viewHandler.updateSentinels(_endSentinel, _startSentinel, _endSentinel);
                if (_underlying != null)
                    viewHandler.updateViewSizesAndCounts(removed, _underlying._endSentinel);
            }

            if (_underlying != null) {
                var node = _startSentinel.Next;

                while (node != _endSentinel) {
                    node.Next.Prev = _startSentinel;
                    _startSentinel.Next = node.Next;
                    RemoveFromTagGroupPrivate(node);
                    node = node.Next;
                }
            }
            else
                Taggroups = 0;

            // classic

            UpdateVersion();

            _startSentinel.Next = _endSentinel;
            _endSentinel.Prev = _startSentinel;
            if (_underlying != null) {
                _underlying.Count -= Count;
            }
            Count = 0;
        }

        private void UpdateVersion(bool updateUnderlying = true)
        {
            _version++;
            if (updateUnderlying)
            {
                if (_underlying != null)
                {
                    _underlying._version++;
                }
            }
        }

        /*private void UpdateVersion(bool updateUnderlying = true)
        {
            _version++;
            //if (updateUnderlying)
            //{
            //    if (_underlying != null)
            //    {
            //        _underlying._version++;
            //    }
            //}
        }*/

        private void InsertNodeBeforePrivate(bool updateViews, Node succ, Node node) // ??? updateViews
        {
            UpdateVersion();

            node.Next = succ;
            var pred = node.Prev = succ.Prev;
            succ.Prev.Next = node;
            succ.Prev = node;

            Count++;
            if (_underlying != null) {
                _underlying.Count++;
            }

            SetTagPrivate(node);
            if (updateViews)
                FixViewsAfterInsertPrivate(succ, pred, 1, 0); // View: 
        }

        private bool FindOrAddToHashPrivate(T item, Node node)
        {
            if (_itemNode.ContainsKey(item)) {
                return true;
            }

            _itemNode[item] = node;
            return false;
        }

        private bool CheckVersion(int version)
        {
            if (version == _version) {
                return true;
            }

            // See https://msdn.microsoft.com/library/system.collections.ienumerator.movenext.aspx
            throw new InvalidOperationException(CollectionWasModified);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="otherView"></param>
        /// <returns>The position of View(otherOffset, otherSize) wrt. this view</returns>
        MutualViewPosition ViewPositionPrivate(HashedLinkedList<T> otherView)
        {
            Node otherstartsentinel = otherView._startSentinel, otherendsentinel = otherView._endSentinel,
                first = _startSentinel.Next, last = _endSentinel.Prev,
                otherfirst = otherstartsentinel.Next, otherlast = otherendsentinel.Prev;
            if (last.Precedes(otherfirst) || otherlast.Precedes(first))
                return MutualViewPosition.NonOverlapping;
            if (Count == 0 || (otherstartsentinel.Precedes(first) && last.Precedes(otherendsentinel)))
                return MutualViewPosition.Contains;
            if (otherView.Count == 0 || (_startSentinel.Precedes(otherfirst) && otherlast.Precedes(_endSentinel)))
                return MutualViewPosition.ContainedIn;
            return MutualViewPosition.Overlapping;
        }

        private void DisposeOverlappingViewsPrivate(bool reverse)
        {
            if (_views == null)
                return;

            foreach (var view in _views) {
                if (view == this)
                    continue;

                switch (ViewPositionPrivate(view)) {
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

        private void FixViewsAfterInsertPrivate(Node succ, Node pred, int added, int realInsertionIndex)
        {
            if (_views == null)
                return;

            foreach (var view in _views) {
                if (view == this)
                    continue;

                if (pred.Precedes(view._startSentinel) || (view._startSentinel == pred && view.Count > 0))
                    view.Offset += added;
                if (view._startSentinel.Precedes(pred) && succ.Precedes(view._endSentinel))
                    view.Count += added;
                if (view._startSentinel == pred && view.Count > 0)
                    view._startSentinel = succ.Prev;
                if (view._endSentinel == succ)
                    view._endSentinel = pred.Next;
            }
        }

        private void FixViewsBeforeSingleRemovePrivate(Node node, int realRemovalIndex)
        {
            if (_views == null)
                return;

            foreach (var view in _views) {
                if (view == this)
                    continue;

                if (view._startSentinel.Precedes(node) && node.Precedes(view._endSentinel))
                    view.Count--;
                if (!view._startSentinel.Precedes(node))
                    view.Offset--;
                if (view._startSentinel == node)
                    view._startSentinel = node.Prev;
                if (view._endSentinel == node)
                    view._endSentinel = node.Next;
            }
        }


        private void RaiseForReversed() => OnCollectionChanged();

        private void RaiseForInsert(T item, int index)
        {
            if (ActiveEvents.HasFlag(Inserted | Added)) {
                OnItemInserted(item, index);
                OnItemsAdded(item, 1);
            }

            OnCollectionChanged();
        }

        private void RaiseForShuffle() => OnCollectionChanged();

        private void RaiseForSort() => OnCollectionChanged();

        private void RaiseForInsertRange(int index, SCG.IEnumerable<T> items)
        {
            Requires(items != null);

            if (ActiveEvents.HasFlag(Inserted | Added)) {
                var i = 0;
                foreach (var item in items) {
                    OnItemInserted(item, index + i++);
                    OnItemsAdded(item, 1);
                }
            }

            OnCollectionChanged();
        }

        private void RaiseForRemoveAt(T item, int index)
        {
            if (ActiveEvents.HasFlag(RemovedAt | Removed)) {
                OnItemRemovedAt(item, index);
                OnItemsRemoved(item, 1);
            }

            OnCollectionChanged();
        }

        private void RaiseForRemoveIndexRange(int startIndex, int count)
        {
            if (ActiveEvents.HasFlag(Cleared)) {
                OnCollectionCleared(false, count, startIndex);
            }

            OnCollectionChanged();
        }

        private void RaiseForIndexSetter(T oldItem, T item, int index)
        {
            if (ActiveEvents == None) {
                return;
            }

            OnItemRemovedAt(oldItem, index);
            OnItemsRemoved(oldItem, 1);
            OnItemInserted(item, index);
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForUpdate(T item, T oldItem)
        {
            if (ActiveEvents.HasFlag(Removed | Added)) {
                OnItemsRemoved(oldItem, 1);
                OnItemsAdded(item, 1);
            }

            OnCollectionChanged();
        }

        private void RaiseForRemove(T item, int count)
        {
            if (ActiveEvents.HasFlag(Removed)) {
                OnItemsRemoved(item, count);
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

        private void RaiseForAdd(T item)
        {
            if (ActiveEvents.HasFlag(Added)) {
                OnItemsAdded(item, 1);
            }

            OnCollectionChanged();
        }

        private void RaiseForAddRange(SCG.IEnumerable<T> array)
        {
            Requires(array != null);

            if (ActiveEvents.HasFlag(Added)) {
                foreach (var item in array) {
                    OnItemsAdded(item, 1);
                }
            }

            OnCollectionChanged();
        }

        #region Invoking methods

        private void OnItemInserted(T item, int index)
            => _itemInserted?.Invoke(this, new ItemAtEventArgs<T>(item, index));

        private void OnItemRemovedAt(T item, int index)
            => _itemRemovedAt?.Invoke(this, new ItemAtEventArgs<T>(item, index));

        private void OnItemsRemoved(T item, int count)
            => _itemsRemoved?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnCollectionCleared(bool full, int count, int? start = null)
            => _collectionCleared?.Invoke(this, new ClearedEventArgs(full, count, start));

        private void OnCollectionChanged() => _collectionChanged?.Invoke(this, EventArgs.Empty);

        private void OnItemsAdded(T item, int count)
            => _itemsAdded?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        #endregion

        #endregion

        #region Utils



        #endregion

        #region Nested types

        #region Tag staff 

        /// <summary>
        /// A group of nodes with the same high tag. Purpose is to be
        /// able to tell the sequence order of two nodes without having to scan through
        /// the list.
        /// </summary>
        [Serializable]
        private class TagGroup
        {
            internal int Tag, Count; // ??? Capital

            internal Node First, Last; // ??? Capital

            /// <summary>
            /// Pretty print a tag group
            /// </summary>
            /// <returns>Formatted tag group</returns>
            public override string ToString()
                => $"TagGroup(tag={Tag}, cnt={Count}, fst={First}, lst={Last})";
        }


        // ??? in Fields region or Private methods

        //Constants for tag maintenance
        private const int WordSize = 32;

        private const int LoBits = 3;

        private const int HiBits = LoBits + 1;

        private const int LoSize = 1 << LoBits;

        private const int Hisize = 1 << HiBits;

        private const int LogWordSize = 5;

        private TagGroup GetTagGroup(Node pred, Node succ, out int lowbound, out int highbound)
        {
            TagGroup predgroup = pred.taggroup, succgroup = succ.taggroup;

            if (predgroup == succgroup) {
                lowbound = pred.tag + 1;
                highbound = succ.tag - 1;
                return predgroup;
            }

            if (predgroup.First != null) {
                lowbound = pred.tag + 1;
                highbound = int.MaxValue;
                return predgroup;
            }

            if (succgroup.First != null) {
                lowbound = int.MinValue;
                highbound = succ.tag - 1;
                return succgroup;
            }

            lowbound = int.MinValue;
            highbound = int.MaxValue;
            return new TagGroup();
        }

        /// <summary>
        /// Put a tag on a node (already inserted in the list). Split taggroups and renumber as 
        /// necessary.
        /// </summary>
        /// <param name="node">The node to tag</param>
        private void SetTagPrivate(Node node)
        {
            Node pred = node.Prev, succ = node.Next;
            TagGroup predgroup = pred.taggroup, succgroup = succ.taggroup;

            if (predgroup == succgroup) {
                node.taggroup = predgroup;
                predgroup.Count++;
                if (pred.tag + 1 == succ.tag)
                    SplitTagGroupPrivate(predgroup);
                else
                    node.tag = (pred.tag + 1) / 2 + (succ.tag - 1) / 2;
            }
            else if (predgroup.First != null) {
                node.taggroup = predgroup;
                predgroup.Last = node;
                predgroup.Count++;
                if (pred.tag == int.MaxValue)
                    SplitTagGroupPrivate(predgroup);
                else
                    node.tag = pred.tag / 2 + int.MaxValue / 2 + 1;
            }
            else if (succgroup.First != null) {
                node.taggroup = succgroup;
                succgroup.First = node;
                succgroup.Count++;
                if (succ.tag == int.MinValue)
                    SplitTagGroupPrivate(node.taggroup);
                else
                    node.tag = int.MinValue / 2 + (succ.tag - 1) / 2;
            }
            else {
                System.Diagnostics.Debug.Assert(Taggroups == 0);

                var newgroup = new TagGroup();

                Taggroups = 1;
                node.taggroup = newgroup;
                newgroup.First = newgroup.Last = node;
                newgroup.Count = 1;
            }
        }

        /// <summary>
        /// Remove a node from its taggroup.
        /// <br/> When this is called, node must already have been removed from the underlying list
        /// </summary>
        /// <param name="node">The node to remove</param>
        private void RemoveFromTagGroupPrivate(Node node)
        {
            TagGroup taggroup = node.taggroup;

            if (--taggroup.Count == 0) {
                Taggroups--;
                return;
            }

            if (node == taggroup.First)
                taggroup.First = node.Next;

            if (node == taggroup.Last)
                taggroup.Last = node.Prev;

            //node.taggroup = null;
            if (taggroup.Count != LoSize || Taggroups == 1)
                return;

            TagGroup otg;
            // bug20070911:
            Node neighbor;
            if ((neighbor = taggroup.First.Prev) != _startSentinel
                && (otg = neighbor.taggroup).Count <= LoSize)
                taggroup.First = otg.First;
            else if ((neighbor = taggroup.Last.Next) != _endSentinel
                     && (otg = neighbor.taggroup).Count <= LoSize)
                taggroup.Last = otg.Last;
            else
                return;

            Node n = otg.First;

            for (int i = 0, length = otg.Count; i < length; i++) {
                n.taggroup = taggroup;
                n = n.Next;
            }

            taggroup.Count += otg.Count;
            Taggroups--;
            n = taggroup.First;

            const int ofs = WordSize - HiBits;

            for (int i = 0, count = taggroup.Count; i < count; i++) {
                n.tag = (i - LoSize) << ofs; //(i-8)<<28 
                n = n.Next;
            }
        }

        private void SplitTagGroupPrivate(TagGroup taggroup)
        {
            var n = taggroup.First;
            var ptgt = taggroup.First.Prev.taggroup.Tag;
            var ntgt = taggroup.Last.Next.taggroup.Tag;

            System.Diagnostics.Debug.Assert(ptgt + 1 <= ntgt - 1);

            var ofs = WordSize - HiBits;
            var newtgs = (taggroup.Count - 1) / Hisize;
            int tgtdelta = (int) ((ntgt + 0.0 - ptgt) / (newtgs + 2)), tgtag = ptgt;

            tgtdelta = tgtdelta == 0 ? 1 : tgtdelta;
            for (var j = 0; j < newtgs; j++) {
                var newtaggroup = new TagGroup {
                    Tag = (tgtag = tgtag >= ntgt - tgtdelta ? ntgt : tgtag + tgtdelta),
                    First = n,
                    Count = Hisize
                };

                for (var i = 0; i < Hisize; i++) {
                    n.taggroup = newtaggroup;
                    n.tag = (i - LoSize) << ofs; //(i-8)<<28 
                    n = n.Next;
                }

                newtaggroup.Last = n.Prev;
            }

            var rest = taggroup.Count - Hisize * newtgs;

            taggroup.First = n;
            taggroup.Count = rest;
            taggroup.Tag = (tgtag = tgtag >= ntgt - tgtdelta ? ntgt : tgtag + tgtdelta);
            ofs--;
            for (var i = 0; i < rest; i++) {
                n.tag = (i - Hisize) << ofs; //(i-16)<<27 
                n = n.Next;
            }

            taggroup.Last = n.Prev;
            Taggroups += newtgs;
            if (tgtag == ntgt)
                RedistributeTagGroupsPrivate(taggroup);
        }

        private void RedistributeTagGroupsPrivate(TagGroup taggroup)
        {
            TagGroup pred = taggroup, succ = taggroup, tmp;
            double limit = 1, bigt = Math.Pow(Taggroups, 1.0 / 30); //?????
            int bits = 1, count = 1, lowmask = 0, himask = 0, target = 0;

            do {
                bits++;
                lowmask = (1 << bits) - 1;
                himask = ~lowmask;
                target = taggroup.Tag & himask;
                while ((tmp = pred.First.Prev.taggroup).First != null && (tmp.Tag & himask) == target) {
                    count++;
                    pred = tmp;
                }

                while ((tmp = succ.Last.Next.taggroup).Last != null && (tmp.Tag & himask) == target) {
                    count++;
                    succ = tmp;
                }

                limit *= bigt;
            } while (count > limit);

            //redistibute tags
            int lob = pred.First.Prev.taggroup.Tag, upb = succ.Last.Next.taggroup.Tag;
            int delta = upb / (count + 1) - lob / (count + 1);

            System.Diagnostics.Debug.Assert(delta > 0);
            for (int i = 0; i < count; i++) {
                pred.Tag = lob + (i + 1) * delta;
                pred = pred.Last.Next.taggroup;
            }
        }

        #endregion

        #region Position, PositionComparer and ViewHandler nested types

        [Serializable]
        class PositionComparer : SCG.IComparer<Position>
        {
            static PositionComparer _default;
            PositionComparer() { }

            public static PositionComparer Default
            {
                get { return _default ?? (_default = new PositionComparer()); }
            }

            public int Compare(Position a, Position b)
            {
                return a.Endpoint == b.Endpoint ? 0 : a.Endpoint.Precedes(b.Endpoint) ? -1 : 1;

            }
        }


        /// <summary>
        /// During RemoveAll, we need to cache the original endpoint indices of views
        /// </summary>
        struct Position
        {
            public readonly HashedLinkedList<T> View;
            public bool Left;
            public readonly Node Endpoint;

            public Position(HashedLinkedList<T> view, bool left)
            {
                View = view;
                Left = left;
                Endpoint = left ? view._startSentinel.Next : view._endSentinel.Prev;

            }

            public Position(Node node, int foo)
            {
                this.Endpoint = node;
                View = null;
                Left = false;
            }

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

            internal ViewHandler(HashedLinkedList<T> list)
            {
                leftEndIndex = rightEndIndex = leftEndIndex2 = rightEndIndex2 = viewCount = 0;
                leftEnds = rightEnds = null;

                if (list._views != null)
                    foreach (HashedLinkedList<T> v in list._views)
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

            internal void skipEndpoints(int removed, Node n)
            {
                if (viewCount > 0) {
                    Position endpoint;
                    while (leftEndIndex < viewCount && ((endpoint = leftEnds[leftEndIndex]).Endpoint.Prev.Precedes(n))) {
                        HashedLinkedList<T> view = endpoint.View;
                        view.Offset = view.Offset - removed; //TODO: extract offset.Value?
                        view.Count += removed;
                        leftEndIndex++;
                    }
                    while (rightEndIndex < viewCount && (endpoint = rightEnds[rightEndIndex]).Endpoint.Precedes(n)) {
                        HashedLinkedList<T> view = endpoint.View;
                        view.Count -= removed;
                        rightEndIndex++;
                    }
                }
                if (viewCount > 0) {
                    Position endpoint;
                    while (leftEndIndex2 < viewCount && (endpoint = leftEnds[leftEndIndex2]).Endpoint.Prev.Precedes(n))
                        leftEndIndex2++;
                    while (rightEndIndex2 < viewCount && (endpoint = rightEnds[rightEndIndex2]).Endpoint.Next.Precedes(n))
                        rightEndIndex2++;
                }
            }

            /// <summary>
            /// To be called with n pointing to the right of each node to be removed in a stretch. 
            /// And at the endsentinel. 
            /// 
            /// Update offset of a view whose left endpoint (has not already been handled and) is n or precedes n.
            /// I.e. startsentinel precedes n.
            /// Also update the size as a prelude to handling the right endpoint.
            /// 
            /// Update size of a view not already handled and whose right endpoint precedes n.
            /// </summary>
            /// <param name="removed">The number of nodes left of n to be removed</param>
            /// <param name="n"></param>
            internal void updateViewSizesAndCounts(int removed, Node n)
            {
                if (viewCount > 0) {
                    Position endpoint;
                    while (leftEndIndex < viewCount && ((endpoint = leftEnds[leftEndIndex]).Endpoint.Prev.Precedes(n))) {
                        HashedLinkedList<T> view = endpoint.View;
                        view.Offset = view.Offset - removed; //TODO: fix use of offset
                        view.Count += removed;
                        leftEndIndex++;
                    }
                    while (rightEndIndex < viewCount && (endpoint = rightEnds[rightEndIndex]).Endpoint.Precedes(n)) {
                        HashedLinkedList<T> view = endpoint.View;
                        view.Count -= removed;
                        rightEndIndex++;
                    }
                }
            }

            /// <summary>
            /// To be called with n being the first not-to-be-removed node after a (stretch of) node(s) to be removed.
            /// 
            /// It will update the startsentinel of views (that have not been handled before and) 
            /// whose startsentinel precedes n, i.e. is to be deleted.
            /// 
            /// It will update the endsentinel of views (...) whose endsentinel precedes n, i.e. is to be deleted.
            /// 
            /// PROBLEM: DOESNT WORK AS ORIGINALLY ADVERTISED. WE MUST DO THIS BEFORE WE ACTUALLY REMOVE THE NODES. WHEN THE 
            /// NODES HAVE BEEN REMOVED, THE precedes METHOD WILL NOT WORK!
            /// </summary>
            /// <param name="n"></param>
            /// <param name="newstart"></param>
            /// <param name="newend"></param>
            internal void updateSentinels(Node n, Node newstart, Node newend)
            {
                if (viewCount > 0) {
                    Position endpoint;
                    while (leftEndIndex2 < viewCount && (endpoint = leftEnds[leftEndIndex2]).Endpoint.Prev.Precedes(n)) {
                        HashedLinkedList<T> view = endpoint.View;
                        view._startSentinel = newstart;
                        leftEndIndex2++;
                    }
                    while (rightEndIndex2 < viewCount && (endpoint = rightEnds[rightEndIndex2]).Endpoint.Next.Precedes(n)) {
                        HashedLinkedList<T> view = endpoint.View;
                        view._endSentinel = newend;
                        rightEndIndex2++;
                    }
                }
            }
        }

        #endregion


        /// <summary>
        /// Node ???
        /// </summary>
        /// <typeparam name="V"></typeparam>
        private sealed class Node // Why not Node<T> ??
        {
            public Node Next; // why public ???
            public Node Prev; // why public ???
            public T item;

            #region Tag support

            internal int tag;

            internal TagGroup taggroup;

            internal bool Precedes(Node that)
            {
                //Debug.Assert(taggroup != null, "taggroup field null");
                //Debug.Assert(that.taggroup != null, "that.taggroup field null");
                int t1 = taggroup.Tag;
                int t2 = that.taggroup.Tag;

                return t1 < t2 ? true : t1 > t2 ? false : tag < that.tag;
            }

            #endregion

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


        /// <summary>
        /// This class is shared between the linked list and array list implementations.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        [Serializable]
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
        private sealed class ItemSet : CollectionValueBase<T>, ICollectionValue<T> // ??? CollectionValueBase
        {
            #region Fields

            private readonly HashedLinkedList<T> _base;
            private readonly int _version;
            private HashedLinkedList<T> _set;

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
            public ItemSet(HashedLinkedList<T> list)
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
                return _base.Choose(); // TODO: Is this necessarily an item in the collection value?!
                // Why _base.Chose(), but not _set.Choose(); ???
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                Set.CopyTo(array, arrayIndex);
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            // ? Why do we need it? Isn't that enough to overrire GetEnumerator()?
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
                    //var set = new SCG.HashSet<T>(_base.EqualityComparer);
                    var set = new HashedLinkedList<T>(_base.EqualityComparer);

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

            public override int GetHashCode()
            {
                CheckVersion();
                return base.GetHashCode(); // ??? warning
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
            private ICollectionValue<T> Set => _set ?? (_set = new HashedLinkedList<T>(_base, _base.EqualityComparer));

            #endregion
        }


        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class Duplicates : CollectionValueBase<T>, ICollectionValue<T>
        {
            #region Fields

            private readonly HashedLinkedList<T> _base;
            private readonly int _version;
            private readonly T _item;
            private HashedLinkedList<T> _list;

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
            public Duplicates(HashedLinkedList<T> list, T item)
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
                // No! return _base.Choose(); // TODO: Is this necessarily an item in the collection value?!
                return List.Choose();
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                List.CopyTo(array, arrayIndex);
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            public override SCG.IEnumerator<T> GetEnumerator()
            {
                CheckVersion();
                if (List.IsEmpty) {
                    yield break;
                }
                yield return List.Choose();
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

                    var item = _item;
                    return _base.Find(ref item)
                        ? new HashedLinkedList<T>(new[] { item }, _base.EqualityComparer)
                        : new HashedLinkedList<T>(_base.EqualityComparer);
                }
            }

            #endregion
        }


        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class Range : CollectionValueBase<T>, IDirectedCollectionValue<T>
        {
            #region Fields

            private readonly HashedLinkedList<T> _base;
            private readonly int _version, _startIndex, _count, _sign;
            private readonly EnumerationDirection _direction;
            private readonly Node _startNode;

            #endregion

            #region Constructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="Range"/> class that starts at the specified index and spans the next
            ///     <paramref name="count"/> items in the specified direction.
            /// </summary>
            /// <param name="list">
            ///     The underlying <see cref="HashedLinkedList{T}"/>.
            /// </param>
            /// <param name="startIndex">
            ///     The zero-based <see cref="HashedLinkedList{T}"/> index at which the range starts.
            /// </param>
            /// <param name="count">
            ///     The number of items in the range.
            /// </param>
            /// <param name="direction">
            ///     The direction of the range.
            /// </param>
            public Range(HashedLinkedList<T> list, int startIndex, int count, EnumerationDirection direction)
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

                // HashedLinkedList<T> list, int startIndex, int count, EnumerationDirection direction

                _base = list;
                _version = list._version;
                _sign = (int) direction;
                _startIndex = startIndex;
                _count = count;
                _direction = direction;
                if (count > 0) {
                    _startNode = list.GetNodeByIndexPrivate(startIndex);
                }
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
                return _startNode.item;
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();

                // Use enumerator instead of copying and then reversing
                base.CopyTo(array, arrayIndex); // ??? for backwards. Would it work? Yes, it is the enum, who decides the dir.
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            public override SCG.IEnumerator<T> GetEnumerator()
            {
                if (Count <= 0) {
                    yield break;
                }

                var cursor = _startNode;
                for (var i = 0; i < Count; i++) {
                    CheckVersion();
                    yield return cursor.item;
                    cursor = _direction.IsForward() ? cursor.Next : cursor.Prev;
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

        #endregion
    }
}