﻿// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using static System.Diagnostics.Contracts.Contract;

using static C6.Contracts.ContractHelperExtensions;
using static C6.Contracts.ContractMessage;

using SC = System.Collections;
using SCG = System.Collections.Generic;


namespace C6
{
    /// <summary>
    ///     Represents a sequenced generic collection whose items are accessible by index.
    /// </summary>
    [ContractClass(typeof(IIndexedContract<>))]
    public interface IIndexed<T> : ISequenced<T>
    {
        /// <summary>
        ///     Gets a value characterizing the asymptotic complexity of <see cref="this"/> proportional to collection size
        ///     (worst-case or amortized as relevant).
        /// </summary>
        /// <value>
        ///     A characterization of the asymptotic speed of <see cref="this"/> proportional to collection size.
        /// </value>
        [Pure]
        Speed IndexingSpeed { get; }

        /// <summary>
        ///     Gets the item at the specified index.
        /// </summary>
        /// <param name="index">
        ///     The zero-based index of the item to get.
        /// </param>
        /// <value>
        ///     The item at the specified index. <c>null</c> is allowed, if <see cref="ICollectionValue{T}.AllowsNull"/> is
        ///     <c>true</c>.
        /// </value>
        [IndexerName("Item")]
        [Pure]
        T this[int index] { get; }

        /// <summary>
        ///     Returns an <see cref="IDirectedCollectionValue{T}"/> containing the items in the specified index range of this
        ///     collection.
        /// </summary>
        /// <param name="startIndex">
        ///     The index of the first item in the <see cref="IDirectedCollectionValue{T}"/>.
        /// </param>
        /// <param name="count">
        ///     The number of items in the <see cref="IDirectedCollectionValue{T}"/>.
        /// </param>
        /// <returns>
        ///     An <see cref="IDirectedCollectionValue{T}"/> containing the items in the specified index range of this collection.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The <see cref="IDirectedCollectionValue{T}"/> is useful for enumerating an index range, either forwards or
        ///         backwards. Often used together with <see cref="IndexOf"/>. The forwards enumerator is equal to <c>
        ///             coll.Skip(startIndex).Take(count)
        ///         </c>, but potentially much faster.
        ///     </para>
        ///     <para>
        ///         The returned <see cref="IDirectedCollectionValue{T}"/> has the same status as an enumerator of the collection:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     You can use the <see cref="IDirectedCollectionValue{T}"/> to read the relevant data from the
        ///                     collection, but not to modify the collection.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     The <see cref="IDirectedCollectionValue{T}"/> does not have exclusive access to the collection so
        ///                     the <see cref="IDirectedCollectionValue{T}"/> remains valid as long as the collection remains
        ///                     unchanged. If changes are made to the collection, such as adding, modifying, or deleting items, the
        ///                     <see cref="IDirectedCollectionValue{T}"/> is invalidated and any call to its members will throw an
        ///                     <see cref="InvalidOperationException"/>.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///         The <see cref="IDirectedCollectionValue{T}"/> is lazy and will defer execution as much as possible. The return
        ///         value of one call can profitably be shared, as the result is cached. The directed collection value's
        ///         <see cref="ICollectionValue{T}.CountSpeed"/> can be used to indicate whether the full result has already been
        ///         computed.
        ///     </para>
        /// </remarks>
        [Pure]
        IDirectedCollectionValue<T> GetIndexRange(int startIndex, int count);

        /// <summary>
        ///     Searches from the beginning of the collection for the specified item and returns the zero-based index of the first
        ///     occurrence within the collection.
        /// </summary>
        /// <param name="item">
        ///     The item to locate in the collection. <c>null</c> is allowed, if <see cref="ICollectionValue{T}.AllowsNull"/> is
        ///     <c>true</c>.
        /// </param>
        /// <returns>
        ///     The zero-based index of the first occurrence of item within the entire collection, if found; otherwise, the ones'
        ///     complement of the index at which <see cref="ICollection{T}.Add"/> would put the item.
        /// </returns>
        [Pure]
        int IndexOf(T item);

        /// <summary>
        ///     Searches from the end of the collection for the specified item and returns the zero-based index of the first
        ///     occurrence within the collection.
        /// </summary>
        /// <param name="item">
        ///     The item to locate in the collection. <c>null</c> is allowed, if <see cref="ICollectionValue{T}.AllowsNull"/> is
        ///     <c>true</c>.
        /// </param>
        /// <returns>
        ///     The zero-based index of the last occurrence of item within the entire collection, if found; otherwise, the ones'
        ///     complement of the index at which <see cref="ICollection{T}.Add"/> would put the item.
        /// </returns>
        [Pure]
        int LastIndexOf(T item);

