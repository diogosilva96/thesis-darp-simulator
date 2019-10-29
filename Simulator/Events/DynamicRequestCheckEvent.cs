using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Events
{
    public class DynamicRequestCheckEvent:Event
    {
        private readonly double _probability;
        private  double _probabilityThreshold;
        public bool GenerateNewDynamicRequest => _probability <= _probabilityThreshold;
        public DynamicRequestCheckEvent(int category, int time) : base(category, time)
        {
            Random rnd = new Random();
            _probability = rnd.NextDouble();
            _probabilityThreshold = 0.02;
        }

        public override string GetTraceMessage()
        {

            string splitter = ", ";
            string message = "";
            message = this.ToString() + splitter + Time;
            return message;
        }

        public void SetThreshold(double probabilityThreshold)
        {
            _probabilityThreshold = probabilityThreshold;

        }

        public override void Treat()
        {
            AlreadyHandled = true;
            if (GenerateNewDynamicRequest)
            {
                //Console.WriteLine("Dynamic request check event:" + _probability);
            }
        }
    }
}
