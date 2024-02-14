using System.Text.Json;
using Confluent.Kafka;

namespace Producer;

internal class KafkaJsonSerializer<T> : ISerializer<T>, IDeserializer<T>
{
    private readonly JsonSerializerOptions _options;

    public KafkaJsonSerializer(JsonSerializerOptions options = null) => _options = options;

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        JsonSerializer.Deserialize<T>(data, _options);

    public byte[] Serialize(T data, SerializationContext context) =>
        JsonSerializer.SerializeToUtf8Bytes(data, _options);
}
