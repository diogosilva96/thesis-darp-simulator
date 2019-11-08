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

        public long Objective => Solution.ObjectiveValue();

        public int TotalServedCustomers => Solver.GetSolutionObject(Solution) != null ? Solver.GetSolutionObject(this.Solution).CustomerNumber : 0;
        public int TotalDistanceTraveledInMeters => Solver.GetSolutionObject(Solution) != null ? (int)Solver.GetSolutionObject(this.Solution).TotalDistanceInMeters:0;

        public int TotalRouteTimesInMinutes => Solver.GetSolutionObject(Solution) != null
            ? (int) TimeSpan.FromSeconds(Solver.GetSolutionObject(this.Solution).TotalTimeInSeconds).TotalMinutes
            : 0;
        protected AlgorithmTester(RoutingDataModel dataModel,bool allowDropNodes)
        {
            DataModel = dataModel;
            SolutionIsFeasible = false;
            Solver = new RoutingSolver(dataModel,allowDropNodes);
            _hasBeenTested = false;
        }

        public string GetCSVResultsMessage()
        {
            if (_hasBeenTested)
            {
                string splitter = ",";

                string message = Name + splitter +AllowDropNodes+splitter+ SolutionIsFeasible+splitter+SearchTimeLimitInSeconds + splitter + ComputationTimeInSeconds + splitter + Solution.ObjectiveValue() + splitter + MaxUpperBoundInMinutes + splitter + TotalServedCustomers + splitter + TotalDistanceTraveledInMeters + splitter + TotalRouteTimesInMinutes;
                return message;
            }

            return null;
        }
        public override string ToString()
        {
            return "["+GetType().Name+"]";
        }

        public void Test() //tests the algorithm using different maxUpperBound values until it finds the earliest feasible maxupperbound value, then saves its metrics
        {
            Console.WriteLine(this.ToString() + " testing algorithm: " + Name);

            var watch = Stopwatch.StartNew();
            var solution = TryGetSolution();
            _hasBeenTested = true;
            watch.Stop();
            var elapsedSeconds = watch.ElapsedMilliseconds * 0.001;
            SolutionIsFeasible = solution != null;
            if (SolutionIsFeasible) //solution != null (means earliest feasible solution was found)
            {
                //Saves the important metrics for the earliest feasible solution
                MaxUpperBoundInMinutes = (int)TimeSpan.FromSeconds(Solver.MaxUpperBound).TotalMinutes;
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

                toPrintList.Add("Number of served requests: " + TotalServedCustomers);
                toPrintList.Add("Total distance traveled: " + TotalDistanceTraveledInMeters + " meters.");
                toPrintList.Add("Total route times: " + TotalRouteTimesInMinutes + " minutes.");
                toPrintList.Add("Total Load: " + Solver.GetSolutionObject(Solution).TotalLoad);
                toPrintList.Add("Average Distance traveled per request:" + TotalDistanceTraveledInMeters /
                                TotalServedCustomers + " meters.");
                toPrintList.Add("Solution Objective value: " + Objective);
                toPrintList.Add("-----------------------------");
                return toPrintList;
            }

            return null;
        }

        public abstract Assignment TryGetSolution();
    }
}
