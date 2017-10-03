﻿using System;
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
    public class HashedLinkedList<T> : ICollection<T>
    {
        #region Fields

        private Node _startSentinel, _endSentinel;
        private SCG.IDictionary<T, Node> _itemNode; // ??? initialize here or somewhere else

        private HashedLinkedList<T> _underlying; // null for proper list
        private WeakViewList<HashedLinkedList<T>> _views; 
        private WeakViewList<HashedLinkedList<T>>.Node _myWeakView; // null for proper list

        private int _version, _sequencedHashCodeVersion = -1, _unsequencedHashCodeVersion = -1;
        private int _sequencedHashCode, _unsequencedHashCode;

        private event EventHandler _collectionChanged;
        private event EventHandler<ClearedEventArgs> _collectionCleared;
        private event EventHandler<ItemAtEventArgs<T>> _itemInserted, _itemRemovedAt;        
        private event EventHandler<ItemCountEventArgs<T>> _itemsAdded, _itemsRemoved;        

        private int _taggroups;
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

        public HashedLinkedList(SCG.IEnumerable<T> items, SCG.IEqualityComparer<T> equalityComparer = null):
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

        #region IList

        public virtual HashedLinkedList<T> Underlying => _underlying; // Do it IList<T> ???

        public virtual int Offset { get; private set; }

        #endregion

        #endregion

        #region Public methods

        #region ICollectionValue

        public T Choose() => First;

        public virtual void CopyTo(T[] array, int arrayIndex)
        {            
            foreach (var item in this)
            {
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
            if (FindOrAddToHashPrivate(item, node))
            {
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
            if (array.IsEmpty())
            {
                return false;
            }

            // ??? C6.LinkedList: All this below is in a private method InsertRangePrivate
            var countAdded = 0;
            foreach (var item in array) // or items???
            {
                var node = new Node(item);
                if (FindOrAddToHashPrivate(item, node))
                {
                    continue;
                }

                InsertNodeBeforePrivate(false, _endSentinel, node); // why false ???
                countAdded++;
            }

            if (countAdded <= 0)
            {
                return false;
            }

            UpdateVersion();

            (_underlying ?? this).RaiseForAddRange(array); // not array, only the added ones ???
            return true;
        }

        #endregion

        #region ICollection

        public virtual void Clear()
        {
            if (Count <= 0)
            {
                return;
            }

            var oldCount = Count;
            // Clear dict
            if (_underlying == null)
            {
                _itemNode.Clear();
            }
            else
            {
                foreach (var item in this)
                {
                    _itemNode.Remove(item);
                }
            }

            // linkedlist
            ClearPrivate();

            (_underlying ?? this).RaiseForClear(oldCount);
        }

        public virtual bool Contains(T item) => IndexOf(item) >= 0;

        public virtual bool ContainsRange(SCG.IEnumerable<T> items)
        {
            var array = items.ToArray(); // ??? to array(). Why ???
            if (array.IsEmpty())
            {
                return true;
            }

            if (IsEmpty)
            {
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
            if (!ContainsItemPrivate(item, out node))
            {
                return false;
            }

            item = node.item;
            return true;
        }

        public virtual ICollectionValue<T> FindDuplicates(T item) => new Duplicates(this, item);

        public virtual bool FindOrAdd(ref T item) => Find(ref item) || !Add(item);

        public virtual int GetUnsequencedHashCode()
        {
            if (_unsequencedHashCodeVersion == _version)
            {
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
            if (Count <= 0)
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
            RemoveAtPrivate(node, index); // remove from linked list

            (_underlying ?? this).RaiseForRemove(removedItem, 1);
            return true;
        }
        
        public virtual bool Remove(T item)
        {
            T removedItem;
            return Remove(item, out removedItem);
        }

        public virtual bool RemoveDuplicates(T item)
        {
            throw new NotImplementedException();
        }

        public virtual bool RemoveRange(SCG.IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public virtual bool RetainRange(SCG.IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }

        public virtual ICollectionValue<T> UniqueItems()
            => new ItemSet(this);

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
            Node node;            
            if (!ContainsItemPrivate(item, out node))
            {
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

        #region IIndexed

        public virtual int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IList

        public virtual T First => _startSentinel.Next.item;


        #endregion

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

        #region Explicit implementations

        SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        public SCG.IEnumerator<T> GetEnumerator() // overrides valuebase 
        {
            var version = (_underlying ?? this)._version; // ??? underlying

            var cursor = _startSentinel.Next;
            while (cursor != _endSentinel && CheckVersion(version))
            {
                yield return cursor.item;
                cursor = cursor.Next;
            }
        }

        void SCG.ICollection<T>.Add(T item) => Add(item);

        void SCG.ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        bool SCG.ICollection<T>.Contains(T item)
        {
            throw new NotImplementedException();
        }

        bool SCG.ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private methods

        private void RaiseForUpdate(T item, T oldItem)
        {
            if (ActiveEvents == None)
            {
                return;
            }

            OnItemsRemoved(oldItem, 1);
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        private T RemoveAtPrivate(Node node, int index)
        {
            //View: fixViewsBeforeSingleRemove(node, Offset + index);
            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;

            Count--;
            if (_underlying != null)
                _underlying.Count--;
            RemoveFromTagGroup(node);

            return node.item;
        }

        private bool TryRemoveFromHash(T item, out Node node) // ??? from Hash or Dict
        {
            if (_underlying == null)
            {
                return _itemNode.TryGetValue(item, out node) && _itemNode.Remove(item);
            }
            else
            {
                if (!ContainsItemPrivate(item, out node))
                {
                    return false;
                }

                _itemNode.TryGetValue(item, out node);
                _itemNode.Remove(item);
                return true;
            }
        }

        private bool ContainsItemPrivate(T item, out Node node)   // ??? remove: out Node node                 
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
            //TODO: mix with tag maintenance to only run through list once?
            ViewHandler viewHandler = new ViewHandler(this);
            if (viewHandler.viewCount > 0)
            {
                int removed = 0;
                Node n = _startSentinel.Next;
                viewHandler.skipEndpoints(0, n);
                while (n != _endSentinel)
                {
                    removed++;
                    n = n.Next;
                    viewHandler.updateViewSizesAndCounts(removed, n);
                }
                viewHandler.updateSentinels(_endSentinel, _startSentinel, _endSentinel);
                if (_underlying != null)
                    viewHandler.updateViewSizesAndCounts(removed, _underlying._endSentinel);
            }

            if (_underlying != null)
            {
                Node n = _startSentinel.Next;

                while (n != _endSentinel)
                {
                    n.Next.Prev = _startSentinel;
                    _startSentinel.Next = n.Next;
                    RemoveFromTagGroup(n);
                    n = n.Next;
                }
            }
            else
                Taggroups = 0;

            // classic

            UpdateVersion();

            _startSentinel.Next = _endSentinel;
            _endSentinel.Prev = _startSentinel;
            if (_underlying != null)
            {
                _underlying.Count -= Count;
            }
            Count = 0;
        }

        private void UpdateVersion() => _version++;

        private void InsertNodeBeforePrivate(bool updateViews, Node succ, Node node) // ??? updateViews
        {
            UpdateVersion();

            node.Next = succ;
            node.Prev = succ.Prev; // ??? why skipped in C5
            succ.Prev.Next = node;
            succ.Prev = node;

            Count++;
            if (_underlying != null)
            {
                _underlying.Count++;
            }

            SetTagPrivate(node);

            // View: if (updateViews) fixViewsAfterInsert(succ, pred, 1, 0);
        }

        private bool FindOrAddToHashPrivate(T item, Node node)
        {
            if (_itemNode.ContainsKey(item))
            {
                return true;
            }

            _itemNode[item] = node;
            return false;
        }

        private bool CheckVersion(int version)
        {
            if (version == _version)
            {
                return true;
            }

            // See https://msdn.microsoft.com/library/system.collections.ienumerator.movenext.aspx
            throw new InvalidOperationException(CollectionWasModified);
        }


        private void RaiseForRemove(T item, int count)
        {
            if (!ActiveEvents.HasFlag(Removed))
            {
                return;
            }

            OnItemsRemoved(item, count);
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
        
        private void RaiseForAdd(T item)
        {
            if (!ActiveEvents.HasFlag(Added))
            {
                return;
            }
            OnItemsAdded(item, 1);
            OnCollectionChanged();
        }

        private void RaiseForAddRange(SCG.IEnumerable<T> array) // ??? not correct; raise for really added ones.
        {
            if (! ActiveEvents.HasFlag(Added))
            {
                return;
            }

            foreach (var item in array)
            {
                OnItemsAdded(item, 1);
            }
            OnCollectionChanged();
        }

        #region Invoking methods

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

        /// <summary>
        /// Put a tag on a node (already inserted in the list). Split taggroups and renumber as 
        /// necessary.
        /// </summary>
        /// <param name="node">The node to tag</param>
        private void SetTagPrivate(Node node)
        {
            Node pred = node.Prev, succ = node.Next;
            TagGroup predgroup = pred.taggroup, succgroup = succ.taggroup;

            if (predgroup == succgroup)
            {
                node.taggroup = predgroup;
                predgroup.Count++;
                if (pred.tag + 1 == succ.tag)
                    SplitTagGroupPrivate(predgroup);
                else
                    node.tag = (pred.tag + 1) / 2 + (succ.tag - 1) / 2;
            }
            else if (predgroup.First != null)
            {
                node.taggroup = predgroup;
                predgroup.Last = node;
                predgroup.Count++;
                if (pred.tag == int.MaxValue)
                    SplitTagGroupPrivate(predgroup);
                else
                    node.tag = pred.tag / 2 + int.MaxValue / 2 + 1;
            }
            else if (succgroup.First != null)
            {
                node.taggroup = succgroup;
                succgroup.First = node;
                succgroup.Count++;
                if (succ.tag == int.MinValue)
                    SplitTagGroupPrivate(node.taggroup);
                else
                    node.tag = int.MinValue / 2 + (succ.tag - 1) / 2;
            }
            else
            {
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
        private void RemoveFromTagGroup(Node node)
        {
            TagGroup taggroup = node.taggroup;

            if (--taggroup.Count == 0)
            {
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

            for (int i = 0, length = otg.Count; i < length; i++)
            {
                n.taggroup = taggroup;
                n = n.Next;
            }

            taggroup.Count += otg.Count;
            Taggroups--;
            n = taggroup.First;

            const int ofs = WordSize - HiBits;

            for (int i = 0, count = taggroup.Count; i < count; i++)
            {
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
            int tgtdelta = (int)((ntgt + 0.0 - ptgt) / (newtgs + 2)), tgtag = ptgt;

            tgtdelta = tgtdelta == 0 ? 1 : tgtdelta;
            for (var j = 0; j < newtgs; j++)
            {
                var newtaggroup = new TagGroup {
                    Tag = (tgtag = tgtag >= ntgt - tgtdelta ? ntgt : tgtag + tgtdelta),
                    First = n,
                    Count = Hisize
                };

                for (var i = 0; i < Hisize; i++)
                {
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
            for (var i = 0; i < rest; i++)
            {
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

            do
            {
                bits++;
                lowmask = (1 << bits) - 1;
                himask = ~lowmask;
                target = taggroup.Tag & himask;
                while ((tmp = pred.First.Prev.taggroup).First != null && (tmp.Tag & himask) == target)
                {
                    count++;
                    pred = tmp;
                }

                while ((tmp = succ.Last.Next.taggroup).Last != null && (tmp.Tag & himask) == target)
                {
                    count++;
                    succ = tmp;
                }

                limit *= bigt;
            } while (count > limit);

            //redistibute tags
            int lob = pred.First.Prev.taggroup.Tag, upb = succ.Last.Next.taggroup.Tag;
            int delta = upb / (count + 1) - lob / (count + 1);

            System.Diagnostics.Debug.Assert(delta > 0);
            for (int i = 0; i < count; i++)
            {
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
            public static PositionComparer Default { get { return _default ?? (_default = new PositionComparer()); } }
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
            public Position(Node node, int foo) { this.Endpoint = node; View = null; Left = false; }

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
                leftEnds.Sort(PositionComparer.Default);
                rightEnds.Sort(PositionComparer.Default);
            }

            internal void skipEndpoints(int removed, Node n)
            {
                if (viewCount > 0)
                {
                    Position endpoint;
                    while (leftEndIndex < viewCount && ((endpoint = leftEnds[leftEndIndex]).Endpoint.Prev.Precedes(n)))
                    {
                        HashedLinkedList<T> view = endpoint.View;
                        view.Offset = view.Offset - removed; //TODO: extract offset.Value?
                        view.Count += removed;
                        leftEndIndex++;
                    }
                    while (rightEndIndex < viewCount && (endpoint = rightEnds[rightEndIndex]).Endpoint.Precedes(n))
                    {
                        HashedLinkedList<T> view = endpoint.View;
                        view.Count -= removed;
                        rightEndIndex++;
                    }
                }
                if (viewCount > 0)
                {
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
                if (viewCount > 0)
                {
                    Position endpoint;
                    while (leftEndIndex < viewCount && ((endpoint = leftEnds[leftEndIndex]).Endpoint.Prev.Precedes(n)))
                    {
                        HashedLinkedList<T> view = endpoint.View;
                        view.Offset = view.Offset - removed; //TODO: fix use of offset
                        view.Count += removed;
                        leftEndIndex++;
                    }
                    while (rightEndIndex < viewCount && (endpoint = rightEnds[rightEndIndex]).Endpoint.Precedes(n))
                    {
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
                if (viewCount > 0)
                {
                    Position endpoint;
                    while (leftEndIndex2 < viewCount && (endpoint = leftEnds[leftEndIndex2]).Endpoint.Prev.Precedes(n))
                    {
                        HashedLinkedList<T> view = endpoint.View;
                        view._startSentinel = newstart;
                        leftEndIndex2++;
                    }
                    while (rightEndIndex2 < viewCount && (endpoint = rightEnds[rightEndIndex2]).Endpoint.Next.Precedes(n))
                    {
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
        private sealed class WeakViewList<V> where V : class
        {
            Node start;

            [Serializable]
            internal class Node
            {
                internal WeakReference weakview; internal Node prev, next;
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
                // Why _base.Chose(), but not _set.Choose(); ???
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
                    var set = new HashedLinkedList<T>(_base.EqualityComparer);

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

                protected internal set { base.IsValid = value; }
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
                #region Code contracts

                Requires(IsValid);

                #endregion

                CheckVersion();                
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
                get
                {
                    if (_list != null)
                    {
                        return _list;
                    }

                    var item = default(T);
                    _base.Find(ref item);
                    return item == null ?
                        new HashedLinkedList<T>() :
                        new HashedLinkedList<T> { item };
                }
            }

            #endregion
        }


        #endregion

    }
}

