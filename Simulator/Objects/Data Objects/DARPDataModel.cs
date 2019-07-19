using System;
using System.Collections.Generic;

namespace Simulator.Objects.Data_Objects
{
    internal class DarpDataModel
    {
        private readonly List<Stop> _stops;
        public int DepotIndex;
        public long[,] DistanceMatrix;
        public long[][] InitialRoutes;
        public int[][] PickupsDeliveries;
        public int VehicleNumber;


        public DarpDataModel(int vehicleNumber, int depotId, int[][] pickupsDeliveries, List<Stop> stops)
        {
            _stops = stops;

            var distanceMatrixBuilder = new DistanceMatrixBuilder();
            DistanceMatrix = distanceMatrixBuilder.Generate(_stops);
            VehicleNumber = vehicleNumber;
            DepotIndex = _stops.FindIndex(s => s.Id == depotId);
            PickupsDeliveries = pickupsDeliveries;
        }

        public DarpDataModel(int vehicleNumber, int depotId, int[][] pickupsDeliveries, long[][] initialRoutes,
            List<Stop> stops)
        {
            _stops = stops;

            var distanceMatrixBuilder = new DistanceMatrixBuilder();
            DistanceMatrix = distanceMatrixBuilder.Generate(_stops);
            VehicleNumber = vehicleNumber;
            DepotIndex = _stops.FindIndex(s => s.Id == depotId);
            PickupsDeliveries = pickupsDeliveries;
            InitialRoutes = initialRoutes;
        }

        public Stop GetStop(int index)
        {
            return _stops[index];
        }

        public void PrintPickupDeliveries()
        {
            Console.WriteLine(ToString() + "Pickups and deliveries:");
            foreach (var pickupDelivery in PickupsDeliveries)
                Console.WriteLine(GetStop(pickupDelivery[0]).Id + "->" + GetStop(pickupDelivery[1]).Id);
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