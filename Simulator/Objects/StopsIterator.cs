using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphLibrary.Objects;
using Simulator.Objects.Data_Objects;

namespace Simulator.Objects
{
    public class StopsIterator
    {
        public Stop CurrentStop;
        public Stop NextStop;

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

        
        public void Reset()
        {
            if (_stops != null)
            {
                _currentIndex = 0;
                if (_stops.Count > 0)
                {
                    CurrentStop = _stops[_currentIndex];
                    IsDone = false;
                    NextStop = _stops[_currentIndex+1];
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
                    CurrentStop = _stops[_currentIndex];
                    NextStop = _stops[_currentIndex + 1];
                    return true;
                }
                else
                {
                    CurrentStop = _stops[_currentIndex];
                    NextStop = null;
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
