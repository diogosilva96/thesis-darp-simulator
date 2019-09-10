using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.PDTW;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    class SearchAlgorithmTester:AlgorithmTester
    {
        public SearchAlgorithmTester(PdtwDataModel dataModel,LocalSearchMetaheuristic.Types.Value algorithmValue,int searchTimeLimitInSecondsInSeconds) : base(dataModel)
        {
            AlgorithmValue = algorithmValue;
            SearchTimeLimitInSeconds = searchTimeLimitInSecondsInSeconds;
            Type = "Search Algorithm";
            Name = algorithmValue.ToString();
        }

        public override Assignment TryGetSolutionHookMethod(int maxUpperBound)
        {
            Assignment solution = null;
            if (AlgorithmValue is LocalSearchMetaheuristic.Types.Value localSearchAlgorithm)
            {
                solution = Solver.TryGetSolutionWithSearchStrategy(DataModel, maxUpperBound,
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
