using System;
using System.Collections.Generic;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class RoutingDataModel //Routing DataModel with all the data necessary to be used by the routing Solver
    {
        public long[] VehicleCapacities;

        public long[,] TimeMatrix; //time matrix that contains the travel time to each stop

        public int[] Starts;

        public int[] Ends;

        public DataModelIndexManager IndexManager; //Index manager with the data vehicle,customer and stops objects, responsible for giving the required indexed data, and also enables to convert those indices in its respective object

        public long[,] TimeWindows;

        public long[] Demands;

        public int[][] PickupsDeliveries;

        public int MaxCustomerRideTime; //maximum time a customer can spend in a vehicle (in seconds)

        public int MaxAllowedUpperBoundTime; //maximum delay in the timeWindows, to be used by RoutingSolver to find feasible solutions when the current timeWindowUpperBound isnt feasible

        public RoutingDataModel(DataModelIndexManager indexManger,int maxCustomerRideTime,int maxAllowedUpperBound) //if different end and start depot
        {
                IndexManager = indexManger;
                MaxAllowedUpperBoundTime = maxAllowedUpperBound;
                MaxCustomerRideTime = maxCustomerRideTime;
                Starts = IndexManager.GetVehicleStarts();
                Ends = IndexManager.GetVehicleEnds();
                VehicleCapacities = IndexManager.GetVehicleCapacities();
                TimeMatrix = IndexManager.GetTimeMatrix(true); //calculates timeMatrix using Haversine distance formula
                TimeWindows = IndexManager.GetTimeWindows();
                PickupsDeliveries = IndexManager.GetPickupDeliveries();
                Demands = IndexManager.GetDemands();
        }


        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void PrintTimeMatrix()
        {

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
            Console.WriteLine("---------------------------------------------------------------");
        }

        public string GetCSVSettingsMessage()
        {
            string splitter = ",";
            string message = IndexManager.Customers.Count + splitter + IndexManager.Vehicles.Count + splitter +
                             TimeSpan.FromSeconds(MaxCustomerRideTime).TotalMinutes + splitter + TimeSpan.FromSeconds(MaxAllowedUpperBoundTime).TotalMinutes;
            return message;

        }
        public List<string> GetDataModelSettingsPrintableList()
        {
            List<string> stringList = new List<string>();
           stringList.Add("-------------------------------");
           stringList.Add("|     Data Model Settings     |");
           stringList.Add("-------------------------------");
           stringList.Add("Number of vehicles: "+IndexManager.Vehicles.Count);
           stringList.Add("Number of customers: "+IndexManager.Customers.Count);
           stringList.Add("Maximum Customer Ride Time: "+TimeSpan.FromSeconds(MaxCustomerRideTime).TotalMinutes +" minutes");
           stringList.Add("Maximum Allowed Upper Bound Time: "+TimeSpan.FromSeconds(MaxAllowedUpperBoundTime).TotalMinutes + " minutes");
           return stringList;
        }

        public void PrintPickupDeliveries()
        {
            Console.WriteLine(this.ToString() + "Pickups and Deliveries with Time Windows for the expected Customers(total: " + IndexManager.Customers.Count + "):");
            foreach (var customer in IndexManager.Customers)
            {
                if (!customer.IsInVehicle)
                {
                    customer.PrintPickupDelivery();
                }
                else
                {
                    var vehicle = IndexManager.Vehicles.Find(v => v.Customers.Contains(customer));
                    Console.Write("Already in vehicle "+vehicle.Id+ " - ");
                    customer.PrintPickupDelivery();
                }
            }
           
            Console.WriteLine("---------------------------------------------------------------");
        }


        public void PrintTimeWindows() //For debug purposes
        {
            Console.WriteLine(this.ToString() + "Time Windows:");
            for (int i = 0; i < TimeWindows.GetLength(0); i++)
            {
                Console.Write(IndexManager.GetStop(i) + "{");
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
            Console.WriteLine("---------------------------------------------------------------");

        }

    }
    
}
