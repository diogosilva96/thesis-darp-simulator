using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects
{
    public class RouteServicesStatistics // contains the metrics for a set of completed services
    {
        private readonly List<Trip> _completedTrips;


        public RouteServicesStatistics(List<Vehicle> routeVehicles)
        {
            _completedTrips = new List<Trip>();
            foreach (var vehicle in routeVehicles)
            {
                if (vehicle.TripIterator.Current.IsDone)
                {
                    _completedTrips.Add(vehicle.TripIterator.Current);
                }
            }
        }

        public double AverageRouteDuration
        {
            get { return _completedTrips.Average(s => s.RouteDuration); }
        }

        public double AverageNumberRequests
        {
            get { return _completedTrips.Average(s => s.TotalRequests); }
        }

        public double AverageNumberServicedRequests
        {
            get { return _completedTrips.Average(s => s.TotalServicedRequests); }
        }

        public double AverageNumberDeniedRequests
        {
            get
            {
                try
                {
                    return _completedTrips.Average(s => s.TotalDeniedRequests);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double AverageCustomerRideTime
        {
            get
            {
                try
                {
                    return _completedTrips.Average(s => s.ServicedCustomers.Average(c => c.RideTime));
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double AverageCustomerWaitTime
        {
            get
            {
                try
                {
                    return _completedTrips.Average(s => s.ServicedCustomers.Average(c => c.WaitTime));
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double AverageCustomerDelayTime
        {
            get
            {
                try
                {
                    return _completedTrips.Average(s => s.ServicedCustomers.Average(c => c.DelayTime));
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
        public double AverageDistanceTraveled
        {
            get
            {
                try
                {
                    return _completedTrips.Average(s => s.TotalDistanceTraveled);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double LongestRouteDuration
        {
            get
            {
                try
                {
                    return _completedTrips.Max(s => s.RouteDuration);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double LongestRouteDistance
        {
            get
            {
                try
                {
                    return _completedTrips.Max(s => s.TotalDistanceTraveled);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double TotalDistanceTraveled
        {
            get
            {
                try
                {
                    return _completedTrips.Sum(s => s.TotalDistanceTraveled);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double AverageServicedRequestsRatio => AverageNumberServicedRequests / AverageNumberRequests;

        public double AverageDeniedRequestsRatio => 1 - AverageServicedRequestsRatio;

        public double AverageNumberRequestsPerStop
        {
            get
            {
                return (double) decimal.Divide(_completedTrips.Sum(s => s.TotalRequests),
                    _completedTrips.Sum(s => s.StopsIterator.TotalStops));
                ;
            }
        }


        public List<string> GetOverallStatsPrintableList()
        {
            var toPrintList = new List<string>();
            //toPrintList.Add("Total number of completed services:" + _completedTrips.Count);
            //if (_completedTrips.Count > 0)
            //{
            //    toPrintList.Add("Trip ServiceTrips:");
            //    var serviceTrips = new List<Trip>();
            //    foreach (var trip in _completedTrips)
            //    {
            //        if (!serviceTrips.Contains(trip))
            //        {
            //            serviceTrips.Add(trip);
            //            var completedTripServices =
            //                _completedTrips.FindAll(t => t.Headsign == trip.Headsign && t.IsDone);
            //            toPrintList.Add(" - " + trip + " - Route Length:" +
            //                            Math.Round(_completedTrips.Find(t => t == trip)
            //                                .TotalDistanceTraveled) + " meters, Number of Stops: " +
            //                            _completedTrips.Find(t => t == trip).StopsIterator.TotalStops +
            //                            ", Number of trips completed:" +
            //                            completedTripServices.Count);
            //        }
            //    }
            //}

            toPrintList.Add("Total Distance Traveled: " + TotalDistanceTraveled + " meters.");
            toPrintList.Add(" ");
            toPrintList.Add("Statistics (Average per service):");
            toPrintList.Add("Average route duration:" + TimeSpan.FromSeconds(AverageRouteDuration).TotalMinutes +
                            " minutes.");
            toPrintList.Add("Average number of requests:" + AverageNumberRequests);
            toPrintList.Add("Average number of serviced requests: " + AverageNumberServicedRequests);
            toPrintList.Add("Average number of denied requests: " + AverageNumberDeniedRequests);
            toPrintList.Add("Average Serviced Requests ratio: " + AverageServicedRequestsRatio);
            toPrintList.Add("Average Denied Requests Ratio: " + AverageDeniedRequestsRatio);
            toPrintList.Add("Average customer ride time: " + AverageCustomerRideTime + " seconds.");
            toPrintList.Add("Average Distance traveled: " + AverageDistanceTraveled + " meters.");
            toPrintList.Add("Average number of requests per stop:" + AverageNumberRequestsPerStop);
            toPrintList.Add("Average Customer Wait Time: " + AverageCustomerWaitTime + " seconds.");
            toPrintList.Add("Average Customer Delay Time: "+AverageCustomerDelayTime+ " seconds.");
            toPrintList.Add("Longest route duration: " + TimeSpan.FromSeconds(LongestRouteDuration).TotalMinutes +
                            " minutes.");
            toPrintList.Add("Longest route distance: "+ LongestRouteDistance+" meters.");
            return toPrintList;
        }

        public List<string> GetPerServiceStatsPrintableList()
        {
            var printableList = new List<string>();
            foreach (var trip in _completedTrips)
            {
                printableList.Add("Route:" + trip.Route.Name + " , Route Id:"+trip.Route.Id+" ServiceStartTime: " + trip.StartTime + " [" +
                                  TimeSpan.FromSeconds(trip.StartTime) + " - " +
                                  TimeSpan.FromSeconds(trip.EndTime) + "]" + " Trip Id:" + trip.Id);
                printableList.Add("Route duration: " + trip.RouteDuration);
                printableList.Add("Total stops:" + trip.StopsIterator.TotalStops);
                printableList.Add("Total requests: " + trip.TotalRequests);
                printableList.Add("Serviced requests: " + trip.TotalServicedRequests);
                printableList.Add("Denied requests: " + trip.TotalDeniedRequests);
                try
                {
                    printableList.Add(
                        "Average Customer Ride time:" + trip.ServicedCustomers.Average(c => c.RideTime));
                }
                catch (Exception)
                {
                    printableList.Add("Average Customer ride time: NaN");
                }
                printableList.Add("Average Customer Wait time: "+trip.ServicedCustomers.Average(c=>c.WaitTime));
                printableList.Add("Average number of requests per stop:" +
                                  trip.TotalRequests / trip.StopsIterator.TotalStops);
                printableList.Add("Distance traveled: " + trip.TotalDistanceTraveled);
                printableList.Add("");
            }
            return printableList;
        }
    }
}