// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using C6.Collections;
using C6.Contracts;

using static C6.Contracts.ContractMessage;


using SCG = System.Collections.Generic;
using SC = System.Collections;


namespace C6.UserGuideExamples
{    
    public class ListExample
    {
        public static void Main()
        {
            //var eq = new C6.ComparerFactory.EqualityComparer<string>(ReferenceEquals,
            //    SCG.EqualityComparer<string>.Default.GetHashCode);

            //var eq = CaseInsensitiveStringComparer.Default;
            var items = new[] { "1", "Ab", "3", "4", "5", "6", "7" };
            var ll = new HashedLinkedList<string>(items, null);

            Console.WriteLine(ll.Add("ab"));
            
          




            // Act            


            return;
            // Construct list using collection initializer
            //var list = new ArrayList<int>() { 2, 3, 5, 5, 7, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33};
            var list = new ArrayList<int>() { 2, 3};
            var backList = list.Backwards();
            backList.ToList().ForEach(x => Console.Write(x + ", ") );
            Console.WriteLine(backList.IsValid);

            list.Add(10);
            Console.WriteLine(backList.IsValid);
            //backList.ToList().ForEach(x => Console.Write(x));


            //var list = list1.View(2, list1.Count-2);
            //var v = list.View(3,4);
            //var v2 = v.View(1, 2);
            //var items = new ArrayList<int>() { 3, 13, 7, 17};
            //Console.WriteLine(ArrayList<int>.EmptyArray);           

            
            
            var dupl = list.FindDuplicates(5);
            Console.WriteLine(dupl);
            list.Add(-100);
            var arr = dupl.ToArray();
            list.Dispose();



            //en.ToList().ForEach(x => Console.WriteLine(x));


            //Console.WriteLine(v);
            //Console.WriteLine(v2);
            //Console.WriteLine(list);

            return;

            // Get index of item
            var index = list.IndexOf(23);

            // Get an index range
            var range = list.GetIndexRange(index, 4);

            // Print range in reverse order
            foreach (var prime in range.Backwards())
            {
                Console.WriteLine(prime);
            }

            // Remove items within index range
            list.RemoveIndexRange(10, 3);

            // Remove item at index
            var second = list.RemoveAt(1);

            // Remove first item
            var first = list.RemoveFirst();

            // Remove last item
            var last = list.RemoveLast();

            // Create array with items in list
            var array = list.ToArray();

            // Clear list
            list.Clear();

            // Check if list is empty
            var isEmpty = list.IsEmpty;

            // Add item
            list.Add(first);

            // Add items from enumerable
            list.AddRange(array);

            // Insert item into list
            list.Insert(1, second);

            // Add item to the end
            list.Add(last);

            // Check if list is sorted
            var isSorted = list.IsSorted();

            // Reverse list
            list.Reverse();

            // Check if list is sorted
            var reverseComparer = ComparerFactory.CreateComparer<int>((x, y) => y.CompareTo(x));
            isSorted = list.IsSorted(reverseComparer);

            // Shuffle list
            var random = new Random(0);
            list.Shuffle(random);

            // Print list using indexer
            for (var i = 0; i < list.Count; i++) {
                Console.WriteLine($"{i,2}: {list[i],2}");
            }

            // Check if list contains all items in enumerable
            var containsRange = list.ContainsRange(array);

            // Construct list using enumerable
            var otherList = new ArrayList<int>(array);

            // Add every third items from list
            otherList.AddRange(list.Where((x, i) => i % 3 == 0));

            containsRange = list.ContainsRange(otherList);

            // Remove all items not in enumerable
            otherList.RetainRange(list);

            // Remove all items in enumerable from list
            list.RemoveRange(array);

            // Sort list
            list.Sort();

            // Copy to array
            list.CopyTo(array, 2);

            return;
        }
    }

    public class CaseInsensitiveStringComparer : SCG.IEqualityComparer<string>, SCG.IComparer<string>
    {
        private CaseInsensitiveStringComparer() { }

        public static CaseInsensitiveStringComparer Default => new CaseInsensitiveStringComparer();

        public int GetHashCode(string item) => ToLower(item).GetHashCode();

        public bool Equals(string x, string y) => ToLower(x).Equals(ToLower(y));

        // ReSharper disable once StringCompareToIsCultureSpecific
        public int Compare(string x, string y) => ToLower(x).CompareTo(ToLower(y));

        private string ToLower(string item) => item?.ToLower() ?? string.Empty;
    }
}