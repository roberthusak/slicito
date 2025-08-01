namespace Slicito.DotNet;

public static class DotNetAttributeValues
{
    public static class Runtime
    {
        public const string DotNet = "DotNet";
    }

    public static class Language
    {
        public const string CSharp = "CSharp";
    }

    public static class Kind
    {
        public const string Solution = "Solution";
        public const string Project = "Project";
        public const string Namespace = "Namespace";
        public const string Type = "Type";
        public const string Property = "Property";
        public const string Field = "Field";
        public const string Method = "Method";
        public const string LocalFunction = "LocalFunction";
        public const string Lambda = "Lambda";
        public const string Operation = "Operation";

        public const string References = "References";
        public const string Overrides = "Overrides";
        public const string Calls = "Calls";
    }

    public static class OperationKind
    {
        public const string Assignment = "Assignment";
        public const string ConditionalJump = "ConditionalJump";
        public const string Call = "Call";
    }
}
