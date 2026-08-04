using System;
using System.Text;
using System.Text.RegularExpressions;

namespace OwOTranslator;

public static class Owoifier
{
    private static readonly Random Rng = new();

    private static readonly (Regex Pattern, string Replacement)[] WordSwaps =
    {
        (new Regex(@"\b(you)\b", RegexOptions.IgnoreCase), "uwu"),
        (new Regex(@"\b(no)\b", RegexOptions.IgnoreCase), "nu"),
        (new Regex(@"\b(has)\b", RegexOptions.IgnoreCase), "haz"),
        (new Regex(@"\b(have)\b", RegexOptions.IgnoreCase), "haz"),
        (new Regex(@"\b(the)\b", RegexOptions.IgnoreCase), "da"),
        (new Regex(@"\b(this)\b", RegexOptions.IgnoreCase), "dis"),
        (new Regex(@"\b(small)\b", RegexOptions.IgnoreCase), "smol"),
        (new Regex(@"\b(love)\b", RegexOptions.IgnoreCase), "wuv"),
    };

    private static readonly string[] Faces =
    {
        "owo", "OwO", "UwU", "uwu", ">w<", "^w^", "(・`ω´・)", ":3", "x3", "( ͡o ω ͡o )"
    };

    public enum Intensity
    {
        Leve = 0,
        Normal = 1,
        Extremo = 2
    }

    public static string Transform(string input, Intensity intensity)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var text = input;

        foreach (var (pattern, replacement) in WordSwaps)
            text = pattern.Replace(text, replacement);

        text = ReplaceRlWithW(text);

        text = Regex.Replace(text, "n([aeiouAEIOU])", "ny$1");

        text = AddStutter(text, intensity);

        text = AddFaces(text, intensity);

        return text;
    }

    private static string ReplaceRlWithW(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case 'r':
                case 'l':
                    sb.Append('w');
                    break;
                case 'R':
                case 'L':
                    sb.Append('W');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string AddStutter(string text, Intensity intensity)
    {
        var chance = intensity switch
        {
            Intensity.Leve => 0.05,
            Intensity.Normal => 0.15,
            Intensity.Extremo => 0.35,
            _ => 0.1
        };

        var words = text.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            var w = words[i];
            if (w.Length > 1 && char.IsLetter(w[0]) && Rng.NextDouble() < chance)
                words[i] = $"{w[0]}-{w}";
        }
        return string.Join(' ', words);
    }

    private static string AddFaces(string text, Intensity intensity)
    {
        var chance = intensity switch
        {
            Intensity.Leve => 0.15,
            Intensity.Normal => 0.35,
            Intensity.Extremo => 0.7,
            _ => 0.25
        };

        if (Rng.NextDouble() < chance)
        {
            var face = Faces[Rng.Next(Faces.Length)];
            text = $"{text} {face}";
        }

        if (intensity == Intensity.Extremo)
            text = text.Replace("!", "!! >w<");

        return text;
    }
}
