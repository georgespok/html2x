using Html2x.Diagnostics.Contracts;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Diagnostics;

internal static class DiagnosticFieldAssertions
{
    public static double NumberField(DiagnosticRecord record, string fieldName)
    {
        return record.Fields[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;
    }

    public static string StringField(DiagnosticRecord record, string fieldName)
    {
        return record.Fields[fieldName].ShouldBeOfType<DiagnosticStringValue>().Value;
    }
}
