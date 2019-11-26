using System;
using System.Collections.Generic;
using System.Threading;

namespace Simulator.Objects.Data_Objects.Simulation_Objects
{
    public class Customer
    {
        private static int nextId;
        public int Id { get; internal set; }
        public long RideTime => RealTimeWindow[1]-RealTimeWindow[0];

        public Stop[] PickupDelivery;

        public long[] RealTimeWindow;//in seconds

        public long[] DesiredTimeWindow; //in seconds

        public bool IsInVehicle;

        public bool AlreadyServed;
        
        public int RequestTime;//request time in seconds

        public long WaitTime => RealTimeWindow[0] - DesiredTimeWindow[0];

        public long DelayTime => RealTimeWindow[1] - DesiredTimeWindow[1] > 0 ? RealTimeWindow[1] - DesiredTimeWindow[1] : 0;

        public Customer(Stop[] pickupDelivery, int requestTime)
        {
            PickupDelivery = pickupDelivery;
            RequestTime = requestTime;
            Init();
         
        }

        public Customer(List<Stop> stopsList, List<Stop> excludedStops, int requestTime, int[] pickupTimeWindow)
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
            var deliveryTime = rng.Next(pickupTime + 15 * 60, pickupTime + 45 * 60); //delivery time will be at minimum 15 minutes above the pickuptime and at max 45 minutes from the pickup time
            if (pickupTime > deliveryTime)
            {
                throw new ArgumentException("Pickup time greater than deliveryTime");
            }

            PickupDelivery= new[] { pickup, delivery };
            DesiredTimeWindow = new[] { (long)pickupTime, (long)deliveryTime };
            Init();
        }

        public Customer(Stop[] pickupDelivery, long[] desiredTimeWindow, int requestTime)
        {
            PickupDelivery = pickupDelivery;
            DesiredTimeWindow = desiredTimeWindow;
            Init();
        }

        public void Init()
        {
            Id = Interlocked.Increment(ref nextId);
            IsInVehicle = false;
            AlreadyServed = false;
            RealTimeWindow = new long[2];
        }
        public override string ToString()
        {
            return "Customer "+Id+" ";
        }

        public void PrintPickupDelivery()
        {
            string stringToBePrinted = this.ToString() + " - PickupDelivery: [" + PickupDelivery[0] + " -> " + PickupDelivery[1] + "]";
            if (DesiredTimeWindow != null)
            {
                stringToBePrinted = stringToBePrinted + " - TimeWindows (in seconds): {"+DesiredTimeWindow[0]+","+DesiredTimeWindow[1]+"}";
            }
            Console.WriteLine(stringToBePrinted);
        }

    }
}
