using Godot;
using Godot.Collections;
using Ardot.DialogueTrees.DialogueNodes;

namespace Ardot.DialogueTrees.DialogueVariables;

public abstract partial class DialogueVariableConditionInstance : GodotObject
{
    public DialogueVariableNodeInstance VariableNode;

    public abstract void Ready(Array data);

    public abstract bool RunCondition(Variant variableValue);
}
