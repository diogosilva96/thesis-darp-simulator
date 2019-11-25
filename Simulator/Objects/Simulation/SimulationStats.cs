﻿using System;
using System.Collections.Generic;
using Simulator.Logger;
using Simulator.Objects.Data_Objects;

namespace Simulator.Objects.Simulation
{
    public class SimulationStats
    {

        private readonly Logger.Logger _consoleLogger;

        private readonly Simulation _simulation;

        public int TotalDynamicRequests;

        public int TotalServedDynamicRequests;

        public int TotalEventsHandled;

        public int ValidationsCounter;


        public SimulationStats(Simulation simulation)
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(consoleRecorder);
            _simulation = simulation;
            TotalDynamicRequests = 0;
            TotalServedDynamicRequests = 0;
            TotalEventsHandled = 0;
            ValidationsCounter = 0;
        }

        public void SaveStats(string path)
        {
            IRecorder fileRecorder =
                new FileRecorder(path);
            var myFileLogger = new Logger.Logger(fileRecorder);
            var statsPrintableList = GetStatsPrintableList();
            myFileLogger.Log(statsPrintableList);

        }

        public void PrintStats()
        {
            var statsPrintableList = GetStatsPrintableList();
            _consoleLogger.Log(statsPrintableList);
        }
        private List<string> GetStatsPrintableList()
        {
           
            var toPrintList = new List<string>();
            var alreadyHandledEvents = _simulation.Events.FindAll(e => e.AlreadyHandled);
            toPrintList.Add("Total number of events handled: " +
                            alreadyHandledEvents.Count + " out of " + _simulation.Events.Count + ".");
            if (alreadyHandledEvents.Count <= _simulation.Events.Count)
            {
                var notHandledEvents = _simulation.Events.FindAll(e => !e.AlreadyHandled);
                foreach (var notHandledEvent in notHandledEvents)
                {
                    _consoleLogger.Log((string) notHandledEvent.ToString());
                }
            }

            toPrintList.Add("Total Number of vehicles available: " + _simulation.VehicleFleet.Count + " vehicle(s).");
            toPrintList.Add("Total number of vehicles used: " +
                            _simulation.VehicleFleet.FindAll(v => v.TripIterator != null));
            toPrintList.Add("Average Dynamic requests per hour: " + TotalDynamicRequests /
                            TimeSpan.FromSeconds(_simulation.Params.TotalSimulationTime).TotalHours);
            toPrintList.Add("Total simulation time: " +
                            TimeSpan.FromSeconds(_simulation.Params.TotalSimulationTime).TotalHours + " hours.");
            toPrintList.Add("Total Dynamic Requests Served: " + TotalServedDynamicRequests + " out of " +
                            TotalDynamicRequests);
            toPrintList.Add("-------------------------------------");
            toPrintList.Add("|   Overall Simulation statistics   |");
            toPrintList.Add("-------------------------------------");
            foreach (var vehicle in _simulation.VehicleFleet.FindAll(v => v.FlexibleRouting))
            {
                if (vehicle.TripIterator != null && vehicle.TripIterator.Current != null)
                {
                    vehicle.PrintRoute(vehicle.TripIterator.Current.Stops,
                        vehicle.TripIterator.Current.ScheduledTimeWindows,
                        vehicle.TripIterator.Current.ServicedCustomers); //scheduled route
                    vehicle.PrintRoute(vehicle.TripIterator.Current.VisitedStops,
                        vehicle.TripIterator.Current.StopsTimeWindows,
                        vehicle.TripIterator.Current.ServicedCustomers); //simulation route
                }
            }

            foreach (var route in TransportationNetwork.Routes)
            {

                var allRouteVehicles = _simulation.VehicleFleet.FindAll(v =>
                    v.TripIterator != null && v.TripIterator.Current != null && v.TripIterator.Current.Route == route);

                if (allRouteVehicles.Count > 0)
                {

                    toPrintList.Add(route.ToString());
                    toPrintList.Add("Number of services:" + allRouteVehicles.Count);
                    foreach (var v in allRouteVehicles)
                    {
                        //For debug purposes---------------------------------------------------------------------------
                        if (v.ServiceTrips.Count != v.ServiceTrips.FindAll(s => s.IsDone).Count)
                        {
                            toPrintList.Add("ServiceTrips Completed:");
                            foreach (var service in v.ServiceTrips)
                                if (service.IsDone)
                                    toPrintList.Add(" - " + service + " - [" +
                                                    TimeSpan.FromSeconds(service.StartTime) + " - " +
                                                    TimeSpan.FromSeconds(service.EndTime) + "]");
                        }

                        if (v.Customers.Count > 0)
                        {
                            toPrintList.Add("Number of customers inside:" + v.Customers.Count);
                            foreach (var cust in v.Customers)
                                toPrintList.Add(
                                    cust + "Pickup:" + cust.PickupDelivery[0] + "Delivery:" + cust.PickupDelivery[1]);
                        }

                        //End of debug purposes---------------------------------------------------------------------------

                    }

                    var vehicleServiceStatistics = new VehicleServiceStatistics(allRouteVehicles);
                    var overallStatsPrintableList = vehicleServiceStatistics.GetOverallStatsPrintableList();
                    var perServiceStatsPrintableList = vehicleServiceStatistics.GetPerServiceStatsPrintableList();

                    foreach (var perServiceStats in perServiceStatsPrintableList)
                    {
                        toPrintList.Add(perServiceStats);
                    }

                    foreach (var overallStats in overallStatsPrintableList)
                    {
                        toPrintList.Add(overallStats);
                    }
                }
            }

            toPrintList.Add(" ");

            return toPrintList;
        }
    }


}