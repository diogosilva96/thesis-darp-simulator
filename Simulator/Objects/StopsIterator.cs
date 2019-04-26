using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphLibrary.Objects;

namespace Simulator.Objects
{
    public class StopsIterator
    {
        public Stop CurrentStop;
        public Stop NextStop;
        public bool IsDone;

        private List<Stop> _stops;

        private int _index;

        public StopsIterator(List<Stop> stops)
        {
            _stops = stops;
            Reset();
        }

        
        public void Reset()
        {
            if (_stops != null)
            {
                _index = 0;
                if (_stops.Count > 1)
                {
                    CurrentStop = _stops[_index];
                    IsDone = false;
                    NextStop = _stops[_index+1];
                }
            } 
        }

        public bool Next()
        {
            if (_stops != null && _stops.Count >0)
            {
                _index++;
                if (_index +1 <= _stops.Count - 1)
                {
                    CurrentStop = _stops[_index];
                    NextStop = _stops[_index + 1];
                 

                    return true;
                }
                else
                {
                    CurrentStop = _stops[_index];
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
