using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Objects.Data_Objects
{
    public class MetricsContainer
    {
        private Dictionary<string, int> _metricValueDictionary;

        public MetricsContainer()
        {
            _metricValueDictionary = new Dictionary<string, int>();
        }

        public void AddMetric(string metricName, int metricValue)
        {
            if (!_metricValueDictionary.ContainsKey(metricName))
            {
                _metricValueDictionary.Add(metricName,metricValue);
            }
        }

        public Dictionary<string, int> GetMetricsDictionary()
        {
            return _metricValueDictionary;
        }

        public void PrintMetrics()
        {
            foreach (var metricValue in GetMetricsDictionary())
            {
                Console.WriteLine(MetricToString(metricValue));
            }
        }

        public string MetricToString(KeyValuePair<string, int> metricValue)
        {
            string message = metricValue.Key + ": " + metricValue.Value;
            return message;
        }
    }
}
