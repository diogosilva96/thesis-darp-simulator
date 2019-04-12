using System;
using System.Collections.Generic;
using System.Text;
using GraphLibrary.Objects;

namespace Simulator.Objects
{
    public class Router
    {
        public Stop CurrentStop;
        public Stop NextStop;


        public Trip Trip
        {
            get => _trip;
            set
            {
                _trip = value;
                _stopsEnum = _trip.Stops.GetEnumerator();
                Init();
            }
        }

        private Trip _trip;

        private int _numStopsIterated;
        private IEnumerator<Stop> _stopsEnum;

        public Router()
        {
            _trip = null;
            Init();
        }

        public void Init()
        {
            if (Trip != null && _stopsEnum != null && Trip.Stops.Count >0)
            {
                _stopsEnum.MoveNext();
                CurrentStop = _stopsEnum.Current;
                NextStop = Trip.Stops[1];
            }
            _numStopsIterated = 0;
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

        public void Reset()
        {
            if (_stopsEnum == null || Trip == null) return;
            _stopsEnum.Reset();
            Init();
        }
    }
}
