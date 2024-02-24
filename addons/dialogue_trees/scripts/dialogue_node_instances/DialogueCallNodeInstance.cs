using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueCallNodeInstance : DialogueNodeInstance
{
    public int ConnectedFunctionIndex;

    public override void Ready(Array data)
    {
        ConnectedFunctionIndex = data[0].AsInt32();
    }

    public override void RecievePortInput(int portIndex)
    {
        DialogueFunctionNodeInstance connectedFunction = DialogueTree.GetDialogueNodeInstance<DialogueFunctionNodeInstance>(ConnectedFunctionIndex);

        if(connectedFunction == null)
        {
            DialogueTree.EndDialogue();
            return;
        }

        connectedFunction.SendPortOutput(0);
    }
}
