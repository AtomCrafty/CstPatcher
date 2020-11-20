using System;

namespace CstPatcher
{
    internal static class Log
    {
        public static void WriteLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void Info(string text) => WriteLine(text, ConsoleColor.Cyan);
        public static void Warn(string text) => WriteLine(text, ConsoleColor.Yellow);
        public static void Error(string text) => WriteLine(text, ConsoleColor.Red);
    }
}