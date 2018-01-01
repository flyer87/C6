Offset, Count, IsEmpty, First, Last, (the sequence is correct)
			// Result's Count is equal to count
            // Ensures(Result<IList<T>>().Count == count);
			
			// Result is empty if this is
            // Ensures(Result<IList<T>>().IsEmpty == (count == 0));
			
			// Result's First is correct
            // Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().First().Equals(Underlying.Skip(Offset + offset).Take(count).First()));

            // Result's Last is correct
            // Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().Last().Equals(Underlying.Skip(Offset + offset).Take(count).Last()));
			
			//Result is correct
            //Ensures(Result<IList<T>>().IsSameSequenceAs(Underlying.Skip(OldValue(Offset) + offset).Take(count))); // ??? others to change
			
			==========================			
			==========================
			// Result's Offset is equal to index
            Ensures(Result<IList<T>>().Offset == Offset + offset);
			new:
			// true
			Ensures(!Result<bool> || Offset == OldValue(Offset) + offset );
			// false
			Ensures( Result<bool> || Offset == OldValue(Offset) );
			-----			
            // Result's Count is equal to count
            Ensures(Result<IList<T>>().Count == count);
			new:
			// true
			Ensures(!Result<bool> || Count == count );
			// false
			Ensures( Result<bool> || Count == OldValue(Count) );
			------------------------------
			// Result is empty if this is
            Ensures(Result<IList<T>>().IsEmpty == (count == 0));
			new:
			// true
			Ensures(!Result<bool> || IsEmpty == (count == 0) );
			// false
			Ensures( Result<bool> || IsEmpty == OldValue(IsEmpty) );		
			------------------------------
			// Result's First is correct
            Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().First.Equals(Underlying.Skip(Offset + offset).Take(count).First()));			
			new:
			// true
			Ensures(!Result<bool> || First.IsSameAs(Underlying.Skip(OldValue(Offset) + offset).Take(count).First()));
			// false
			Ensures( Result<bool> || First.IsSameAs(OldValue(First)));
			-----------------------------------
            // Last is correct, if Result is true
            Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>.Last.Equals(Underlying.Skip(Offset + offset).Take(count).Last()));			
			// true
			Ensures(!Result<bool> || Result<IList<T>>().IsEmpty || Last.IsSameAs(Underlying.Skip(OldValue(Offset) + offset).Take(count).Last()));			
			// false
			Ensures( Result<bool> || Last.IsSameAs(OldValue(Last)));			
			-----------------------------------
			//Result is correct            
			Ensures(Result<IList<T>>().IsSameSequenceAs(Underlying.Skip(Offset+offset).Take(count)));
			new:
			// true
			Ensures(!Result<bool> || IsSameSequenceAs(Underlying.Skip(OldValue(Offset) + offset).Take(count)));
			// false
			Ensures( Result<bool> || IsSameSequenceAs(OldValue(ToArray()));