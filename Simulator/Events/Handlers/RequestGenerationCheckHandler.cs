using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Objects.Data_Objects;
using Simulator.Objects.Data_Objects.Routing;
using Simulator.Objects.Data_Objects.Simulation_Data_Objects;
using Simulator.Objects.Data_Objects.Simulation_Objects;
using Simulator.Objects.Simulation;

namespace Simulator.Events.Handlers
{
    class RequestGenerationCheckHandler:EventHandler
    {
        public RequestGenerationCheckHandler(Simulation simulation) : base(simulation)
        {
        }

        public override void Handle(Event evt)
        {
            if (evt.Category == 5 && evt is DynamicRequestCheckEvent dynamicRequestCheckEvent) // if the event is a dynamic request check event and the current event time is lower than the end time of the simulation
            {
                Log(evt);
                evt.AlreadyHandled = true;

                var nextDynamicRequestCheckEventTime = evt.Time + 10;
                //event handle if theres a dynamic request to be generated
                if (dynamicRequestCheckEvent.GenerateNewDynamicRequest) // checks if the current event dynamic request event check is supposed to generate a new customer dynamic request event
                {
                    List<Stop> excludedStops = new List<Stop>();
                    excludedStops.Add(TransportationNetwork.Depot);
                   
                    var requestTime = evt.Time + 1;
                    var pickupTimeWindow = new int[] { requestTime + 5 * 60, requestTime + 60 * 60 };
                    var customer = CustomerFactory.Instance().CreateRandomCustomer(TransportationNetwork.Stops, excludedStops, requestTime, pickupTimeWindow);//Generates a random customer
                    var nextCustomerRequestEvent =
                        EventGenerator.Instance().GenerateCustomerRequestEvent(requestTime, customer); //Generates a pickup and delivery customer request (dynamic)
                    Simulation.AddEvent(nextCustomerRequestEvent);
                }
                //end of event handle
                if (nextDynamicRequestCheckEventTime < Simulation.Params.SimulationTimeWindow[1]) //if the new event time is lower than the end time of the simulation, generates a new dynamic request check event
                {
                    var eventDynamicRequestCheck = EventGenerator.Instance()
                        .GenerateDynamicRequestCheckEvent(nextDynamicRequestCheckEventTime,
                            Simulation.Params
                                .DynamicRequestThreshold); //generates a new dynamic request check 10 seconds later than the current evt
                    Simulation.AddEvent(eventDynamicRequestCheck);
                }

            }
            else
            {
                Successor?.Handle(evt);
            }
        }
    }
}
