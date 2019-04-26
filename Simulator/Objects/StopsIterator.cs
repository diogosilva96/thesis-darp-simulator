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

        private int _numStopsIterated;

        private IEnumerator<Stop> _stopsEnum;


        public StopsIterator(List<Stop> stops)
        {
            _stops = stops;
            Reset();
            IsDone = false;
        }

        
        public void Reset()
        {
            if (_stops != null)
            {
                _stopsEnum = _stops.GetEnumerator();
                if (_stopsEnum != null && _stops.Count > 0)
                {
                    _stopsEnum.MoveNext();
                    CurrentStop = _stopsEnum.Current;
                    _numStopsIterated = 0;
                    IsDone = false;
                    NextStop = _stops[1];
                }
            } 
        }

        public bool Next()
        {
            if (_stops != null && _stops.Count >0)
            {
                if (_numStopsIterated < _stops.Count - 2)
                {
                    CurrentStop = NextStop;
                 
                    while (_stopsEnum.Current != CurrentStop)
                    {
                        _stopsEnum.MoveNext();
                    }

                    if (_stopsEnum.MoveNext())
                    {
                        NextStop = _stopsEnum.Current;
                        _numStopsIterated++;
                    }

                    return true;
                }
                else
                {
                    CurrentStop = NextStop;
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
