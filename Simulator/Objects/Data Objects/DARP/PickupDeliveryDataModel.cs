﻿using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.DARP
{
    public class PickupDeliveryDataModel
    {
        public int DepotIndex => Stops.IndexOf(Depot);

        public int VehicleNumber => Vehicles.Count ; // number of vehicles

        public long[,] TimeMatrix; //time window matrix

        public readonly List<Stop> Stops; // a list with all the distinct stops for the pickup and deliveries

        public List<Vehicle> Vehicles;

        public readonly Stop Depot; //The depot stop

        public List<Customer> Customers; //A list with all the customers for the pickupDeliveries

        public long[][] InitialRoutes;

        public long[,] TimeWindows => GetTimeWindowsArray();

        public readonly int VehicleSpeed;

        private readonly int _dayInSeconds = 60*60*24; //24hours = 86400 seconds

        public int[][] PickupsDeliveries => GetPickupDeliveryIndexMatrix();

        public PickupDeliveryDataModel(Stop depot, int vehicleSpeed)
        {
            Customers = new List<Customer>();
            Vehicles = new List<Vehicle>();
            Depot = depot;
            Stops = new List<Stop>();
            Stops.Add(depot);
            InitialRoutes = new long[0][];
            VehicleSpeed = vehicleSpeed;
        }
        private int[][] GetPickupDeliveryIndexMatrix()//returns the pickupdelivery stop matrix using indexes (based on the pickupdeliverystop list) instead of stop id's
        {
            int[][] pickupsDeliveries = new int[Customers.Count][];
            //Transforms the data from stop the list into index matrix list in order to use it in google Or tools
            int insertCounter = 0;
            foreach (var customer in Customers)
            {
                var pickup = customer.PickupDelivery[0];
                var delivery = customer.PickupDelivery[1];
                var pickupDeliveryInd = new int[] { Stops.IndexOf(pickup), Stops.IndexOf(delivery) };
                pickupsDeliveries[insertCounter] = pickupDeliveryInd;
                insertCounter++;
            }

            return pickupsDeliveries;
        }

        public void AddInitialRoute(List<Stop> stopSequence)
        {
            // Initial route creation
            long[] initialRoute = new long[stopSequence.Count];
            int index = 0;
            foreach (var stop in stopSequence)
            {
                if (!Stops.Contains(stop)) Stops.Add(stop);
                initialRoute[index] = Stops.IndexOf(stop);
                index++;
            }

            Array.Resize(ref InitialRoutes, InitialRoutes.Length + 1);
            InitialRoutes[InitialRoutes.Length - 1] = initialRoute;
        }

        public void AddCustomer(Customer customer)
        {
            if (!Customers.Contains(customer))
            {
                Customers.Add(customer);
                AddPickupDeliveryStops(customer);
            }
        }


        public void AddVehicle(Vehicle vehicle)
        {
            if (!Vehicles.Contains(vehicle) && vehicle.FlexibleRouting)
            {
                Vehicles.Add(vehicle);
            }
        }

        private void AddPickupDeliveryStops(Customer customer)
        {
            bool valueChanged = false;
            for (int i = 0; i < customer.PickupDelivery.Length; i++)
            {
                var pickupDelivery = customer.PickupDelivery[i];
                if (Stops.Contains(pickupDelivery)) continue;
                Stops.Add(pickupDelivery);//if the pickup stop isn't in the list, add it to the stop list
                valueChanged = true;
            }
            if (valueChanged)
            {
                UpdateTimeMatrix();
            }

        }

        private void UpdateTimeMatrix()
        {
            TimeMatrix = new MatrixBuilder().GetTimeMatrix(Stops, VehicleSpeed);
        }

        
        public Stop IndexToStop(int index)
        {
            return Stops[index];
        }

        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void PrintMatrix()
        {
            string matrixType;
            Console.WriteLine(ToString() + "TimeMatrix:");
            var counter = 0;
            foreach (var val in TimeMatrix)
                if (counter == TimeMatrix.GetLength(1) - 1)
                {
                    counter = 0;
                    Console.WriteLine(val + " ");
                }
                else
                {
                    Console.Write(val + " ");
                    counter++;
                }
        }

        public void PrintInitialRoutes()
        {
            if (InitialRoutes != null)
            {
                int count = 0;
                Console.WriteLine("Initial Routes:");
                foreach (var initialRoute in InitialRoutes)
                {
                    Console.Write("Vehicle " + count + ":");
                    for (int i = 0; i < initialRoute.Length; i++)
                    {
                        if (i != initialRoute.Length - 1)
                        {
                            Console.Write(IndexToStop((int)initialRoute[i]).Id + " -> ");
                        }
                        else
                        {
                            Console.WriteLine(IndexToStop((int)initialRoute[i]).Id);
                        }
                    }
                    count++;
                }
            }
        }

        public void PrintPickupDeliveries()
        {
            Console.WriteLine(this.ToString() + "Pickups and Deliveries with Time Windows (total: " + Customers.Count + "):");
            foreach (var customer in Customers)
                customer.PrintPickupDelivery();
        }

        private long[,] GetInitialTimeWindowsArray(long[,] timeWindows)
        {
            //Loop to initialize each cell of the timewindow array at the maximum minutes value (1440minutes - 24 hours)
            for (int i = 0; i < timeWindows.GetLength(0); i++)
            {
                timeWindows[i, 0] = 0; //lower bound of the timewindow is initialized with 0
                timeWindows[i, 1] = _dayInSeconds; //Upper bound of the timewindow with a max long value

            }

            return timeWindows;
        }

        public long[,] GetTimeWindowsArray()
        {
            long[,] timeWindows = new long[Stops.Count, 2];
            timeWindows = GetInitialTimeWindowsArray(timeWindows);

            foreach (var customer in Customers)
            {
                //LOWER BOUND (MINIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                var customerMinTimeWindow = customer.DesiredTimeWindow[0]; //customer min time window in seconds
                var arrayMinTimeWindow = timeWindows[Stops.IndexOf(customer.PickupDelivery[0]), 0]; //gets current min timewindow for the pickupstop in minutes
                //If there are multiple min time window values for a given stop, the minimum time window will be the maximum timewindow between all those values
                //because the vehicle must arrive that stop at most, at the greatest min time window value, in order to satisfy all requests
                timeWindows[Stops.IndexOf(customer.PickupDelivery[0]), 0] =
                        Math.Max((long)arrayMinTimeWindow, (long)customerMinTimeWindow); //the mintimewindow (lower bound) is the maximum value between the current timewindow in the array and the current customer timewindow


                //UPPER BOUND (MAXIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                var customerMaxTimeWindow = customer.DesiredTimeWindow[1]; //customer max time window in seconds
                var arrayMaxTimeWindow = timeWindows[Stops.IndexOf(customer.PickupDelivery[1]), 1]; //gets curent max timewindow for the delivery stop in minutes
                //If there are multiple max timewindows for a given stop, the maximum time window will be the minimum between all those values
                //because the vehicle must arrive that stop at most, at the lowest max time window value, in order to satisfy all the requests
                timeWindows[Stops.IndexOf(customer.PickupDelivery[1]), 1] = Math.Min((long)arrayMaxTimeWindow, (long)customerMaxTimeWindow);//the maxtimewindow (upper bound) is the minimum value between the current  timewindow in the array and the current customer timewindow



            }

            // timeWindows = ClearTimeWindowsArray(timeWindows);

            return timeWindows;
        }

        private long[,] ClearTimeWindowsArray(long[,] timeWindows)
        {
            //loop to remove the previously inserted maxvalues
            for (int i = 0; i < timeWindows.GetLength(0); i++)
            {
                var minTime = timeWindows[i, 0];
                var maxTime = timeWindows[i, 1];
                if (minTime == _dayInSeconds || maxTime == _dayInSeconds)
                {
                    if (minTime == _dayInSeconds)
                    {
                        timeWindows[i, 0] = 0; //removes the maxvalue
                    }
                    if (maxTime == _dayInSeconds)
                    {
                        minTime = timeWindows[i, 0];
                        timeWindows[i, 1] = minTime + 5;//min time plus 5 mins
                    }
                }

            }
            return timeWindows;
        }
        public void PrintTimeWindows() //For debug purposes
        {
            Console.WriteLine(this.ToString() + "Time Windows:");
            for (int i = 0; i < TimeWindows.GetLength(0); i++)
            {
                Console.Write(IndexToStop(i) + "{");
                for (int j = 0; j < TimeWindows.GetLength(1); j++)
                {
                    if (j == 0)
                    {
                        Console.Write(TimeWindows[i, j] + ",");
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
