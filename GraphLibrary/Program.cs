using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using GraphLibrary.GraphLibrary;
using GraphLibrary.GraphPathAlgorithms;
using GraphLibrary.Objects;

namespace GraphLibrary
{
    public class Program
    {

        
        static void Main(string[] args)
        {
            TripStopsDataObject tripStopsDataObject = new TripStopsDataObject();
            StopsNetworkGraph stopsNetworkGraph = new StopsNetworkGraph(tripStopsDataObject,false);
            DirectedGraph<Stop, double> dGraph = stopsNetworkGraph.StopsGraph;
         
            //DijkstraAlgorithm dA = new DijkstraAlgorithm();
            //int o_vert = 1515;
            //int d_vert = 1964;
            //var pList = dA.DijkstraPath(dGraph, o_vert, d_vert);
            //var shortestPath = dA.DijkstraPathToString(pList);
            //Console.WriteLine("Path from " + o_vert + " to " + d_vert + ":");
            //Console.WriteLine(shortestPath);

            //int v_Nr;
            //bool stopcondition = false;
            //while (!stopcondition)
            //{
            //    Console.WriteLine("-------------------------------------------------------------");
            //    Console.WriteLine("Write a vertex number:");
            //    if (!int.TryParse(Console.ReadLine(), out v_Nr))
            //    {
            //        Console.WriteLine("Error parsing to int");
            //        stopcondition = true;
            //    }

            //    Stop chosen_vert = null;
            //    var ver_Set = dGraph.GetVertexSet();
            //    foreach (var vert in ver_Set)
            //    {
            //        if (vert.Id == v_Nr)
            //        {
            //            chosen_vert = vert;
            //        }
            //    }

            //    if (chosen_vert != null)
            //    {
            //        int InDeg = dGraph.InDegree(chosen_vert);
            //        int OutDeg = dGraph.OutDegree(chosen_vert);
            //        int Deg = dGraph.Degree(chosen_vert);
            //        Console.WriteLine("Number of vertices with vertex " + v_Nr + " as origin (inbound): " + InDeg);
            //        Console.WriteLine(
            //            "Number of vertices with vertex " + v_Nr + " as destination (outbound): " + OutDeg);
            //        Console.WriteLine("Number of vertices with vertex " + v_Nr + " either as inbound or outbound: " +
            //                          Deg);
            //        dGraph.PrintEdges(chosen_vert);
            //    }
            //}


            DirectedGraph<Stop,double> dirGraph = new DirectedGraph<Stop,double>();
           List<Stop> vertexList = new List<Stop>();
           Random rnd = new Random();
           for (int i = 0; i <= 5; i++) // Generates 5 vertices
           {
               Stop stop = new Stop(i, i + "ABC", "Name " + i, "Descript", rnd.NextDouble(), rnd.NextDouble());
                vertexList.Add(stop);
           }

           dirGraph.AddVertex(vertexList);
           foreach (var vertexOrigin in dirGraph.GetVertexSet()) // Generates at the very least 1 random edge for each vertex with random weight(cost) and has 50% chance to insert more vertices
           {
            
               foreach (var vertexDestination in dirGraph.GetVertexSet())
               {

                   double Cost;
                   double prob;
                   prob = rnd.NextDouble();
                   Cost = rnd.Next(1, 11);
                   if (prob <= 1 && vertexDestination != vertexOrigin) //If prob is lower or equal to 0.33, inserts another new edge between vertex and vertexDestination
                   {

                       dirGraph.AddEdge(vertexOrigin, vertexDestination, Cost);
                       Debug.WriteLine("Edge added: " + vertexOrigin + "->" + vertexDestination.ToString());
                   }
               }
           }
           dirGraph.PrintGraph();
           DijkstraAlgorithm da = new DijkstraAlgorithm();
           int v_origin;
           int v_destination;
           bool stopcondition = false;
           while (!stopcondition)
           {
               Console.WriteLine("-------------------------------------------------------------");
               Console.WriteLine("Write a vertex origin number:");
               if (!int.TryParse(Console.ReadLine(), out v_origin))
               {
                   Console.WriteLine("Error parsing to int");
                   stopcondition = true;
               }
               var ver_Set = dirGraph.GetVertexSet();
            
               Console.WriteLine("Write a vertex destination number:");
               if (!int.TryParse(Console.ReadLine(), out v_destination))
               {
                   Console.WriteLine("Error parsing to int");
                   stopcondition = true;
               }
            
                var pathList = da.DijkstraPath(dirGraph, v_origin, v_destination);
                if (pathList == null)
                {
                    Console.WriteLine("No path");
                }
                else
                {
                    int cost = 0;
                    Stop currentVert = null;
                    Stop nextVert = null;
                    int Index = 0;
                    foreach (var vert in pathList)
                    {
                        if (Index < pathList.Count - 1)
                        {
                            foreach (var stop in dirGraph.GetVertexSet())
                            {
                                if (stop.Id == vert)
                                {
                                    currentVert = stop;
                                }

                                if (stop.Id == pathList[Index + 1])
                                {
                                    nextVert = stop;
                                }
                            }

                            cost = Convert.ToInt32(dirGraph.GetWeight(currentVert, nextVert))+cost;
                        }
                        
                        Index++;
                    }

                    string path = da.DijkstraPathToString(pathList);
                    Console.WriteLine(path+" - total cost: "+cost);;
                }

            }
            

            DijkstraAlgorithm pc = new DijkstraAlgorithm();
           //var shortestPath = pc.ShortestPathBFS(dirGraph, 0.ToString());
           //foreach (var v in dirGraph.GetVertexSet())
           //{
           //    Console.WriteLine("shortest path to {0,2}: {1}",
           //        v, string.Join(", ", shortestPath(v)));
           // }
           // pc.dijkstra(dirGraph,0,dirGraph.VerticesNumber());
           
          
        }
   
    }
}
