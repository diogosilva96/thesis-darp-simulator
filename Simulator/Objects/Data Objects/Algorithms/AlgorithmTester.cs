using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.OrTools.ConstraintSolver;
using Simulator.Objects.Data_Objects.Routing;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    public abstract class AlgorithmTester
    {
        public string Name;
        public double ComputationTimeInSeconds; //in seconds
        public string Type;
        public object AlgorithmValue;
        public int MaxUpperBoundInMinutes; //in minutes
        public Assignment Solution;
        public RoutingDataModel DataModel;
        public bool SolutionIsFeasible;
        public bool AllowDropNodes => Solver.DropNodesAllowed;
        public int SearchTimeLimitInSeconds; //in seconds
        public RoutingSolver Solver;
        private bool _hasBeenTested;
        private RoutingSolutionObject SolutionObject => Solver.GetSolutionObject(Solution);


        protected AlgorithmTester()
        {

            SolutionIsFeasible = false;
            _hasBeenTested = false;
        }

        public string GetCSVResultsMessage()
        {
            if (_hasBeenTested)
            {
                string splitter = ",";
                int allowDropNodes = AllowDropNodes ? 1 : 0;
                string message = Name + splitter + allowDropNodes + splitter +
                                 SearchTimeLimitInSeconds + splitter + ComputationTimeInSeconds + splitter + MaxUpperBoundInMinutes;
                foreach (var metric in SolutionObject.MetricsDictionary)
                {
                    message += splitter + metric.Value;
                }
                message += splitter + DataModel.Id;
                return message;
            }

            return null;
        }

        public string GetCSVMessageStyle()
        {
            string splitter = ",";
            string message = nameof(Name) + splitter + nameof(AllowDropNodes) + splitter +
                             nameof(SearchTimeLimitInSeconds) + splitter + nameof(ComputationTimeInSeconds) + splitter + nameof(MaxUpperBoundInMinutes);
            foreach (var metric in SolutionObject.MetricsDictionary)
            {
                message += splitter + metric.Key;
            }
            message += splitter + nameof(DataModel.Id);
            return message;
        }
        public override string ToString()
        {
            return "["+GetType().Name+"]";
        }

        public void Test(RoutingDataModel dataModel,bool dropNodes) //tests the algorithm using different maxUpperBound values until it finds the earliest feasible maxupperbound value, then saves its metrics
        {
            Console.WriteLine(this.ToString() + " testing algorithm: " + Name+ " (Search time: "+SearchTimeLimitInSeconds+" seconds)");
            DataModel = dataModel;
            Solver = new RoutingSolver(dataModel,dropNodes);
                var watch = Stopwatch.StartNew();
            var solution = GetSolution();
            _hasBeenTested = true;
            watch.Stop();
            var elapsedSeconds = watch.ElapsedMilliseconds * 0.001;
            SolutionIsFeasible = solution != null;
            if (SolutionIsFeasible) //solution != null (means earliest feasible solution was found)
            {
                //Saves the important metrics for the earliest feasible solution           
                MaxUpperBoundInMinutes = (int)TimeSpan.FromSeconds(Solver.MaximumDeliveryDelayTime).TotalMinutes;
                ComputationTimeInSeconds = elapsedSeconds;
                Solution = solution;
                Solver.PrintSolution(solution);
            }
        }

        public List<string> GetResultPrintableList()
        {
            if (_hasBeenTested)
            {
                List<string> toPrintList = new List<string>();

                
                toPrintList.Add("Algorithm: " + Name + " (" + Type + ")");
                toPrintList.Add("Solution is feasible: " + SolutionIsFeasible);
                toPrintList.Add("Computation time: " + ComputationTimeInSeconds + " seconds");
                toPrintList.Add("Maximum upper bound value (Delay): " + MaxUpperBoundInMinutes + " minutes");
                if (SearchTimeLimitInSeconds != 0)
                {
                    toPrintList.Add("Search time limit: " + SearchTimeLimitInSeconds + " seconds");
                }

                foreach (var metricDict in SolutionObject.MetricsDictionary)
                {
                    toPrintList.Add(metricDict.Key+": "+metricDict.Value);
                }
                toPrintList.Add("Seed: "+RandomNumberGenerator.Seed);
                toPrintList.Add("-----------------------------");
                return toPrintList;
            }

            return null;
        }

        public abstract Assignment GetSolution();
    }
}
