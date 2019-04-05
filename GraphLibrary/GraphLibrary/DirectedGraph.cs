using System;
using System.Collections.Generic;

namespace GraphLibrary.GraphLibrary
{
    public class DirectedGraph<T, K> : AbstractGraph<T, K>
    {
        public void PrintGraph()
        {
            Console.WriteLine("All Edges:");
            foreach (var p in EdgeSet)
            {
                Console.WriteLine("Edge: " + p.GetFirst() + "->" + p.GetSecond() + " - Cost:" + GetWeight(p.GetFirst(), p.GetSecond()));
            }
            Console.WriteLine("Graph total vertices:"+VerticesNumber()+", total edges:"+EdgesNumber());
        }

        public void PrintEdges(T vertex)
        {
            var AdjVert = AdjacentVertices(vertex); //returns all vertices with the specified vertex as origin
            foreach (var out_vert in AdjVert)
            {
                Console.WriteLine("Edge: "+vertex.ToString() +"->"+ out_vert.ToString()+" - weight: "+GetWeight(vertex,out_vert));   
            }

            var OutAdjVert = OutAdjacentVertices(vertex); // returns all vertices with specified vertex as destination
            foreach (var in_vert in OutAdjVert)
            {
                Console.WriteLine("Edge: " + in_vert.ToString() + "->" + vertex.ToString() + " - weight: " + GetWeight(in_vert, vertex));
            }
        }
        public override bool AddEdge(T v1, T v2, K weight)
        {
            if (v1 == null || v2 == null || weight == null)
                throw new ArgumentNullException();
            if (!VertexSet.Contains(v1) || !VertexSet.Contains(v2))
                return false;
            IPairValue<T> pairValue = new PairValue<T>(v1, v2);
            if (EdgeSet.Contains(pairValue)
            ) //If EdgeSet already contains PairValue returns False, otherwise adds it to the EdgeSet and assigns its weight.
                return false;
            EdgeSet.Add(pairValue);
            Weights[pairValue] = weight;
            return true;
        }

        public override K GetWeight(T v1, T v2) // returns the weight value
        {
            if (v1 == null || v2 == null)
                throw new ArgumentNullException();
            IPairValue<T> pairValue = new PairValue<T>(v1, v2);
            if (!Weights.ContainsKey(pairValue))
                throw new ArgumentException();
            return Weights[pairValue];
        }

        public override bool DeleteEdge(T v1, T v2)
        {
            if (v1 == null || v2 == null)
                throw new ArgumentNullException();
            IPairValue<T> pairValue = new PairValue<T>(v1, v2);
            if (EdgeSet.Contains(pairValue) && Weights.ContainsKey(pairValue))
            {
                EdgeSet.Remove(pairValue);
                Weights.Remove(pairValue);
                return true;
            }

            return false;
        }

        public override bool AreAdjacent(T v1, T v2) //Returns true if v1 and v2 are adjacent
        {
            if (v1 == null || v2 == null)
                throw new ArgumentNullException();
            if (!VertexSet.Contains(v1) || !VertexSet.Contains(v2))
                throw new ArgumentException();
            return EdgeSet.Contains(new PairValue<T>(v1, v2));
        }

        public override IEnumerable<T> AdjacentVertices(T vertex) // returns all adjacent vertices to vertex (the vertices which have the specified vertex as inbound (origin)).
        {
            foreach (IPairValue<T> pairValue in EdgeSet)
            {
                if (pairValue.GetFirst().Equals(vertex))
                    yield return pairValue.GetSecond();
            }
        }

        public IEnumerable<T> OutAdjacentVertices(T vertex) // returns all adjacent vertices that has the specified vertex has outbound(destination)
        {
            foreach (IPairValue<T> pairValue in EdgeSet)
            {
                if (pairValue.GetSecond().Equals(vertex))
                    yield return pairValue.GetFirst();
            }
        }

        public override int Degree(T vertex) // returns the sum of the inbound and outbound degrees
        {
            if (vertex == null)
                throw new ArgumentNullException();
            if (!VertexSet.Contains(vertex))
                throw new ArgumentException();
            return InDegree(vertex) + OutDegree(vertex);
        }

        public override int InDegree(T vertex) // returns the number of vertices that has the speficied vertex as an inbound (origin)
        {
            if (vertex == null)
                throw new ArgumentNullException();
            if (!VertexSet.Contains(vertex))
                throw new ArgumentException();
            int counter = 0;
            foreach (var pairValue in EdgeSet)
            {
                if (pairValue.GetFirst().Equals(vertex))
                    counter++;
            }

            return counter;
        }

        public override int OutDegree(T vertex) // Returns the number of vertices that has the specified vertex as an outbound (destination)
        {
            if (vertex == null)
                throw new ArgumentNullException();
            if (!VertexSet.Contains(vertex))
                throw new ArgumentException();
            int counter = 0;
            foreach (var pairValue in EdgeSet)
            {
                if (pairValue.GetSecond().Equals(vertex))
                    counter++;
            }

            return counter;
        }
    }
}
