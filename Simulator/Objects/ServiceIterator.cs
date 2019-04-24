using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects
{
    public class ServiceIterator
    {
        private List<Service> _services;

        private int _currentServiceIndex;

        private Service _currentService;

        public ServiceIterator(List<Service> services)
        {
            _services = services;
        }

        public Service Current
        {
            get => _services[_currentServiceIndex];
            set
            {
                if (!_services.Contains(value)) return;
                _currentService = value;
                _currentServiceIndex = _services.FindIndex(s => s == _currentService);
            }
        }

        public bool Next()
        {
            if (_currentService == null)
            {
                if (_services.Count > 0)
                {
                    Reset();
                    return true;
                }
            }
            if (_currentServiceIndex + 1 < _services.Count)
            {
                Current = _services[_currentServiceIndex + 1];
                return true;
            }

            return false;
        }

        public void Reset()
        {
            if (_services.Count > 0)
            {
                Current = _services[0];
            }
        }
    }
}
