using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects;

namespace Simulator.Iterator
{
    public interface IServiceIterator
    {
        Service First();
        Service Next();
        bool IsDone { get; }

        Service Current { get; }
    }
}
