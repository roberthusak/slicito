using FluentAssertions;

using Slicito.DotNet.Implementation;
using Slicito.ProgramAnalysis.Strings;

namespace Slicito.DotNet.Tests;

[TestClass]
public class StringPatternCreatorTests
{
    [TestMethod]
    public void Pattern_For_Sample_Validation_Regex_Is_Correct()
    {
        // Arrange & Act
        var pattern = StringPatternCreator.ParseRegex("^[a-z0-9-]{1,64}$");

        // Assert
        pattern.Should().BeEquivalentTo(
            new StringPattern.Loop(
                new StringPattern.Character(
                    new CharacterClass.Union(
                        new CharacterClass.Union(
                            new CharacterClass.Range('a', 'z'),
                            new CharacterClass.Range('0', '9')
                        ),
                        new CharacterClass.Single('-')
                    )
                ),
                1,
                64));
    }

    [TestMethod]
    public void Pattern_Without_Anchors_Contains_Matching_All_Instead()
    {
        // Arrange & Act
        var pattern = StringPatternCreator.ParseRegex("abc");

        // Assert
        pattern.Should().BeEquivalentTo(
            new StringPattern.Sequence(
                new StringPattern.Sequence(
                    StringPattern.All.Instance,
                    new StringPattern.Literal("abc")
                ),
                StringPattern.All.Instance
            )
        );
    }
}
