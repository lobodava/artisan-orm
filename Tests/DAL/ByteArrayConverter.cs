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

			byte[] data = (byte[])value;

			// Compose an array.
			writer.WriteStartArray();

			for (var i = 0; i < data.Length; i++)
			{
				JsonSerializer.Serialize(writer, data[i], options);
			}

			writer.WriteEndArray();
		}

		public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{

			if (reader.TokenType != JsonTokenType.StartArray)
			{
				throw new JsonException();
			}
			reader.Read();

			var elements = new Stack<byte>();

			while (reader.TokenType != JsonTokenType.EndArray)
			{
				elements.Push(JsonSerializer.Deserialize<byte>(ref reader, options));

				reader.Read();
			}

			return elements.ToArray();


			//if (reader.TokenType == JsonToken.StartArray)
			//{
			//	var byteList = new List<byte>();

			//	while (reader.Read())
			//	{
			//		switch (reader.TokenType)
			//		{
			//			case JsonToken.Integer:
			//				byteList.Add(Convert.ToByte(reader.Value));
			//				break;
			//			case JsonToken.EndArray:
			//				return byteList.ToArray();
			//			case JsonToken.Comment:
			//				// skip
			//				break;
			//			default:
			//				throw new Exception(
			//					$"Unexpected token when reading bytes: {reader.TokenType}");
			//		}
			//	}

			//	throw new Exception("Unexpected end when reading bytes.");
			//}
			//else
			//{
			//	throw new Exception(
			//		"Unexpected token parsing binary. " + $"Expected StartArray, got {reader.TokenType}.");
			//}
		}

		//public override bool CanConvert(Type objectType)
		//{
		//	return objectType == typeof(byte[]);
		//}
	}
}