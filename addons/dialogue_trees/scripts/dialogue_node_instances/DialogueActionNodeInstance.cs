using Ardot.DialogueTrees.DialogueActions;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueActionNodeInstance : DialogueNodeInstance
{
    public DialogueAction ConnectedAction;

    public override void Ready(Array data)
    {
        ConnectedAction = DialogueTree.GetNodeOrNull<DialogueAction>(data[0].AsNodePath());
    }

    public override void RecievePortInput(int portIndex)
    {
        ConnectedAction?.Invoke();

        SendPortOutput(0);
    }
}
