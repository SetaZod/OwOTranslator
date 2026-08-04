using System;
using System.Text;
using System.Text.RegularExpressions;

namespace OwOTranslator;

public static class Nyanifier
{
    private static readonly Random Rng = new();

    private static readonly (Regex Pattern, string[] Options)[] WordSwaps =
    {
        (new Regex(@"\b(you)\b", RegexOptions.IgnoreCase), new[] { "nya", "nyu" }),
        (new Regex(@"\b(your)\b", RegexOptions.IgnoreCase), new[] { "nyour" }),
        (new Regex(@"\b(my)\b", RegexOptions.IgnoreCase), new[] { "nyah", "myah" }),
        (new Regex(@"\b(no)\b", RegexOptions.IgnoreCase), new[] { "nyo" }),
        (new Regex(@"\b(not)\b", RegexOptions.IgnoreCase), new[] { "nyat" }),
        (new Regex(@"\b(that)\b", RegexOptions.IgnoreCase), new[] { "nyat" }),
        (new Regex(@"\b(now)\b", RegexOptions.IgnoreCase), new[] { "nyow" }),
        (new Regex(@"\b(please)\b", RegexOptions.IgnoreCase), new[] { "nyease" }),
        (new Regex(@"\b(okay|ok)\b", RegexOptions.IgnoreCase), new[] { "nyokay" }),
        (new Regex(@"\b(hello|hi)\b", RegexOptions.IgnoreCase), new[] { "nyahello", "meow" }),
    };

    private static readonly (Regex Pattern, string Replacement)[] CatPuns =
    {
        (new Regex(@"\bperfect(ly)?\b", RegexOptions.IgnoreCase), "purr-fect$1"),
        (new Regex(@"\bhysterical(ly)?\b", RegexOptions.IgnoreCase), "hiss-terical$1"),
        (new Regex(@"\bmarvelous(ly)?\b", RegexOptions.IgnoreCase), "meow-velous$1"),
        (new Regex(@"\bterrific(ally)?\b", RegexOptions.IgnoreCase), "purr-rific$1"),
        (new Regex(@"\bcurious(ly)?\b", RegexOptions.IgnoreCase), "cat-urious$1"),
        (new Regex(@"\bawesome(ly)?\b", RegexOptions.IgnoreCase), "paw-some$1"),
        (new Regex(@"\battention\b", RegexOptions.IgnoreCase), "cat-tention"),
    };

    private static readonly Regex NVowelPattern = new(@"n([aeiouAEIOU])", RegexOptions.IgnoreCase);

    private static readonly string[] Faces =
    {
        "=^..^=", ":3", "^•ω•^", "(=^･ω･^=)", "(=ФωФ=)", "*purr*", ">w<"
    };

    private static readonly (string Word, string[] Decorations)[] TicBank =
    {
        ("nya", new[] { "", "~", "!", "~~" }),
        ("nyaa", new[] { "", "~", "~~" }),
        ("nyan", new[] { "", "!" }),
        ("nyah", new[] { "", "~", "!", "~~" }),
    };

    public static string Transform(string input, Owoifier.Intensity intensity)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var scale = intensity switch
        {
            Owoifier.Intensity.Leve => 0.35,
            Owoifier.Intensity.Normal => 0.65,
            Owoifier.Intensity.Extremo => 1.0,
            _ => 0.5
        };

        var text = input;

        foreach (var (pattern, options) in WordSwaps)
            text = pattern.Replace(text, m => RandomPick(options));

        foreach (var (pattern, replacement) in CatPuns)
            text = pattern.Replace(text, replacement);

        text = NVowelPattern.Replace(text, "ny$1");

        text = AddStutter(text, scale);
        text = AddMeowInterjections(text, scale);
        text = AddNyaTics(text, scale);
        text = AddFinalFace(text, scale);

        return text;
    }

    private static string RandomPick(string[] options) => options[Rng.Next(options.Length)];

    private static string AddStutter(string text, double scale)
    {
        var chance = 0.20 * scale;
        var words = text.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            var w = words[i];
            if (w.Length > 1 && char.IsLetter(w[0]) && Rng.NextDouble() < chance)
                words[i] = $"{w[0]}-{w}";
        }
        return string.Join(' ', words);
    }

    private static readonly Regex ExclamatorySentenceStart = new(
        @"(^|[.!?]\s+)([A-Z])");

    private static string AddMeowInterjections(string text, double scale)
    {
        var chance = 0.18 * scale;

        return ExclamatorySentenceStart.Replace(text, m =>
        {
            if (Rng.NextDouble() >= chance)
                return m.Value;

            return $"{m.Groups[1].Value}Meow! {m.Groups[2].Value}";
        });
    }

    private static readonly Regex ClauseBoundary = new(@"(\w+)(,|[.!?]+|$)");

    private static string AddNyaTics(string text, double scale)
    {
        var chance = 0.55 * scale;

        return ClauseBoundary.Replace(text, m =>
        {
            if (Rng.NextDouble() >= chance)
                return m.Value;

            var word = m.Groups[1].Value;
            var boundary = m.Groups[2].Value;
            var (ticWord, decorations) = TicBank[Rng.Next(TicBank.Length)];
            var tic = ticWord + decorations[Rng.Next(decorations.Length)];

            if (boundary == ",")
                return $"{word}, {tic}";

            return Rng.Next(3) switch
            {
                0 => $"{word}-{tic}{boundary}",        
                1 => $"{word} {tic}{boundary}",          
                _ => $"{word}{boundary} {tic}",         
            };
        });
    }

    private static string AddFinalFace(string text, double scale)
    {
        var chance = 0.5 * scale;

        if (Rng.NextDouble() < chance)
        {
            var face = Faces[Rng.Next(Faces.Length)];
            text = $"{text} {face}";
        }

        return text;
    }
}
