using System;
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
    public class HashedLinkedList<T> : IExtensible<T>
    {
        #region Fields

        private Node _startSentinel, _endSentinel;
        private SCG.IDictionary<T, Node> _itemNode; // ??? initialize here or somewhere else

        private HashedLinkedList<T> _underlying; // null for proper list
        private WeakViewList<HashedLinkedList<T>> _views; 
        private WeakViewList<HashedArrayList<T>>.Node _myWeakView; // null for proper list

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

        #region IList

        public virtual HashedLinkedList<T> Underlying => _underlying; // Do it IList<T> ???


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

        #endregion

        #region Private methods

        private void UpdateVersion() => _version++;

        private void InsertNodeBeforePrivate(bool updateViews, Node succ, Node node) // ??? updateViews
        {
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

        #endregion
        
                
    }
}

