using System;
using System.Collections.Generic;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {
            TripStopsDataObject tripStopsDataObject = new TripStopsDataObject();
            var Trips = tripStopsDataObject.Trips;
            DirectedGraph<Stop, double> stopsGraph = new DirectedGraph<Stop, double>();
            stopsGraph.AddVertex(Trips[0].Stops);
            var ind = 0;
            var rnd = new Random();

            DistanceCalculator dc = new DistanceCalculator();
            foreach (var Stop in Trips[0].Stops)
            {
                if (ind < Trips[0].Stops.Count - 1)
                {
                    var nextStop = Trips[0].Stops[ind + 1];
                    var w = dc.CalculateDistance(Stop.Latitude, Stop.Longitude, nextStop.Latitude, nextStop.Longitude);
                    stopsGraph.AddEdge(Stop, nextStop, w);
                    stopsGraph.AddEdge(nextStop, Stop, w + rnd.Next(1, 16));
                }

                ind++;
            }
            var population = new List<Person>();
            for (int i = 0; i < 40; i++)
            {
                var c = new Customer();
                population.Add(c);
            }

         
            AbstractSimulation sim = new Simulation(tripStopsDataObject.Trips,stopsGraph);
            sim.Simulate();
            
            
            Console.Read();

        }
    }
}
