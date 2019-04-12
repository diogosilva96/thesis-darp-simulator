using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;
using GraphLibrary;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {
            //StopsNetworkGraph stopsNetworkGraph = new StopsNetworkGraph();
            //DirectedGraph<Stop, double> dGraph = stopsNetworkGraph.StopsGraph;
            //MatrixBuilder matrixBuilder = new MatrixBuilder();
            //var distanceMatrix = matrixBuilder.BuildDistanceMatrix(dGraph);
            TripStopsDataObject tripStopsDataObject = new TripStopsDataObject();
            StopsNetworkGraph stopsNetworkGraph = new StopsNetworkGraph(tripStopsDataObject,true);
            stopsNetworkGraph.LoadGraph();

            //var Trips = tripStopsDataObject.Trips;
            //DirectedGraph<Stop, double> stopsGraph = new DirectedGraph<Stop, double>();
            //stopsGraph.AddVertex(Trips[0].Stops);
            //var ind = 0;
            //var rnd = new Random();

            //DistanceCalculator dc = new DistanceCalculator();
            //foreach (var Stop in Trips[0].Stops)
            //{
            //    if (ind < Trips[0].Stops.Count - 1)
            //    {
            //        var nextStop = Trips[0].Stops[ind + 1];
            //        var w = dc.CalculateDistance(Stop.Latitude, Stop.Longitude, nextStop.Latitude, nextStop.Longitude);
            //        stopsGraph.AddEdge(Stop, nextStop, w);
            //        stopsGraph.AddEdge(nextStop, Stop, w + rnd.Next(1, 16));
            //    }

            //    ind++;
            //}



         
            AbstractSimulation sim = new Simulation(tripStopsDataObject.Routes,stopsNetworkGraph.StopsGraph);
            sim.Simulate();
            
            
            Console.Read();

        }
    }
}
