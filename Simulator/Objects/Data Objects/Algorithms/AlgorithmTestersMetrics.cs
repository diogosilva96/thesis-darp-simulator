using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    class AlgorithmTestersMetrics
    {
        public List<AlgorithmTester> TestedAlgorithms;
        public Dictionary<Tuple<string,string>, Dictionary<string, double>> SearchTimeMetricsDictionary;//dict with key search time,algorithm name, num customers, contains a dictionary with a metric and its respective value

        private List<string> searchTimes;
        private List<string> algorithmNames;

        private int firstLineIndexToPrint;
        public AlgorithmTestersMetrics()
        {
            SearchTimeMetricsDictionary = new Dictionary<Tuple<string,string>, Dictionary<string, double>>();
            TestedAlgorithms = new List<AlgorithmTester>();
            firstLineIndexToPrint = 0;
        }


        public void AddTestedAlgorithm(AlgorithmTester testedAlgorithm)
        {
            TestedAlgorithms.Add(testedAlgorithm);
        }

        public void InitializeMetricsAttributesList()
        {
            searchTimes = new List<string>();
            algorithmNames = new List<string>();
            searchTimes.Add("all");
            algorithmNames.Add("all");
            foreach (var testedAlgorithm in TestedAlgorithms)
            {
                if (!searchTimes.Contains(testedAlgorithm.SearchTimeLimitInSeconds.ToString()))
                {
                    searchTimes.Add(testedAlgorithm.SearchTimeLimitInSeconds.ToString());
                }

                if (!algorithmNames.Contains(testedAlgorithm.Name))
                {
                    algorithmNames.Add(testedAlgorithm.Name);
                }
                
            }
        }

        private void ComputeAlgorithmMetrics()
        {
            InitializeMetricsAttributesList();

            foreach (var algorithmName in algorithmNames)
            {               
                foreach (var searchTime in searchTimes)
                {
                 
                        Tuple<string,string> algorithmSearchTimeTuple = new Tuple<string, string>(algorithmName,searchTime.ToString());
                        if (!SearchTimeMetricsDictionary.ContainsKey(algorithmSearchTimeTuple))
                        {
                            SearchTimeMetricsDictionary.Add(algorithmSearchTimeTuple,new Dictionary<string, double>());
                        }
                }
            }

            for (int i = 0; i < SearchTimeMetricsDictionary.Count; i++)
            {
                var searchTimeMetricDictionary = SearchTimeMetricsDictionary.ElementAt(i);
                var searchTime = searchTimeMetricDictionary.Key.Item2;
                var algName = searchTimeMetricDictionary.Key.Item1;
                

                List<AlgorithmTester> testedAlgorithmsForCurrentSearchTimeAndName = null;
                if (algName== "all" && searchTime != "all") // algName = all && searchtime != all
                {
                    testedAlgorithmsForCurrentSearchTimeAndName = TestedAlgorithms.FindAll(ta => ta.SearchTimeLimitInSeconds == int.Parse(searchTime));
                } else if (algName != "all" && searchTime == "all") //algName != all && searchTime = all
                {
                    testedAlgorithmsForCurrentSearchTimeAndName = TestedAlgorithms.FindAll(ta => ta.Name == algName);
                }
                else if (searchTime == "all" && algName == "all") //algName = all && searchTime = all
                {
                    testedAlgorithmsForCurrentSearchTimeAndName = TestedAlgorithms;
                }
                else
                {
                    testedAlgorithmsForCurrentSearchTimeAndName = TestedAlgorithms.FindAll(ta =>ta.Name == algName && ta.SearchTimeLimitInSeconds == int.Parse(searchTime));
                }
                    foreach (var testedAlgorithms in testedAlgorithmsForCurrentSearchTimeAndName)
                    {
                        foreach (var metric in testedAlgorithms.Metrics) //totalMetrics
                        {
                            var metricName = "total" + metric.Key;
                            if (!searchTimeMetricDictionary.Value.ContainsKey(metricName))
                            {
                                searchTimeMetricDictionary.Value.Add(metricName, 0);
                            }

                            searchTimeMetricDictionary.Value[metricName] += metric.Value;
                        }
                    }

                    var numMetricsToBeAverage = searchTimeMetricDictionary.Value.Count;
                    if (firstLineIndexToPrint == 0)
                    {
                        firstLineIndexToPrint = numMetricsToBeAverage;
                    }

                    var totalTests =
                        testedAlgorithmsForCurrentSearchTimeAndName
                            .Count; //total of tests for current alg name and searchtime
                    searchTimeMetricDictionary.Value.Add(nameof(totalTests), totalTests);
                    for (int j = 0; j < numMetricsToBeAverage; j++)
                    {
                        if (totalTests > 0)
                        {
                            var currentMetric = searchTimeMetricDictionary.Value.ElementAt(j);
                            var currentMetricName = currentMetric.Key;
                            var currentMetricValue = currentMetric.Value;
                            var averageMetricName = "average" + currentMetricName;
                            if (!searchTimeMetricDictionary.Value.ContainsKey(averageMetricName))
                            {
                                searchTimeMetricDictionary.Value.Add(averageMetricName, 0);
                            }

                            searchTimeMetricDictionary.Value[averageMetricName] =
                                (double) (currentMetricValue / totalTests); //computes average
                        }

                    }

                }
            

        }

       
        public void SaveMetrics()
        {
           ComputeAlgorithmMetrics();

            PrintMetrics();

       
        }

        public void PrintMetrics()
        {
            foreach (var searchTimeMetricsDict in SearchTimeMetricsDictionary)
            {
                Console.WriteLine("SearchTime:"+searchTimeMetricsDict.Key.Item2+"Algorithm: "+searchTimeMetricsDict.Key.Item1);
                var count = 0;
                foreach (var metricDict in searchTimeMetricsDict.Value)
                {
                    if (count >= firstLineIndexToPrint)
                    {
                        Console.WriteLine(metricDict.Key + ":" + metricDict.Value);
                    }

                    count++;
                }
                
            }
        }
    }
}
