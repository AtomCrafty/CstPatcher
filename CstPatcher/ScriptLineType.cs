namespace CstPatcher
{
    internal enum ScriptLineType : byte
    {
        Input = 0x02,
        Page = 0x03,
        Message = 0x20,
        Name = 0x21,
        Command = 0x30,
        ScriptName = 0xF0,
        LineNo = 0xF1
    }
}