using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Simulator.Objects
{
    public class Person
    {
        private static int nextId;
        public int Id { get; internal set; }

        public Person()
        {
            Id = Interlocked.Increment(ref nextId);
        }

    }
}
