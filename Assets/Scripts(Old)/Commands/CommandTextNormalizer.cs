using System.Text;

public static class CommandTextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Trim().ToLowerInvariant();

        // Allow the final period to be optional.
        while (text.EndsWith("."))
        {
            text = text.Substring(
                0,
                text.Length - 1
            ).TrimEnd();
        }

        // Collapse repeated whitespace.
        StringBuilder builder = new StringBuilder();
        bool previousWasWhitespace = false;

        foreach (char character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }
            }
            else
            {
                builder.Append(character);
                previousWasWhitespace = false;
            }
        }

        return builder.ToString().Trim();
    }
}
