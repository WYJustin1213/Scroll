using System;

[Serializable]
public class RuntimeInstruction
{
    public InstructionType type;
    public string originalText;

    public RuntimeInstruction(InstructionType type, string originalText)
    {
        this.type = type;
        this.originalText = originalText;
    }
}
