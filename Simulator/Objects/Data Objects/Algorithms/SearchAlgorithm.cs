using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.Routing;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    class SearchAlgorithm:Algorithm
    {
        public SearchAlgorithm(LocalSearchMetaheuristic.Types.Value algorithmValue,int searchTimeLimitInSecondsInSeconds)
        {
            AlgorithmValue = algorithmValue;
            SearchTimeLimitInSeconds = searchTimeLimitInSecondsInSeconds;
            Type = "Search Algorithm";
            Name = algorithmValue.ToString();
        }

        public override Assignment GetSolution()
        {
            Assignment solution = null;
            if (AlgorithmValue is LocalSearchMetaheuristic.Types.Value localSearchAlgorithm)
            {
                RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.LocalSearchMetaheuristic= localSearchAlgorithm;
                searchParameters.TimeLimit = new Duration {Seconds = SearchTimeLimitInSeconds};
                solution = Solver.TryGetSolution(searchParameters);
            }
            else
            {
                throw new ArgumentException("algorithm value is invalid");
            }
            return solution;
        }
    }
}
