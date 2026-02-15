namespace KE.MSTS.SimisSharp.Tests;

public class WildcardMatcherTest
{
    [Theory]
    // =========================
    // Empty / Null-Like Cases
    // =========================
    [InlineData("", "", false, true)]
    [InlineData("", "*", false, true)]
    [InlineData("", "**", false, true)]
    [InlineData("", "?", false, false)]
    [InlineData("", "?*", false, false)]
    [InlineData("a", "", false, false)]

    // =========================
    // Single Character Cases
    // =========================
    [InlineData("a", "a", false, true)]
    [InlineData("a", "b", false, false)]
    [InlineData("a", "?", false, true)]
    [InlineData("a", "*", false, true)]
    [InlineData("a", "**", false, true)]
    [InlineData("a", "??", false, false)]
    [InlineData("a", "*?", false, true)]
    [InlineData("a", "?*", false, true)]

    // =========================
    // Only Literal Matching
    // =========================
    [InlineData("abc", "abc", false, true)]
    [InlineData("abc", "abcd", false, false)]
    [InlineData("abcd", "abc", false, false)]
    [InlineData("abc", "abx", false, false)]

    // =========================
    // Only '*' Patterns
    // =========================
    [InlineData("abc", "*", false, true)]
    [InlineData("abc", "**", false, true)]
    [InlineData("abc", "***", false, true)]

    // =========================
    // '?' Wildcards
    // =========================
    [InlineData("abc", "???", false, true)]
    [InlineData("abc", "??", false, false)]
    [InlineData("abc", "a?c", false, true)]
    [InlineData("abc", "?b?", false, true)]
    [InlineData("abc", "?a?", false, false)]

    // =========================
    // Mixed '?' and '*'
    // =========================
    [InlineData("abc", "*?", false, true)]
    [InlineData("abc", "?*", false, true)]
    [InlineData("abc", "*?*", false, true)]
    [InlineData("abc", "*a*", false, true)]
    [InlineData("abc", "*b*", false, true)]
    [InlineData("abc", "*d*", false, false)]
    [InlineData("abc", "a*?", false, true)]
    [InlineData("abc", "a*?d", false, false)]
    [InlineData("abc", "*a", false, false)]
    [InlineData("abc", "c*", false, false)]

    // =========================
    // Consecutive '*' (Normalization)
    // =========================
    [InlineData("abc", "a**c", false, true)]
    [InlineData("abc", "a***c", false, true)]
    [InlineData("abc", "***a**b**c***", false, true)]
    [InlineData("abc", "***a**b**d***", false, false)]

    // =========================
    // Leading and Trailing '*'
    // =========================
    [InlineData("abcdef", "*def", false, true)]
    [InlineData("abcdef", "abc*", false, true)]
    [InlineData("abcdef", "*cd*", false, true)]
    [InlineData("abcdef", "*ce*", false, false)]

    // =========================
    // Minimum Length Boundary (Pruning)
    // =========================
    [InlineData("abc", "abcd*", false, false)]
    [InlineData("abc", "????", false, false)]
    [InlineData("abc", "?a?c", false, false)]

    // =========================
    // Classic Tricky Cases
    // =========================
    [InlineData("adceb", "*a*b", false, true)]
    [InlineData("acdcb", "a*c?b", false, false)]
    [InlineData("aa", "a", false, false)]
    [InlineData("aa", "*", false, true)]
    [InlineData("cb", "?a", false, false)]

    // =========================
    // Complex Patterns
    // =========================
    [InlineData("mississippi", "m*iss*?ppi", false, true)]
    [InlineData("mississippi", "m*iss*?ppx", false, false)]
    [InlineData("abefcdgiescdfimde", "ab*cd?i*de", false, true)]

    // =========================
    // Case-Insensitive Matching
    // =========================
    [InlineData("ABC", "abc", true, true)]
    [InlineData("abc", "ABC", true, true)]
    [InlineData("AbC", "aBc", true, true)]
    [InlineData("AbC", "a?c", true, true)]
    [InlineData("AbC", "*b*", true, true)]
    [InlineData("HelloWorld", "he*ld", true, true)]
    [InlineData("HelloWorld", "he*lx", true, false)]
    [InlineData("CaseTest", "casetest", false, false)]
    public void IsMatchTest(string text, string pattern, bool ignoreCase, bool expected)
    {
        // Act
        bool result = WildcardMatcher.IsMatch(text, pattern, ignoreCase);

        // Assert
        Assert.Equal(expected, result);
    }
}
