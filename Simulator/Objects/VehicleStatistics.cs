using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects
{
    public class VehicleStatistics // contains the metrics for a set of completed services
    {
     

        private readonly List<Vehicle> _vehicles;

        public MetricsContainer MetricsContainer;

        public VehicleStatistics(List<Vehicle> routeVehicles)
        {
  
            _vehicles = routeVehicles;
            MetricsContainer = new MetricsContainer();
            ComputeOverallMetrics();
        }

        public double AverageRouteDurationInSeconds
        {
            get
            {
                return _vehicles.Average(s => s.RouteDuration);
            }
        }

        public double AverageNumberRequests
        {
            get
            {
                return _vehicles.Average(s => s.TotalRequests);
            }
        }

        public double AverageNumberServicedRequests
        {
            get { return _vehicles.Average(v =>v.TotalServedRequests); }
        }

        public double AverageNumberDeniedRequests
        {
            get
            {
                try
                {
                    return _vehicles.Average(v => v.TotalDeniedRequests);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double TotalCustomersDelayed => TotalCustomers - TotalCustomerServicedEarlierOrOnTime;

        public double TotalDynamicServedCustomers
        {
            get
            {

                var total = 0;
                foreach (var vehicle in _vehicles)
                {
                    var numDynamicCustomers = vehicle.ServedCustomers.FindAll(c =>c.IsDynamic).Count; //find all dynamic customers
                    total += numDynamicCustomers;
                }
                return total;
            }
        }

        public double AverageCustomerRideTimeInSeconds
        {
            get
            {
                try
                {
                    return _vehicles.Average(v => v.ServedCustomers.Average(c => c.RideTime));
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double MaximumCustomerRideTimeInSeconds
        {
            get
            {

                try
                {
                    return _vehicles.Max(v => v.ServedCustomers.Max(c => c.RideTime));
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double MinimumCustomerRideTimeInSeconds
        {
            get
            {

                try
                {
                    return _vehicles.Min(v => v.ServedCustomers.Max(c => c.RideTime));
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
                    return _vehicles.Average(v => v.ServedCustomers.Average(c => c.WaitTime));
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
                foreach (var vehicle in _vehicles)
                {
                    var delayedCustomers =vehicle.ServedCustomers.FindAll(c => c.DelayTime > 0);
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
                foreach (var vehicle in  _vehicles)
                {
                    var earlyCustomers =vehicle.ServedCustomers.FindAll(c => c.DelayTime < 0);
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
                    return _vehicles.Average(s => s.TotalDistanceTraveled);
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
                foreach (var vehicle in _vehicles)
                {
                    var numCustomers = vehicle.ServedCustomers.FindAll(c => c.DelayTime <= 0).Count;
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
                foreach (var vehicle in _vehicles)
                {
                    var numCustomers = vehicle.ServedCustomers.Count;
                    total += numCustomers;
                }

                return total;
            }
        }

        public double MaximumRouteDurationInSeconds
        {
            get
            {
                try
                {
                    return _vehicles.Max(s => s.RouteDuration);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double MinimumRouteDurationInSeconds
        {
            get
            {
                try
                {
                    return _vehicles.Min(s => s.RouteDuration);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double MinimumRouteDistanceInMeters
        {
            get
            {
                try
                {
                    return _vehicles.Min(s => s.TotalDistanceTraveled);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double MaximumRouteDistanceInMeters
        {
            get
            {
                try
                {
                    return _vehicles.Max(s => s.TotalDistanceTraveled);
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
                    return _vehicles.Sum(s => s.TotalDistanceTraveled);
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
                    foreach (var vehicle in _vehicles)
                    {
                        foreach (var customer in vehicle.ServedCustomers)
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
                foreach (var vehicle in _vehicles)
                {
                    foreach (var customer in vehicle.ServedCustomers)
                    {
                        rideTimes += (int)customer.RideTime;
                    }
                }

                return rideTimes;
            }
        }

        public double AverageServicedRequestsRatio => AverageNumberServicedRequests / AverageNumberRequests;

        public double AverageDeniedRequestsRatio => 1 - AverageServicedRequestsRatio;

        private void ComputeOverallMetrics()
        {
            MetricsContainer.AddMetric(nameof(TotalDistanceTraveledInMeters),(int)TotalDistanceTraveledInMeters);
            MetricsContainer.AddMetric(nameof(TotalCustomers),TotalCustomers);
            MetricsContainer.AddMetric(nameof(TotalCustomerServicedEarlierOrOnTime),TotalCustomerServicedEarlierOrOnTime);
            MetricsContainer.AddMetric(nameof(TotalCustomersDelayed), (int)(TotalCustomersDelayed));
            MetricsContainer.AddMetric(nameof(TotalDynamicServedCustomers),(int)TotalDynamicServedCustomers);
            MetricsContainer.AddMetric(nameof(TotalCustomerWaitTimesInSeconds),(int)TotalCustomerWaitTimesInSeconds);
            MetricsContainer.AddMetric(nameof(TotalCustomerRideTimesInSeconds), (int) TotalCustomerRideTimesInSeconds);
            MetricsContainer.AddMetric(nameof(MaximumRouteDurationInSeconds), (int)MaximumRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(MaximumRouteDistanceInMeters),(int)MaximumRouteDistanceInMeters);
            MetricsContainer.AddMetric(nameof(MaximumCustomerRideTimeInSeconds),(int)MaximumCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(MinimumRouteDurationInSeconds), (int)MinimumRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(MinimumRouteDistanceInMeters), (int)MinimumRouteDistanceInMeters);
            MetricsContainer.AddMetric(nameof(MinimumCustomerRideTimeInSeconds), (int)MinimumCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageRouteDurationInSeconds),(int)AverageRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(AverageNumberRequests), (int) AverageNumberRequests);
            MetricsContainer.AddMetric(nameof(AverageNumberServicedRequests), (int) AverageNumberServicedRequests);
            MetricsContainer.AddMetric(nameof(AverageNumberDeniedRequests), (int) AverageNumberDeniedRequests);
            MetricsContainer.AddMetric(nameof(AverageServicedRequestsRatio), (int) AverageServicedRequestsRatio);
            MetricsContainer.AddMetric(nameof(AverageDeniedRequestsRatio), (int) AverageNumberDeniedRequests);
            MetricsContainer.AddMetric(nameof(AverageDistanceTraveledInMeters), (int)AverageDistanceTraveledInMeters);
            MetricsContainer.AddMetric(nameof(AverageCustomerRideTimeInSeconds), (int)AverageCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerWaitTimeInSeconds), (int)AverageCustomerWaitTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerDelayTimeInSeconds),(int)AverageCustomerDelayTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerEarlyTimeInSeconds),(int)AverageCustomerEarlyTimeInSeconds);
        }
        public List<string> GetOverallStatsPrintableList()
        {
            var toPrintList = new List<string>();

            toPrintList.Add("Overall statistics for "+_vehicles.Count+" vehicles:");
            foreach (var metricValue in MetricsContainer.GetMetricsDictionary())
            {
             toPrintList.Add(MetricsContainer.MetricToString(metricValue));   
            }
            return toPrintList;
        }
    }
}