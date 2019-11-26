using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects;

namespace Simulator.Events
{
    public class DynamicRequestCheckEvent:Event
    {
        private readonly double _probability;
        private  double _probabilityThreshold;
        public bool GenerateNewDynamicRequest => _probability <= _probabilityThreshold;
        public DynamicRequestCheckEvent(int category, int time) : base(category, time)
        {
            _probability = RandomNumberGenerator.Random.NextDouble();
            _probabilityThreshold = 0.02;
        }

        public override string GetTraceMessage()
        {
            string message;
            string timestamp = DateTime.Now.ToString();
            string splitter = ", ";
            if (GenerateNewDynamicRequest)
            {
                message = timestamp + splitter + this.ToString() + splitter + "Probability: " + _probability;
            }
            else
            {
                message = "";
            }
             
            return message;
        }

        public void SetThreshold(double probabilityThreshold)
        {
            _probabilityThreshold = probabilityThreshold;

        }
    }
}
