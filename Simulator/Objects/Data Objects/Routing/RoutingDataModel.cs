﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using MathNet.Numerics.LinearAlgebra.Solvers;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class RoutingDataModel //Routing DataModel with all the data necessary to be used by the routing Solver
    {
        private static int nextId;
        public int Id { get; internal set; }

        public long[] VehicleCapacities; //Array that contains vehicle capacities, Array size: [Vehicles.Count]

        public long[,] TravelTimes; //time matrix that contains the travel time to each stop, Matrix size: [Stops.Count,Stops.Count]

        public int[] Starts; //Array that contains the index of the start depots for each vehicle, Array size: [Vehicles.Count]

        public int[] Ends; //Array that contains the index of the end depots for each vehicle, Array size:[Vehicles.Count]

        public DataModelIndexManager IndexManager; //Index manager with the data vehicle,customer and stops objects, responsible for giving the required indexed data, and also enables to convert those indices in its respective object

        public long[,] TimeWindows; //TimeWindow pairs that must be satisfied for each stop index, Matrix size: [stops.Count,2]

        public long[] Demands; //Demand at each stop index, Array size:[Stops.Count]

        public int[][] PickupsDeliveries; //pickupDelivery indices pairs for each customer (not in vehicle), matrix size: [CustomersNotInVehicle.Count,2]

        public int MaxCustomerRideTime; //maximum time a customer can spend in a vehicle (in seconds)

        public int MaxAllowedDeliveryDelayTime; //maximum delay in the timeWindows, to be used by RoutingSolver to find feasible solutions when the current timeWindowUpperBound isnt feasible

        public int[] CustomersVehicle; // array that contains each customers vehicle index (for the customers that are already inside a veihcle), each cell has the vehicle index and each array index indicates the customer

        public long[] CustomersRideTimes; //matrix that contains the customer ride times, for the already in vehicle customers
        public bool ForceCumulToZero
        {
            get
            {
                if (Starts != null && TimeWindows != null)
                {
                    var countStartTimeZero = 0;
                    foreach (var starts in Starts)
                    {
                       
                        if (TimeWindows[starts, 0] == 0)
                        {
                            countStartTimeZero++;
                        }
                    }

                    if (countStartTimeZero == Starts.Length)
                    {
                        return true;
                    }
                    
                }

                return false;
            }
        }

        public RoutingDataModel(DataModelIndexManager indexManger,int maxCustomerRideTime,int maxAllowedUpperBound) //if different end and start depot
        {
                Initialize(indexManger,maxCustomerRideTime,maxAllowedUpperBound);
        }

        public void Initialize(DataModelIndexManager indexManger, int maxCustomerRideTime, int maxAllowedUpperBound)
        {
            Id = Interlocked.Increment(ref nextId);
            IndexManager = indexManger;
            MaxAllowedDeliveryDelayTime = maxAllowedUpperBound;
            MaxCustomerRideTime = maxCustomerRideTime;
            Starts = IndexManager.GetVehicleStarts();
            Ends = IndexManager.GetVehicleEnds();
            VehicleCapacities = IndexManager.GetVehicleCapacities();
            PickupsDeliveries = IndexManager.GetPickupDeliveries();
            TravelTimes = IndexManager.GetTimeMatrix(true); //calculates timeMatrix using Haversine distance formula
            TimeWindows = IndexManager.GetTimeWindows();
            Demands = IndexManager.GetDemands();
            CustomersVehicle = IndexManager.GetCustomersVehicle();
            CustomersRideTimes = IndexManager.GetCustomersRideTime();
        }

        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void PrintTravelTimes()
        {

            Console.WriteLine(ToString() + "TravelTimes:");
            var counter = 0;
            foreach (var val in TravelTimes)
                if (counter == TravelTimes.GetLength(1) - 1)
                {
                    counter = 0;
                    Console.WriteLine(val + " ");
                }
                else
                {
                    Console.Write(val + " ");
                    counter++;
                }
            Console.WriteLine("---------------------------------------------------------------");
        }

        public string GetCSVSettingsMessage()
        {
            string splitter = ",";
            string message = Id+splitter+IndexManager.Customers.Count + splitter + IndexManager.Vehicles.Count + splitter +
                             TimeSpan.FromSeconds(MaxCustomerRideTime).TotalMinutes + splitter + TimeSpan.FromSeconds(MaxAllowedDeliveryDelayTime).TotalMinutes+splitter+RandomNumberGenerator.Seed;
            return message;

        }
        public List<string> GetSettingsPrintableList()
        {
            List<string> stringList = new List<string>();
           stringList.Add("-------------------------------");
           stringList.Add("|     Data Model Settings     |");
           stringList.Add("-------------------------------");
           stringList.Add("Number of vehicles: "+IndexManager.Vehicles.Count);
           stringList.Add("Number of customers: "+IndexManager.Customers.Count);
           stringList.Add("Maximum Customer Ride Time: "+TimeSpan.FromSeconds(MaxCustomerRideTime).TotalMinutes +" minutes");
           stringList.Add("Maximum Allowed Upper Bound Time: "+TimeSpan.FromSeconds(MaxAllowedDeliveryDelayTime).TotalMinutes + " minutes");
           return stringList;
        }

        public void PrintPickupDeliveries()
        {
            Console.WriteLine(this.ToString()+ " PickupDeliveries: ");

            for (int i = 0; i < PickupsDeliveries.GetLength(0);i++)
            {
                Console.WriteLine(i +" - ("+PickupsDeliveries[i][0]+", "+PickupsDeliveries[i][1]+")");
            }
        }

        public void PrintDemands()
        {
            Console.WriteLine(this.ToString() + "Demands: ");
            for (int i = 0; i < Demands.GetLength(0); i++)
            {
                Console.WriteLine(+i+" - "+Demands[i]);
            }

        }

        public void PrintDataStructures()
        {
            PrintStarts();
            PrintEnds();
            PrintVehicleCapacities();
            PrintPickupDeliveries();
            PrintTravelTimes();
            PrintTimeWindows();
            PrintDemands();
            PrintCustomersRideTimes();
            PrintCustomersVehicle();
        }

        public void PrintCustomersVehicle()
        {
            Console.WriteLine(this.ToString()+"CustomersVehicle: ");
            for (int i = 0; i < CustomersVehicle.GetLength(0); i++)
            {
                Console.WriteLine(i + " - " + CustomersVehicle[i]);
            }
        }

        public void PrintCustomersRideTimes()
        {
            Console.WriteLine(this.ToString() + "CustomersRideTimes: ");
            for (int i = 0; i < CustomersRideTimes.GetLength(0); i++)
            {
                Console.WriteLine( i + " - " + CustomersRideTimes[i]);
            }
        }
        public void PrintStarts()
        {
            Console.WriteLine(this.ToString() + "Starts: ");
            for (int i = 0; i < Starts.GetLength(0); i++)
            {
                Console.WriteLine(i + " - " + Starts[i]);
            }
        }

        public void PrintEnds()
        {
            Console.WriteLine(this.ToString() + "Ends: ");
            for (int i = 0; i < Ends.GetLength(0); i++)
            {
                Console.WriteLine(i + " - " + Ends[i]);
            }
        }

        public void PrintVehicleCapacities()
        {
            Console.WriteLine(this.ToString() + "VehicleCapacities: ");
            for (int i = 0; i < VehicleCapacities.GetLength(0); i++)
            {
                Console.WriteLine(i + " - " + VehicleCapacities[i]);
            }
        }
        public void PrintTimeWindows() //For debug purposes
        {
            Console.WriteLine(this.ToString() + "Time Windows:");
            for (int i = 0; i < TimeWindows.GetLength(0); i++)
            {
                Console.Write(i + " - T(");
                for (int j = 0; j < TimeWindows.GetLength(1); j++)
                {
                    if (j == 0)
                    {
                        Console.Write(TimeWindows[i, j] + ", ");
                    }
                    else
                    {

                        Console.WriteLine(TimeWindows[i, j] + ")");
                    }
                }
            }
            Console.WriteLine("---------------------------------------------------------------");

        }

    }
    
}
