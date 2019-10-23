using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simulator.Objects.Data_Objects;

namespace Simulator.Objects
{
    public class StopsIterator
    {
        public Stop CurrentStop => _stops[_currentIndex];

        public Stop NextStop => _currentIndex + 1 >= _stops.Count ? null:_stops[_currentIndex + 1];

        public int TotalStops => _stops.Count;

        public bool IsDone;

        private readonly List<Stop> _stops;

        private int _currentIndex;

        public int CurrentIndex {
            get { return _currentIndex; }
        }

        public StopsIterator(List<Stop> stops)
        {
            _stops = stops;
            Reset();
        }


        public void Init(int index)
        {
            if (index < _stops.Count && index >= 0)
            {
                _currentIndex = index;
            }
        }

        public void Reset()
        {
            if (_stops != null)
            {
                _currentIndex = 0;
                if (_stops.Count > 0)
                {
                    IsDone = false;
                }
            } 
        }

        public bool Next()
        {
            if (IsDone)
            {
                return false;
            }
            if (_stops != null && _stops.Count >0)
            {
                _currentIndex++;
                if (_currentIndex +1 <= _stops.Count - 1)
                {
                    return true;
                }
                else
                {
                    IsDone = true;
                    return true;
                }
                
            }

            return false;
        }
      
        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }
    }
}
