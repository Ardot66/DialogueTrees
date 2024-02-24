using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueOutputNodeInstance : DialogueNodeInstance
{
    public string Character;
    public string OutputText;

    public override void Ready(Array data)
    {
        OutputText = data[0].AsString();
        Character = data[1].AsString();
    }

    public override void RecievePortInput(int portIndex)
    {
        SendDialogueOutput(OutputText, Character);
    }

    public override void RecieveDialogueInput(string dialogue, Variant[] parameters)
    {
        SendPortOutput(0);
    }

    public override DialogueInputOption[] GetDialogueInputOptions()
    {
        return new DialogueInputOption[] {new("")};
    }
}
