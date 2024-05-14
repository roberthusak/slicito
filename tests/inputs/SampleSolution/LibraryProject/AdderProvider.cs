namespace LibraryProject;

public static class AdderProvider
{
    public static IAdder GetAdder(int a, int b)
    {
        return new Implementation.Adder(a, b);
    }
}
