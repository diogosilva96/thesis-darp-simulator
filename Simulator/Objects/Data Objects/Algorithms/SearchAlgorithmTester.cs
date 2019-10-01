using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.DARP;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    class SearchAlgorithmTester:AlgorithmTester
    {
        public SearchAlgorithmTester(DarpDataModel dataModel,bool allowDropNodes,int maxAllowedRideDurationMultiplier,LocalSearchMetaheuristic.Types.Value algorithmValue,int searchTimeLimitInSecondsInSeconds) : base(dataModel,allowDropNodes,maxAllowedRideDurationMultiplier)
        {
            AlgorithmValue = algorithmValue;
            SearchTimeLimitInSeconds = searchTimeLimitInSecondsInSeconds;
            Type = "Search Algorithm";
            Name = algorithmValue.ToString();
        }

        public override Assignment TryGetSolutionHookMethod()
        {
            Assignment solution = null;
            if (AlgorithmValue is LocalSearchMetaheuristic.Types.Value localSearchAlgorithm)
            {
                solution = Solver.TryGetSolutionWithSearchStrategy(DataModel,
                    SearchTimeLimitInSeconds, localSearchAlgorithm);
            }
            else
            {
                throw new ArgumentException("algorithm value is invalid");
            }
            return solution;
        }
    }
}
