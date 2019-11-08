using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Events;

namespace Simulator.EventAppender__COR_Pattern_
{
    public interface IEventAppender
    {
        void SetNext(IEventAppender nextEventAppender);
        void Append(Event evt);
    }
}
