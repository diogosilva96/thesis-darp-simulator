using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Simulation_Data_Objects
{
    class CustomerFactory
    {
        private static CustomerFactory _instance;
        //Lock syncronization object for multithreading (might not be needed)
        private static object syncLock = new object();
        public static CustomerFactory Instance() //Singleton
        {
            // Support multithreaded apps through Double checked locking pattern which (once the instance exists) avoids locking each time the method is invoked

            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new CustomerFactory();
                    }
                }
            }
            return _instance;
        }
        public Customer CreateRandomCustomer(List<Stop> stopsList, List<Stop> excludedStops, int requestTime, int[] pickupTimeWindow,bool isDynamic, int vehicleAverageSpeed)
        {
            var rng = RandomNumberGenerator.Random;
            var pickup = stopsList[rng.Next(0, stopsList.Count)];
            while (excludedStops.Contains(pickup)) //if the pickup is the depot has to generate another pickup stop
            {
                pickup = stopsList[rng.Next(0, stopsList.Count)];
            }

            var delivery = pickup;

            while (delivery == pickup || excludedStops.Contains(delivery)) //if the delivery stop is equal to the pickup stop or depot stop, it needs to generate a different delivery stop
            {

                delivery = stopsList[rng.Next(0, stopsList.Count)];
            }


            var pickupTime = rng.Next(pickupTimeWindow[0], pickupTimeWindow[1]); //the minimum pickup time will be inside the interval [pickupTimeWindow[0],pickupTimeWindow[1]]
            var distance = Calculator.CalculateHaversineDistance(pickup.Latitude, pickup.Longitude, delivery.Latitude,
                delivery.Longitude);
            var travelTime = Calculator.DistanceToTravelTime(vehicleAverageSpeed, distance);
            var deliveryTime = rng.Next(pickupTime + (int)travelTime, pickupTime + (int)travelTime+ 30*60); //delivery time will be at minimum the pickuptime + travelTime and at max 30 minutes from pickup + travelTime

            //var deliveryTime = 24 * 60 * 60;
            if (pickupTime > deliveryTime)
            {
                throw new ArgumentException("Pickup time greater than deliveryTime");
            }

            var pickupDelivery = new[] { pickup, delivery };
            var desiredTimeWindow = new[] { (long)pickupTime, (long)deliveryTime };
            var customer = new Customer(pickupDelivery,desiredTimeWindow,requestTime,isDynamic);
            return customer;
        }
    }
}
