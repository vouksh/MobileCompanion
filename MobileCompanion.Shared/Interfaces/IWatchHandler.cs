using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileCompanion.Shared.Interfaces
{
    public interface IWatchHandler
    {
        void SendCommand(string command, string stringData, double numberData);
        void SendCommand(string command, string stringData, double numberData, MessagePriority priority);

        void ProcessResponse(ICommunicationPacket packet);
        void Connect();
        void Disconnect();
        bool WatchIsReachable { get; }
    }
    public enum MessagePriority
    {
        Normal,
        High
    }
}
