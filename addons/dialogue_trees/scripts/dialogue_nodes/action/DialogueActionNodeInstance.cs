using Ardot.DialogueTrees.DialogueActions;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueActionNodeInstance : DialogueNodeInstance
{
    public DialogueAction ConnectedAction;

    public override void Ready(DialogueNodeSaveData data)
    {
        ConnectedAction = DialogueTree.GetNodeOrNull<DialogueAction>(data.General[0].AsNodePath());
    }

    public override void RecievePortInput(int portIndex)
    {
        ConnectedAction?.Invoke();

        SendPortOutput(0);
    }
}
