namespace CstPatcher
{
    internal class ScriptLine
    {
        public string ScriptName;
        public int Id;

        public ScriptLineType Type;
        public string Content;

        public string Location => $"{ScriptName}:{Id}";
    }
}