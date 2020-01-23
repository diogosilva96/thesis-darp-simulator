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

        public double TotalCustomersDelayed => TotalCustomers - TotalCustomerDeliveredOnTime;

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

                return (long)(totalEarlyTime / TotalCustomerDeliveredOnTime);
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

        public int TotalCustomerDeliveredOnTime
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

        public double CustomersDeliveredOnTimeRatio
        {
            get
            {
                var totalCustomersOnTime = 0;
                foreach (var vehicle in _vehicles)
                {
                    var numCustomers = vehicle.ServedCustomers.FindAll(c => c.DelayTime <= 0).Count;
                    totalCustomersOnTime += numCustomers;
                }

                double ratio = ((double)totalCustomersOnTime / (double)TotalCustomers);
                return ratio;
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
            MetricsContainer.AddMetric(nameof(TotalDistanceTraveledInMeters),TotalDistanceTraveledInMeters);
            MetricsContainer.AddMetric(nameof(TotalCustomers),TotalCustomers);
            MetricsContainer.AddMetric(nameof(TotalCustomerDeliveredOnTime),TotalCustomerDeliveredOnTime);
            MetricsContainer.AddMetric(nameof(CustomersDeliveredOnTimeRatio),CustomersDeliveredOnTimeRatio);
            MetricsContainer.AddMetric(nameof(TotalDynamicServedCustomers),TotalDynamicServedCustomers);
            MetricsContainer.AddMetric(nameof(TotalCustomerWaitTimesInSeconds),TotalCustomerWaitTimesInSeconds);
            MetricsContainer.AddMetric(nameof(TotalCustomerRideTimesInSeconds),  TotalCustomerRideTimesInSeconds);
            MetricsContainer.AddMetric(nameof(MaximumRouteDurationInSeconds), MaximumRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(MaximumRouteDistanceInMeters),MaximumRouteDistanceInMeters);
            MetricsContainer.AddMetric(nameof(MaximumCustomerRideTimeInSeconds),MaximumCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(MinimumRouteDurationInSeconds), MinimumRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(MinimumRouteDistanceInMeters), MinimumRouteDistanceInMeters);
            MetricsContainer.AddMetric(nameof(MinimumCustomerRideTimeInSeconds), MinimumCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageRouteDurationInSeconds),AverageRouteDurationInSeconds);
            MetricsContainer.AddMetric(nameof(AverageNumberRequests),  AverageNumberRequests);
            MetricsContainer.AddMetric(nameof(AverageNumberServicedRequests),  AverageNumberServicedRequests);
            MetricsContainer.AddMetric(nameof(AverageNumberDeniedRequests),  AverageNumberDeniedRequests);
            MetricsContainer.AddMetric(nameof(AverageServicedRequestsRatio),  AverageServicedRequestsRatio);
            MetricsContainer.AddMetric(nameof(AverageDeniedRequestsRatio),  AverageNumberDeniedRequests);
            MetricsContainer.AddMetric(nameof(AverageDistanceTraveledInMeters), AverageDistanceTraveledInMeters);
            MetricsContainer.AddMetric(nameof(AverageCustomerRideTimeInSeconds), AverageCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerWaitTimeInSeconds), AverageCustomerWaitTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerDelayTimeInSeconds),AverageCustomerDelayTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerEarlyTimeInSeconds),AverageCustomerEarlyTimeInSeconds);
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