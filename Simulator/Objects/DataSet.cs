using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;

namespace Simulator.Objects
{
    public class DataSet
    {
        private string Path;
        public List<Stop> Stops;
        public List<Customer> Customers;
        public double[,] Coordinates;
        public int[] VehicleCapacities;
        public int[] Demands;
        public long[,] TimeWindows;//in minutes

        private int TotalPickups => Demands != null ? Array.FindAll(Demands, d => d > 0).Length:0;
        private int TotalDeliveries => Demands != null ? Array.FindAll(Demands, d => d < 0).Length : 0;


        public DataSet(string path)
        {
            Path = path;
            ImportData(path);
            GenerateDatasetCustomers();
        }

        public void PrintDataInfo()
        {
            Console.WriteLine("---------------------------");
            Console.WriteLine("|   Dataset Information   |");
            Console.WriteLine("---------------------------");
            Console.WriteLine("Dataset file path: "+Path);
            Console.WriteLine("Total Stops: "+Stops.Count);
            Console.WriteLine("Total Customers: "+Customers.Count);
        }

        public void PrintDistances()
        {
            foreach (var stopO in Stops)
            {
                foreach (var stopD in Stops)
                {
                    var euclideanDistance = DistanceCalculator.CalculateEuclideanDistance(stopO.Latitude,stopO.Longitude,stopD.Latitude,stopD.Longitude)*1000;//distance in meters
                    var havDistance = DistanceCalculator.CalculateHaversineDistance(stopO.Latitude, stopO.Longitude,
                        stopD.Latitude, stopD.Longitude);
                    var speed = 40;
                    var eucTravelTime = DistanceCalculator.DistanceToTravelTime(speed, euclideanDistance);
                    var havTravelTime = DistanceCalculator.DistanceToTravelTime(speed,havDistance);
                    Console.WriteLine(stopO.Id +" -> "+stopD.Id+ " - euclideanDistance: "+ euclideanDistance+"; haversineDistance:"+havDistance+"; speed = "+speed+ " ; eucTT: "+eucTravelTime+ " ; havTT: "+havTravelTime);
                    Console.WriteLine();
                }
            }
        }

        public void PrintTimeWindows()
        {

            for (int i =0;i<TimeWindows.GetLength(0);i++)
            {
                Console.WriteLine(Stops[i] + " - T("+TimeWindows[i,0]+","+TimeWindows[i,1]+")");
            }
        }
        private void GenerateDatasetCustomers()
        {
            Customers = new List<Customer>();
            List<Stop> stopsAlreadyChosen = new List<Stop>();
            var rng = RandomNumberGenerator.Random;
            while (Customers.Count != TotalPickups)
            {
                generateRandomPickupLabel:
                var randomPickupStop = rng.Next(Demands.Length);
                var pickupIndex = 0;
                if (!stopsAlreadyChosen.Contains(Stops[randomPickupStop]))
                {
                    if (Demands[randomPickupStop] > 0)
                    {
                        pickupIndex = randomPickupStop;
                        stopsAlreadyChosen.Add(Stops[randomPickupStop]);
                    }

                    if (Demands[randomPickupStop] == 0)
                    {
                        stopsAlreadyChosen.Add(Stops[randomPickupStop]);
                    }

                    if (Demands[randomPickupStop] < 0)
                    {
                        goto generateRandomPickupLabel;
                    }
                }
                else
                {
                    goto generateRandomPickupLabel;
                }

                generateRandomDeliveryLabel:
                var randomDeliveryStop = rng.Next(Demands.Length);
                var deliveryIndex = 0;
                if (!stopsAlreadyChosen.Contains(Stops[randomDeliveryStop]))
                {
                    if (Demands[randomDeliveryStop] > 0)
                    {
                        goto generateRandomDeliveryLabel;
                    }

                    if (Demands[randomDeliveryStop] == 0)
                    {
                        stopsAlreadyChosen.Add(Stops[randomDeliveryStop]);
                    }

                    if (Demands[randomDeliveryStop] < 0)
                    {
                        stopsAlreadyChosen.Add(Stops[randomDeliveryStop]);
                        deliveryIndex = randomDeliveryStop;
                    }
                }
                else
                {
                    goto generateRandomDeliveryLabel;
                }

                var pickupDelivery = new Stop[] {Stops[pickupIndex], Stops[deliveryIndex]};
                var timeWindows = new long[] {TimeWindows[pickupIndex, 0], TimeWindows[deliveryIndex, 1]};
                var customer = new Customer(pickupDelivery, timeWindows, 0,false);
                Customers.Add(customer);
            }
        }
        private void ImportData(string path)
        {
            string line;
            StreamReader file = new StreamReader(path);
            var arraySize = 7; //number of file data to read
            List<string[]> dataList = new List<string[]>();
            bool firstLine = true;
            while ((line = file.ReadLine()) != null)
            {
                var lineSplit = line.Split(" ");
                int index = 0;
                if (firstLine)
                {
                    //doesnt read first line
                    firstLine = false;
                }
                else
                {
                    string[] stringArray = new string[arraySize];
                    foreach (var split in lineSplit)
                    {
                        if (split != "")
                        {
                            stringArray[index] = split;
                            index++;
                        }
                    }
                    dataList.Add(stringArray);
                }
            }
            file.Close();
            var currentIndex = 0;

            Stops = new List<Stop>();
            Coordinates = new double[dataList.Count,2];
            VehicleCapacities = new int[dataList.Count];
            Demands = new int[dataList.Count];
            TimeWindows = new long[dataList.Count,2];
            foreach (var data in dataList)
            {
                Coordinates[currentIndex, 0] = double.Parse(data[1]);
                Coordinates[currentIndex, 1] = double.Parse(data[2]);
                var stop = new Stop(20000 + currentIndex, "", "", Coordinates[currentIndex, 0],
                    Coordinates[currentIndex, 1]);
                Stops.Add(stop);
                VehicleCapacities[currentIndex] = int.Parse(data[3]);
                Demands[currentIndex] = int.Parse(data[4]);
                TimeWindows[currentIndex, 0] = long.Parse(data[5])*100;
                TimeWindows[currentIndex, 1] = long.Parse(data[6])*100;
                currentIndex++;
            }
        }

    }
}
