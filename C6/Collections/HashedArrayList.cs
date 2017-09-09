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
    public class HashedArrayList<T> : ICollectionValue<T>
    {
        #region Fields

        public static readonly T[] EmptyArray = new T[0];
        private T[] _items;
        private SCG.HashSet<KeyValuePair<T, int>> _itemIndex;

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

        public bool IsValid { get; }

        public int Count { get; protected set; } // to_base

        public bool AllowsNull => false; // by defintion!

        public Speed CountSpeed => Constant; // to_base: abstract

        public bool IsEmpty => Count == 0; // to base:virtual

        public virtual SCG.IEqualityComparer<T> EqualityComparer { get; } // ??? virtual

        public T Choose() => _items[0]; //to_base: virtual // Count - 1

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

        #endregion


    }
}