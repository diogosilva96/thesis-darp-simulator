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

        public long Objective => Solution.ObjectiveValue();

        public int TotalServedCustomers => SolutionObject != null ? (int)SolutionObject.TotalLoad : 0;

        public int TotalDistanceTraveledInMeters => SolutionObject != null ? (int) SolutionObject.TotalDistanceInMeters : 0;

        public int TotalRouteTimesInMinutes => SolutionObject != null ? (int) TimeSpan.FromSeconds(SolutionObject.TotalTimeInSeconds).TotalMinutes : 0;

        public int TotalVehiclesUsed => SolutionObject != null ? (int) SolutionObject.TotalVehiclesUsed : 0;
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
                int feasible = SolutionIsFeasible ? 1 : 0;
                string message = Name + splitter +allowDropNodes+splitter+ feasible+splitter+SearchTimeLimitInSeconds + splitter + ComputationTimeInSeconds + splitter + Solution.ObjectiveValue() + splitter + MaxUpperBoundInMinutes + splitter + TotalServedCustomers + splitter + TotalDistanceTraveledInMeters + splitter + TotalRouteTimesInMinutes + splitter + TotalVehiclesUsed+splitter+DataModel.Id;
                return message;
            }

            return null;
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
                MaxUpperBoundInMinutes = (int)TimeSpan.FromSeconds(Solver.MaxUpperBound).TotalMinutes;
                ComputationTimeInSeconds = elapsedSeconds;
                Solution = solution;
                Solver.PrintSolutionWithCumulVars(solution);
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
                toPrintList.Add("Total Load: " + SolutionObject.TotalLoad);
                if (SolutionObject.TotalLoad != SolutionObject.CustomerNumber)
                {
                    throw new Exception("EXCEPTION : TOTAL LOAD != CUSTOMERNUMBER");
                }
                toPrintList.Add("Total vehicles used: " + TotalVehiclesUsed);
                toPrintList.Add("Average Distance traveled per request:" + TotalDistanceTraveledInMeters /
                                TotalServedCustomers + " meters.");
                toPrintList.Add("Solution Objective value: " + Objective);
                toPrintList.Add("Seed: "+RandomNumberGenerator.Seed);
                toPrintList.Add("-----------------------------");
                return toPrintList;
            }

            return null;
        }

        public abstract Assignment GetSolution();
    }
}
