           // Result is not null
            Ensures(Result<IList<T>>() != null);

            // Result's underlying is the same as the underlying of this or this
            Ensures(Result<IList<T>>().Underlying == (Underlying ?? this));

            // Result's Offset is equal to index
            Ensures(Result<IList<T>>().Offset == index);

            // Result's Count is equal to count
            Ensures(Result<IList<T>>().Count == count);

            // Result's IsFixedSize is the same as this's
            Ensures(Result<IList<T>>().IsFixedSize == IsFixedSize);

            // Result's IsReadOnly is the same as this's
            Ensures(Result<IList<T>>().IsReadOnly == IsReadOnly);

            // Result's IndexingSpeed is the same as this's
            Ensures(Result<IList<T>>().IndexingSpeed == IndexingSpeed);

            // Result's ContainsSpeed is the same as this's
            Ensures(Result<IList<T>>().ContainsSpeed == ContainsSpeed);

            // Result's AllowsDuplicates is the same as this's
            Ensures(Result<IList<T>>().AllowsDuplicates == AllowsDuplicates);

            // Result's DuplicatesByCounting is the same as this's
            Ensures(Result<IList<T>>().DuplicatesByCounting == DuplicatesByCounting);

            // Result's First is correct
            Ensures(Result<IList<T>>().First.Equals(this.Skip(index).Take(count).First()));

            // Result's Last is correct
            Ensures(Result<IList<T>>().Last.Equals(this.Skip(index).Take(count).Last()));            

            // Result's direction is the same as this's direction
            Ensures(Result<IList<T>>().Direction == this.Direction);

            // Result is empty if this is
            Ensures(Result<IList<T>>().IsEmpty == (count == 0));            
