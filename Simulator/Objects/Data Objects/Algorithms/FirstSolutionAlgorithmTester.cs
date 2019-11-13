using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Simulator.Objects.Data_Objects.Routing;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    class FirstSolutionAlgorithmTester:AlgorithmTester
    {
        public FirstSolutionAlgorithmTester(RoutingDataModel dataModel, bool allowDropNodes, FirstSolutionStrategy.Types.Value algorithmValue) : base(dataModel, allowDropNodes)
        {
            AlgorithmValue = algorithmValue;
            Type = "First Solution Algorithm";
            Name = algorithmValue.ToString();
        }

        public override Assignment GetSolution()
        {
            Assignment solution = null;
            if (AlgorithmValue is FirstSolutionStrategy.Types.Value firstSolutionAlgorithm)
            {
                RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.FirstSolutionStrategy = firstSolutionAlgorithm;
                //searchParameters.TimeLimit = new Duration { Seconds = SearchTimeLimitInSeconds };
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
