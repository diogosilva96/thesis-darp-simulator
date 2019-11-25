using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Events
{
    public interface IEventHandler
    {
        void Handle(Event evt);
    }
}
