using System;
using System.Collections.Generic;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class DataModelIndexManager //index manager with the vehicles, stops, depots, and customer data, which enables to convert any of these objects to its index on the different lists to be used by the dataModel and routingSolver
    {
        public List<Stop> Stops;
        public readonly List<Vehicle> Vehicles;
        public readonly List<Customer> Customers;
        public readonly List<Stop> StartDepots;
        public readonly List<Stop> EndDepots;
        public readonly List<long> StartDepotArrivalTimes;


        public DataModelIndexManager(List<Stop> startDepots,List<Stop> endDepots, List<Vehicle> vehicles, List<Customer> customers, List<long> startDepotsArrivalTimes)
        {
            if (startDepotsArrivalTimes.Count != startDepots.Count || vehicles.Count != startDepots.Count ||
                startDepots.Count != endDepots.Count)
            {
                throw new ArgumentException("Index manager input arguments do not have the same size");
            }
            StartDepots = startDepots;
            EndDepots = endDepots;
            Vehicles = vehicles;
            Customers = customers;
            StartDepotArrivalTimes = startDepotsArrivalTimes;
            Stops = GetStops();
        }


        public int GetCustomerIndex(Customer customer)
        {
            return Customers.FindIndex(c=>c == customer);
        }
        public Customer GetCustomer(int index)
        {
            return Customers[index];
        }
        public int GetVehicleIndex(Vehicle vehicle)
        {
            return Vehicles.FindIndex(v => v == vehicle);      
        }

        public int[] GetPickupDeliveryStopIndices(Customer customer) //returns the pickupdelivery stop indices for the customer received as argument
        {
            if (Customers.Contains(customer))
            {
                return new int[] { GetStopIndex(customer.PickupDelivery[0]), GetStopIndex(customer.PickupDelivery[1]) };
            }
            return null;
        }
        public Vehicle GetVehicle(int index)
        {
            if (index >= Vehicles.Count)
            {
                return null;
            }
            else
            {
                return Vehicles[index];
            }
        }

        public Stop GetStop(int index)
        {                  
            if (index >= Stops.Count || index <0)
            {

                return null;

            }

            return Stops[index];
        }
        public int GetStopIndex(Stop stop)
        {
            var numStops = Stops.FindAll(s => s == stop).Count;
            if (numStops > 1)
            {

            }
            var index = Stops.FindIndex(s => s == stop);
            return index;
        }

        private List<Stop> GetStops() //Gets all stops that will be used by the datamodel
        {
            var stops = new List<Stop>(); //clears stop list
            // initializes the list with the start depots
            if (StartDepots != null)
            {
                foreach (var startDepot in StartDepots)
                {
                    if (!stops.Contains(startDepot))
                    {
                        stops.Add(startDepot);
                    }
                }
            }

            if (EndDepots != null)
            {
                //initializes the list with the end depots
                foreach (var endDepot in EndDepots)
                {
                    if (!stops.Contains(endDepot))
                    {
                        stops.Add(endDepot);
                    }
                }
            }

            foreach (var customer in Customers) //loop to add the pickup and delivery stops for each customer, to the stop list
            {
                if (!customer.IsInVehicle)//if the customer isnt in a vehicle adds both pickup and delivery stops
                {
                    foreach (var pickupDelivery in customer.PickupDelivery)
                    {
                        if (!stops.Contains(pickupDelivery))
                        {
                            stops.Add(pickupDelivery); //if the pickup stop isn't in the list, add it to the stop list
                        }
                    }
                }
                else //if the customer is in a vehicle only adds the delivery stop
                {
                    if (!stops.Contains(customer.PickupDelivery[1]))
                    {
                        stops.Add(customer.PickupDelivery[1]);
                    }
                }
            }
            return stops;
        }

        private int[] GetVehiclesDepotArray(List<Stop> depots)
        {
            int[] vehicleDepots = null;
            if (Vehicles != null && Stops != null)
            {
                if (Vehicles.Count > 0 && Vehicles.Count == depots.Count)
                {
                    vehicleDepots = new int[Vehicles.Count];
                    foreach (var vehicle in Vehicles)
                    {
                        var vehicleIndex = GetVehicleIndex(vehicle);
                        vehicleDepots[vehicleIndex] = GetStopIndex(depots[vehicleIndex]); //finds the index of the start depot stop in the stop list
                    }
                }
            }

            return vehicleDepots; //returns vehicle depots, every index of the array represents a vehicle and its content represents the depot stop index
        }

        public int[] GetVehicleStarts()
        {
            return GetVehiclesDepotArray(StartDepots);
        }

        public int[] GetVehicleEnds()
        {
            return GetVehiclesDepotArray(EndDepots);
        }

        public long[] GetVehicleCapacities()
        {
            long[] vehicleCapacities = null;
            var vehicles = Vehicles;
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

        public long[,] GetTimeMatrix(bool useHaversineDistanceFormula)
        {
            return new MatrixBuilder().GetTimeMatrix(Stops, Vehicles[0].Speed, useHaversineDistanceFormula); //gets the time matrix using the same stop indexing of the indexmanager
        }

        public long[,] GetTimeWindows()
        {
            var stops = Stops;
            long[,] timeWindows = new long[stops.Count, 2];
            //Loop to initialize each cell of the timewindow array at the maximum minutes value (1440minutes - 24 hours)
            for (int i = 0; i < timeWindows.GetLength(0); i++)
            {
                timeWindows[i, 0] = 0; //lower bound of the timewindow is initialized with 0
                timeWindows[i, 1] = 24*60*60; //Upper bound of the timewindow with 24 hours (in seconds)
            }
            foreach (var customer in Customers)
            {
                if (!customer.IsInVehicle) //if customer is not inside a vehicle adds the pickup and delivery time windows otherwise only adds the delivery time window
                {
                    //LOWER BOUND (MINIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                    var customerMinTimeWindow = customer.DesiredTimeWindow[0]; //customer min time window in seconds
                    var pickupIndex = GetStopIndex(customer.PickupDelivery[0]); //gets stop pickup index
                    var arrayMinTimeWindow =
                        timeWindows[pickupIndex, 0]; //gets current min timewindow for the pickupstop in minutes
                    //If there are multiple min time window values for a given stop, the minimum time window will be the maximum timewindow between all those values
                    //because the vehicle must arrive that stop at most, at the greatest min time window value, in order to satisfy all requests

                    var lowerBoundValue =
                        Math.Max((long)arrayMinTimeWindow,
                            (long)customerMinTimeWindow); //the lower bound value is the maximum value between the current timewindow in the array and the current customer timewindow
                    //Console.WriteLine("LowerBound value " + customer.PickupDelivery[0] + " = MAX:" + arrayMinTimeWindow + "," + customerMinTimeWindow + " = " + lowerBoundValue);//debug
                    timeWindows[pickupIndex, 0] =
                        lowerBoundValue; //Updates the timeWindow matrix with the new lowerBoundValue
                }

                //UPPER BOUND (MAXIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                var customerMaxTimeWindow = customer.DesiredTimeWindow[1]; //customer max time window in seconds
                var deliveryIndex = GetStopIndex(customer.PickupDelivery[1]);//get stop delivery index
                var arrayMaxTimeWindow = timeWindows[deliveryIndex, 1]; //gets curent max timewindow for the delivery stop in minutes
                //If there are multiple max timewindows for a given stop, the maximum time window will be the minimum between all those values
                //because the vehicle must arrive that stop at most, at the lowest max time window value, in order to satisfy all the requests
                var upperBoundValue = Math.Min((long)arrayMaxTimeWindow, (long)customerMaxTimeWindow);//the upper bound Value is the minimum value between the current  timewindow in the array and the current customer timewindow;
                //Console.WriteLine("UpperBound value " + customer.PickupDelivery[1] + " = Min:" + arrayMaxTimeWindow + "," + customerMaxTimeWindow + " = " + upperBoundValue); //debug
                timeWindows[deliveryIndex, 1] = upperBoundValue; //Updates the timeWindow matrix with the new lowerBoundValue
            }
            //depot timewindows initialization
            if (StartDepots != null && timeWindows != null)
            {

                for (int j = 0; j < StartDepots.Count; j++)
                {

                    timeWindows[GetStopIndex(StartDepots[j]), 0] = StartDepotArrivalTimes[j];
                    timeWindows[GetStopIndex(StartDepots[j]), 1] = 24*60*60; //24 hours in seconds
                }
            }
            // end of depot timewindow initialization
            return timeWindows;
        }

        public int[][] GetPickupDeliveries() //returns the pickupdelivery stop matrix using indices (based on the stop list) instead of stop objects
        {
            var customers = Customers;
            var numberOfCustomersOutsideOfVehicle = Customers.FindAll(c => !c.IsInVehicle).Count;
            int[][] pickupsDeliveries = new int[numberOfCustomersOutsideOfVehicle][];
            //Transforms the data from stop the list into index matrix list in order to use it in google Or tools
            var index = 0;
            foreach (var customer in customers)
            {
                if (!customer.IsInVehicle) //if customer is not in a vehicle gets the pickup and delivery
                {
                    pickupsDeliveries[index] = GetPickupDeliveryStopIndices(customer);
                    index++;
                }
            }

            return pickupsDeliveries;
        }

        public long[] GetDemands()
        {
            long[] demands = null;
            var customers =Customers;
            var stops = Stops;
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
                        if (!customer.IsInVehicle) //if customer is not in vehicle adds pickup and delivery, otherwise only adds the delivery indices
                        {
                            var pickupIndex =
                                GetStopIndex(
                                    customer.PickupDelivery[0]); //gets the index of the pickup stop
                            demands[pickupIndex] += 1; //adds 1 to the demand of the pickup index
                        }

                        var deliveryIndex = GetStopIndex(customer.PickupDelivery[1]); //gets the index of the delivery stop
                        demands[deliveryIndex] -= 1; //subtracts 1  to the demand of the delivery index
                    }
                }
                foreach (var vehicle in Vehicles)
                {
                    demands[GetStopIndex(StartDepots[GetVehicleIndex(vehicle)])] = vehicle.Customers.FindAll(c => c.IsInVehicle).Count;//the demand at the start depot for the current vehicle will be the number of customers inside that vehicle
                }
            }

            return demands;
        }
    }
}
