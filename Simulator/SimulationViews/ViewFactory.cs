using System;
using System.Collections.Generic;
using System.Text;
using Simulator.EventAppender__COR_Pattern_;
using Simulator.Events;
using Simulator.Objects.Simulation;

namespace Simulator.SimulationViews
{
    public class ViewFactory
    {
        private static ViewFactory _instance;
        //Lock syncronization object for multithreading (might not be needed)
        private static object syncLock = new object();
        public static ViewFactory Instance() //Singleton
        {
            // Support multithreaded apps through Double checked locking pattern which (once the instance exists) avoids locking each time the method is invoked

            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new ViewFactory();
                    }
                }
            }
            return _instance;
        }
        public IView Create(int code,Simulation simulation)
        {
            switch (code)
            {
                case 0:
                    return  new MainMenuView(simulation);
                case 1:
                    return new StandardRouteSimulationView(simulation);

                case 2:
                    return new FlexibleRouteSimulationView(simulation);

                case 3:
                    return new AlgorithmsComparisonView(simulation);

                case 4:
                    return new ConfigSimulationParamsView(simulation);
                case 5:
                    Environment.Exit(0);
                    return null;
                default:
                    return new MainMenuView(simulation);
                
     
            }
        }
    }
}
