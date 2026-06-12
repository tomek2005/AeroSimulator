namespace AeroSimulator.Infrastructure;

public static class Input
{
    public static bool KeyAvailable => !Console.IsInputRedirected && Console.KeyAvailable;

    public static ConsoleKeyInfo ReadKey()
    {
        if (!Console.IsInputRedirected)
        {
            return Console.ReadKey(true);
        }

        var value = Console.Read();
        if (value < 0)
        {
            return new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false);
        }

        var ch = (char)value;
        var key = ch switch
        {
            >= '0' and <= '9' => ConsoleKey.D0 + (ch - '0'),
            >= 'a' and <= 'z' => ConsoleKey.A + (ch - 'a'),
            >= 'A' and <= 'Z' => ConsoleKey.A + (ch - 'A'),
            ' ' => ConsoleKey.Spacebar,
            '\u001b' => ConsoleKey.Escape,
            '\t' => ConsoleKey.Tab,
            _ => ConsoleKey.NoName
        };

        return new ConsoleKeyInfo(ch, key, false, false, false);
    }
}
