using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects;

namespace Simulator.Iterator
{
    public class ServiceIterator:IServiceIterator
    {
        private ServiceCollection _services;
        private int _current = 0;

        public ServiceIterator(ServiceCollection services)
        {
            _services = services;

        }


        public Service First()
        {
            _current = 0;
            return _services[_current] as Service;
        }

        public Service Next()
        {
            _current++;
            if (!IsDone)
            {
                return _services[_current] as Service;
                
            }
            else
            {
                return null;
            }
        }


        public bool IsDone => _current >= _services.Count;
        public Service Current => _services[_current] as Service;
    }
}
