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

                var departTime = departEvent.Time; //the time the vehicle departed on the previous depart event
                departEvent.Vehicle.Depart(departEvent.Stop, departTime); //vehicle depart
                evt.AlreadyHandled = true;


                //INSERTION (APPEND) OF VEHICLE NEXT STOP ARRIVE EVENT
                if (departEvent.Vehicle.TripIterator.Current != null)
                {
                    var currentStop = departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop.IsDummy ? TransportationNetwork.Stops.Find(s => s.Id == departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop.Id) : departEvent.Vehicle.TripIterator.Current.StopsIterator.CurrentStop;//if it is a dummy stop gets the real object in TransportationNetwork stops list
                    if (departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop != null)
                    {
                        var nextStop = departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop.IsDummy
                            ? TransportationNetwork.Stops.Find(s =>
                                s.Id == departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop.Id)
                            : departEvent.Vehicle.TripIterator.Current.StopsIterator.NextStop;
                        var stopTuple = Tuple.Create(currentStop, nextStop);
                        TransportationNetwork.ArcDictionary.TryGetValue(stopTuple, out var distance);

                        if (distance == 0)
                        {
                            distance = DistanceCalculator.CalculateHaversineDistance(currentStop.Latitude,
                                currentStop.Longitude, nextStop.Latitude, nextStop.Longitude);
                        }

                        var travelTime =
                            DistanceCalculator.DistanceToTravelTime(departEvent.Vehicle.Speed,
                                distance); //Gets the time it takes to travel from the currentStop to the nextStop
                        var nextArrivalTime =
                            Convert.ToInt32(departTime +
                                            travelTime); //computes the arrival time for the next arrive event
                        departEvent.Vehicle.TripIterator.Current.StopsIterator
                            .Next(); //Moves the iterator to the next stop
                        var nextArriveEvent =
                            EventGenerator.Instance().GenerateVehicleArriveEvent(departEvent.Vehicle,
                                nextArrivalTime); //generates the arrive event
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
