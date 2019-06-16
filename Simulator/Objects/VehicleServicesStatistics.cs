using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simulator.Objects.Data_Objects;

namespace Simulator.Objects
{
    public class VehicleServicesStatistics// contains the metrics for a set of completed services
    {
        private readonly List<Service> _completedServices;

        private readonly Vehicle _vehicle;

        public double AverageRouteDuration
        {
            get
            {
                return _completedServices.Average(s => s.RouteDuration);
            }
        }

        public double AverageNumberRequests
        {
            get { return _completedServices.Average(s => s.TotalRequests); }
        }

        public double AverageNumberServicedRequests
        {
            get { return _completedServices.Average(s => s.TotalServicedRequests); }
        }

        public double AverageNumberDeniedRequests
        {
            get
            {
                try
                {
                    return _completedServices.Average(s => s.TotalDeniedRequests);
                }
                catch (Exception e)
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
                    return (_completedServices.Average(s => s.ServicedCustomers.Average(c => c.RideTime)));
                }
                catch (Exception e)
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
                    return (_completedServices.Average(s => s.TotalDistanceTraveled));
                }
                catch (Exception e)
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
                    return _completedServices.Max(s => s.RouteDuration);
                }
                catch (Exception e)
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
                    return _completedServices.Sum(s => s.TotalDistanceTraveled);
                }
                catch (Exception e)
                {
                    return 0;
                }
            }
        }

        public double AverageServicedRequestsRatio => AverageNumberServicedRequests / AverageNumberRequests;

        public double AverageDeniedRequestsRatio => 1 - AverageServicedRequestsRatio;

        public VehicleServicesStatistics(Vehicle vehicle)
        {
            _vehicle = vehicle;
            _completedServices = vehicle.Services.FindAll(s=>s.IsDone);
        }

        public double AverageNumberRequestsPerStop
        {
            get
            {
                return _completedServices.Sum(s => s.TotalRequests) /
                       _completedServices.Sum(s => s.StopsIterator.TotalStops);
            }
        }


        public List<string> GetOverallStatsPrintableList()
        {
            List<string> toPrintList = new List<string>();
            toPrintList.Add("Total number of completed services:" + _completedServices.Count + " out of " + _vehicle.Services.Count);
            if (_vehicle.Services.Count > 0)
            {
                
                toPrintList.Add("Service Trips:");
                List<Trip> serviceRoutes = new List<Trip>();
                foreach (var service in _vehicle.Services)
                {
                    if (!serviceRoutes.Contains(service.Trip))
                    {
                        serviceRoutes.Add(service.Trip);
                        var completedServices = _vehicle.Services.FindAll(s => s.Trip == service.Trip && s.IsDone);
                        toPrintList.Add(" - " + service.Trip.ToString() + " - Route Length:" + Math.Round(_vehicle.Services.Find(s => s.Trip == service.Trip).TotalDistanceTraveled) + " meters, Number of Stops: "+_vehicle.Services.Find(s=>s.Trip == service.Trip).StopsIterator.TotalStops+", Number of services completed:" +
                                        completedServices.Count);
                    }
                }
            }
            toPrintList.Add("Total Distance Traveled: " + TotalDistanceTraveled + " meters.");
            toPrintList.Add(" ");
            toPrintList.Add("Statistics (per service):");
            toPrintList.Add("Average route duration:" + TimeSpan.FromSeconds(AverageRouteDuration).TotalMinutes + " minutes.");
            toPrintList.Add("Average number of requests:" + AverageNumberRequests);
            toPrintList.Add("Average number of serviced requests: " + AverageNumberServicedRequests);
            toPrintList.Add("Average number of denied requests: " + AverageNumberDeniedRequests);
            toPrintList.Add("Average Serviced Requests ratio: "+AverageServicedRequestsRatio);
            toPrintList.Add("Average Denied Requests Ratio: "+AverageDeniedRequestsRatio);
            toPrintList.Add("Average customer ride time: "+AverageCustomerRideTime+" seconds.");
            toPrintList.Add("Average Distance traveled: "+AverageDistanceTraveled);
            toPrintList.Add("Average number of requests per stop:"+AverageNumberRequestsPerStop);
            toPrintList.Add("Longest route duration: "+TimeSpan.FromSeconds(LongestRouteDuration).TotalMinutes+" minutes.");
            return toPrintList;
        }

        public List<string> GetPerServiceStatsPrintableList()
        {
            List<string> printableList = new List<string>();
            foreach (var service in _completedServices)
            {
                printableList.Add(service.ToString()+"Service Id: "+service.Id+" ["+TimeSpan.FromSeconds(service.StartTime).ToString()+" - "+TimeSpan.FromSeconds(service.EndTime)+"]");
                printableList.Add("Route duration: "+service.RouteDuration);
                printableList.Add("Total stops:"+service.StopsIterator.TotalStops);
                printableList.Add("Total requests: "+service.TotalRequests);
                printableList.Add("Serviced requests: "+service.TotalServicedRequests);
                printableList.Add("Denied requests: "+service.TotalDeniedRequests);
                printableList.Add("Average Customer Ride time:"+service.ServicedCustomers.Average(c=>c.RideTime));
                printableList.Add("Average number requests per stop:"+service.TotalRequests/service.StopsIterator.TotalStops);
                printableList.Add("Distance traveled: "+service.TotalDistanceTraveled);
                printableList.Add("");  
            }
            return printableList;
        }

    }
}
