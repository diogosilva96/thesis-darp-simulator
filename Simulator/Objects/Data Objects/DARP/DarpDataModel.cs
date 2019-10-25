using System;
using System.Collections.Generic;
using System.IO;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.DARP
{
    public class DarpDataModel //pickup delivery with time windows data model
    {
        public long[] VehicleCapacities;

        public long[,] TimeMatrix; //time matrix that contains the travel time to each stop

        public int[] Starts;

        public int[] Ends;

        public DataModelIndexManager IndexManager; //Index manager with the data vehicle,customer and stops objects

        public long[,] TimeWindows;

        public long[] Demands;

        public int VehicleSpeed;

        public long[,] DistanceMatrix;

        private const int DayInSeconds = 60 * 60 * 24; //24hours = 86400 seconds = 60 secs * 60 mins * 24 hours

        public int[][] PickupsDeliveries;

        public long[] StartDepotsArrivalTimes;//minimum time to be at the depot for each vehicle

        public int MaxCustomerRideTime; //maximum time a customer can spend in a vehicle (in seconds)

        public int MaxAllowedUpperBoundTime; //maximum delay in the timeWindows, to be used by DarpSolver to find feasible solutions when the current timeWindowUpperBound isnt feasible

        public DarpDataModel(List<Stop> startDepots, List<Stop> endDepots, List<Vehicle> vehicles, List<Customer> customers,long[] startDepotsArrivalTimes,int maxCustomerRideTime,int maxAllowedUpperBound) //if different end and start depot
        {
            if (startDepotsArrivalTimes.Length != startDepots.Count || vehicles.Count != startDepots.Count ||
                startDepots.Count != endDepots.Count)
            {
                throw new ArgumentException("Data model input arguments do not have the same size");
            }
            else
            {
                MaxAllowedUpperBoundTime = maxAllowedUpperBound;
                MaxCustomerRideTime = maxCustomerRideTime;
                StartDepotsArrivalTimes = startDepotsArrivalTimes;
                VehicleSpeed = vehicles[0].Speed;
                var stops = GetStops(startDepots, endDepots, customers);
                IndexManager = new DataModelIndexManager(stops, vehicles, customers);
                Starts = GetVehicleDepots(startDepots, IndexManager);
                Ends = GetVehicleDepots(endDepots, IndexManager);
                VehicleCapacities = GetVehicleCapacities(IndexManager);
                TimeMatrix = new MatrixBuilder().GetTimeMatrix(IndexManager.Stops, VehicleSpeed);
                TimeWindows = GetTimeWindows(IndexManager);
                PickupsDeliveries = GetPickupDeliveries(IndexManager);
                Demands = GetDemands(IndexManager);
            }
        }



        private int[] GetVehicleDepots(List<Stop> depots, DataModelIndexManager indexManager)
        {
            int[] vehicleDepots = null;
            if (indexManager.Vehicles != null && indexManager.Stops != null)
            {
                if (indexManager.Vehicles.Count > 0 && indexManager.Vehicles.Count == depots.Count)
                {
                    vehicleDepots = new int[indexManager.Vehicles.Count];
                    foreach (var vehicle in indexManager.Vehicles)
                    {
                        var vehicleIndex = indexManager.GetVehicleIndex(vehicle);
                        vehicleDepots[vehicleIndex] = indexManager.GetStopIndex(depots[vehicleIndex]); //finds the index of the start depot stop in the stop list
                    }
                }
            }

            return vehicleDepots;
        }

        private List<Stop> GetStops(List<Stop> startDepots, List<Stop> endDepots, List<Customer> customers)
        {
            var stops = new List<Stop>(); //clears stop list
            // initializes the list with the start depots
            if (startDepots != null)
            {
                foreach (var startDepot in startDepots)
                {
                    if (!stops.Contains(startDepot))
                    {
                        stops.Add(startDepot);
                    }
                }
            }

            if (endDepots != null)
            {
                //initializes the list with the end depots
                foreach (var endDepot in endDepots)
                {
                    if (!stops.Contains(endDepot))
                    {
                        stops.Add(endDepot);
                    }
                }
            }

            foreach (var customer in customers) //loop to add the pickup and delivery stops for each customer, to the stop list
            {
                foreach (var pickupDelivery in customer.PickupDelivery)
                {
                    if (!stops.Contains(pickupDelivery))
                    {
                        stops.Add(pickupDelivery); //if the pickup stop isn't in the list, add it to the stop list
                    }
                }
            }

            return stops;
        }

    
        private int[][] GetPickupDeliveries(DataModelIndexManager indexManager) //returns the pickupdelivery stop matrix using indexes (based on the stop list) instead of stop id's
        {
            var customers = indexManager.Customers;
            int[][] pickupsDeliveries = new int[customers.Count][];
            //Transforms the data from stop the list into index matrix list in order to use it in google Or tools
            foreach (var customer in customers)
            {
                pickupsDeliveries[indexManager.GetCustomerIndex(customer)] = indexManager.GetPickupDeliveryStopIndices(customer);
            }

            return pickupsDeliveries;
        }

     

        private long[] GetDemands(DataModelIndexManager indexManager)
        {
            long[] demands = null;
            var customers = indexManager.Customers;
            var stops = indexManager.Stops;
            if (stops.Count > 0)
            {
                demands = new long[stops.Count];
                //loop that initializes demands
                for (int i = 0; i < stops.Count; i++)
                {
                    demands[i] = 0; //init demand at 0 at each index
                }

                if (customers.Count > 0)
                {
                    foreach (var customer in customers)
                    {
                        var pickupIndex = IndexManager.GetStopIndex(customer.PickupDelivery[0]); //gets the index of the pickup stop
                        var deliveryIndex =
                            IndexManager.GetStopIndex(customer.PickupDelivery[1]); //gets the index of the delivery stop
                        demands[pickupIndex] += 1; //adds 1 to the demand of the pickup index
                        demands[deliveryIndex] -= 1; //subtracts 1  to the demand of the delivery index
                    }
                }
            }

            return demands;
        }

        //public void UpdateDemands(int index, long value)
        //{
        //    Demands[index] = value;
        //}

        private long[] GetVehicleCapacities(DataModelIndexManager indexManager)
        {
            long[] vehicleCapacities = null;
            var vehicles = indexManager.Vehicles;
            if (vehicles.Count > 0)
            {
                vehicleCapacities = new long[vehicles.Count];
                for (int i = 0; i < vehicles.Count; i++)
                {
                    vehicleCapacities[i] = vehicles[i].Capacity;
                }

            }

            return vehicleCapacities;
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


        public void PrintPickupDeliveries()
        {
            Console.WriteLine(this.ToString() + "Pickups and Deliveries with Time Windows (total: " + IndexManager.Customers.Count + "):");
            foreach (var customer in IndexManager.Customers)
            {
                customer.PrintPickupDelivery();
            }
            Console.WriteLine("---------------------------------------------------------------");
        }

        private static long[,] GetInitialTimeWindowsArray(long[,] timeWindows)
        {
            //Loop to initialize each cell of the timewindow array at the maximum minutes value (1440minutes - 24 hours)
            for (int i = 0; i < timeWindows.GetLength(0); i++)
            {
                timeWindows[i, 0] = 0; //lower bound of the timewindow is initialized with 0
                timeWindows[i, 1] = DayInSeconds; //Upper bound of the timewindow with a max long value

            }

            return timeWindows;
        }

        public long[,] GetTimeWindows(DataModelIndexManager indexManager)
        {
            var stops = indexManager.Stops;
            long[,] timeWindows = new long[stops.Count, 2];
            timeWindows = GetInitialTimeWindowsArray(timeWindows);
            foreach (var customer in indexManager.Customers)
            {
                //LOWER BOUND (MINIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                var customerMinTimeWindow = customer.DesiredTimeWindow[0]; //customer min time window in seconds
                var pickupIndex = indexManager.GetStopIndex(customer.PickupDelivery[0]);//gets stop pickup index
                var arrayMinTimeWindow = timeWindows[pickupIndex, 0]; //gets current min timewindow for the pickupstop in minutes
                                                                      //If there are multiple min time window values for a given stop, the minimum time window will be the maximum timewindow between all those values
                                                                      //because the vehicle must arrive that stop at most, at the greatest min time window value, in order to satisfy all requests

                var lowerBoundValue = Math.Max((long)arrayMinTimeWindow, (long)customerMinTimeWindow); //the lower bound value is the maximum value between the current timewindow in the array and the current customer timewindow
                //Console.WriteLine("LowerBound value " + customer.PickupDelivery[0] + " = MAX:" + arrayMinTimeWindow + "," + customerMinTimeWindow + " = " + lowerBoundValue);//debug
                timeWindows[pickupIndex, 0] = lowerBoundValue; //Updates the timeWindow matrix with the new lowerBoundValue


                //UPPER BOUND (MAXIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                var customerMaxTimeWindow = customer.DesiredTimeWindow[1]; //customer max time window in seconds
                var deliveryIndex = indexManager.GetStopIndex(customer.PickupDelivery[1]);//get stop delivery index
                var arrayMaxTimeWindow = timeWindows[deliveryIndex, 1]; //gets curent max timewindow for the delivery stop in minutes
                //If there are multiple max timewindows for a given stop, the maximum time window will be the minimum between all those values
                //because the vehicle must arrive that stop at most, at the lowest max time window value, in order to satisfy all the requests
                var upperBoundValue = Math.Min((long)arrayMaxTimeWindow, (long)customerMaxTimeWindow);//the upper bound Value is the minimum value between the current  timewindow in the array and the current customer timewindow;
                //Console.WriteLine("UpperBound value " + customer.PickupDelivery[1] + " = Min:" + arrayMaxTimeWindow + "," + customerMaxTimeWindow + " = " + upperBoundValue); //debug
                timeWindows[deliveryIndex, 1] = upperBoundValue; //Updates the timeWindow matrix with the new lowerBoundValue
            }
            //depot timewindows initialization
            if (Starts != null && timeWindows != null)
            {
                for (int j = 0; j < Starts.Length; j++)
                {

                    timeWindows[Starts[j], 0] = StartDepotsArrivalTimes[j];
                    timeWindows[Starts[j], 1] = DayInSeconds;
                }
            }
            // end of depot timewindow initialization
            return timeWindows;
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
