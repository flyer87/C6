using System;
using C6.Collections;
using System.Diagnostics.Contracts;

namespace C6.MyTests
{
    class Program
    {
        static void Main(string[] args)
        {
            // Construct list using collection initializer
            var list = new ArrayList<int> { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 };
            var list2 = new ArrayList<String>();
            //list2.Add(null);
            //list.Insert(-1, 1);

            var view = list.View(2, 1);
            Console.WriteLine("First: " + view.First);
            Console.WriteLine("Last: " + view.Last);
            Console.WriteLine("Offset: " + view.Offset);
            //Console.WriteLine("Print: " + view.Print());
            //Console.WriteLine("Print: " + list.Print());
        }
    }
}