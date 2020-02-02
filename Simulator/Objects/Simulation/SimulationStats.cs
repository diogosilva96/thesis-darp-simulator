using System;
using System.Collections.Generic;
using System.Linq;
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

        public MetricsContainer SimulationMetricsContainer;

        public int TotalSimulationTime => _simulation.Events.Max(e => e.Time)-_simulation.Events.Min(e=>e.Time);


        public SimulationStats(Simulation simulation)
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(consoleRecorder);
            _simulation = simulation;
            TotalDynamicRequests = 0;
            TotalServedDynamicRequests = 0;
            TotalEventsHandled = 0;
            ValidationsCounter = 0;
            SimulationMetricsContainer = new MetricsContainer();
        }

        public void SaveStats(string path)
        {
            IRecorder fileRecorder =
                new FileRecorder(path);
            var myFileLogger = new Logger.Logger(fileRecorder);
            var statsPrintableList = GetStatsPrintableList();
            myFileLogger.Log(statsPrintableList);

        }

        public string GetSimulationStatsCSVMessage()
        {
            if (SimulationMetricsContainer.GetMetricsDictionary().Count == 0)
            {
                BuildAllSimulationMetrics();
            }
            string message = _simulation.Params.Seed.ToString();
            if (SimulationMetricsContainer.GetMetricsDictionary().Count > 0)
            {
                foreach (var metricDict in SimulationMetricsContainer.GetMetricsDictionary())
                {
                    message += ", " + metricDict.Value;
                }
            }

            return message;
        }

        public string GetSimulationStatsCSVFormatMessage()
        {
            if (SimulationMetricsContainer.GetMetricsDictionary().Count == 0)
            {
                BuildAllSimulationMetrics();
            }
            string message = "Seed";
            if (SimulationMetricsContainer.GetMetricsDictionary().Count > 0)
            {
                foreach (var metricDict in SimulationMetricsContainer.GetMetricsDictionary())
                {
                    message += ", "+metricDict.Key;
                }
            }

            return message;
        }

        public void PrintStats()
        {
            var statsPrintableList = GetStatsPrintableList();
            _consoleLogger.Log(statsPrintableList);
        }

        public void BuildAllSimulationMetrics()
        {

            SimulationMetricsContainer.AddMetric("NumberAvailableVehicles", _simulation.Context.VehicleFleet.Count);
            SimulationMetricsContainer.AddMetric("NumberVehiclesUsed", _simulation.Context.VehicleFleet.FindAll(v => v.TripIterator != null).Count);
            SimulationMetricsContainer.AddMetric("NumberDynamicRequestsPerHour",TotalDynamicRequests);
            SimulationMetricsContainer.AddMetric("TotalSimulationTime",TotalSimulationTime);
            SimulationMetricsContainer.AddMetric("TotalDynamicRequestsServed",TotalServedDynamicRequests);
            SimulationMetricsContainer.AddMetric("TotalDynamicRequests",TotalDynamicRequests);
            var percentServedRequests = (int)(((double)TotalServedDynamicRequests / TotalDynamicRequests) * 100);
            SimulationMetricsContainer.AddMetric("PercentageServedDynamicRequests",percentServedRequests);
            var allRouteVehicles = _simulation.Context.VehicleFleet.FindAll(v =>
                v.TripIterator != null && v.TripIterator.Current != null);
            var vehicleServiceStatistics = new VehicleStatistics(allRouteVehicles);
            var allVehicleMetricsDictionary = vehicleServiceStatistics.MetricsContainer.GetMetricsDictionary();
            foreach (var allVehicleMetrics in allVehicleMetricsDictionary)
            {
                SimulationMetricsContainer.AddMetric(allVehicleMetrics.Key,allVehicleMetrics.Value);
            }
                

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

            toPrintList.Add("Total Number of vehicles available: " + _simulation.Context.VehicleFleet.Count + " vehicle(s).");
            toPrintList.Add("Total number of vehicles used: " +
                            _simulation.Context.VehicleFleet.FindAll(v => v.TripIterator != null).Count);
            toPrintList.Add("Average Dynamic requests per hour: " + TotalDynamicRequests /
                            TimeSpan.FromSeconds(_simulation.Params.TotalSimulationTime).TotalHours);
            toPrintList.Add("Total simulated time: " +
                            TimeSpan.FromSeconds(TotalSimulationTime).TotalHours + " hours.");
            toPrintList.Add("Total Dynamic Requests Served: " + TotalServedDynamicRequests + " out of " +
                            TotalDynamicRequests);
            var percentServedRequests = (int)(((double)TotalServedDynamicRequests / TotalDynamicRequests) * 100);
            toPrintList.Add("Percentage of served Dynamic requests: "+ percentServedRequests+"%");
            toPrintList.Add("-------------------------------------");
            toPrintList.Add("|   Overall Simulation statistics   |");
            toPrintList.Add("-------------------------------------");
            foreach (var vehicle in _simulation.Context.VehicleFleet.FindAll(v => v.FlexibleRouting))
            {

                vehicle.PrintRoute();
            }

            foreach (var route in _simulation.Context.Routes)
            {

                var allRouteVehicles = _simulation.Context.VehicleFleet.FindAll(v =>
                    v.TripIterator != null && v.TripIterator.Current != null && v.TripIterator.Current.Route == route);

                if (allRouteVehicles.Count > 0)
                {

                    toPrintList.Add(route.ToString());
                    toPrintList.Add("Number of Routes completed:" + allRouteVehicles.Count);
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

                    var vehicleServiceStatistics = new VehicleStatistics(allRouteVehicles);
                    var overallStatsPrintableList = vehicleServiceStatistics.GetOverallStatsPrintableList();
                    //var perServiceStatsPrintableList = vehicleServiceStatistics.GetPerServiceStatsPrintableList();               
                    //foreach (var perServiceStats in perServiceStatsPrintableList)
                    //{
                    //    toPrintList.Add(perServiceStats);
                    //}

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
