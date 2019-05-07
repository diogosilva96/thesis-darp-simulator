using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphLibrary.Objects;
using Simulator.Objects.Data_Objects;

namespace Simulator.Objects
{
    public class ServicesMetricContainer//Has metrics for a set of completed services
    {
        private List<Service> _completedServices;

        private Vehicle _vehicle;

        public double AverageRouteDuration
        {
            get { return _completedServices.Average(s => s.RouteDuration); }
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
            get { return _completedServices.Average(s => s.TotalDeniedRequests); }
        }

        public double AverageCustomerRideTime
        {
            get
            {
                return (_completedServices.Average(s => s.ServicedCustomers.Average(c => c.RideTime)));
            }
        }

        public double AverageDistanceTraveled
        {
            get
            {
                return (_completedServices.Average(s => s.TotalDistanceTraveled));
            }
        }

        public double LongestRouteDuration
        {
            get { return _completedServices.Max(s => s.RouteDuration); }
        }

        public double TotalDistanceTraveled
        {
            get { return _completedServices.Sum(s => s.TotalDistanceTraveled); }
        }

        public double AverageServicedRequestsRatio => AverageNumberServicedRequests / AverageNumberRequests;

        public double AverageDeniedRequestsRatio => 1 - AverageServicedRequestsRatio;

        public ServicesMetricContainer(Vehicle vehicle)
        {
            _vehicle = vehicle;
            _completedServices = vehicle.Services.FindAll(s=>s.IsDone);
        }


        public List<string> GetPrintableList()
        {
            List<string> printableList = new List<string>();
            printableList.Add("Total number of completed services:" + _completedServices.Count + " out of " + _vehicle.Services.Count);
            if (_vehicle.Services.Count > 0)
            {
                
                printableList.Add("Service Trips:");
                List<Trip> serviceRoutes = new List<Trip>();
                foreach (var service in _vehicle.Services)
                {
                    if (!serviceRoutes.Contains(service.Trip))
                    {
                        serviceRoutes.Add(service.Trip);
                        var completedServices = _vehicle.Services.FindAll(s => s.Trip == service.Trip && s.IsDone);
                        printableList.Add(" - " + service.Trip.ToString() + " - Route Length:" + Math.Round(_vehicle.Services.Find(s => s.Trip == service.Trip).TotalDistanceTraveled) + " meters, Number of services completed:" +
                                        completedServices.Count);
                    }
                }
            }
            printableList.Add("Total Distance Traveled: " + TotalDistanceTraveled + " meters.");
            printableList.Add(" ");
            printableList.Add("Metrics (per service):");
            printableList.Add("Average route duration:" + TimeSpan.FromSeconds(AverageRouteDuration).TotalMinutes + " minutes.");
            printableList.Add("Average number of requests:" + AverageNumberRequests);
            printableList.Add("Average number of serviced requests: " + AverageNumberServicedRequests);
            printableList.Add("Average number of denied requests: " + AverageNumberDeniedRequests);
            printableList.Add("Average Serviced Requests ratio: "+AverageServicedRequestsRatio);
            printableList.Add("Average Denied Requests Ratio: "+AverageDeniedRequestsRatio);
            printableList.Add("Average customer ride time: "+AverageCustomerRideTime+" seconds.");
            printableList.Add("Average Distance traveled: "+AverageDistanceTraveled);
            printableList.Add("Longest route duration: "+TimeSpan.FromSeconds(LongestRouteDuration).TotalMinutes+" minutes.");
            return printableList;
        }

        public List<string> GetEachServicePrintableList()
        {
            List<string> printableList = new List<string>();
            foreach (var service in _completedServices)
            {
                printableList.Add(service.ToString()+" ["+TimeSpan.FromSeconds(service.StartTime).ToString()+" - "+TimeSpan.FromSeconds(service.EndTime)+"]");
                printableList.Add("Route duration: "+service.RouteDuration);
                printableList.Add("Total requests: "+service.TotalRequests);
                printableList.Add("Serviced requests: "+service.TotalServicedRequests);
                printableList.Add("Denied requests: "+service.TotalDeniedRequests);
                printableList.Add("Average Customer Ride time:"+service.ServicedCustomers.Average(c=>c.RideTime));
                printableList.Add("Distance traveled: "+service.TotalDistanceTraveled);
                printableList.Add("");
                
            }
            return printableList;
        }

    }
}
