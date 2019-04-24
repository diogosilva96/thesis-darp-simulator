using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Iterator
{
    public interface IServiceCollection
    {
        IServiceIterator CreateIterator();
    }
}
