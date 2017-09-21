﻿using System;
using C6.Collections;
using System.Diagnostics.Contracts;


namespace C6.MyTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var sq = new Square(1, "");
            Console.WriteLine(sq.Squared());
        }
    }

    public abstract class Shape
    {
        private string name;

        public Shape(string s)
        {
            // calling the set accessor of the Id property.
            Id = s;
        }

        public string Id
        {
            get { return name; }

            set { name = value; }
        }
        
        // Area is a read-only property - only a get accessor is needed:
        public abstract double Area { get; protected set; }

        public override string ToString()
        {
            return Id + " Area = " + string.Format("{0:F2}", Area);
        }

        public bool Squared() => false;
    }


    public class Square : Shape
    {
        private int side;

        public Square(int side, string id)
            : base(id)
        {
            this.side = side;
        }

        public override double Area
        {
            get {
                // Given the side, return the area of a square:
                return side * side;
            }

            protected set { value = 2; }
        }
        
    }
}