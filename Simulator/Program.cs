using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using GraphLibrary.GraphLibrary;
using MathNet.Numerics.Properties;
using Simulator.Objects;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using GraphLibrary;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using Simulator.Objects.Data_Objects;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {
            //StopsNetworkGraph stopsNetworkGraph = new StopsNetworkGraph();
            //DirectedGraph<Stop, double> dGraph = stopsNetworkGraph.StopsGraph;
            //DistanceMatrixBuilder matrixBuilder = new DistanceMatrixBuilder();
            //var distanceMatrix = matrixBuilder.Build(dGraph);


            //var _trips = tripStopsDataObject._trips;
            //DirectedGraph<Stop, double> stopsGraph = new DirectedGraph<Stop, double>();
            //stopsGraph.AddVertex(_trips[0].Stops);
            //var ind = 0;
            //var rnd = new Random();

            //HaversineDistanceCalculator dc = new HaversineDistanceCalculator();
            //foreach (var Stop in _trips[0].Stops)
            //{
            //    if (ind < _trips[0].Stops.Count - 1)
            //    {
            //        var nextStop = _trips[0].Stops[ind + 1];
            //        var w = dc.Calculate(Stop.Latitude, Stop.Longitude, nextStop.Latitude, nextStop.Longitude);
            //        stopsGraph.AddEdge(Stop, nextStop, w);
            //        stopsGraph.AddEdge(nextStop, Stop, w + rnd.Next(1, 16));
            //    }

            //    ind++;
            //}





            //AbstractSimulation sim = new Simulation();
            //sim.Simulate();
            //Console.Read();

            var RoutesDataObject = new RoutesDataObject(true);
            var stopsNetworkGraph = new StopsNetworkGraphLoader(RoutesDataObject.Stops, RoutesDataObject.Routes);
            
            DistanceMatrixBuilder distanceMatrixBuilder = new DistanceMatrixBuilder();
            Random rand = new Random();
            var PlacesToStop = new List<Stop>();
            Console.WriteLine("Selected stops:");
            for (int index = 0; index < 100; index++)
            {
                label:
                var stop = RoutesDataObject.Stops[rand.Next(0, RoutesDataObject.Stops.Count)];
                if (PlacesToStop.Contains(stop))
                {
                   goto label;
                }
                PlacesToStop.Add(stop);
                Console.WriteLine(stop.Id);
            }


            var distanceMatrix = distanceMatrixBuilder.Build(PlacesToStop);

            //Distance matrix printer
            var counter = 0;
            foreach (var val in distanceMatrix)
            {
                if (counter == PlacesToStop.Count-1)
                {
                    counter = 0;
                    Console.WriteLine(val+ " ");
                }
                else
                {
                    Console.Write(val + " ");
                    counter++;
                }
            }


            //GOOGLE OR TOOLS
            // Instantiate the data problem.
        VrpGlobalSpan.DataModel data = new VrpGlobalSpan.DataModel(distanceMatrix,2,0);
        

            // Create Routing Index Manager
            RoutingIndexManager manager = new RoutingIndexManager(
                data.DistanceMatrix.GetLength(0),
                data.VehicleNumber,
                data.Depot);


            // Create Routing Model.
            RoutingModel routing = new RoutingModel(manager);

            // Create and register a transit callback.
            int transitCallbackIndex = routing.RegisterTransitCallback(
                (long fromIndex, long toIndex) => {
                    // Convert from routing variable Index to distance matrix NodeIndex.
                    var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);
                    return data.DistanceMatrix[fromNode, toNode];
                }
            );

            // Define cost of each arc.
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // Add Distance constraint.
            routing.AddDimension(transitCallbackIndex, 0, 99999999, 
                true,  // start cumul to zero
                "Distance");
            RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
            distanceDimension.SetGlobalSpanCostCoefficient(100);

            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy =
                FirstSolutionStrategy.Types.Value.PathCheapestArc;

            // Solve the problem.
            Assignment solution = routing.SolveWithParameters(searchParameters);

            // Print solution on console.
            VrpGlobalSpan.PrintSolution(data, routing, manager, solution);
        }
    }
}
