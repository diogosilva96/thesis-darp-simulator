using System;
using System.Collections.Generic;
using Simulator.GraphLibrary;

namespace GraphLibrary.GraphLibrary
{
    public abstract class AbstractGraph<T, K> : IGraph<T, K>
    {
        protected readonly List<T> VertexSet = new List<T>();
        protected readonly List<IPairValue<T>> EdgeSet = new List<IPairValue<T>>();
        protected readonly Dictionary<IPairValue<T>,K> Weights = new Dictionary<IPairValue<T>, K>();

        public bool AddVertex(T vertex)
        {
            if (vertex == null)
                throw new ArgumentNullException();
            if (VertexSet.Contains(vertex))
                return false;
            VertexSet.Add(vertex);
            return true;
        }

        public void AddVertex(IEnumerable<T> vertexSet)
        {
            if (vertexSet == null)
                throw new ArgumentNullException();
            using (var iterator = vertexSet.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    if (iterator.Current != null && !VertexSet.Contains(iterator.Current))//If the vertex isn't in the VertexSet of the graph, add it
                        VertexSet.Add(iterator.Current);
                }
            }
        }

        public abstract IEnumerable<T> AdjacentVertices(T vertex);

        public abstract bool AreAdjacent(T v1, T v2);

        public abstract bool AddEdge(T v1, T v2, K weight);
        public abstract K GetWeight(T v1, T v2);
        public abstract bool DeleteEdge(T v1, T v2);

        public bool DeleteVertex(T vertex)
        {
            if (vertex == null)
                throw new ArgumentNullException();
            if (!VertexSet.Contains(vertex)) //If it isn't in Vertex set returns false, otherwise removes the vertex and returns true.
                return false;
            VertexSet.Remove(vertex);
            return true;
        }

        public void DeleteVertex(IEnumerable<T> vertexSet)
        {
            if (vertexSet == null)
                throw new ArgumentNullException();
            using (var iterator = vertexSet.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    if (iterator.Current != null)
                        VertexSet.Remove(iterator.Current); //Remove o vertex atual (vertexSet) do VertexSet
                }
            }
        }

        public int VerticesNumber()
        {
            return VertexSet.Count;
        }

        public int EdgesNumber()
        {
            return EdgeSet.Count;
        }

        public IEnumerable<IPairValue<T>> GetEdgeSet()
        {
            return EdgeSet;
        }

        public IEnumerable<T> GetVertexSet()
        {
            return VertexSet;
        }

        public abstract int Degree(T vertex);

        public abstract int InDegree(T vertex);

        public abstract int OutDegree(T vertex);

    }
}
