using System;
using System.Collections.Generic;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.DARP;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    public class AlgorithmStatistics
    {
        private List<FirstSolutionStrategy.Types.Value> _firstSolutionAlgorithms;
        private List<LocalSearchMetaheuristic.Types.Value> _searchStrategyAlgorithms;
        public DarpDataModel DataModel;

        public override string ToString()
        {
            return "["+this.GetType().Name+"]";
        }

        public AlgorithmStatistics(DarpDataModel dataModel)
        {
           InitFirstSolutionList();
           InitSearchStrategyList();
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

        private void InitSearchStrategyList()
        {
            _searchStrategyAlgorithms = new List<LocalSearchMetaheuristic.Types.Value>();
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GenericTabuSearch);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GreedyDescent);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.SimulatedAnnealing);
            _searchStrategyAlgorithms.Add(LocalSearchMetaheuristic.Types.Value.TabuSearch);
        }

        public List<AlgorithmTester> GetSearchAlgorithmsResultsList(int searchTimeLimitInSeconds,bool allowDropNodes)
        {
            List<AlgorithmTester> testedAlgorithmsList = new List<AlgorithmTester>();
            //TEST ALL the criterions (performance, time to compute, solution cost, etc)
            foreach (var searchStrategy in _searchStrategyAlgorithms)
            {
                AlgorithmTester algorithmTester= new SearchAlgorithmTester(DataModel,allowDropNodes,searchStrategy,10);
                algorithmTester.Test();
                testedAlgorithmsList.Add(algorithmTester); //adds it to the list
                
            }
            return testedAlgorithmsList;
        }

        public List<string> GetPrintableStatisticsList(List<AlgorithmTester> testedAlgorithms)
        {
            List<string> toPrintList = new List<string>();
            var customers = DataModel.IndexManager.Customers;
            var vehicles = DataModel.IndexManager.Vehicles;
            toPrintList.Add("----------------------------");
            toPrintList.Add("| Data Model Configuration |");
            toPrintList.Add("----------------------------");
            toPrintList.Add("Total Requests: "+customers.Count);
            toPrintList.Add("Number of available vehicles: " + vehicles.Count);
            string capacitiesString = "Vehicle Capacities: ";
            foreach (var vehicle in vehicles)
            {
                capacitiesString += "{" + vehicle.Capacity + "} ";
            }
            toPrintList.Add(capacitiesString);
            toPrintList.Add("------------------------");
            toPrintList.Add("| Algorithm Statistics |");
            toPrintList.Add("------------------------");
            foreach (var algorithm in testedAlgorithms)
            { 
                toPrintList.Add("Algorithm:"+algorithm.Name+" ("+algorithm.Type+")");
                toPrintList.Add("Solution is feasible: " + algorithm.SolutionIsFeasible);
                toPrintList.Add("Computation time: "+algorithm.ComputationTimeInSeconds+" seconds");
                toPrintList.Add("Maximum upper bound value (Delay): "+algorithm.MaxUpperBoundInMinutes+" minutes");
                if (algorithm.SearchTimeLimitInSeconds != 0)
                {
                    toPrintList.Add("Search time limit: " + algorithm.SearchTimeLimitInSeconds + " seconds");
                }
                toPrintList.Add("Number of served requests: "+algorithm.Solver.GetSolutionObject(algorithm.Solution).CustomerNumber);
                toPrintList.Add("Total distance traveled: "+algorithm.Solver.GetSolutionObject(algorithm.Solution).TotalDistanceInMeters+" meters.");
                toPrintList.Add("Total time: "+TimeSpan.FromSeconds(algorithm.Solver.GetSolutionObject(algorithm.Solution).TotalTimeInSeconds).TotalMinutes+" minutes.");
                toPrintList.Add("Total Load: "+algorithm.Solver.GetSolutionObject(algorithm.Solution).TotalLoad);
                toPrintList.Add("-----------------------------");

            }

            return toPrintList;
        }

    }
}
