using System.Text.Json.Serialization;

namespace NationsConverterShared.Models;

[JsonConverter(typeof(JsonStringEnumConverter<WaypointType>))]
public enum WaypointType
{
    Start,
    StartFinish,
    Checkpoint,
    Finish
}
