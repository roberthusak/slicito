using System.Reflection;

namespace Slicito.Common.Implementation.Reflection;

internal static class MemberSignatureFormatter
{
    public static string Format(MemberInfo member)
    {
        if (member is not MethodInfo method)
        {
            return $"{member.DeclaringType?.Name}.{member.Name}";
        }

        var parameterTypes = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));

        return $"{member.DeclaringType?.Name}.{member.Name}({parameterTypes})";
    }
}