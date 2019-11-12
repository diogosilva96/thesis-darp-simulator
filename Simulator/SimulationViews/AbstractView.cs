using System;
using System.Collections.Generic;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Logger;

namespace Simulator.SimulationViews
{
    public abstract class AbstractView:IView
    {
        protected IView NextView;

        protected Logger.Logger ConsoleLogger;

        protected Objects.Simulation.Simulation Simulation;


        protected AbstractView(Objects.Simulation.Simulation simulation)
        {
            IRecorder consoleRecorder = new ConsoleRecorder();
            ConsoleLogger = new Logger.Logger(consoleRecorder);
            Simulation = simulation;
        }



        public abstract void PrintView();

        public int GetIntInput(int minVal, int maxVal)
        {
            wrongKeyLabel:
            int key = 0;
            try
            {
                key = int.Parse(Console.ReadLine());
                if (key < minVal || key > maxVal)
                {
                    ConsoleLogger.Log("Wrong input, please retype using a valid integer number value needs to be in the range [" + minVal + "," + maxVal + "]");
                    goto wrongKeyLabel;
                }
            }
            catch (Exception)
            {
                ConsoleLogger.Log("Wrong input, please retype using a valid integer number!");
                goto wrongKeyLabel;
            }

            return key;
        }

    }
}
