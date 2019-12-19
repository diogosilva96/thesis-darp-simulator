using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    class AlgorithmTestersMetrics
    {
        public List<AlgorithmTester> TestedAlgorithms;
        public Dictionary<string, Dictionary<string, int>> SearchTimeMetricsDictionary;//dict with key search time, contains a dictionary with a metric and its respective value
        public AlgorithmTestersMetrics()
        {
            SearchTimeMetricsDictionary = new Dictionary<string, Dictionary<string, int>>();
            TestedAlgorithms = new List<AlgorithmTester>();
        }


        public void AddTestedAlgorithm(AlgorithmTester testedAlgorithm)
        {
            TestedAlgorithms.Add(testedAlgorithm);
        }

        public void SaveOverallMetrics()
        {
            foreach (var algorithm in TestedAlgorithms)
            {
                var searchTime = algorithm.SearchTimeLimitInSeconds;
                if (!SearchTimeMetricsDictionary.ContainsKey(searchTime.ToString()))
                {
                    SearchTimeMetricsDictionary.Add(searchTime.ToString(),null);//initializes the first searchTime Metric container
                }

                SearchTimeMetricsDictionary.TryGetValue(searchTime.ToString(), out var currentSearchTimeMetricsDictionary);
                foreach (var algorithmMetric in algorithm.Metrics)
                {
                    var metricName = "Total"+algorithmMetric.Key;

                    if (currentSearchTimeMetricsDictionary != null)
                    {
                        if (!currentSearchTimeMetricsDictionary.ContainsKey(metricName))
                        {
                            currentSearchTimeMetricsDictionary.Add(metricName,0);//init metric if not in dict yet
                        }

                        currentSearchTimeMetricsDictionary[metricName] = currentSearchTimeMetricsDictionary[metricName] + algorithmMetric.Value; //updates its value
                    }
                }
            }

            var TotalTests = TestedAlgorithms.Count;
            SearchTimeMetricsDictionary.Add("overall", null);//initializes overall metrics (for all searchtimes)
            //var overallSearchTimeMetricsDictionary = SearchTimeMetricsDictionary["overall"];
            foreach (var searchTimeMetricDict in SearchTimeMetricsDictionary)
            {
                var currentSearchTime = searchTimeMetricDict.Key;
                TotalTests = TestedAlgorithms.FindAll(ta => ta.SearchTimeLimitInSeconds == int.Parse(currentSearchTime)).Count;
                searchTimeMetricDict.Value.Add(nameof(TotalTests),TotalTests);

                //averages computation
                foreach (var metricDict in searchTimeMetricDict.Value)
                {
                    var metricName = metricDict.Key;
                    var metricValue = metricDict.Value;
                    if (metricName != nameof(TotalTests))
                    {
                        var averageMetricName = "average" + metricName;
                        //if (!overallSearchTimeMetricsDictionary.ContainsKey(metricName))
                        //{
                        //    overallSearchTimeMetricsDictionary.Add(metricName, 0); //init                          
                        //}

                        //overallSearchTimeMetricsDictionary[metricName] += metricValue;
                        if (!searchTimeMetricDict.Value.ContainsKey(averageMetricName))
                        {
                            searchTimeMetricDict.Value.Add(averageMetricName, 0); //init
                        }

                        searchTimeMetricDict.Value[averageMetricName] =
                            (int) (metricValue / TotalTests); //computes the average for current search time
                    }
                }
              
            }

            TotalTests = TestedAlgorithms.Count;
            //computes averages for overallMetricsDict
            //overallSearchTimeMetricsDictionary.Add(nameof(TotalTests),TotalTests);
            //foreach ( var overallMetric in overallSearchTimeMetricsDictionary)
            //{
            //    var metricName = overallMetric.Key;
            //    var metricValue = overallMetric.Value;
            //    if (metricName != nameof(TotalTests))
            //    {
            //        var averageMetricName = "average" + metricName;
            //        if (!overallSearchTimeMetricsDictionary.ContainsKey(averageMetricName))
            //        {
            //            overallSearchTimeMetricsDictionary.Add(averageMetricName,0);
            //        }

            //        overallSearchTimeMetricsDictionary[averageMetricName] = (int)(metricValue / TotalTests);
            //    }
                

            //}

       
        }

        public void PrintMetrics()
        {
            foreach (var searchTimeMetricsDict in SearchTimeMetricsDictionary)
            {
                Console.WriteLine("SearchTime: "+searchTimeMetricsDict.Key);
                foreach (var metricDict in searchTimeMetricsDict.Value)
                {
                    Console.WriteLine(metricDict.Key+":"+metricDict.Value);
                }
                
            }
        }
    }
}
