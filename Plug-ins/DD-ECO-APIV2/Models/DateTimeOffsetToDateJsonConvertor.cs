using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DD_ECO_API.Models;
#pragma warning disable 1591
public class DateTimeOffsetToYearJsonConvertor : JsonConverter<DateTimeOffset>
{
	public override DateTimeOffset Read(
		ref Utf8JsonReader    reader,
		Type                  typeToConvert,
		JsonSerializerOptions options) =>
		DateTimeOffset.ParseExact(reader.GetString() ?? string.Empty, "dd-MM-yyyy", CultureInfo.InvariantCulture);

	public override void Write(
		Utf8JsonWriter        writer,
		DateTimeOffset        dateTimeValue,
		JsonSerializerOptions options) =>
		writer.WriteStringValue(dateTimeValue.UtcDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
}
	
public class NullableDateTimeOffsetToYearJsonConvertor : JsonConverter<DateTimeOffset?>
{
	public override DateTimeOffset? Read(
		ref Utf8JsonReader    reader,
		Type                  typeToConvert,
		JsonSerializerOptions options) =>
		DateTimeOffset.ParseExact(reader.GetString() ?? string.Empty, "dd-MM-yyyy", CultureInfo.InvariantCulture);

	public override void Write(
		Utf8JsonWriter        writer,
		DateTimeOffset?       dateTimeValue,
		JsonSerializerOptions options) =>
		writer.WriteStringValue(dateTimeValue?.UtcDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
}

#pragma warning restore 1591