using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Simulator.Objects.Data_Objects
{
    public class DarpDataModel
    {
        public List<Customer> PickupDeliveryCustomers; //A list with all the customers for the pickupDeliveries

        private readonly Stop _depot; //The depot stop

        public int DepotIndex => _pickupDeliveryStops.IndexOf(_depot);

        public int[][] PickupsDeliveries => PickupDeliveryCustomersToPickupDeliveryIndexMatrix();

        public long[,] DistanceMatrix;

        public int VehicleNumber => Vehicles.Count;

        public List<Vehicle> Vehicles;

        public long[][] InitialRoutes; 

        private readonly List<Stop> _pickupDeliveryStops; // a list with all the distinct stops for the pickup and deliveries

        public DarpDataModel(Stop depot)
        {
            Vehicles = new List<Vehicle>();
            PickupDeliveryCustomers = new List<Customer>();
            _depot = depot;
            _pickupDeliveryStops = new List<Stop>();
            _pickupDeliveryStops.Add(depot);
            InitialRoutes = new long[0][];
        }

        public void AddCustomer(Stop pickup, Stop delivery, int requestTime)
        {
            if (pickup != null && delivery != null)
            {
                var customer = new Customer(pickup, delivery, requestTime);
                PickupDeliveryCustomers.Add(customer);
                AddPickupDeliveryStops(customer);
            }
        }

        private void UpdateDistanceMatrix()
        {
            DistanceMatrix = new MatrixBuilder().GetDistanceMatrix(_pickupDeliveryStops);
        }

        public void AddInitialRoute(List<Stop> stopSequence)
        {
            // Initial route creation
            long[] initialRoute = new long[stopSequence.Count];
            int index = 0;
            foreach (var stop in stopSequence)
            {
                if (!_pickupDeliveryStops.Contains(stop)) _pickupDeliveryStops.Add(stop);
                initialRoute[index] = _pickupDeliveryStops.IndexOf(stop);
                index++;
            }

            Array.Resize(ref InitialRoutes, InitialRoutes.Length + 1);
            InitialRoutes[InitialRoutes.Length-1] = initialRoute;
        }

        public void AddCustomer(Customer customer)
        {
            if (!PickupDeliveryCustomers.Contains(customer))
            {
                PickupDeliveryCustomers.Add(customer);
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
                var pickupdelivery = customer.PickupDelivery[i];
                if (_pickupDeliveryStops.Contains(pickupdelivery)) continue;
                _pickupDeliveryStops.Add(pickupdelivery);//if the pickup stop isn't in the list, add it to the stop list
                valueChanged = true;
            }
            if (valueChanged)
            {
                UpdateDistanceMatrix();
            }

        }

        public Stop GetStop(int index)
        {
            return _pickupDeliveryStops[index];
        }
        private int[][] PickupDeliveryCustomersToPickupDeliveryIndexMatrix()//returns the pickupdelivery stop matrix using indexes (based on the pickupdeliverystop list) instead of stop id's
        {
            int[][] pickupsDeliveries = new int[PickupDeliveryCustomers.Count][];
            //Transforms the data from stop the list into index matrix list in order to use it in google Or tools
            int insertCounter = 0;
            foreach (var customer in PickupDeliveryCustomers)
            {
                var pickup = customer.PickupDelivery[0];
                var delivery = customer.PickupDelivery[1];
                var pickupDeliveryInd = new int[] { _pickupDeliveryStops.IndexOf(pickup), _pickupDeliveryStops.IndexOf(delivery) };
                pickupsDeliveries[insertCounter] = pickupDeliveryInd;
                insertCounter++;
            }

            return pickupsDeliveries;
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
                            Console.Write(GetStop((int)initialRoute[i]).Id + " -> ");
                        }
                        else
                        {
                            Console.WriteLine(GetStop((int)initialRoute[i]).Id);
                        }
                    }
                    count++;
                }
            }
        }
        public void PrintPickupDeliveries()
        {
            Console.WriteLine(ToString() + "Pickups and deliveries (total: "+PickupsDeliveries.Length+"):");
            foreach (var customer in PickupDeliveryCustomers)
                Console.WriteLine("Customer "+customer.Id+" - PickupDelivery: {"+customer.PickupDelivery[0].Id+" -> "+customer.PickupDelivery[1].Id+"} - Request Timestamp: "+TimeSpan.FromSeconds(customer.RequestTime).ToString());
        }

        public override string ToString()
        {
            return "[" + GetType().Name + "] ";
        }

        public void PrintDistanceMatrix()
        {
            Console.WriteLine(ToString() + "Distance Matrix:");
            var counter = 0;
            foreach (var val in DistanceMatrix)
                if (counter == DistanceMatrix.GetLength(1) - 1)
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
    }
}
