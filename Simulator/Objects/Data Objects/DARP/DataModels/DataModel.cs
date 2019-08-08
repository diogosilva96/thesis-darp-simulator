using System;
using System.Collections.Generic;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.DARP.DataModels
{
    public abstract class DataModel
    {
        public int DepotIndex => Stops.IndexOf(Depot);

        public int VehicleNumber => Vehicles.Count ; // number of vehicles

        public long[,] Matrix; //can be a distance or time window matrix

        public readonly List<Stop> Stops; // a list with all the distinct stops for the pickup and deliveries

        public List<Vehicle> Vehicles;

        public readonly Stop Depot; //The depot stop

        public List<Customer> Customers; //A list with all the customers for the pickupDeliveries

        public long[][] InitialRoutes;

        public int[][] PickupsDeliveries => GetPickupDeliveryIndexMatrix();

        protected DataModel(Stop depot)
        {
            Customers = new List<Customer>();
            Vehicles = new List<Vehicle>();
            Depot = depot;
            Stops = new List<Stop>();
            Stops.Add(depot);
            InitialRoutes = new long[0][];
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
                UpdateMatrix();
            }

        }

        protected abstract void UpdateMatrix();

        public Stop GetStop(int index)
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
            if (this is PickupDeliveryDataModel)
            {
                matrixType = "Distance";
            }
            else
            {
                matrixType = "Time";
            }
            Console.WriteLine(ToString() + matrixType+" Matrix:");
            var counter = 0;
            foreach (var val in Matrix)
                if (counter == Matrix.GetLength(1) - 1)
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
            string printString;
            if (this is PickupDeliveryDataModel)
            {
                printString = ToString() + "Pickups and deliveries (total: " + Customers.Count + "):";
            }
            else
            {
                printString = ToString() + "Pickups and Deliveries with Time Windows (total: " + Customers.Count + "):";
            }

            Console.WriteLine(printString);
            foreach (var customer in Customers)
                customer.PrintPickupDelivery();
        }

    }
}
