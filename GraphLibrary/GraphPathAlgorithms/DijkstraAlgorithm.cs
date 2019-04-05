using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GraphLibrary.GraphLibrary;
using GraphLibrary.Objects;

namespace GraphLibrary.GraphPathAlgorithms
{
    class DijkstraAlgorithm
    {
        private readonly string _classDescriptor;

        public DijkstraAlgorithm()
        {
            _classDescriptor = "[" + GetType() + "] ";
        }
        public List<int> DijkstraPath(DirectedGraph<Stop,double> graph, int originVertex, int destinationVertex)
        {
            var n = graph.VerticesNumber();

            var distance = new int[n];
            for (int i = 0; i < n; i++)
            {
                distance[i] = int.MaxValue;
            }

            distance[originVertex] = 0;

            var used = new bool[n];
            var previous = new int?[n];
            var watch = Stopwatch.StartNew();
            Console.WriteLine(_classDescriptor+"Started calculating shortest path from vertex "+ originVertex +" to "+destinationVertex);

            while (true)
            {
                var minDistance = int.MaxValue;
                var minNode = 0;
                for (int i = 0; i < n; i++)
                {
                    if (!used[i] && minDistance > distance[i])
                    {
                        minDistance = distance[i];
                        minNode = i;
                    }
                }

                if (minDistance == int.MaxValue)
                {
                    break;
                }

                Stop stopMinNode = null;
                Stop stopI = null;
                used[minNode] = true;
                for (int i = 0; i < n; i++)
                {
                    bool foundMinNode = false;
                    bool foundI = false;
                    foreach (var vertStop in graph.GetVertexSet())
                    {
                        if (vertStop.Id == minNode)
                        {
                            stopMinNode = vertStop;
                            foundMinNode = true;
                        }

                        if (vertStop.Id == i)
                        {
                            stopI = vertStop;
                            foundI = true;
                        }

                        if (foundMinNode && foundI)
                        {
                            goto FoundValues;
                        }
                    }
                    FoundValues:
                    if (stopMinNode != null && stopI != null)
                    {
                        if (graph.AreAdjacent(stopMinNode, stopI))
                        {
                            var shortestToMinNode = distance[minNode];
                            var distanceToNextNode = graph.GetWeight(stopMinNode, stopI);
                            var totalDistance = shortestToMinNode + distanceToNextNode;

                            if (totalDistance < distance[i])
                            {
                                distance[i] = Convert.ToInt32(totalDistance);
                                previous[i] = minNode;
                            }
                        }
                    }
                }
            }

            if (distance[destinationVertex] == int.MaxValue)
            {
                return null;
            }

            var path = new LinkedList<int>();
            int? currentNode = destinationVertex;
            while (currentNode != null)
            {
                path.AddFirst(currentNode.Value);
                currentNode = previous[currentNode.Value];
                


            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var seconds = elapsedMs * 0.001;
            Console.WriteLine(_classDescriptor + "Shortest path from vertex " + originVertex + " to " + destinationVertex+" successfully calculated in " +seconds+ " seconds.");
            return path.ToList();
        }

        public string DijkstraPathToString(List<int> pathList)
        {
            string path;
            if (pathList == null)
            {
                path = "No path";
            }
            else
            {
                string pathString = "";
                bool firstLine = true;
                foreach (var vert in pathList)
                {
                    if (firstLine)
                    {
                        pathString = vert.ToString();
                        firstLine = false;
                    }
                    else
                    {
                        pathString = pathString + "->" + vert.ToString();
                    }

                }

                path = pathString;
            }

            return path;
        }

        public int minDistance(int[] dist, bool[] sptSet, int TotalVertices)
        {

            // Initialize min value 
            int min = int.MaxValue;
            int min_index = -1;

            for (int v = 0; v < TotalVertices; v++)
                if (sptSet[v] == false && dist[v] <= min)
                {
                    min = dist[v];
                    min_index = v;
                }

            return min_index;

        }

        void printSolution(int[] dist, int TotalVertices,int src)// for dijkstra algorithm
        {
            Console.Write("Vertex shortest Distance "
                          + "from source (vertex "+src+") \n");
            for (int i = 0; i < TotalVertices; i++)
                if (dist[i] != int.MaxValue)
                {
                    Console.Write(i + " \t\t " + dist[i] + "\n");
                }
                else
                {
                    Console.Write(i + " \t\t " +"No Path"+ "\n");
                }
        }

        public void dijkstra(DirectedGraph<string, int> graph, int src, int TotalVertices) //get minimum cost to all vertices
        {
            IPairValue<int> pv;
            List<IPairValue<int>> Path = new List<IPairValue<int>>();
            int[] dist = new int[TotalVertices]; // The output array. dist[i] 
            // will hold the shortest 
            // distance from src to i 

            // sptSet[i] will true if vertex 
            // i is included in shortest path 
            // tree or shortest distance from 
            // src to i is finalized 
            bool[] sptSet = new bool[TotalVertices];

            // Initialize all distances as 
            // INFINITE and stpSet[] as false 
            for (int i = 0; i < TotalVertices; i++)
            {
                dist[i] = int.MaxValue;
                sptSet[i] = false;
            }

            // Distance of source vertex 
            // from itself is always 0 
            dist[src] = 0;

            // Find shortest path for all vertices 
            for (int count = 0; count < TotalVertices - 1; count++)
            {
                // Pick the minimum distance vertex 
                // from the set of vertices not yet 
                // processed. u is always equal to 
                // src in first iteration. 
                int u = minDistance(dist, sptSet, TotalVertices);

                // Mark the picked vertex as processed 
                sptSet[u] = true;
                
                // Update dist value of the adjacent 
                // vertices of the picked vertex. 
                for (int v = 0; v < TotalVertices; v++)
                {
                    IEnumerable<IPairValue<string>> edgeSet = graph.GetEdgeSet();

                    foreach (var edge in edgeSet)
                    {

                        if (int.Parse(edge.GetFirst()) == u && int.Parse(edge.GetSecond()) == v)
                            if (!sptSet[v] && dist[u] != int.MaxValue && dist[u] != int.MaxValue &&
                                dist[u] + graph.GetWeight(edge.GetFirst(), edge.GetSecond()) < dist[v])
                            {
                                dist[v] = dist[u] + graph.GetWeight(edge.GetFirst(), edge.GetSecond());
                                pv = new PairValue<int>(int.Parse(edge.GetFirst()), int.Parse(edge.GetSecond()));
                                Path.Add(pv);

                            }
                    }

                }
            }
            // print the constructed distance array                
            printSolution(dist, TotalVertices,src);
            foreach (var p in Path)
            {
                Console.WriteLine(p.GetFirst() +"->"+ p.GetSecond());
            }
            {
                
            }       
                
        }
    }
}

