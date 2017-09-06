using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C6
{
    interface IMyInterface
    {
        void DoSomething();
    }

    class MyClass : IMyInterface
    {
        public void DoSomething()
        {
            throw new NotImplementedException();
        }
    }
}
