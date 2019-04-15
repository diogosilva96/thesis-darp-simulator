using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphLibrary.Objects;

namespace Simulator.Objects
{
    public class Router
    {
        public Stop CurrentStop;
        public Stop NextStop;


        public int[] StartEndTimeWindow { get; set; }

        private List<Trip> Trips { get; set; }

        public Trip CurrentTrip
        {
            get {
                if (_currentTripIndex < Trips.Count)
                {
                   return Trips[_currentTripIndex];
                }

                return null;
            }

        set
            {
                _currentTrip = value;
                _currentTripIndex = Trips.FindIndex(t => t == _currentTrip);
                _stopsEnum = _currentTrip.Stops.GetEnumerator();
                Init();
            }
        }

        private Trip _currentTrip;

        private int _currentTripIndex;

        private int _numStopsIterated;

        private IEnumerator<Stop> _stopsEnum;

        public bool AddTrip(Trip trip)
        {
            if (!Trips.Contains(trip))
            {
                Trips.Add(trip);
                Trips = Trips.OrderBy(t => t.StartTime).ToList();
                CurrentTrip = Trips[0];
                return true;
            }

            return false;
        }
        public Router()
        {
            Trips = new List<Trip>();
            StartEndTimeWindow = new int[2];
            Init();
        }

        
        public void Init()
        {
            if (CurrentTrip != null && _stopsEnum != null && CurrentTrip.Stops.Count >0)
            {
                _stopsEnum.MoveNext();
                CurrentStop = _stopsEnum.Current;
                NextStop = CurrentTrip.Stops[1];
            }
            _numStopsIterated = 0;
        }

        public void NextTrip()
        {
            if (CurrentTrip != null && _currentTripIndex <= Trips.Count-1)
            {
                CurrentTrip = Trips[_currentTripIndex + 1];
            }
        }
        public void GoToNextStop()
        {
            if (CurrentTrip != null && CurrentTrip.Stops.Count >0)
            {
                if (_numStopsIterated < CurrentTrip.Stops.Count - 2)
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

        public void Reset()
        {
            if (_stopsEnum == null || CurrentTrip == null) return;
            _stopsEnum.Reset();
            Init();
        }
    }
}
