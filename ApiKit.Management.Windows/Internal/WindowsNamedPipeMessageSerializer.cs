using System.Buffers.Binary;
using System.Text.Json;

namespace ApiKit.Management.Windows.Internal;

internal static class WindowsNamedPipeMessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static JsonElement ToElement<T>(T value) =>
        JsonSerializer.SerializeToElement(value, SerializerOptions);

    public static T? FromElement<T>(JsonElement? value)
    {
        if (value is null)
        {
            return default;
        }

        return value.Value.Deserialize<T>(SerializerOptions);
    }

    public static async ValueTask WriteAsync<T>(
        Stream stream,
        T value,
        int maxMessageBytes,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        if (payload.Length > maxMessageBytes)
        {
            throw new InvalidOperationException(
                $"Размер management-сообщения {payload.Length} байт превышает лимит {maxMessageBytes} байт.");
        }

        var lengthBuffer = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, payload.Length);

        await stream.WriteAsync(lengthBuffer, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async ValueTask<T> ReadAsync<T>(
        Stream stream,
        int maxMessageBytes,
        CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(lengthBuffer, cancellationToken);

        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        if (length <= 0 || length > maxMessageBytes)
        {
            throw new InvalidDataException(
                $"Некорректный размер management-сообщения: {length} байт.");
        }

        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, cancellationToken);

        return JsonSerializer.Deserialize<T>(payload, SerializerOptions)
            ?? throw new InvalidDataException("Management-сообщение не удалось десериализовать.");
    }
}
