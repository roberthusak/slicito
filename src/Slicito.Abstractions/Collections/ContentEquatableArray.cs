using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Slicito.Abstractions.Collections;

[JsonConverter(typeof(ContentEquatableArrayJsonConverterFactory))]
public readonly struct ContentEquatableArray<T> : IEquatable<ContentEquatableArray<T>>, IEnumerable<T>
{
    public ImmutableArray<T> Array { get; }

    public ContentEquatableArray(ImmutableArray<T> array)
    {
        Array = array;
    }

    public ContentEquatableArray(IEnumerable<T> items)
    {
        Array = [.. items];
    }

    public static implicit operator ImmutableArray<T>(ContentEquatableArray<T> array)
    {
        return array.Array;
    }

    public static implicit operator ContentEquatableArray<T>(ImmutableArray<T> array)
    {
        return new(array);
    }

    public static bool operator ==(ContentEquatableArray<T> left, ContentEquatableArray<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContentEquatableArray<T> left, ContentEquatableArray<T> right)
    {
        return !left.Equals(right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is ContentEquatableArray<T> other)
        {
            return Equals(other);
        }
        return false;
    }

    public bool Equals(ContentEquatableArray<T> other)
    {
        if (Array.Length != other.Array.Length)
        {
            return false;
        }

        return Array.SequenceEqual(other.Array);
    }

    public override int GetHashCode()
    {
        var hashCode = 0;
        foreach (var item in Array)
        {
            hashCode = HashCode.Combine(hashCode, item?.GetHashCode() ?? 0);
        }
        return hashCode;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>) Array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) Array).GetEnumerator();
    }
}

public class ContentEquatableArrayJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(ContentEquatableArray<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ContentEquatableArrayJsonConverter<>).MakeGenericType(elementType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public class ContentEquatableArrayJsonConverter<T> : JsonConverter<ContentEquatableArray<T>>
{
    public override ContentEquatableArray<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        var items = new List<T>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            var item = JsonSerializer.Deserialize<T>(ref reader, options);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return new ContentEquatableArray<T>(items);
    }

    public override void Write(Utf8JsonWriter writer, ContentEquatableArray<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value.Array)
        {
            JsonSerializer.Serialize(writer, item, options);
        }
        writer.WriteEndArray();
    }
}
