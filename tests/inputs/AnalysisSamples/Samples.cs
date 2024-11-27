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
}
