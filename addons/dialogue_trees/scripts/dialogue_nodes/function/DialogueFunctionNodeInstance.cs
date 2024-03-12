using Godot;
using System;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueFunctionNodeInstance : DialogueNodeInstance
{
    public void CallFunction()
    {
        SendPortOutput(0);
    }
}
