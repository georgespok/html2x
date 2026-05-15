using Html2x.Diagnostics.Contracts;
using Shouldly;

namespace Html2x.TestSupport;

internal static class DiagnosticFieldReader
{
    public static DiagnosticArray ArrayField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticArray>();

    public static DiagnosticArray ArrayField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticArray>();

    public static DiagnosticObject ObjectField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticObject>();

    public static string StringField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticStringValue>().Value;

    public static string StringField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticStringValue>().Value;

    public static string StringFieldOrEmpty(DiagnosticObject value, string fieldName) =>
        StringFieldOrNull(value, fieldName) ?? string.Empty;

    public static string? StringFieldOrNull(DiagnosticObject value, string fieldName) =>
        value[fieldName] is DiagnosticStringValue stringValue ? stringValue.Value : null;

    public static double NumberField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    public static double NumberField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    public static bool BoolField(DiagnosticRecord record, string fieldName) =>
        record.Fields[fieldName].ShouldBeOfType<DiagnosticBooleanValue>().Value;

    public static bool? NullableBoolField(DiagnosticObject value, string fieldName) =>
        value[fieldName] is DiagnosticBooleanValue boolValue ? boolValue.Value : null;

    public static void AssertStringField(DiagnosticRecord record, string fieldName, string expected) =>
        StringField(record, fieldName).ShouldBe(expected);
}
