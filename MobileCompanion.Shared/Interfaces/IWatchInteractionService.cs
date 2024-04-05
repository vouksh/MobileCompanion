using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileCompanion.Shared.Interfaces
{
    public interface IWatchInteractionService
    {
        IWatchHandler WatchHandler { get; }

        void Connect();
        void SendMessage(string command, string commandData = "", double numberData = double.NaN, MessagePriority priority = MessagePriority.Normal);
        void StartTimer();
    }
}
