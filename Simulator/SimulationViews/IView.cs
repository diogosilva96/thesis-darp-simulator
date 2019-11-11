using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Events;

namespace Simulator.EventAppender__COR_Pattern_
{
    public interface IView
    {
        void SetNext(IView nextView);
        void PrintView(int option);
    }
}
