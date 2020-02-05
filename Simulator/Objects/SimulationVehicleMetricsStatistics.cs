using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects
{
    public class SimulationVehicleMetricsStatistics // contains the metrics for a set of completed services
    {
     

        private readonly List<Vehicle> _vehiclesUsed;

        private Simulation.Simulation _simulation;

        public MetricsContainer MetricsContainer;

        public SimulationVehicleMetricsStatistics(Simulation.Simulation simulation)
        {
            _simulation = simulation;
            _vehiclesUsed = _simulation.Context.VehicleFleet.FindAll(v => v.TripIterator?.Current != null &&v.ServedCustomers.Count >0);
            //debug;
            List<Customer> allServedCustomers = new List<Customer>();
            foreach (var v in _vehiclesUsed)
            {
                foreach (var cust in v.ServedCustomers)
                {
                    if (allServedCustomers.FindAll(c => c.Id == cust.Id).Count > 0)
                    {
                        Console.WriteLine("A");
                    }
                    allServedCustomers.Add(cust);
                }
            }
            //end of debug
            MetricsContainer = new MetricsContainer();
            ComputeOverallMetrics();
        }

        public double AverageRouteDurationInSeconds
        {
            get
            {
                return _vehiclesUsed.Average(s => s.RouteDuration);
            }
        }

        public int TotalSimulationTime => _simulation.Stats.TotalSimulationTime;
        public int NumberAvailableVehicles => _simulation.Context.VehicleFleet.Count;

        public double NumberServedDynamicRequests => _simulation.Stats.TotalServedDynamicRequests;

        public double NumberDynamicRequests => _simulation.Stats.TotalDynamicRequests;

        public double DynamicRequestsServedRatio => (double)((double)_simulation.Stats.TotalServedDynamicRequests / (double)_simulation.Stats.TotalDynamicRequests);
        public double NumberVehiclesUsed
        {
            get
            {
                return _simulation.Context.VehicleFleet.FindAll(v => v.TripIterator != null).Count;

            }
        }

        public double NumberServedRequests => _simulation.Stats.TotalServedCustomers;

        public double AverageNumberRequestsPerVehicleUsed
        {
            get
            {
                return (double) (_vehiclesUsed.Sum(s => s.TotalRequests) /
                                 NumberVehiclesUsed);
            }
        }

        public double AverageNumberServicedRequestsPerVehicleUsed
        {
            get { return (double)_vehiclesUsed.Sum(v =>v.TotalServedRequests)/NumberVehiclesUsed; }
        }

        public double AverageNumberDeniedRequestsPerVehicleUsed
        {
            get
            {
                try
                {
                    return _vehiclesUsed.Sum(v => v.TotalDeniedRequests)/NumberVehiclesUsed;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public double TotalCustomersDelayed => TotalCustomersServed - TotalCustomerDeliveredOnTime;

        public double TotalDynamicServedCustomers
        {
            get
            {
                return _vehiclesUsed.Sum(v => v.ServedCustomers.FindAll(c => c.IsDynamic).Count); 
            }
        }

        public double AverageCustomerRideTimeInSeconds
        {
            get
            {
                try
                {
                    var totalRideTime = (double)_vehiclesUsed.Sum(v => v.ServedCustomers.Sum(c => c.RideTime));
                    return (double)((double)totalRideTime / (double)TotalCustomersServed);
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
                    return _vehiclesUsed.Max(v => v.ServedCustomers.Max(c => c.RideTime));
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
                    return _vehiclesUsed.Min(v => v.ServedCustomers.Max(c => c.RideTime));
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
                    
                    var totalWaitTime =(double) _vehiclesUsed.Sum(v=>v.ServedCustomers.Sum(c => c.WaitTime));                
                    return (double) ((double)totalWaitTime / (double) TotalCustomersServed);                   
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
                var totalCustomerDelay =
                    _vehiclesUsed.Sum(v => v.ServedCustomers.FindAll(c => c.DelayTime > 0).Sum(cust => cust.DelayTime));
                return (long)(totalCustomerDelay / TotalCustomersDelayed);
            }
        }

        public double AverageCustomerEarlyTimeInSeconds
        {
            get
            {
                var totalEarlyTime = _vehiclesUsed.Sum(v => v.ServedCustomers.FindAll(c => c.DelayTime <= 0).Sum(cust => cust.DelayTime));
                
                return (long)(totalEarlyTime / TotalCustomerDeliveredOnTime);
            }
        }
        public double AverageDistanceTraveledInMeters
        {
            get
            {
                try
                {
                    return _vehiclesUsed.Average(s => s.TotalDistanceTraveled);
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
                return _vehiclesUsed.Sum(v => v.ServedCustomers.FindAll(c => c.DelayTime <= 0).Count);
                
            }
        }

        public int TotalCustomersDeliveredDelayed
        {
            get { return _vehiclesUsed.Sum(v => v.ServedCustomers.FindAll(c => c.DelayTime > 0).Count); }
        }

        public double CustomersDeliveredOnTimeRatio => (double)((double)TotalCustomerDeliveredOnTime / (double)TotalCustomersServed);

        public int TotalCustomersServed
        {
            get
            {
                return _vehiclesUsed.Sum(v => v.ServedCustomers.Count); ;
            }
        }

        public double MaximumRouteDurationInSeconds
        {
            get
            {
                try
                {
                    return _vehiclesUsed.Max(s => s.RouteDuration);
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
                    return _vehiclesUsed.Min(s => s.RouteDuration);
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
                    return _vehiclesUsed.Min(s => s.TotalDistanceTraveled);
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
                    return _vehiclesUsed.Max(s => s.TotalDistanceTraveled);
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
                    return _vehiclesUsed.Sum(s => s.TotalDistanceTraveled);
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
                    foreach (var vehicle in _vehiclesUsed)
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
                foreach (var vehicle in _vehiclesUsed)
                {
                    foreach (var customer in vehicle.ServedCustomers)
                    {
                        rideTimes += (int)customer.RideTime;
                    }
                }

                return rideTimes;
            }
        }

        private void ComputeOverallMetrics()
        {
            MetricsContainer.AddMetric(nameof(NumberAvailableVehicles),NumberAvailableVehicles);
            MetricsContainer.AddMetric(nameof(NumberVehiclesUsed),NumberVehiclesUsed);
            MetricsContainer.AddMetric(nameof(NumberDynamicRequests),NumberDynamicRequests);
            MetricsContainer.AddMetric(nameof(NumberServedRequests),NumberServedRequests);
            MetricsContainer.AddMetric(nameof(NumberServedDynamicRequests),NumberServedDynamicRequests);
            MetricsContainer.AddMetric(nameof(TotalSimulationTime),TotalSimulationTime);
            MetricsContainer.AddMetric(nameof(DynamicRequestsServedRatio), DynamicRequestsServedRatio);
            MetricsContainer.AddMetric(nameof(TotalDistanceTraveledInMeters),TotalDistanceTraveledInMeters);
            MetricsContainer.AddMetric(nameof(TotalCustomersServed),TotalCustomersServed);
            MetricsContainer.AddMetric(nameof(TotalCustomerDeliveredOnTime),TotalCustomerDeliveredOnTime);
            MetricsContainer.AddMetric(nameof(TotalCustomersDeliveredDelayed),TotalCustomersDeliveredDelayed);
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
            MetricsContainer.AddMetric(nameof(AverageNumberRequestsPerVehicleUsed),  AverageNumberRequestsPerVehicleUsed);
            MetricsContainer.AddMetric(nameof(AverageNumberServicedRequestsPerVehicleUsed),  AverageNumberServicedRequestsPerVehicleUsed);
            MetricsContainer.AddMetric(nameof(AverageNumberDeniedRequestsPerVehicleUsed),  AverageNumberDeniedRequestsPerVehicleUsed);           
            MetricsContainer.AddMetric(nameof(AverageDistanceTraveledInMeters), AverageDistanceTraveledInMeters);
            MetricsContainer.AddMetric(nameof(AverageCustomerRideTimeInSeconds), AverageCustomerRideTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerWaitTimeInSeconds), AverageCustomerWaitTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerDelayTimeInSeconds),AverageCustomerDelayTimeInSeconds);
            MetricsContainer.AddMetric(nameof(AverageCustomerEarlyTimeInSeconds),AverageCustomerEarlyTimeInSeconds);
        }
        public List<string> GetOverallStatsPrintableList()
        {
            var toPrintList = new List<string>();

            toPrintList.Add("Overall statistics for "+_vehiclesUsed.Count+" vehicles:");
            foreach (var metricValue in MetricsContainer.GetMetricsDictionary())
            {
             toPrintList.Add(MetricsContainer.MetricToString(metricValue));   
            }
            return toPrintList;
        }
    }
}