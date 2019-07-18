using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects.Data_Objects
{
    class DarpDataModel
    {
            public long[][] InitialRoutes;
            public long[,] DistanceMatrix;
            public int VehicleNumber;
            public int DepotIndex;
            public int[][] PickupsDeliveries;
            private readonly List<Stop> _stops;

            
            public DarpDataModel(int vehicleNumber, int depotId, int[][] pickupsDeliveries, List<Stop> stops)
            {
                _stops = stops;

                DistanceMatrixBuilder distanceMatrixBuilder = new DistanceMatrixBuilder();
                DistanceMatrix = distanceMatrixBuilder.Generate(_stops);
                VehicleNumber = vehicleNumber;
                DepotIndex = _stops.FindIndex(s => s.Id == depotId);
                PickupsDeliveries = pickupsDeliveries;
            }

            public DarpDataModel(int vehicleNumber, int depotId, int[][] pickupsDeliveries, long[][] initialRoutes, List<Stop> stops)
            {
                _stops = stops;

                DistanceMatrixBuilder distanceMatrixBuilder = new DistanceMatrixBuilder();
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
            Console.WriteLine(this.ToString() + "Pickup and deliveries:");
            foreach (var pickupDelivery in PickupsDeliveries)
                Console.WriteLine(GetStop(pickupDelivery[0]).Id+"->"+GetStop(pickupDelivery[1]).Id);
            }

            public override string ToString()
            {
                return "["+this.GetType().Name + "] " ;
            }

            public void PrintDistanceMatrix()
            {
                Console.WriteLine(this.ToString()+"Distance Matrix:");
                var counter = 0;
                foreach (var val in DistanceMatrix)
                {
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
}
