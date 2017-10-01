using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static C6.Collections.ExceptionMessages;

using SCG = System.Collections.Generic;
using SC = System.Collections;

namespace C6.Collections
{
    public class HashedLinkedList<T> : ICollectionValue<T>
    {
        #region Fields

        private Node _startSenteinel, _endSentinel;

        private HashedLinkedList<T> _underlying;
        private WeakViewList<HashedLinkedList<T>> _views;
        private WeakViewList<HashedArrayList<T>>.Node _myWeakView;

        private int _version, _sequencedHashCodeVersion = -1, _unsequencedHashCodeVersion = -1;
        private int _sequencedHashCode, _unsequencedHashCode;

        #endregion

        #region Constructors



        #endregion

        #region Properties

        private int UnderlyingCount => (_underlying ?? this).Count; 

        #region ICollectionValue

        public virtual int Count { get; private set; }
        public bool AllowsNull => false;
        public Speed CountSpeed => Speed.Constant;
        public bool IsEmpty => Count == 0;
        public bool IsValid { get; private set; }
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

        #region IList

        public virtual T First => _startSenteinel.Next.item;
        

        #endregion

        #endregion

        #region Explicit implementations

        SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        public SCG.IEnumerator<T> GetEnumerator() // overrides valuebase 
        {
            var version = (_underlying ?? this)._version; // ??? underlying

            var cursor = _startSenteinel.Next;
            while (cursor != _endSentinel && CheckVersion(version))
            {
                yield return cursor.item;
                cursor = cursor.Next;
            }
        }

        #endregion

        #region Private methods

        private bool CheckVersion(int version)
        {
            if (version == _version)
            {
                return true;
            }

            // See https://msdn.microsoft.com/library/system.collections.ienumerator.movenext.aspx
            throw new InvalidOperationException(CollectionWasModified);
        }

        #endregion

        #region Nested types

        /// <summary>
        /// Node ???
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

