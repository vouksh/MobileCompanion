using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileCompanion.Shared.Interfaces
{
    public interface ICommandObject
    {
        public DateTime TimeStamp { get; init; }

        public string Command { get; init; }

        public string[]? ArrayData { get; init; }

        public string? StringData { get; init; }
        public double? NumberData { get; init; }
    }
}
