using Ardot.DialogueTrees.DialogueConditions;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueConditionNodeInstance : DialogueNodeInstance
{
    public DialogueCondition ConnectedCondition;

    public override void Ready(DialogueNodeSaveData data)
    {
        ConnectedCondition = DialogueTree.GetNodeOrNull<DialogueCondition>(data.General[0].AsNodePath());
    }

    public override void RecievePortInput(int portIndex)
    {   
        if(ConnectedCondition != null && ConnectedCondition.Invoke())
            SendPortOutput(0);
        else
            SendPortOutput(1);
    }
}