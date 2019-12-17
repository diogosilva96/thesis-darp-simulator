using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.Routing
{
    public class
        DataModelIndexManager //index manager with the vehicles, stops, depots, and customer data, which enables to convert any of these objects to its index on the different lists to be used by the dataModel and routingSolver
    {
        public List<Stop> Stops;//Same stop can have multiple indexes, depending on which customer or vehicle it was assigned to
        public readonly List<Vehicle> Vehicles;//Each vehicle has a unique index
        public readonly List<Customer> Customers; //Each customer has a unique index
        public List<Stop> StartDepots;//each index represents the vehicle in that index in the Vehicles list.
        public List<Stop> EndDepots;//each index represents the vehicle in that index in the Vehicles list.
        public readonly List<long> StartDepotArrivalTimes;
        private Dictionary<Customer, int[]> _customersPickupDeliveriesDictionary; //customerPickupDeliverydict, value => [0] = pickup index, [1] = delivery index
        private Dictionary<Vehicle,int[]> _vehicleStartEndsDictionary; //vehicleStartEndDictionary, value => [0] = start index [1] = end index


        public DataModelIndexManager(List<Vehicle> vehicles, List<Customer> customers, List<long> startDepotsArrivalTimes)
        {
            Vehicles = vehicles;
            Customers = customers;
            StartDepots = GetStartDepots();
            EndDepots = GetEndDepots();
            StartDepotArrivalTimes = startDepotsArrivalTimes;
            _customersPickupDeliveriesDictionary = new Dictionary<Customer, int[]>();
            _vehicleStartEndsDictionary = new Dictionary<Vehicle, int[]>();
            Stops = GetStops();
        }


        public int GetCustomerIndex(Customer customer)
        {
            return Customers.FindIndex(c => c == customer);
        }

        private List<Stop> GetStartDepots()
        {
            List<Stop> startDepots = new List<Stop>();
            foreach (var vehicle in Vehicles)
            {
                if (vehicle.CurrentStop != null)
                {
                    startDepots.Add(vehicle.CurrentStop);
                }
                else
                {
                    startDepots.Add(vehicle.StartStop);
                }

            }

            return startDepots;
        }
        private List<Stop> GetEndDepots()
        {
            List<Stop> endDepots = new List<Stop>();
            foreach (var vehicle in Vehicles)
            {
                endDepots.Add(vehicle.EndStop);

            }

            return endDepots;
        }
        public Customer GetCustomer(int index)
        {
            return Customers[index];
        }

        public int[] GetVehicleStartEndStopIndices(Vehicle vehicle)
        {
            int[] indices = new int[] {-1,-1};
            if (_vehicleStartEndsDictionary.ContainsKey(vehicle))
            {
                _vehicleStartEndsDictionary.TryGetValue(vehicle, out var startEndIndices);
                if (startEndIndices != null)
                {
                    indices = startEndIndices;
                }
            }

            return indices;
        }
        public int GetVehicleIndex(Vehicle vehicle)
        {
            return Vehicles.FindIndex(v => v == vehicle);
        }

        public int[] GetCustomerPickupDeliveryIndices(Customer customer)
        {
            int[] pickupDeliveryIndices = new int[]{-1,-1};
            if (_customersPickupDeliveriesDictionary.ContainsKey(customer))
            {
                _customersPickupDeliveriesDictionary.TryGetValue(customer, out var pdIndices);
                if (pdIndices != null)
                {
                    pickupDeliveryIndices = pdIndices;
                }
            }

            return pickupDeliveryIndices;
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
            if (index >= Stops.Count || index < 0)
            {

                return null;

            }

            return Stops[index];
        }

        public int GetStopIndex(Stop stop)
        {
            var index = Stops.FindIndex(s => s == stop);
            return index;
        }


        public int[] GetCustomersVehicle()
        {
            int[] customersVehicle = new int[Customers.Count];
            foreach (var customer in Customers)
            {
                var vehicleIndex = -1;
                var pdIndices = GetCustomerPickupDeliveryIndices(customer);
                if (pdIndices[0] == -1)//customer is inside a vehicle
                {
                    vehicleIndex = Vehicles.FindIndex(v => v.Customers.Contains(customer));
                }

                customersVehicle[GetCustomerIndex(customer)] = vehicleIndex;
            }

            return customersVehicle;

        }

        public long[] GetCustomersRideTime() //gets current customersRideTime for the customers already inside a vehicle, to be used on the maxRideTime constraint if needed!
        {
            long[] customersRideTime = new long[Customers.Count];
            var customersVehicle = GetCustomersVehicle();
            for (int customerIndex = 0; customerIndex < customersVehicle.Length; customerIndex++)
            {
                if (customersVehicle[customerIndex] != -1)
                {
                    var vehicleIndex = customersVehicle[customerIndex];
                    var currentTime = StartDepotArrivalTimes[vehicleIndex];
                    var customer = GetCustomer(customerIndex);
                    var rideTime = currentTime - customer.RealTimeWindow[0];
                    customersRideTime[customerIndex] = rideTime;
                    Console.WriteLine(customer.ToString()+ " current Ride time: "+rideTime);
                }
            }

            return customersRideTime;
        }
        private List<Stop> GetStops() //Gets all stops that will be used by the datamodel
        {
            var stops = new List<Stop>(); //clears stop list

            // initializes the list with the start depots
            if (StartDepots.Count == EndDepots.Count)
            {
                for(int vehicleIndex =0;vehicleIndex<StartDepots.Count;vehicleIndex++)// 
                {
                    
                    stops.Add(StartDepots[vehicleIndex]);//add current stop index
                    var startIndex = stops.Count - 1;
                    stops.Add(EndDepots[vehicleIndex]);//add current end index
                    var endIndex = stops.Count - 1;
                    _vehicleStartEndsDictionary.Add(Vehicles[vehicleIndex],new int[]{startIndex,endIndex});
                }
            }
            else
            {
                throw new Exception("StopDepots size != EndDepots size");
            }

            foreach (var customer in Customers) //loop to add the pickup and delivery stops for each customer, to the stop list, adds a stop for each customer (even if it is repeated)
            {
                var addedPickupIndex = -1; //pickupIndex will be -1 if a customer is inside a vehicle, otherwise it is the pickupIndex
                var addedDeliveryIndex = -1;
                if (!customer.IsInVehicle) //if the customer isnt in a vehicle adds both pickup and delivery stops
                {
                    stops.Add(customer.PickupDelivery[0]); //if the pickup stop isn't in the list, add it to the stop list
                    addedPickupIndex = stops.Count - 1;
                }
                stops.Add(customer.PickupDelivery[1]);
                addedDeliveryIndex = stops.Count - 1;
 
                _customersPickupDeliveriesDictionary.Add(customer, new int[] { addedPickupIndex, addedDeliveryIndex }); //adds to the dictionary
              
            }

            return stops;
        }


        public int[] GetVehicleStarts()
        {
            int[] vehicleStarts = null;
            if (Vehicles.Count == _vehicleStartEndsDictionary.Count)//for checking purposes
            {
                vehicleStarts = new int[Vehicles.Count];
                for (int i = 0; i < Vehicles.Count; i++)
                {
                    var vehicle = Vehicles[i];
                    var startIndex = GetVehicleStartEndStopIndices(vehicle)[0]; //gets start index
                    if (startIndex != -1)// if a valid index was found
                    {
                        vehicleStarts[i] = startIndex;
                    }
                }
            }
            else
            {
                throw new Exception("Vehicle number != vehicleStartEnd dictionary size");
            }
            return vehicleStarts; //returns vehicle depots, every index of the array represents a vehicle and its content represents the depot stop index
        }

        public int[] GetVehicleEnds()
        {
            int[] vehicleEnds = null;
            if (Vehicles.Count == _vehicleStartEndsDictionary.Count)
            {
                vehicleEnds = new int[Vehicles.Count];
                for (int i = 0; i < Vehicles.Count; i++)
                {
                    var vehicle = Vehicles[i];
                    var endIndex = GetVehicleStartEndStopIndices(vehicle)[1]; //gets end index
                    if (endIndex != -1)// if a valid index was found
                    {
                        vehicleEnds[i] = endIndex;
                    }

                }
            }
            else
            {
                throw new Exception("Vehicle number != vehicleStartEnd dictionary size");
            }
            return vehicleEnds; //returns vehicle depots, every index of the array represents a vehicle and its content represents the depot stop index
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
                timeWindows[i, 1] = 24 * 60 * 60; //Upper bound of the timewindow with 24 hours (in seconds)
            }

            
            long lowerBoundValue = 0;
            foreach (var customer in Customers)
            {
                var pickupDelivery = GetCustomerPickupDeliveryIndices(customer);
                if (pickupDelivery[0] != -1) //if pickupIndex is -1 it means the customer is inside a vehicle
                {
                    //LOWER BOUND (MINIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                    var customerMinTimeWindow =
                        customer.DesiredTimeWindow[0]; //customer min time window in seconds
                    var pickupIndex = pickupDelivery[0]; //gets stop pickup index
                    var arrayMinTimeWindow =
                        timeWindows[pickupIndex, 0]; //gets current min timewindow for the pickupstop in minutes
                    //If there are multiple min time window values for a given stop, the minimum time window will be the maximum timewindow between all those values
                    //because the vehicle must arrive that stop at most, at the greatest min time window value, in order to satisfy all requests

                    lowerBoundValue =
                        Math.Max((long)arrayMinTimeWindow,
                            (long)customerMinTimeWindow); //the lower bound value is the maximum value between the current timewindow in the array and the current customer timewindow
                    //Console.WriteLine("LowerBound value " + customer.PickupDelivery[0] + " = MAX:" + arrayMinTimeWindow + "," + customerMinTimeWindow + " = " + lowerBoundValue);//debug
                    timeWindows[pickupIndex, 0] =
                        lowerBoundValue; //Updates the timeWindow matrix with the new lowerBoundValue
                }


                //UPPER BOUND (MAXIMUM ARRIVAL VALUE AT A CERTAIN STOP) TIMEWINDOW CALCULATION
                var customerMaxTimeWindow =
                    customer.DesiredTimeWindow[1]; //customer max time window in seconds
                var deliveryIndex = pickupDelivery[1]; //get stop delivery index
                var arrayMaxTimeWindow =
                    timeWindows[deliveryIndex, 1]; //gets curent max timewindow for the delivery stop in minutes
                //If there are multiple max timewindows for a given stop, the maximum time window will be the minimum between all those values
                //because the vehicle must arrive that stop at most, at the lowest max time window value, in order to satisfy all the requests
                var upperBoundValue = Math.Min((long)arrayMaxTimeWindow, (long)customerMaxTimeWindow); //the upper bound Value is the minimum value between the current  timewindow in the array and the current customer timewindow;
                //Console.WriteLine("UpperBound value " + customer.PickupDelivery[1] + " = Min:" + arrayMaxTimeWindow + "," + customerMaxTimeWindow + " = " + upperBoundValue); //debug
                timeWindows[deliveryIndex, 1] = upperBoundValue; //Updates the timeWindow matrix with the new lowerBoundValue
            }

            //depot timewindows initialization
            if (StartDepots != null && timeWindows != null)
            {
                for (int j = 0; j < StartDepots.Count; j++)
                {
                    var currentVehicle = Vehicles[j];
                    var startIndex = GetVehicleStartEndStopIndices(currentVehicle)[0];//gets vehicle start index
                    if (startIndex != -1)//if index was found
                    {
                        timeWindows[startIndex, 0] = StartDepotArrivalTimes[j]; //assigns min value for depot timeWindow for currentVehicle
                        timeWindows[startIndex, 1] = 24 * 60 * 60; //assigns max value for depot timeWindow for current Vehicle, 24 hours in seconds
                        
                    }
                }
            }

            for (int i = 0; i < timeWindows.GetLength(0); i++)
            {
                if (timeWindows[i, 0] > timeWindows[i, 1])
                {
                    timeWindows[i, 1] = timeWindows[i, 0];
                    throw new Exception("TW problem");
                }

            }

            // end of depot timewindow initialization
            return timeWindows;
        }

        public int[][] GetPickupDeliveries() //returns the pickupdelivery stop matrix using indices (based on the stop list) instead of stop objects
        {
            var customers = Customers;
            var customerNumber = Customers.Count;
            int[][] pickupsDeliveries = new int[customerNumber][];
            //Transforms the data from stop the list into index matrix list in order to use it in google Or tools
            for(int customerIndex = 0; customerIndex<Customers.Count;customerIndex++)
            {
                var customer = Customers[customerIndex];
                var customerPickupDeliveryIndices = GetCustomerPickupDeliveryIndices(customer);
                pickupsDeliveries[customerIndex] = customerPickupDeliveryIndices;
            }
            return pickupsDeliveries;
        }

        public long[] GetDemands()
        {
            long[] demands = null;
            var customers = Customers;
            var stops = Stops;
            if (stops.Count > 0)
            {
                demands = new long[stops.Count];

                if (Customers.Count > 0)
                {
                    foreach (var customer in Customers)
                    {
                        var pickupDelivery = GetCustomerPickupDeliveryIndices(customer);
                        if (pickupDelivery[0] != -1)//if customer pickupIndex isnt -1 (-1 happends when a customer is inside avehicle)
                        {
                            var pickupIndex = pickupDelivery[0];//gets the index of the pickup stop
                            demands[pickupIndex] += 1;//adds 1 to the demand of the pickup index 
                        }
                        var deliveryIndex = pickupDelivery[1];//gets the index of the delivery stop
                        demands[deliveryIndex] -= 1;//subtracts 1 to the demand of the delivery index
                    }
                }

                foreach (var vehicle in Vehicles)
                {
                    var startIndex = GetVehicleStartEndStopIndices(vehicle)[0];
                    
                    if (startIndex != -1) //if start index was found
                    {
                        demands[startIndex] = vehicle.Customers.FindAll(c => c.IsInVehicle).Count; //the demand at the start depot for the current vehicle will be the number of customers inside that vehicle
                    }
                }
            }
            return demands;
        }
    }
}
