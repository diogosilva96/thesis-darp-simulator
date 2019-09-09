using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;

namespace Simulator.Objects.Data_Objects.PDTW
{
    public class AlgorithmTester
    {
        private List<FirstSolutionStrategy.Types.Value> _firstSolutionAlgorithms;
        private List<LocalSearchMetaheuristic.Types.Value> _searchStrategyAlgorithms;
        public PdtwDataModel DataModel;
        public AlgorithmTester(PdtwDataModel dataModel)
        {
           InitFirstSolutionList();
           InitSearchList();
           DataModel = dataModel;
        }

        private void InitFirstSolutionList()
        {
           _firstSolutionAlgorithms = new List<FirstSolutionStrategy.Types.Value>();

           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathCheapestArc);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.Automatic);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathCheapestArc);
           _firstSolutionAlgorithms.Add(FirstSolutionStrategy.Types.Value.PathMostConstrainedArc);
           //...
        }

        private void InitSearchList()
        {
            _searchStrategyAlgorithms = new List<LocalSearchMetaheuristic.Types.Value>();
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GenericTabuSearch);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GreedyDescent);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.SimulatedAnnealing);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.TabuSearch);
        }

        public Dictionary<LocalSearchMetaheuristic.Types.Value,string> GetSearchStrategiesResults()
        {
            Dictionary<LocalSearchMetaheuristic.Types.Value,string> searchDictionary = new Dictionary<LocalSearchMetaheuristic.Types.Value, string>();
            //TEST ALL the criterions (performance, time to compute, solution cost, etc)
            foreach (var searchStrategy in _searchStrategyAlgorithms)
            {
                //test the strategy and save results
            }
            return searchDictionary;
        }


    }
}
