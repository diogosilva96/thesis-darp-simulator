using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects
{
    class CustomerGenerator
    {
        public Customer GenerateRandomCustomer(List<Stop> stopsList,List<Stop> excludedStops,int requestTime,int[] pickupTimeWindow)
        {
            Customer customer = null;
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
            var pickupTime =
                rng.Next(pickupTimeWindow[0], pickupTimeWindow[1]); //the minimum pickup time is 0 minutes above the requestTime and maximum pickup is the end time of the simulation 
            var deliveryTime = rng.Next(pickupTime + 15 * 60, pickupTime + 45 * 60); //delivery time will be at minimum 15 minutes above the pickuptime and at max 45 minutes from the pickup time
            Stop[] pickupDelivery = new[] { pickup, delivery };
            long[] desiredTimeWindow = new[] { (long)pickupTime, (long)deliveryTime };
            customer = new Customer(pickupDelivery,desiredTimeWindow,requestTime);
            return customer;
        }
    }
}
