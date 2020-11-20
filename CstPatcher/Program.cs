using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CstPatcher
{
    internal class Program
    {
        public static Encoding ShiftJis = Encoding.GetEncoding("Shift-Jis", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        private static readonly string Cwd = Directory.GetCurrentDirectory();
        private static readonly string ScriptInputPath = Path.Combine(Cwd, "scene/original");
        private static readonly string ScriptOutputPath = Path.Combine(Cwd, "scene");
        private static readonly string TranslationFilePath = Path.Combine(Cwd, "Translation.xlsx");
        private static readonly string[] IgnoredScripts = { "character_test", "test", "start" };
        private static readonly bool CompressScripts = true;

        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var tlData = TranslationData.ImportFrom(TranslationFilePath);
            TranslateScripts(ScriptInputPath, ScriptOutputPath, tlData);
            Console.ReadLine();
        }

        private static void TranslateScripts(string inPath, string outPath, TranslationData tlData)
        {
            foreach (string filePath in Directory.EnumerateFiles(inPath, "*.cst", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(filePath);
                string targetPath = Path.Combine(outPath, fileName);
                TranslateScript(filePath, targetPath, tlData);
            }
        }

        private static void TranslateScript(string inPath, string outPath, TranslationData tlData)
        {
            string baseName = Path.GetFileNameWithoutExtension(inPath);

            if (IgnoredScripts.Contains(baseName))
            {
                File.Copy(inPath, outPath, true);
                return;
            }

            var script = new CstScript();
            using (var fs = File.OpenRead(inPath)) script.ReadFrom(fs, baseName);

            var lastType = ScriptLineType.Command;
            for (int i = 0; i < script.Lines.Count; i++)
            {
                var line = script.Lines[i];
                if (line.Type == ScriptLineType.Message && lastType == ScriptLineType.Message)
                {
                    Debug.Assert(line.Content.StartsWith("\\n"));
                    //Log.Info($"Merging lines {baseName}:{i - 1} and {baseName}:{i}");
                    //Log.Info("    " + script.Lines[i - 1].Content);
                    //Log.Info("  + " + line.Content.Substring(2));
                    script.Lines[i - 1].Content += line.Content.Substring(2);
                    script.DeleteLine(i);
                    i--;
                }
                lastType = line.Type;
            }

            var lines = tlData.LinesFor(baseName);
            using var enumerator = lines.GetEnumerator();

            foreach (var scriptLine in script.Lines)
            {
                if (scriptLine.Content.Length == 0) continue;

                switch (scriptLine.Type)
                {
                    case ScriptLineType.Message:

                        if (!enumerator.MoveNext())
                            throw new Exception($"Not enough translation lines for {scriptLine.Location}\n    {scriptLine.Content}");

                        var tlLine = enumerator.Current;

                        if (scriptLine.Content != tlLine!.JapaneseText)
                            throw new Exception($"Mismatching japanese text in {scriptLine.Location}\n    {scriptLine.Content}\n    {tlLine.JapaneseText}");

                        scriptLine.Content = ProcessText(scriptLine, tlLine.EnglishText);

                        break;

                    case ScriptLineType.Name:
                        if (!tlData.Names.ContainsKey(scriptLine.Content))
                            throw new Exception($"Untranslated name in {baseName}: {scriptLine.Content}");

                        scriptLine.Content = tlData.Names[scriptLine.Content];

                        break;
                }
            }

            using (var fs = File.OpenWrite(outPath)) script.WriteTo(fs, CompressScripts);
        }

        private static string ProcessText(ScriptLine line, string text)
        {
            // Prevent word wrapping in the middle of words: CS2 "[word]" syntax
            text = Regex.Replace(text, @"\S+", m => Regex.IsMatch(m.Value, @"\\[^@]") ? m.Value : $"[{m.Value}]");

            text = ReplaceSpecialChars(text);
            text = text.Replace("\r\n", "\\n");

            if (line.Content.EndsWith("\\@"))
                text += "\\@";

            return text;
        }

        private static string ReplaceSpecialChars(string str)
        {
            StringBuilder result = new StringBuilder(str.Length);
            byte[] bytes = new byte[2];
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '\'' || c == '"')
                {
                    result.Append($@"\${(int)c};");
                }
                else
                {
                    try
                    {
                        ShiftJis.GetBytes(str, i, 1, bytes, 0);
                        result.Append(str[i]);
                    }
                    catch
                    {
                        result.Append($@"\${(int)c};");
                    }
                }
            }
            return result.ToString();
        }
    }
}
