using MobileCompanion.Shared;
using MobileCompanion.Shared.Enums;
using MobileCompanion.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static AndroidX.ConstraintLayout.Core.State.State;

namespace MobileCompanion.Phone.Platforms.Android;

internal class CommandObject : ICommandObject
{
    public DateTime TimeStamp { get; init; }
    public required string Command { get; init; }
    public string[]? ArrayData { get; init; }
    public string? StringData { get; init; }
    public double? NumberData { get; init; }

    public string GetPayload()
    {
        var opts = Constants.JsonSerializerOptions;
        opts.Converters.Add(new KotlinFriendlyDateTimeConverter());
        var jsonString = JsonSerializer.Serialize(this, opts);
        return jsonString;
    }

    public byte[] GetBytes()
    {
        return Encoding.UTF8.GetBytes(GetPayload());
    }
}
public class CommunicationPacket : ICommunicationPacket
{
    public CommandType Command { get; set; }
    public required ICommandObject CommandData { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class KotlinFriendlyDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString() ?? string.Empty);
    }
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("s"));
    }
}
