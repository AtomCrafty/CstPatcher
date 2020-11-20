namespace CstPatcher
{
    internal sealed class TranslationLine
    {
        public int Id { get; set; }
        public string ScriptName { get; set; }
        public string JapaneseName { get; set; }
        public string JapaneseText { get; set; }
        public string EnglishName { get; set; }
        public string Translation { get; set; }
        public string Edit { get; set; }

        public bool IsTranslated => !string.IsNullOrWhiteSpace(Edit) || !string.IsNullOrWhiteSpace(Translation);

        // lines ending in \@ won't prompt input
        public bool HasAttachedScriptBlock => JapaneseText.Trim().EndsWith("\\@");

        public string EnglishText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Edit)) return Edit;
                if (!string.IsNullOrWhiteSpace(Translation)) return Translation;
                Log.Warn($"Untranslated line at {Location}: {JapaneseText}");
                return JapaneseText;
            }
        }

        internal string Location => $"{ScriptName}:{Id}";

        internal void SanityCheck()
        {
            if (string.IsNullOrEmpty(JapaneseText))
                Log.Error($"Empty japanese text at {Location}");
            if (!IsTranslated)
                Log.Warn($"Untranslated line at {Location}: {JapaneseText}");
            if (EnglishText.Contains("\\@") && !HasAttachedScriptBlock)
                Log.Error($"Unexpected \\@ at {Location}: {JapaneseText}");
            //if (JapaneseText.Contains(' '))
            //    Log.Warn($"Japanese text containing space at {Location}: {JapaneseText}");
            if (!string.IsNullOrWhiteSpace(JapaneseName) && string.IsNullOrWhiteSpace(EnglishName))
                Log.Error($"Untranslated name at {Location}: {JapaneseText}");
            if ((JapaneseName?.Contains("＠") ?? false) && !(EnglishName?.Contains("＠") ?? false))
                Log.Warn($"Name '{JapaneseName}' contains ＠, but '{EnglishName}' doesn't at {Location}");
        }
    }
}