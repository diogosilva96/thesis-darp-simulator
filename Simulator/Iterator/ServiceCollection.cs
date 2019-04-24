using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simulator.Objects;

namespace Simulator.Iterator
{
    public class ServiceCollection:IServiceCollection
    {
        private ArrayList _items = new ArrayList();

        public int Count
        {
            get { return _items.Count; }
        }

        public object this[int index]
        {
            get
            {
                return _items[index]; 
            }
            set
            {
                _items.Add(value);
            }
        }

        public IServiceIterator CreateIterator()
        {
            return new ServiceIterator(this);
        }
    }
}
