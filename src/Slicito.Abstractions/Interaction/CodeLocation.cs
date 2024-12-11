namespace Slicito.Abstractions.Interaction;

public record CodeLocation(string FullPath, int Line, int Column)
{
    public string Format() => $"{FullPath}:{Line}:{Column}";

    public static CodeLocation? Parse(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var lastColonIndex = value.LastIndexOf(':');
        var secondLastColonIndex = value.LastIndexOf(':', lastColonIndex - 1);
        
        if (lastColonIndex == -1 || secondLastColonIndex == -1)
        {
            throw new FormatException("Invalid code location format. Expected 'path:line:column'");
        }

        var path = value.Substring(0, secondLastColonIndex);
        var line = int.Parse(value.Substring(secondLastColonIndex + 1, lastColonIndex - secondLastColonIndex - 1));
        var column = int.Parse(value.Substring(lastColonIndex + 1));
        
        return new CodeLocation(path, line, column);
    }
}
