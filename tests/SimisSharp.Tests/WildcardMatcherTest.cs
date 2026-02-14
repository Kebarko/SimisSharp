namespace KE.MSTS.SimisSharp.Tests;

public class WildcardMatcherTest
{
    [Theory]
    // =========================
    // Empty / Null-Like Cases
    // =========================
    [InlineData("", "", true)]
    [InlineData("", "*", true)]
    [InlineData("", "**", true)]
    [InlineData("", "?", false)]
    [InlineData("", "?*", false)]
    [InlineData("a", "", false)]

    // =========================
    // Single Character Cases
    // =========================
    [InlineData("a", "a", true)]
    [InlineData("a", "b", false)]
    [InlineData("a", "?", true)]
    [InlineData("a", "*", true)]
    [InlineData("a", "**", true)]
    [InlineData("a", "??", false)]
    [InlineData("a", "*?", true)]
    [InlineData("a", "?*", true)]

    // =========================
    // Only Literal Matching
    // =========================
    [InlineData("abc", "abc", true)]
    [InlineData("abc", "abcd", false)]
    [InlineData("abcd", "abc", false)]
    [InlineData("abc", "abx", false)]

    // =========================
    // Only '*' Patterns
    // =========================
    [InlineData("abc", "*", true)]
    [InlineData("abc", "**", true)]
    [InlineData("abc", "***", true)]

    // =========================
    // '?' Wildcards
    // =========================
    [InlineData("abc", "???", true)]
    [InlineData("abc", "??", false)]
    [InlineData("abc", "a?c", true)]
    [InlineData("abc", "?b?", true)]
    [InlineData("abc", "?a?", false)]

    // =========================
    // Mixed '?' and '*'
    // =========================
    [InlineData("abc", "*?", true)]
    [InlineData("abc", "?*", true)]
    [InlineData("abc", "*?*", true)]
    [InlineData("abc", "*a*", true)]
    [InlineData("abc", "*b*", true)]
    [InlineData("abc", "*d*", false)]
    [InlineData("abc", "a*?", true)]
    [InlineData("abc", "a*?d", false)]
    [InlineData("abc", "*a", false)]
    [InlineData("abc", "c*", false)]

    // =========================
    // Consecutive '*' (Normalization)
    // =========================
    [InlineData("abc", "a**c", true)]
    [InlineData("abc", "a***c", true)]
    [InlineData("abc", "***a**b**c***", true)]
    [InlineData("abc", "***a**b**d***", false)]

    // =========================
    // Leading and Trailing '*'
    // =========================
    [InlineData("abcdef", "*def", true)]
    [InlineData("abcdef", "abc*", true)]
    [InlineData("abcdef", "*cd*", true)]
    [InlineData("abcdef", "*ce*", false)]

    // =========================
    // Minimum Length Boundary (Pruning)
    // =========================
    [InlineData("abc", "abcd*", false)]   // required chars > text
    [InlineData("abc", "????", false)]
    [InlineData("abc", "?a?c", false)]

    // =========================
    // Classic Tricky Cases
    // =========================
    [InlineData("adceb", "*a*b", true)]
    [InlineData("acdcb", "a*c?b", false)]
    [InlineData("aa", "a", false)]
    [InlineData("aa", "*", true)]
    [InlineData("cb", "?a", false)]

    // =========================
    // Complex Patterns
    // =========================
    [InlineData("mississippi", "m*iss*?ppi", true)]
    [InlineData("mississippi", "m*iss*?ppx", false)]
    [InlineData("abefcdgiescdfimde", "ab*cd?i*de", true)]
    public void  IsMatchTest(string text, string pattern, bool expected)
    {
        // Act
        bool result = WildcardMatcher.IsMatch(text, pattern);

        // Assert
        Assert.Equal(expected, result);
    }
}
