using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

public partial class DialogueEnumConditionInstance : DialogueVariableConditionInstance
{
    public string _selectedEnum;

    public override void Ready(Array data)
    {
        _selectedEnum = data[0].AsString();
    }

    public override bool RunCondition(Variant variableValue)
    {
        return variableValue.AsString() == _selectedEnum;
    }
}
