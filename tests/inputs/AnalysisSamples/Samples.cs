namespace AnalysisSamples;

public static class Samples
{
    public static int BasicDataFlowSample(int p)
    {
        int a;
        int b = 0;
        int c = 0;

        do
        {
            a = p;
            if (p == 0)
            {
                c = 1;
                break;
            }
            b = 1;
            a = a + 1;
        } while (a < 10);

        if (p == 0)
        {
            return a;
        }
        else
        {
            return b + c;
        }
    }

    public static int BasicSymbolicExecutionSample(int a, int b)
    {
        if (b == 0)
        {
            b = b + 2;
        }
        else
        {
            if (a > 8)
            {
                b = b - a - 1;
            }
            b = b * 2;
        }

        // Another form of Debug.Assert(b != 0) to be checked
        if (b == 0)
        {
            b = b;
        }

        return b;
    }

    public static bool ConditionalReachabilitySample(int a, int b)
    {
        return a == b + 1;
    }

    public static int Caller(int a)
    {
        int res = 0;
        int i = 1;

        while (i <= a)
        {
            res = res + Callee(i);
            i = i + 1;
        }

        VoidCallee(res);

        return Callee(res);
    }

    public static int Callee(int b)
    {
        return b * b;
    }

    public static void VoidCallee(int _) { }

    public static bool StringValidationSample(string s)
    {
        if (s.Length < 8 || s.Length > 16)
        {
            return false;
        }

        if (!s.StartsWith("<") || !s.EndsWith(">"))
        {
            return false;
        }

        return true;
    }

    public static bool RegexValidationSample(string s)
    {
        return Regex.IsMatch(s, "^[a-z0-9-]{1,64}$");
    }
}
