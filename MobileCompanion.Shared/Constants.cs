using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MobileCompanion.Shared;

public partial struct Constants
{
    public static JsonSerializerOptions JsonSerializerOptions => new()
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PropertyNameCaseInsensitive = true
    };
}

public struct Commands
{
    public struct Phone
    {

    }
    public struct Watch
    {
        public const string TransferFile = "CMD_WATCH_TRANSFER_FILE";
    }
}