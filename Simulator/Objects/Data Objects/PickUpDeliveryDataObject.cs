using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects.Data_Objects
{
    public class PickUpDeliveryDataObject
    {
        public List<Customer> PickupDeliveryCustomers; //A list with all the customers for the pickupDeliveries

        public Stop Depot; //The depot stop

        public List<Stop> PickupDeliveryStops; // a list with all the distinct stops for the pickup and deliveries

        public PickUpDeliveryDataObject(Stop depot)
        {
            PickupDeliveryCustomers = new List<Customer>();
            Depot = depot;
            PickupDeliveryStops = new List<Stop>();
            PickupDeliveryStops.Add(depot);
        }

        public void AddCustomer(Stop pickup, Stop delivery)
        {
            if (pickup != null && delivery != null)
            {
                var customer = new Customer(pickup, delivery);
                PickupDeliveryCustomers.Add(customer);
                AddPickupDeliveryStops(customer);
            }
        }

        public void AddCustomer(Customer customer)
        {
            if (!PickupDeliveryCustomers.Contains(customer))
            {
                PickupDeliveryCustomers.Add(customer);
                AddPickupDeliveryStops(customer);
            }
        }

        private void AddPickupDeliveryStops(Customer customer)
        {
            var pickup = customer.PickupDelivery[0];
            if (!PickupDeliveryStops.Contains(pickup))
            {
                PickupDeliveryStops.Add(pickup);//if the pickup stop isn't in the list, add it to the stop list
            }

            var delivery = customer.PickupDelivery[1];
            if (!PickupDeliveryStops.Contains(delivery))
            {
                PickupDeliveryStops.Add(delivery);//if the delivery stop isn't in the list, add it to the stop list
            }
        }
        public int[][] GetPickupDeliveryIndexMatrix()//returns the pickupdelivery stop matrix using indexes (based on the pickupdeliverystop list) instead of stop id's
        {
            int[][] pickupsDeliveries = new int[PickupDeliveryCustomers.Count][];
            //Transforms the data from stop the list into index matrix list in order to use it in google Or tools
            int insertCounter = 0;
            foreach (var customer in PickupDeliveryCustomers)
            {
                var pickup = customer.PickupDelivery[0];
                var delivery = customer.PickupDelivery[1];
                var pickupDeliveryInd = new int[] { PickupDeliveryStops.IndexOf(pickup), PickupDeliveryStops.IndexOf(delivery) };
                pickupsDeliveries[insertCounter] = pickupDeliveryInd;
                insertCounter++;
            }

            return pickupsDeliveries;
        }
    }
}
