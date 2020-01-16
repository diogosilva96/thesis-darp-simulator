using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Simulator.Logger;

namespace Simulator.Objects.Data_Objects.Algorithms
{
    class AlgorithmTesterMetrics
    {
        public List<AlgorithmTester> TestedAlgorithms;
        public Dictionary<Tuple<string,string,string>, Dictionary<string, double>> MetricsDictionary;//dict with key search time,algorithm name, num customers, contains a dictionary with a metric and its respective value


        public AlgorithmTesterMetrics()
        {
            MetricsDictionary = new Dictionary<Tuple<string,string,string>, Dictionary<string, double>>();
            TestedAlgorithms = new List<AlgorithmTester>();

        }


        public void AddTestedAlgorithm(AlgorithmTester testedAlgorithm)
        {
            TestedAlgorithms.Add(testedAlgorithm);
        }


        private void ComputeAlgorithmMetrics()
        {
            var searchTimes = new List<string>();
            var algorithmNames = new List<string>();
            var customerNumbers = new List<string>();
            //searchTimes.Add("all");
            //algorithmNames.Add("all");
            //customerNumbers.Add("all");
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

                if (!customerNumbers.Contains(testedAlgorithm.DataModel.IndexManager.Customers.Count.ToString()))
                {
                    customerNumbers.Add(testedAlgorithm.DataModel.IndexManager.Customers.Count.ToString());
                }

            }

            foreach (var algorithmName in algorithmNames)
            {               
                foreach (var searchTime in searchTimes)
                {

                    foreach (var customerNumber in customerNumbers)
                    {
                        Tuple<string, string,string> algorithmSearchTimeTuple =new Tuple<string, string, string>(algorithmName, searchTime.ToString(),customerNumber.ToString());
                        if (!MetricsDictionary.ContainsKey(algorithmSearchTimeTuple))
                        {
                            MetricsDictionary.Add(algorithmSearchTimeTuple, new Dictionary<string, double>());
                        }
                    }
                }
            }

            for (int i = 0; i < MetricsDictionary.Count; i++)
            {
                var currentMetricDictionary = MetricsDictionary.ElementAt(i);
                var searchTime = currentMetricDictionary.Key.Item2;
                var customerNumber = currentMetricDictionary.Key.Item3;
                var algName = currentMetricDictionary.Key.Item1;
                

                List<AlgorithmTester> algorithmsWithCurrentSearchTimeAndCustomerNumberAndName = null;
                algorithmsWithCurrentSearchTimeAndCustomerNumberAndName = TestedAlgorithms.FindAll(ta => ta.Name == algName && ta.SearchTimeLimitInSeconds == int.Parse(searchTime) && ta.DataModel.IndexManager.Customers.Count == int.Parse(customerNumber));
                var auxMetricDictionaryWithCurrentSearchTimeAndCustomerNumberAndName = new Dictionary<string,List<double>>();
                var totalTests = algorithmsWithCurrentSearchTimeAndCustomerNumberAndName.Count; //total of tests for current alg name and searchtime
                currentMetricDictionary.Value.Add(nameof(totalTests), totalTests);
                foreach (var testedAlgorithm in algorithmsWithCurrentSearchTimeAndCustomerNumberAndName)
                {
                        foreach (var metric in testedAlgorithm.Metrics) //totalMetrics
                        {
                            var metricName = "Total" + metric.Key;
                            if (!currentMetricDictionary.Value.ContainsKey(metricName))
                            {
                                currentMetricDictionary.Value.Add(metricName, 0);
                            }

                            if (!auxMetricDictionaryWithCurrentSearchTimeAndCustomerNumberAndName.ContainsKey(metric.Key))
                            {
                                var metricList = new List<double>();
                                auxMetricDictionaryWithCurrentSearchTimeAndCustomerNumberAndName.Add(metric.Key,metricList);
                            }
                            auxMetricDictionaryWithCurrentSearchTimeAndCustomerNumberAndName[metric.Key].Add(metric.Value);//adds the metric to the metrics list
                            currentMetricDictionary.Value[metricName] += metric.Value; //this metric does not need to be added (just to check if values are ok)
                        }
                }

                foreach (var auxMetric in auxMetricDictionaryWithCurrentSearchTimeAndCustomerNumberAndName)
                {
                    var metricName = auxMetric.Key;
                    var metricValues = auxMetric.Value;
                    var averageMetricName = "Average" + metricName;
                    var medianMetricName = "Median" + metricName;
                    //average calc
                    var averageMetricValue = (double)(auxMetric.Value.Sum() / auxMetric.Value.Count);//computes average for current metric
                    //median calc
                    var medianMetricValue = Calculator.GetMedian(metricValues);
                    if (!currentMetricDictionary.Value.ContainsKey(averageMetricName))
                    {
                        currentMetricDictionary.Value.Add(averageMetricName,0);
                    }
                    if (!currentMetricDictionary.Value.ContainsKey(medianMetricName))
                    {
                        currentMetricDictionary.Value.Add(medianMetricName, 0);
                    }

                    currentMetricDictionary.Value[averageMetricName] = averageMetricValue; //adds average to dict
                    currentMetricDictionary.Value[medianMetricName] = medianMetricValue; //adds median to dict

                }

            }
            

        }

       
        public void SaveMetrics(string path)
        {         
           ComputeAlgorithmMetrics();
           var searchTimeDict = MetricsDictionary.ElementAt(0);
           string csvFormatMessage = "Algorithm, SearchTime, CustomerNumber"; 
           foreach (var metrics in searchTimeDict.Value)//writes base csv format message 
           {
               var metricName = metrics.Key;
               csvFormatMessage += ", " + metricName;
           }
           var recorder = new FileRecorder(path,csvFormatMessage);
           var fileLogger = new Logger.Logger(recorder);
           foreach (var currentMetricDictionary in MetricsDictionary)
           {
               string csvMessage = currentMetricDictionary.Key.Item1 + ", " + currentMetricDictionary.Key.Item2 + ", " +
                                   currentMetricDictionary.Key.Item3;
               foreach (var metric in currentMetricDictionary.Value)
               {
                   var metricValue = metric.Value;
                   csvMessage += ", " + metricValue;
               }
               fileLogger.Log(csvMessage);
           }
           

       
        }


        public void PrintMetrics()
        {
            foreach (var searchTimeMetricsDict in MetricsDictionary)
            {
                Console.WriteLine("SearchTime:"+searchTimeMetricsDict.Key.Item2+"Algorithm: "+searchTimeMetricsDict.Key.Item1+"CustomerNumber:"+searchTimeMetricsDict.Key.Item3);
                var count = 0;
                foreach (var metricDict in searchTimeMetricsDict.Value)
                {
                    
                        Console.WriteLine(metricDict.Key + ":" + metricDict.Value);

                        count++;
                }
                
            }
        }
    }
}
