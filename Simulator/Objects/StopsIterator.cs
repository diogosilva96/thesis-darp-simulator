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


        public Trip Trip
        {
            get => _trip;

            set
        {
                _trip = value;
                ResetStopsIterator();
            }
        }

        private int _numStopsIterated;

        private IEnumerator<Stop> _stopsEnum;

        private Trip _trip;

        public StopsIterator(Trip trip)
        {
            Trip = trip;
            ResetStopsIterator();
        }

        
        public void ResetStopsIterator()
        {
            if (Trip != null)
            {
                _stopsEnum = Trip.Stops.GetEnumerator();
                if (_stopsEnum != null && Trip.Stops.Count > 0)
                {
                    _stopsEnum.MoveNext();
                    CurrentStop = _stopsEnum.Current;
                    _numStopsIterated = 0;
                    NextStop = Trip.Stops[1];
                }
            } 
        }

        public void GoToNextStop()
        {
            if (Trip != null && Trip.Stops.Count >0)
            {
                if (_numStopsIterated < Trip.Stops.Count - 2)
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
                }
                else
                {
                    CurrentStop = NextStop;
                    NextStop = null;
                }
            }
        }
      
        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }
    }
}
