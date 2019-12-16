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

        public MetricsContainer MetricsContainer;

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
            MetricsContainer = new MetricsContainer();
            ComputeOverallMetrics();
        }

        public double AverageRouteDurationInSeconds
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

        public double TotalCustomersDelayed => TotalCustomers - TotalCustomerServicedEarlierOrOnTime;
        public double AverageCustomerRideTimeInSeconds
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

        public double LongestCustomerRideTimeInSeconds
        {
            get
            {

                try
                {
                    return _completedTrips.Max(s => s.ServicedCustomers.Max(c => c.RideTime));
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double AverageCustomerWaitTimeInSeconds
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

        public double AverageCustomerDelayTimeInSeconds
        {
            get
            {
                long totalDelay = 0;
                foreach (var trip in _completedTrips)
                {
                    var delayedCustomers = trip.ServicedCustomers.FindAll(c => c.DelayTime > 0);
                    foreach (var delayedCustomer in delayedCustomers)
                    {
                        totalDelay += delayedCustomer.DelayTime;
                    }
                }

                return (long)(totalDelay / TotalCustomersDelayed);
            }
        }

        public double AverageCustomerEarlyTimeInSeconds
        {
            get
            {
                long totalEarlyTime = 0;
                foreach (var trip in _completedTrips)
                {
                    var earlyCustomers = trip.ServicedCustomers.FindAll(c => c.DelayTime < 0);
                    foreach (var earlyCustomer in earlyCustomers)
                    {
                        totalEarlyTime += earlyCustomer.DelayTime;
                    }
                }

                return (long)(totalEarlyTime / TotalCustomerServicedEarlierOrOnTime);
            }
        }
        public double AverageDistanceTraveledInMeters
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

        public int TotalCustomerServicedEarlierOrOnTime
        {
            get
            {
                var total = 0;
                foreach (var trip in _completedTrips)
                {
                    var numCustomers = trip.ServicedCustomers.FindAll(c => c.DelayTime <= 0).Count;
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
        public double LongestRouteDurationInSeconds
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

        public double LongestRouteDistanceInMeters
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

        public double TotalDistanceTraveledInMeters
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

        public double TotalCustomerWaitTimesInSeconds
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

        public double TotalCustomerRideTimesInSeconds
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

        private void ComputeOverallMetrics()
        {
            MetricsContainer.AddMetric(nameof(TotalDistanceTraveledInMeters),(int)TotalDistanceTraveledInMeters);
            MetricsContainer.AddMetric(nameof(TotalCustomers),TotalCustomers);
            MetricsContainer.AddMetric(nameof(TotalCustomerServicedEarlierOrOnTime),TotalCustomerServicedEarlierOrOnTime);
            MetricsContainer.AddMetric(nameof(TotalCustomersDelayed), (int)(TotalCustomersDelayed));
            MetricsContainer.AddMetric(nameof(TotalCustomerWaitTimesInSeconds),(int)TotalCustomerWaitTimesInSeconds);
            MetricsContainer.AddMetric(nameof(TotalCustomerRideTimesInSeconds), (int) TotalCustomerRideTimesInSeconds);
            MetricsContainer.AddMetric(nameof(LongestRouteDurationInSeconds), (int)LongestRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(LongestRouteDistanceInMeters),(int)LongestRouteDistanceInMeters);
            MetricsContainer.AddMetric(nameof(LongestCustomerRideTimeInSeconds),(int)LongestCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageRouteDurationInSeconds),(int)AverageRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(AverageNumberRequests), (int) AverageNumberRequests);
            MetricsContainer.AddMetric(nameof(AverageNumberServicedRequests), (int) AverageNumberServicedRequests);
            MetricsContainer.AddMetric(nameof(AverageNumberDeniedRequests), (int) AverageNumberDeniedRequests);
            MetricsContainer.AddMetric(nameof(AverageServicedRequestsRatio), (int) AverageServicedRequestsRatio);
            MetricsContainer.AddMetric(nameof(AverageDeniedRequestsRatio), (int) AverageNumberDeniedRequests);
            MetricsContainer.AddMetric(nameof(AverageDistanceTraveledInMeters), (int) AverageDistanceTraveledInMeters);
            MetricsContainer.AddMetric(nameof(AverageNumberRequestsPerStop), (int) AverageNumberRequestsPerStop);
            MetricsContainer.AddMetric(nameof(AverageCustomerRideTimeInSeconds), (int)AverageCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerWaitTimeInSeconds), (int) AverageCustomerWaitTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerDelayTimeInSeconds),(int)AverageCustomerDelayTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerEarlyTimeInSeconds),(int)AverageCustomerEarlyTimeInSeconds);
        }
        public List<string> GetOverallStatsPrintableList()
        {
            var toPrintList = new List<string>();

            toPrintList.Add("Overall statistics for "+_completedTrips.Count+" trips:");
            foreach (var metricValue in MetricsContainer.GetMetricsDictionary())
            {
             toPrintList.Add(MetricsContainer.MetricToString(metricValue));   
            }
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
                printableList.Add("Total request served on time: "+trip.ServicedCustomers.FindAll(c=>c.DelayTime<=0).Count);
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