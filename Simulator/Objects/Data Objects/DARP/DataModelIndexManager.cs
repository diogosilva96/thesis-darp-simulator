using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects.Data_Objects.DARP
{
    public class DataModelIndexManager //index manager with the vehicle, stop and customer data, also enables to convert any of this object to its index on the list
    {
        public readonly List<Stop> Stops;
        public readonly List<Vehicle> Vehicles;
        public readonly List<Customer> ExpectedCustomers;
        public List<Customer> CustomersInsideVehicle;


        public DataModelIndexManager(List<Stop> stops, List<Vehicle> vehicles, List<Customer> expectedCustomers)
        {
            Stops = stops;
            Vehicles = vehicles;
            ExpectedCustomers = expectedCustomers;
            CustomersInsideVehicle = new List<Customer>();
            foreach (var vehicle in vehicles)
            {
                foreach (var customerInsideVehicle in vehicle.Customers)
                {
                    if (!CustomersInsideVehicle.Contains(customerInsideVehicle))
                    {
                        CustomersInsideVehicle.Add(customerInsideVehicle);
                    }
                }
            }
        }


        public int GetCustomerIndex(Customer customer)
        {
            return ExpectedCustomers.FindIndex(c=>c == customer);
        }
        public Customer GetCustomer(int index)
        {
            return ExpectedCustomers[index];
        }
        public int GetVehicleIndex(Vehicle vehicle)
        {
            return Vehicles.FindIndex(v => v == vehicle);      
        }

        public int[] GetPickupDeliveryStopIndices(Customer customer) //returns the pickupdelivery stop indices for the customer received as argument
        {
            if (ExpectedCustomers.Contains(customer))
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
    }
}
