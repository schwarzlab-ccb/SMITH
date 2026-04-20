using System.Text.Json.Serialization;

namespace SMITH.DataTypes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FitnessEffectType
{
    Birth,
    Death,
    Both
}