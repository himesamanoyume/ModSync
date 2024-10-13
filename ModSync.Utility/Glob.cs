namespace ModSync;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class GlobRegex
{
    private static readonly Regex DotRE = new(@"\.", RegexOptions.Compiled);
    private const string DotPattern = @"\.";

    private static readonly Regex RestRE = new(@"\*\*$", RegexOptions.Compiled);
    private const string RestPattern = "(.+)";

    private static readonly Regex GlobRE = new(@"(?:\*\*\/|\*\*|\*)", RegexOptions.Compiled);
    private static readonly Dictionary<string, string> GlobPatterns = new()
    {
        ["*"] = "([^/]+)",      // no backslashes
        ["**"] = "(.+/)?([^/]+)", // short for "**/*"
        ["**/"] = "(.+/)?"      // one or more directories
    };

    private static string MapToPattern(string str)
    {
        return GlobPatterns[str];
    }

    private static string Replace(string glob)
    {
        return GlobRE.Replace(
            RestRE.Replace(
                DotRE.Replace(glob, DotPattern),
                RestPattern
            ),
            match => MapToPattern(match.Value)
        );
    }

    private static string Join(string[] globs)
    {
        return $"(({string.Join(")|(", Array.ConvertAll(globs, Replace))}))";
    }

    public static Regex Glob(object glob)
    {
        var pattern = glob is string[] globArray
            ? Join(globArray)
            : Replace((string)glob);
        return new Regex($"^{pattern}$", RegexOptions.Compiled);
    }

    public static Regex GlobNoEnd(object glob)
    {
        var pattern = glob is string[] globArray
            ? Join(globArray)
            : Replace((string)glob);
        return new Regex($"^{pattern}", RegexOptions.Compiled);
    }
}