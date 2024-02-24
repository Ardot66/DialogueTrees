using Ardot.DialogueTrees.DialogueConditions;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueConditionNodeInstance : DialogueNodeInstance
{
    public DialogueCondition ConnectedCondition;

    public override void Ready(Array data)
    {
        ConnectedCondition = DialogueTree.GetNodeOrNull<DialogueCondition>(data[0].AsNodePath());
    }

    public override void RecievePortInput(int portIndex)
    {   
        if(ConnectedCondition != null && ConnectedCondition.Invoke())
            SendPortOutput(0);
        else
            SendPortOutput(1);
    }
}