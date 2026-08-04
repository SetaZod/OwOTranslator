namespace OwOTranslator;

public static class Translator
{
    public static string Transform(string input, TranslatorMode mode, Owoifier.Intensity intensity)
    {
        return mode switch
        {
            TranslatorMode.Nyan => Nyanifier.Transform(input, intensity),
            _ => Owoifier.Transform(input, intensity),
        };
    }
}