        /// <summary>
        ///     Removes the item at the specified index of the collection.
        /// </summary>
        /// <param name="index">
        ///     The zero-based index of the item to remove.
        /// </param>
        /// <returns>
        ///     The item removed from the collection.
        /// </returns>
        /// <remarks>
        ///     Raises the following events (in that order) with the collection as sender:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemRemovedAt"/> with the removed item and the index.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemsRemoved"/> with the removed item and a count of one.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionChanged"/>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        T RemoveAt(int index);

        /// <summary>
        ///     Remove all items in the specified index range.
        /// </summary>
        /// <param name="startIndex">
        ///     The index of the first item to remove.
        /// </param>
        /// <param name="count">
        ///     The number of items to remove.
        /// </param>
        /// <remarks>
        ///     If the cleared index range is non-empty, it raises the following events (in that order) with the collection as
        ///     sender:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionCleared"/> as non-full and with count equal to
        ///                 <paramref name="count"/>.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionChanged"/>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        void RemoveIndexRange(int startIndex, int count);
    }


    [ContractClassFor(typeof(IIndexed<>))]
    internal abstract class IIndexedContract<T> : IIndexed<T>
    {
        // ReSharper disable InvocationIsSkipped

        public Speed IndexingSpeed
        {
            get {
                // No preconditions


                // Result is a valid enum constant
                Ensures(Enum.IsDefined(typeof(Speed), Result<Speed>()));


                return default(Speed);
            }
        }

        public T this[int index]
        {
            get {
                // Argument must be within bounds (collection must be non-empty)
                Requires(0 <= index, ArgumentMustBeWithinBounds);
                Requires(index < Count, ArgumentMustBeWithinBounds);

                // Result is item at index
                Ensures(Result<T>().IsSameAs(this.ElementAt(index)));

                return default(T);
            }
        }

        public IDirectedCollectionValue<T> GetIndexRange(int startIndex, int count)
        {
            // Argument must be within bounds
            Requires(0 <= startIndex, ArgumentMustBeWithinBounds);
            Requires(startIndex + count <= Count, ArgumentMustBeWithinBounds);

            // Argument must be non-negative
            Requires(0 <= count, ArgumentMustBeNonNegative);

            // is Valid, not disposed
            Requires(IsValid);



            // Result is non-null
            Ensures(Result<IDirectedCollectionValue<T>>() != null);

            // Result equals subrange
            Ensures(Result<IDirectedCollectionValue<T>>().IsSameSequenceAs(this.Skip(startIndex).Take(count)));

            // Result has the same count
            Ensures(Result<IDirectedCollectionValue<T>>().Count == count);

            // Result allows null if this does
            Ensures(Result<IDirectedCollectionValue<T>>().AllowsNull == AllowsNull);

            // Result count speed is constant
            Ensures(Result<IDirectedCollectionValue<T>>().CountSpeed == Speed.Constant); // TODO: Is this always constant? We would at least like that, right?

            // Result direction is opposite
            Ensures(Result<IDirectedCollectionValue<T>>().Direction == EnumerationDirection.Forwards);

            // Result is empty if this is
            Ensures(Result<IDirectedCollectionValue<T>>().IsEmpty == (count == 0));

            // Result array is backwards                        
            Ensures(Result<IDirectedCollectionValue<T>>().ToArray().IsSameSequenceAs(this.Skip(startIndex).Take(count)));

            return default(IDirectedCollectionValue<T>);
        }

        public int IndexOf(T item)
        {
            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);

            // is Valid, not disposed
            Requires(IsValid);

            // Result is a valid index
            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : 0 <= ~Result<int>() && ~Result<int>() <= Count);


