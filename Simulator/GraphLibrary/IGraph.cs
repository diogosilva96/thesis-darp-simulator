using System.Collections.Generic;

namespace Simulator.GraphLibrary
{
    public interface IGraph<T, K>
    {
        bool AddVertex(T vertex);//Adds a new vertex to the graph. if it does not exist already, returns true on sucess
        void AddVertex(IEnumerable<T> vertexSet);//Adds to the graph the vertices contained in the set. If a vertex already exists then it skips it
        bool DeleteVertex(T vertex);//Deletes the specified vertex if it exists, returns true on sucess.

        void DeleteVertex(IEnumerable<T> vertexSet);//Deletes all vertices in vertexset existing in the graph.

        bool AddEdge(T v1, T v2, K weight); //Adds an edge to the graph, starting from v1 and ending in v2, with the specified weight. Returns true on sucess.

        K GetWeight(T v1, T v2); // returns the weight of the edge starting from v1 and ending in v2.

        bool DeleteEdge(T v1, T v2); // Deletes edge from v1 to v2. Returns true on sucess.

        bool AreAdjacent(T v1, T v2); // returns true if v1 is adjacent to v2.
        int Degree(T vertex); // computes the degree of the specified vertex.

        int OutDegree(T vertex); // computes the outgoing degree of the specified vertex.

        int InDegree(T vertex); // computes the ingoing degree of the specified vertex.

        int VerticesNumber(); // Returns the number of vertices in the graph

        int EdgesNumber(); // Returns the number of edges in the graph

        IEnumerable<T> AdjacentVertices(T vertex); // returns a set of adjacent vertices to the vertex specified as argument

        IEnumerable<T> GetVertexSet();// returns the vertexSet of the graph

        IEnumerable<IPairValue<T>> GetEdgeSet(); // returns the edge set of the grouph represented by couples of vertices


    }

    public interface IPairValue<T>
    {
        T GetFirst();
        T GetSecond();
        bool Contains(T value);
    }
}
