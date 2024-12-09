using LibraryProject;

namespace TestProject;

[TestClass]
public class AdderTest
{
    [TestMethod]
    public void TestAdd()
    {
        var adder = AdderProvider.GetAdder(1, 2);
        Assert.AreEqual(3, adder.Add());
    }
}
