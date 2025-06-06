// http://stackoverflow.com/questions/15226921/how-to-serialize-byte-as-simple-json-array-and-not-as-base64-in-json-net

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tests.DAL
{
	public class ByteArrayConverter : JsonConverter<byte[]>
	{
		public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
		{
			if (value == null)
			{
				writer.WriteNullValue();
				return;
			}

			writer.WriteStartArray();

			for (var i = 0; i < value.Length; i++)
			{
				writer.WriteNumberValue(value[i]);
			}

			writer.WriteEndArray();
		}

		public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.StartArray)
			{
				var byteList = new List<byte>();

				while (reader.Read())
				{
					switch (reader.TokenType)
					{
						case JsonTokenType.Number:
							byteList.Add(reader.GetByte());
							break;
						case JsonTokenType.EndArray:
							return byteList.ToArray();
						case JsonTokenType.Comment:
							break;
						default:
							throw new JsonException(
								$"Unexpected token when reading bytes: {reader.TokenType}");
					}
				}

				throw new JsonException("Unexpected end when reading bytes.");
			}
			else
			{
				throw new JsonException(
					"Unexpected token parsing binary. " + $"Expected StartArray, got {reader.TokenType}.");
			}
		}
	}
}
