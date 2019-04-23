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

        public List<Trip> ServicedTrips;

        public int[,] StartEndTimeWindows { get; set; }

        public int StartTime { get;set; }

        public int EndTime { get; set; }


        public List<Trip> Trips { get;internal set; }

        public Trip CurrentTrip
        {
            get => _currentTrip;

            set
        {
                _currentTrip = value;
                _currentTripIndex = Trips.FindIndex(t => t == _currentTrip);
                ResetStopsIterator();
            }
        }

        private int _currentTripIndex;

        private int _numStopsIterated;

        private IEnumerator<Stop> _stopsEnum;

        private Trip _currentTrip;

        public bool AddTrip(Trip trip)
        {
            if (!Trips.Contains(trip))
            {
                Trips.Add(trip);
                return true;
            }

            return false;
        }
        public Router()
        {
            Trips = new List<Trip>();
            StartEndTimeWindows = new int[,]{};
            ServicedTrips = new List<Trip>();
        }

        
        public void ResetStopsIterator()
        {
            if (CurrentTrip != null)
            {
                _stopsEnum = CurrentTrip.Stops.GetEnumerator();
                if (_stopsEnum != null && CurrentTrip.Stops.Count > 0)
                {
                    _stopsEnum.MoveNext();
                    CurrentStop = _stopsEnum.Current;
                    _numStopsIterated = 0;
                    NextStop = CurrentTrip.Stops[1];
                }
            } 
        }

        public bool NextTrip()
        {

            if (CurrentTrip != null && _currentTripIndex == Trips.Count - 1)
            {
                Console.WriteLine(this + " no more trips");
                return false;
            }
            
            if (CurrentTrip == null)
            {
                InitCurrentTrip();
            }
            if (CurrentTrip != null && _currentTripIndex + 1 < Trips.Count)
            {
                CurrentTrip = Trips[_currentTripIndex + 1];
            }

            return true;


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

        public bool InitCurrentTrip()
        {
            if (Trips.Count > 0)
            {
                CurrentTrip = Trips[0];
                return true;
            }

            return false;
        }
      
        public override string ToString()
        {
            return "["+GetType().Name+"] ";
        }
    }
}
