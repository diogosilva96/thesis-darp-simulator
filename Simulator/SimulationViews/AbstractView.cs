using System;
using System.Collections.Generic;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Logger;

namespace Simulator.SimulationViews
{
    public abstract class AbstractView:IView
    {
        protected IView NextView;

        private Logger.Logger _consoleLogger;

        protected Simulation Simulation;

        protected AbstractView(AbstractSimulation simulation)
        {
            Simulation = (Simulation)simulation;
            IRecorder consoleRecorder = new ConsoleRecorder();
            _consoleLogger = new Logger.Logger(consoleRecorder);
        }

        public void SetNext(IView nextView)
        {
            NextView = nextView;
        }

        public abstract void PrintView(int option);

        public int GetIntInput(int minVal, int maxVal)
        {
            wrongKeyLabel:
            int key = 0;
            try
            {
                key = int.Parse(Console.ReadLine());
                if (key < minVal || key > maxVal)
                {
                    _consoleLogger.Log("Wrong input, please retype using a valid integer number value needs to be in the range [" + minVal + "," + maxVal + "]");
                    goto wrongKeyLabel;
                }
            }
            catch (Exception)
            {
                _consoleLogger.Log("Wrong input, please retype using a valid integer number!");
                goto wrongKeyLabel;
            }

            return key;
        }

        public void Print(string message)
        {
            _consoleLogger.Log(message);
        }

        public void Print(List<string> messages)
        {
            foreach (var message in messages)
            {
                Print(message);
            }
        }

    }
}
