﻿// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

using static System.Diagnostics.Contracts.Contract;

using static C6.Contracts.ContractHelperExtensions;
using static C6.Contracts.ContractMessage;

using SC = System.Collections;
using SCG = System.Collections.Generic;

namespace C6
{
    // This is mainly the intersection of the main stream generic collection interfaces and the priority queue interface, ICollection<T> and IPriorityQueue<T>.
    /// <summary>
    ///     Represents a generic collection to which items can be added.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the items in the collection.
    /// </typeparam>
    [ContractClass(typeof(IExtensibleContract<>))]
    public interface IExtensible<T> : IListenable<T>
    {
        // TODO: Which one does it use, when there is a IComparer as well?!
        /// <summary>
        ///     Gets a value indicating whether the collection allows duplicates.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the collection allows duplicates; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     <c>true</c> if the collection has bag semantics and may contain two items that are duplicate, i.e. equal by the
        ///     collection's comparer or equality comparer. Otherwise <c>false</c>, in which case the collection has set semantics.
        /// </remarks>
        [Pure]
        bool AllowsDuplicates { get; }

        /// <summary>
        ///     Gets a value indicating whether the collection only stores an item once and keeps track of duplicates using a
        ///     counter.
        /// </summary>
        /// <value>
        ///     <c>true</c> if only one representative of a group of equal items is kept in the collection together with a counter;
        ///     <c>false</c> if each item is stored explicitly.
        /// </value>
        /// <remarks>
        ///     Is by convention always <c>false</c> for collections with set semantics, i.e. when <see cref="AllowsDuplicates"/>
        ///     is <c>false</c>.
        /// </remarks>
        [Pure]
        bool DuplicatesByCounting { get; }

        // TODO: wonder where the right position of this is. And the semantics. Should at least be in the same class as AllowsDuplicates!
        /// <summary>
        ///     Gets the <see cref="SCG.IEqualityComparer{T}"/> used by the collection.
        /// </summary>
        /// <value>
        ///     The <see cref="SCG.IEqualityComparer{T}"/> used by the collection.
        /// </value>
        [Pure]
        SCG.IEqualityComparer<T> EqualityComparer { get; }

        /// <summary>
        ///     Gets a value indicating whether the collection has a fixed size.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the collection has a fixed size; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     <para>
        ///         A collection with a fixed size does not allow operations that changes the collection's size.
        ///     </para>
        ///     <para>
        ///         Any collection that is read-only (<see cref="IsReadOnly"/> is <c>true</c>), has a fixed size; the opposite need
        ///         not be true.
        ///     </para>
        /// </remarks>
        [Pure]
        bool IsFixedSize { get; }

        // TODO: Move to ICollectionValue?
        /// <summary>
        ///     Gets a value indicating whether the collection is read-only.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the collection is read-only; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     A collection that is read-only does not allow the addition or removal of items after the collection is created.
        ///     Note that read-only in this context does not indicate whether individual items of the collection can be modified.
        /// </remarks>
        [Pure]
        bool IsReadOnly { get; }

        /// <summary>
        ///     Adds an item to the collection if possible.
        /// </summary>
        /// <param name="item">
        ///     The item to add to the collection. <c>null</c> is allowed, if <see cref="ICollectionValue{T}.AllowsNull"/> is
        ///     <c>true</c>.
        /// </param>
        /// <returns>
        ///     <c>true</c> if item was added; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         If the collection does not allow duplicates, i.e. <see cref="ICollectionValue{T}.AllowsNull"/> is <c>false</c>,
        ///         then the item will only be added if the collection does not already contain an item equal to it. The
        ///         collection's <see cref="EqualityComparer"/> is used to determine item equality.
        ///     </para>
        ///     <para>
        ///         If the item is added, it raises the following events (in that order) with the collection as sender:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemsAdded"/> with the added item and a count of one.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.CollectionChanged"/>.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        bool Add(T item);

        /// <summary>
        ///     Adds each item of the specified enumerable to the collection, if possible, in enumeration order.
        /// </summary>
        /// <param name="items">
        ///     The enumerable whose items should be added to the collection. The enumerable itself cannot be <c>null</c>, but its
        ///     items can, if <see cref="ICollectionValue{T}.AllowsNull"/> is <c>true</c>.
        /// </param>
        /// <returns>
        ///     <c>true</c> if any items were added to the collection; <c>false</c> if collection was unchanged.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         If the collection does not allow duplicates, i.e. <see cref="ICollectionValue{T}.AllowsNull"/> is <c>false</c>,
        ///         then the item will only be added if the collection does not already contain an item equal to it. The
        ///         collection's <see cref="EqualityComparer"/> is used to determine item equality.
        ///     </para>
        ///     <para>
        ///         This is equivalent to <c>
        ///             foreach (var item in items) { Add(item); }
        ///         </c>, but might be more efficient and it only raises the event
        ///         <see cref="IListenable{T}.CollectionChanged"/> once.
        ///     </para>
        ///     <para>
        ///         If the enumerable throws an exception during enumeration, the collection remains unchanged.
        ///     </para>
        ///     <para>
        ///         If any items are added, it raises the following events (in that order) with the collection as sender:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemsAdded"/> once for each item added (using a count of one) in
        ///                     enumeration order.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.CollectionChanged"/> once at the end.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        bool AddRange(SCG.IEnumerable<T> items);
    }


