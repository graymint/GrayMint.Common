namespace GrayMint.Common.Utils;

/// <summary>
/// A change to one field, as `{ "value": ... }`. Leave the field out to keep its value.
/// </summary>
public class Patch<T>(T value)
{
    /// <summary>The field's new value.</summary>
    public T Value { get; } = value;

    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? "".GetHashCode();
    }

    public override string ToString()
    {
        return Value?.ToString() ?? "";
    }

    public override bool Equals(object? obj)
    {
        return Equals(Value, obj);
    }

    public static implicit operator Patch<T>(T value)
    {
        return new Patch<T>(value);
    }

    public static implicit operator T(Patch<T> value)
    {
        return value != null ? value.Value : throw new NullReferenceException("Value has not been set.");
    }
}