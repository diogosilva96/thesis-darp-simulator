using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects
{
    public class ServiceStatistics // contains the metrics for a set of completed services
    {
        private readonly List<Trip> _completedTrips;


        public ServiceStatistics(List<Vehicle> routeVehicles)
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

        public int TotalCustomersServicedOnTime
        {
            get
            {
                var total = 0;
                foreach (var trip in _completedTrips)
                {
                    var numCustomers = trip.ServicedCustomers.FindAll(c => c.ServicedOnTime).Count;
                    total += numCustomers;
                }

                return total;
            }
        }

        public int TotalCustomers
        {
            get
            {
                var total = 0;
                foreach (var trip in _completedTrips)
                {
                    var numCustomers = trip.ServicedCustomers.Count;
                    total += numCustomers;
                }

                return total;
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

        public double TotalCustomerWaitTimes
        {
            get
            {
 
                    var waitTimes = 0;
                    foreach (var trip in _completedTrips)
                    {
                        foreach (var customer in trip.ServicedCustomers)
                        {
                            waitTimes += (int)customer.WaitTime;
                        }
                    }

                    return waitTimes;
            }
        }

        public double TotalCustomerRideTimes
        {
            get
            {

                var rideTimes = 0;
                foreach (var trip in _completedTrips)
                {
                    foreach (var customer in trip.ServicedCustomers)
                    {
                        rideTimes += (int)customer.RideTime;
                    }
                }

                return rideTimes;
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

            toPrintList.Add("Overall statistics:");
            toPrintList.Add("Total Distance Traveled: " + TotalDistanceTraveled + " meters.");
            toPrintList.Add("Total Customers Serviced: "+TotalCustomers);
            toPrintList.Add("Total Customers Serviced On Time: " + TotalCustomersServicedOnTime);
            toPrintList.Add("Total Customers Serviced with delay: "+(int)(TotalCustomers-TotalCustomersServicedOnTime));
            toPrintList.Add("Total Customer Wait Times: "+TimeSpan.FromSeconds(TotalCustomerWaitTimes).TotalMinutes+" minutes");
            toPrintList.Add("Total Customer Ride Times: "+TimeSpan.FromSeconds(TotalCustomerRideTimes).TotalMinutes+" minutes");
            toPrintList.Add("Longest route duration: " + TimeSpan.FromSeconds(LongestRouteDuration).TotalMinutes +
                            " minutes.");
            toPrintList.Add("Longest route distance: " + LongestRouteDistance + " meters.");
            toPrintList.Add(" ");
            toPrintList.Add("Statistics (averages per service):");
   
            toPrintList.Add("Average route duration:" + TimeSpan.FromSeconds(AverageRouteDuration).TotalMinutes +
                            " minutes.");
            toPrintList.Add("Average number of requests:" + AverageNumberRequests);
            toPrintList.Add("Average number of serviced requests: " + AverageNumberServicedRequests);
            toPrintList.Add("Average number of denied requests: " + AverageNumberDeniedRequests);
            toPrintList.Add("Average Serviced Requests ratio: " + AverageServicedRequestsRatio);
            toPrintList.Add("Average Denied Requests Ratio: " + AverageDeniedRequestsRatio);
            toPrintList.Add("Average customer ride time: " + TimeSpan.FromSeconds(AverageCustomerRideTime).TotalMinutes + " minutes.");
            toPrintList.Add("Average Distance traveled: " + AverageDistanceTraveled + " meters.");
            toPrintList.Add("Average number of requests per stop:" + AverageNumberRequestsPerStop);
            toPrintList.Add("Average Customer Wait Time: " + TimeSpan.FromSeconds(AverageCustomerWaitTime).TotalMinutes + " minutes.");
            toPrintList.Add("Average Customer Delay Time: "+TimeSpan.FromSeconds(AverageCustomerDelayTime).TotalMinutes+ " minutes.");
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
                printableList.Add("Total request served on time: "+trip.ServicedCustomers.FindAll(c=>c.ServicedOnTime).Count);
                try
                {
                    printableList.Add(
                        "Average Customer Ride time:" + trip.ServicedCustomers.Average(c => c.RideTime));
                }
                catch (Exception)
                {
                    printableList.Add("Average Customer ride time: NaN");
                }
                printableList.Add("Average Customer Wait time: "+TimeSpan.FromSeconds(trip.ServicedCustomers.Average(c=>c.WaitTime)).TotalMinutes+" minutes");
                printableList.Add("Average number of requests per stop:" +
                                  trip.TotalRequests / trip.StopsIterator.TotalStops);
                printableList.Add("Distance traveled: " + trip.TotalDistanceTraveled);
                printableList.Add("");
            }
            return printableList;
        }
    }
}