            return default(int);
        }

        public int LastIndexOf(T item)
        {
            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);

            // is Valid, not disposed
            Requires(IsValid);

            // Result is a valid index
            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : 0 <= ~Result<int>() && ~Result<int>() <= Count);

            // Item at index equals item            
            Ensures(Result<int>() < 0 || EqualityComparer.Equals(item, this[Result<int>()]));

            // No item after index equals item
            Ensures(Result<int>() < 0 || !this.Skip(Result<int>() + 1).Contains(item, EqualityComparer));


            return default(int);
        }

        public T RemoveAt(int index)
        {
            // is Valid, not disposed
            Requires(IsValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be within bounds (collection must be non-empty)
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index < Count, ArgumentMustBeWithinBounds);


            // Result is the item previously at the specified index            
            Ensures(Result<T>().IsSameAs(OldValue(this[index])));

            // Only the item at index is removed
            Ensures(this.IsSameSequenceAs(OldValue(this.SkipIndex(index).ToList())));

            // Result is non-null
            Ensures(AllowsNull || Result<T>() != null);

            // Removing an item decreases the count by one
            Ensures(Count == OldValue(Count) - 1);

            return default(T);
        }

        public void RemoveIndexRange(int startIndex, int count)
        {
            // is Valid, not disposed
            Requires(IsValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be within bounds (collection must be non-empty)
            Requires(0 <= startIndex, ArgumentMustBeWithinBounds);
            Requires(startIndex + count <= Count, ArgumentMustBeWithinBounds);

            // Argument must be non-negative
            Requires(0 <= count, ArgumentMustBeNonNegative);


            // Only the items in the index range are removed
            Ensures(this.IsSameSequenceAs(OldValue(this.SkipRange(startIndex, count).ToList())));

            // Removing an item decreases the count by one
            Ensures(Count == OldValue(Count) - count);


            return;
        }

        // ReSharper restore InvocationIsSkipped

        #region Non-Contract Methods

        #region SCG.IEnumerable<T>

        public abstract SCG.IEnumerator<T> GetEnumerator();
        SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region IShowable

        public abstract string ToString(string format, IFormatProvider formatProvider);
        public abstract bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider);

        #endregion

        #region ICollectionValue<T>
        public abstract bool IsValid { get; }
        public abstract bool AllowsNull { get; }        
        public abstract Speed CountSpeed { get; }
        public abstract bool IsEmpty { get; }
        public abstract T Choose();
        public abstract T[] ToArray();

        #endregion

        #region IListenable<T>

        public abstract EventTypes ActiveEvents { get; }
        public abstract EventTypes ListenableEvents { get; }
        public abstract event EventHandler CollectionChanged;
        public abstract event EventHandler<ClearedEventArgs> CollectionCleared;
        public abstract event EventHandler<ItemAtEventArgs<T>> ItemInserted;
        public abstract event EventHandler<ItemAtEventArgs<T>> ItemRemovedAt;
        public abstract event EventHandler<ItemCountEventArgs<T>> ItemsAdded;
        public abstract event EventHandler<ItemCountEventArgs<T>> ItemsRemoved;

        #endregion

        #region IDirectedCollectionValue<T>

        public abstract EnumerationDirection Direction { get; }
        public abstract IDirectedCollectionValue<T> Backwards();

        #endregion

        #region IExtensible

        public abstract bool AllowsDuplicates { get; }
        public abstract bool DuplicatesByCounting { get; }
        public abstract SCG.IEqualityComparer<T> EqualityComparer { get; }
        public abstract bool IsFixedSize { get; }
        public abstract bool AddRange(SCG.IEnumerable<T> items);

        #endregion

        #region SCG.ICollection<T>

        void SCG.ICollection<T>.Add(T item) {}

        #endregion

        #region ICollection<T>

        public abstract int Count { get; }
        public abstract Speed ContainsSpeed { get; }
        public abstract bool IsReadOnly { get; }
        public abstract bool Add(T item);
        public abstract void Clear();
        public abstract bool Contains(T item);
        public abstract bool ContainsRange(SCG.IEnumerable<T> items);
        public abstract void CopyTo(T[] array, int arrayIndex);
        public abstract int CountDuplicates(T item);
        public abstract bool Find(ref T item);
        public abstract ICollectionValue<T> FindDuplicates(T item);
        public abstract bool FindOrAdd(ref T item);
        public abstract int GetUnsequencedHashCode();
        public abstract ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities();
        public abstract bool Remove(T item);
        public abstract bool Remove(T item, out T removedItem);
        public abstract bool RemoveDuplicates(T item);
        public abstract bool RemoveRange(SCG.IEnumerable<T> items);
        public abstract bool RetainRange(SCG.IEnumerable<T> items);
        public abstract ICollectionValue<T> UniqueItems();
        public abstract bool UnsequencedEquals(ICollection<T> otherCollection);
        public abstract bool Update(T item);
        public abstract bool Update(T item, out T oldItem);
        public abstract bool UpdateOrAdd(T item);
        public abstract bool UpdateOrAdd(T item, out T oldItem);

        #endregion

        #region ISequenced<T>

        public abstract int GetSequencedHashCode();
        public abstract bool SequencedEquals(ISequenced<T> otherCollection);

        #endregion

        #endregion
    }
}