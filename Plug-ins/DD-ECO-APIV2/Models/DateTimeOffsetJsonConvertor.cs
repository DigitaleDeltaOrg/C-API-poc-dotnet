using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DD_ECO_API.Models;
#pragma warning disable 1591
public class DateTimeOffsetJsonConvertor : JsonConverter<DateTimeOffset>
{
	public override DateTimeOffset Read(
		ref Utf8JsonReader    reader,
		Type                  typeToConvert,
		JsonSerializerOptions options) =>
		DateTimeOffset.ParseExact(reader.GetString() ?? string.Empty, "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

	public override void Write(
		Utf8JsonWriter        writer,
		DateTimeOffset        dateTimeValue,
		JsonSerializerOptions options) =>
		writer.WriteStringValue(dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
}
	
public class NullableDateTimeOffsetJsonConvertor : JsonConverter<DateTimeOffset?>
{
	public override DateTimeOffset? Read(
		ref Utf8JsonReader    reader,
		Type                  typeToConvert,
		JsonSerializerOptions options) =>
		DateTimeOffset.ParseExact(reader.GetString() ?? string.Empty, "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

	public override void Write(
		Utf8JsonWriter        writer,
		DateTimeOffset?       dateTimeValue,
		JsonSerializerOptions options) =>
		writer.WriteStringValue(dateTimeValue?.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
}
#pragma warning restore 1591