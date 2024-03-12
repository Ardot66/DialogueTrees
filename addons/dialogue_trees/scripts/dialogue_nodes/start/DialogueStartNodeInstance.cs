using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees;

public partial class DialogueStartNodeInstance : DialogueNodeInstance
{
	public void Start()
	{
		SendPortOutput(0);
	}
}
