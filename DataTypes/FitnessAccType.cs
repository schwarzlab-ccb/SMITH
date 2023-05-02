using System.Text.Json.Serialization;

namespace SMITH.DataTypes;

// Multiplicative, Additive, ETH paper
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FitnessAccType
{
    Mul,
    Add,
    Lim
}