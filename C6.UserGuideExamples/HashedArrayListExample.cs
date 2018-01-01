// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

using System;
using System.Linq;
using System.Text;

using C6.Collections;
using C6.Contracts;
using C6.Tests;

using static C6.Contracts.ContractMessage;


using SCG = System.Collections.Generic;
using SC = System.Collections;


namespace C6.UserGuideExamples
{
    public class HashedArrayListExample
    {
        public void Main()
        {
            // Construct hashed array list using collection initializer
            var list = new HashedArrayList<int> { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59 };

            // Chose from list
            list.Choose();

            var array = new int[list.Count];
            // Copy list to array
            list.CopyTo(array, 0);                        

            // Add item to the end of list
            list.Add(61);

            // Add range to list
            array = new[] { 67, 71, 73, 79, 83 };
            list.AddRange(array);

            // Check if list contains an item
            list.Contains(list.Choose());

            // Check if list contains all items in enumerable
            list.ContainsRange(array);

            // Count all occuerence of an item
            list.CountDuplicates(list.Choose());

            // Find an item in list
            var itemToFind = list.Last;
            list.Find(ref itemToFind);

            // Return all occurence of an item
            list.FindDuplicates(itemToFind);
                        
            // Remove within range
            list.RemoveIndexRange(0,3);

            var range = new[] { list.First, list.Last };
            // Remove all items in enumerable from list 
            list.RemoveRange(range);

            // Retain all items in enumarable from list
            list.RetainRange(list.ToArray());

            var lastItem = list.Last;
            // Find last index of an item
            list.LastIndexOf(lastItem);

            // Insert at the end of list
            list.InsertLast(100);

            // Insert at the beginning of list
            list.InsertFirst(-100);

            // Reverse list
            list.Reverse();

            // Shuffle list
            list.Shuffle();

            // Sort list
            list.Sort();

            // Check if list is sorted
            var isSorted = list.IsSorted();

            // Print all items in list by indexer
            var index = 0;
            foreach (var item in list) {
                Console.WriteLine($"list[{index++}] = {item}");
            }

            // Clear list
            list.Clear();            
        }
    }
}