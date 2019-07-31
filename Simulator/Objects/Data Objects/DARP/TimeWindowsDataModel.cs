using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using Google.Protobuf.WellKnownTypes;

namespace Simulator.Objects.Data_Objects.DARP
{
    public class TimeWindowsDataModel:DataModel
    {
        public long[,] TimeWindows => GetTimeWindowsArray();

        private readonly int VehicleSpeed;

        public TimeWindowsDataModel(Stop depot,int vehicleSpeed) : base(depot)
        {
            VehicleSpeed = vehicleSpeed;
        }

        protected override void UpdateMatrix()
        {
            Matrix = new MatrixBuilder().GetTimeMatrix(Stops,VehicleSpeed);
        }

        public long[,] GetTimeWindowsArray()
        {
            long[,] timeWindows = new long[Stops.Count,2];
            for (int i = 0; i < timeWindows.GetLength(0); i++)
            {
                for (int j = 0; j < timeWindows.GetLength(1); j++)
                {
                    timeWindows[i, j] = long.MaxValue; //Initializes each cell of the array with the maximum value possible
                }
            }

            foreach (var customer in Customers)
            {
                var  customerMinTimeWindow = customer.DesiredTimeWindow[0];
                var arrayMinTimeWindow = timeWindows[Stops.IndexOf(customer.PickupDelivery[0]), 0]; //gets current min timewindow for the pickupstop
                timeWindows[Stops.IndexOf(customer.PickupDelivery[0]), 0] = Math.Min(arrayMinTimeWindow, customerMinTimeWindow);//the mintimewindow (lower bound) is the minimum value between the current  timewindow in the array and the customer timewindow

                var customerMaxTimeWindow = customer.DesiredTimeWindow[1];
                var arrayMaxTimeWindow = timeWindows[Stops.IndexOf(customer.PickupDelivery[1]), 1]; //gets curent max timewindow for the delivery stop
                timeWindows[Stops.IndexOf(customer.PickupDelivery[1]), 1] = Math.Min(arrayMaxTimeWindow, customerMaxTimeWindow);//the maxtimewindow (upper bound) is the minimum value between the current  timewindow in the array and the customer timewindow
            }

            return timeWindows;
        }

        public void PrintTimeWindows()
        {
            Console.WriteLine(this.ToString()+"Time Windows:");
            for (int i = 0; i < TimeWindows.GetLength(0); i++)
            {
                Console.Write(GetStop(i)+"{");
                for (int j = 0; j < TimeWindows.GetLength(1); j++)
                {
                    if (j == 0)
                    {
                        Console.Write(TimeWindows[i, j]+",");
                    }
                    else
                    {

                        Console.WriteLine(TimeWindows[i, j] + "}");
                    }
                }
            }
            
        }
    }
}
