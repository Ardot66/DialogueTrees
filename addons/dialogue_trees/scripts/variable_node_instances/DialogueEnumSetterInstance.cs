using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

public partial class DialogueEnumSetterInstance : DialogueVariableSetterInstance
{
    private StringName _enumName;

    public override void Ready(Array data)
    {
        _enumName = data[0].AsStringName();
    }

    public override Variant SetVariable(Variant previousVariable)
    {
        return _enumName;
    }
}
