// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

using System;
using System.Linq;

using C6.Collections;
using C6.Tests.Helpers;

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

            var items = new[] { "8", "Ab", "4", "3", "5", "6", "7" };
            ArrayList<string> collection = new ArrayList<string>(items, allowsNull: true);
            var r = new[] { "3", "4" };
            var rng = new ArrayList<string>(r).AsBadEnumerable();


            var view = collection.View(0, 2);
            Console.WriteLine(view);







            // ==============================
            // RemoveRange
            //var items = new[] { "8", "Ab", "3", "4", "5", "6", "7", "9" };
            //var collection = new ArrayList<string>(items);
            //var view1 = collection.View(0, 1); // longer
            //var view2 = collection.View(0, 2);
            //var item = view1.Choose();
            //var itms = new ArrayList<string>(new[] { item });

            //view1.RemoveRange(itms);
            //Console.WriteLine(view2);


            //var items = new[] { "8", "Ab", "3", "4", "5", "6", "7", "9" };     
            // HLL.Reverse        
            //var items = new[] { "a", "b", "c", "d", "e" };
            //var linkedList = new ArrayList<string>(items);            
            //var v1 = linkedList.View(0, linkedList.Count);
            //var v2 = linkedList.View(0, 2);
            //v1.Reverse();
            //v1.Reverse();
            //Console.WriteLine(v2); 

            // HLL.Sort 
            //var items = new[] { "b", "a", "c", "e", "d" };
            //var linkedList = new HashedLinkedList<string>(items);
            //var v1 = linkedList.View(0, 3);
            //var v2 = linkedList.View(3, 2);
            //v1.Sort();
            //Console.WriteLine(v1);
            //Console.WriteLine(v2);

            // HAL.Add()
            //var items = new[] { "8", "Ab", "3", "4", "5", "6", "7", "9" };                             
            //var arrayList = new LinkedList<string>(items);
            //var v1 = arrayList.View(0, 7);
            //var v2 = arrayList.View(0, 7);
            //v1.Add("333333333");
            //Console.WriteLine(v1);
            //Console.WriteLine(v2);


            //Console.WriteLine(view1.IsValid);            
            //Console.WriteLine(view);
            //Console.WriteLine(collection);




            return;
            // Construct list using collection initializer
            //var list = new ArrayList<int>() { 2, 3, 5, 5, 7, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33};
            var list = new ArrayList<int>() { 2, 3 };
            var backList = list.Backwards();
            backList.ToList().ForEach(x => Console.Write(x + ", "));
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
            foreach (var prime in range.Backwards()) {
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