using System;
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
    public class LinkedList<T> : IExtensible<T>
    {
        #region Fields

        private Node _starSentinel, _endSentinel;

        private WeakViewList<LinkedList<T>> _views;
        private LinkedList<T> _underlying; // always null for a proper list
        private WeakViewList<LinkedList<T>>.Node _myWeakReference; // always null for a proper list

        private int _version, _sequencedHashCodeVersion = -1, _unsequencedHashCodeVersion = -1;

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

        #region IList
        public LinkedList<T> Underlying => _underlying; // ??? do it IList<T>

        #endregion

        private int UnderlyingCount => (Underlying ?? this).Count; 

        #endregion
        
        #region Public Methods

        #region ICollectionValue

        public T Choose() => First;

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
                array[arrayIndex++] = item;
        }

        public T[] ToArray()
        {
            var array = new T[Count];
            CopyTo(array,0);
            return array;
        }

        public bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider) 
            => Showing.Show(this, stringBuilder, ref rest, formatProvider);

        public string ToString(string format, IFormatProvider formatProvider)
            => Showing.ShowString(this, format, formatProvider);

        public override string ToString() => ToString(null, null);

        #endregion

        #region IExtensible

        public bool Add(T item)
        {
            #region Code Contracts            
            // The version is updated            
            Ensures(_version != OldValue(_version));            
            #endregion

            InsertPrivate(Count, _endSentinel, item);
            (_underlying ?? this).RaiseForAdd(item);
            return true;
        }
        
        public bool AddRange(SCG.IEnumerable<T> items)
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

        #region IList

        public virtual T First => _starSentinel.Next.item;


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

        #endregion

        #region Private Methods

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

        #region Invoking methods
        private void OnCollectionChanged()
        {
            _collectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnItemsAdded(T item, int count)
        {
            _itemsAdded?.Invoke(this, new ItemCountEventArgs<T>(item, count));
        }
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
