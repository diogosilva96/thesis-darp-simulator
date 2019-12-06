using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;

namespace Simulator.Events.Handlers
{
    class VehicleDepartureHandler:EventHandler
    {
        public override void Handle(Event evt)
        {
            
            if (evt.Category == 1 && evt is VehicleStopEvent departEvent)
            {
                Log(evt);
                evt.AlreadyHandled = true;
                var departTime = departEvent.Time; //the time the vehicle departed on the previous depart event
                //DEPART EVENT HANDLE
                if (departEvent.Vehicle.TripIterator.Current != null && departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop == departEvent.Stop)
                {
                    _consoleLogger.Log(departEvent.Vehicle.ToString() + "DEPARTED from " + departEvent.Stop + " at " + TimeSpan.FromSeconds(departTime) + ".");
                    var tuple = Tuple.Create(departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop,
                        departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop);
                    var currentStopIndex = departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex;
                    departEvent.Vehicle.TripIterator.Current.StopsTimeWindows[currentStopIndex][1] = departTime;
                    Simulation.Context.ArcDictionary.TryGetValue(tuple, out var distance);
    
                    //vehicle start transversing to next stop
                    if (departEvent.Vehicle.TripIterator.Current?.StopsIterator != null && !departEvent.Vehicle.TripIterator.Current.StopsIterator.IsDone)
                    {
                        departEvent.Vehicle.IsIdle = false;
                        var t = TimeSpan.FromSeconds(departTime);
                        departEvent.Vehicle.TripIterator.Current.TotalDistanceTraveled =
                            departEvent.Vehicle.TripIterator.Current.TotalDistanceTraveled + distance;
                        _consoleLogger.Log(departEvent.Vehicle.ToString() + "started traveling to " +
                                          departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop + " (Distance: " + Math.Round(distance) + " meters) at " + t + ".");
                    }
                    //end of vehicle transverse to next stop

                }
                //END OF DEPART EVENT HANDLE

                //INSERTION (APPEND) OF VEHICLE NEXT STOP ARRIVE EVENT
                if (departEvent.Vehicle.TripIterator.Current != null)
                {
                    var currentStop = departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop.IsDummy ? Simulation.Context.Stops.Find(s => s.Id == departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop.Id) : departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop;//if it is a dummy stop gets the real object in TransportationNetwork stops list
                    if (departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop != null)
                    {
                        var nextStop = departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop.IsDummy
                            ? Simulation.Context.Stops.Find(s =>
                                s.Id == departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop.Id)
                            : departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop;
                        var stopTuple = Tuple.Create(currentStop, nextStop);
                        Simulation.Context.ArcDictionary.TryGetValue(stopTuple, out var distance);

                        if (distance == 0)
                        {
                            distance = DistanceCalculator.CalculateHaversineDistance(currentStop.Latitude,
                                currentStop.Longitude, nextStop.Latitude, nextStop.Longitude);
                        }

                        var travelTime = DistanceCalculator.DistanceToTravelTime(departEvent.Vehicle.Speed,
                            distance); //Gets the time it takes to travel from the currentStop to the nextStop
                        var nextArrivalTime = Convert.ToInt32(departTime + travelTime); //computes the arrival time for the next arrive event
                        departEvent.Vehicle.TripIterator.Current.StopsIterator
                            .Next(); //Moves the iterator to the next stop
                        var nextArriveEvent = EventGenerator.Instance().GenerateVehicleArriveEvent(departEvent.Vehicle, nextArrivalTime); //generates the arrive event
                        Simulation.AddEvent(nextArriveEvent);
                        //DEBUG!
                        if (departEvent.Vehicle.FlexibleRouting)
                        {
                            var scheduledArrivalTime =
                                departEvent.Vehicle.TripIterator.Current.ScheduledTimeWindows[
                                    departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentIndex][0];

                            _consoleLogger.Log("Event arrival time:" + nextArrivalTime +
                                              ", Scheduled arrival time:" + scheduledArrivalTime);
                        }
                    }

                    //END DEBUG
                }

            }
            //END OF INSERTION OF VEHICLE NEXT STOP ARRIVE EVENT--------------------------------------
            else
            {
                Successor?.Handle(evt);
            }
        }

        public VehicleDepartureHandler(Simulation simulation) : base(simulation)
        {
        }
    }
}
