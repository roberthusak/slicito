namespace LibraryProject.Implementation;

public class Adder : IAdder
{
    private readonly int _a;
    private readonly int _b;

    public Adder(int a, int b)
    {
        _a = a;
        _b = b;
    }

    public int Add()
    {
        return _a + _b;
    }
}