    [ContractClassFor(typeof(IExtensible<>))]
    internal abstract class IExtensibleContract<T> : IExtensible<T>
    {
        // ReSharper disable InvocationIsSkipped

        public bool AllowsDuplicates
        {
            get {
                // No preconditions

                // A set only contains distinct items // TODO: Is this the right place to put it?
                Ensures(Result<bool>() || Count == this.Distinct(EqualityComparer).Count());


                return default(bool);
            }
        }

        public bool DuplicatesByCounting
        {
            get {
                // No preconditions


                // False by convention for collections with set semantics
                Ensures(AllowsDuplicates || Result<bool>());

                // !!! @@@ Ensures(!AllowsDuplicates || !Result<bool>());


                return default(bool);
            }
        }

        public bool IsFixedSize
        {
            get
            {
                // No preconditions


                // Read-only list has fixed size
                Ensures(!IsReadOnly || Result<bool>());


                return default(bool);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                // No preconditions


                // No postconditions


                return default(bool);
            }
        }

        public SCG.IEqualityComparer<T> EqualityComparer
        {
            get {
                // No preconditions


                // Result is non-null
                Ensures(Result<SCG.IEqualityComparer<T>>() != null);


                return default(SCG.IEqualityComparer<T>);
            }
        }

        public bool Add(T item)
        {
            // is Valid, not disposed
            Requires(IsValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);


            // The collection becomes non-empty
            Ensures(!IsEmpty);

            // The collection will contain the item added
            Ensures(this.Contains(item, EqualityComparer));

            // Adding an item increases the count by one
            Ensures(Count == OldValue(Count) + (Result<bool>() ? 1 : 0));

            // Adding the item increases the number of equal items by one
            Ensures(this.CountDuplicates(item, EqualityComparer) == OldValue(this.CountDuplicates(item, EqualityComparer)) + (Result<bool>() ? 1 : 0));

            // If the item is add and its not a counter that is incremented, that item is in the collection
            Ensures(!Result<bool>() || DuplicatesByCounting || this.ContainsSame(item));

            // If result is false, the collection remains unchanged
            Ensures(Result<bool>() || this.IsSameSequenceAs(OldValue(ToArray())));

            // Returns true if bag semantic, otherwise the opposite of whether the collection already contained the item
            Ensures(AllowsDuplicates ? Result<bool>() : OldValue(this.Contains(item, EqualityComparer)) ? !Result<bool>() : Result<bool>());            
            //Ensures(AllowsDuplicates ? Result<bool>() : !Result<bool>());

            return default(bool);
        }

        public bool AddRange(SCG.IEnumerable<T> items)
        {
            // is Valid, not disposed
            Requires(IsValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be non-null
            Requires(items != null, ArgumentMustBeNonNull);            

            // All items must be non-null if collection disallows null values
            Requires(AllowsNull || ForAll(items, item => item != null), ItemsMustBeNonNull);

            // The collection becomes non-empty, if items are non-empty
            Ensures(items.IsEmpty() || !IsEmpty);

            // The collection will contain the items added            
            Ensures(ForAll(items, item => this.Contains(item, EqualityComparer)));             

            // If items were added, the count increases; otherwise it stays the same
            Ensures(Result<bool>() ? Count > OldValue(Count) : Count == OldValue(Count));

            // Empty enumerable returns false
            Ensures(!items.IsEmpty() || !Result<bool>());

            // Collection doesn't change if enumerator throws an exception
            EnsuresOnThrow<Exception>(this.IsSameSequenceAs(OldValue(ToArray())));

            // If result is false, the collection remains unchanged
            Ensures(Result<bool>() || this.IsSameSequenceAs(OldValue(ToArray())));

            // TODO: Make more exact check of added items - especially for sets

            return default(bool);
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
        public abstract bool AllowsNull { get; }
        public abstract int Count { get; }
        public abstract bool IsValid { get; }
        public abstract Speed CountSpeed { get; }
        public abstract bool IsEmpty { get; }
        public abstract T Choose();
        public abstract void CopyTo(T[] array, int arrayIndex);
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

        #endregion
    }
}