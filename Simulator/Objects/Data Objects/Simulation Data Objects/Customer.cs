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

        public bool ServicedOnTime => RealTimeWindow[1] <= DesiredTimeWindow[1];
        
        public int RequestTime;//request time in seconds

        public long WaitTime => RealTimeWindow[0] - DesiredTimeWindow[0];

        public long DelayTime => RealTimeWindow[1] - DesiredTimeWindow[1] > 0 ? RealTimeWindow[1] - DesiredTimeWindow[1] : 0;

        public Customer(Stop[] pickupDelivery, int requestTime)
        {
            PickupDelivery = pickupDelivery;
            RequestTime = requestTime;
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
