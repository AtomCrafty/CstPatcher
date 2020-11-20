using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Excel = Microsoft.Office.Interop.Excel;

namespace CstPatcher
{
    internal sealed class TranslationData
    {
        public readonly List<TranslationLine> Lines;
        public readonly Dictionary<string, string> Names;

        public TranslationData(List<TranslationLine> lines, Dictionary<string, string> names)
        {
            Lines = lines;
            Names = names;
        }

        public IEnumerable<TranslationLine> LinesFor(string scriptName) =>
            Lines.Where(line => line.ScriptName == scriptName);

        public static TranslationData ImportFrom(string xlsxPath)
        {
            const string TranslationWorkSheetName = "Translation";
            const int IdColumn = 1;
            const int ScriptNameColumn = 2;
            const int JapaneseNameColumn = 3;
            const int JapaneseTextColumn = 4;
            const int EnglishNameColumn = 5;
            const int TranslationColumn = 6;
            const int EditColumn = 7;

            var nameRegex = new Regex("^(?<complete>(?<name>.+?)(?:＠(?<disp>\\S+)))(?:\\s*//.*)??$");
            var lines = new List<TranslationLine>();
            var names = new Dictionary<string, string>();
            var excel = new Excel.Application { DisplayAlerts = false };
            var workbook = excel.Workbooks.Open(xlsxPath);
            try
            {
                var tlSheet = workbook.Worksheets[TranslationWorkSheetName];
                var values = (object[,])tlSheet.UsedRange.Value;
                int lastRow = values.GetUpperBound(0) - 1;

                for (int row = 2; row < lastRow; row++)
                {
                    if (!int.TryParse(values[row, IdColumn] as string, out int id))
                        break;

                    string japaneseText = values[row, JapaneseTextColumn] as string;
                    string translation = values[row, TranslationColumn] as string;
                    string edit = values[row, EditColumn] as string;

                    string japaneseName = values[row, JapaneseNameColumn] as string;
                    string englishName = values[row, EnglishNameColumn] as string;
                    string scriptName = Path.GetFileNameWithoutExtension(values[row, ScriptNameColumn] as string);

                    if (!string.IsNullOrWhiteSpace(japaneseName))
                    {
                        var match = nameRegex.Match(japaneseName);
                        if (match.Success)
                        {
                            //string internalName = match.Groups["name"].Value;
                            //string displayName = match.Groups["disp"].Value;
                            japaneseName = match.Groups["complete"].Value;
                        }

                        if (string.IsNullOrWhiteSpace(englishName))
                        {
                            Log.Warn($"Untranslated name at {scriptName}:{id}: - {japaneseName}");
                        }
                        else if (!names.ContainsKey(japaneseName))
                        {
                            Console.WriteLine(japaneseName + "      -> " + englishName);
                            names[japaneseName] = englishName;
                        }
                        else if (names[japaneseName] != englishName)
                        {
                            Log.Warn($"Mismatching name translation at {scriptName}:{id}: '{japaneseName}' was translated as '{englishName}' and '{names[japaneseName]}'");
                        }
                    }

                    var line = new TranslationLine
                    {
                        Id = id,
                        ScriptName = scriptName,
                        JapaneseName = japaneseName,
                        EnglishName = englishName,
                        JapaneseText = japaneseText,
                        Translation = translation,
                        Edit = edit,
                    };
                    line.SanityCheck();
                    lines.Add(line);
                }
            }
            finally
            {
                workbook.Close();
                excel.Quit();
                Marshal.FinalReleaseComObject(excel);
            }

            return new TranslationData(lines, names);
        }
    }
}