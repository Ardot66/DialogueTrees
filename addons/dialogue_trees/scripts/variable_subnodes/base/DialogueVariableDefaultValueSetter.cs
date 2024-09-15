# if TOOLS

using Godot;
using Ardot.DialogueTrees.DialogueNodes;

namespace Ardot.DialogueTrees.DialogueVariables;

///<summary>Handles the UI for setting the default value of a dialogue variable of a certain type.</summary>
[Tool]
public abstract partial class DialogueVariableDefaultValueSetter : VBoxContainer
{
	public DialogueVariableNode _variableNode;
	///<summary>The <c>DialogueVariableNode</c> that created this <c>DialogueVariableDefaultValueSetterNode</c>.</summary>
	public DialogueVariableNode VariableNode {get => _variableNode;}

	///<summary>Returns the selected default value of this node.</summary>
	public abstract Variant GetValue();

	///<summary>Called on load to set the selected default value of this node.</summary>
	public abstract void SetValue(Variant variant);

	public void Setup(DialogueVariableNode dialogueVariableNode)
	{
		if(_variableNode != null)
			return;

		_variableNode = dialogueVariableNode;

		OnSetup();
	}

	protected virtual void OnSetup()
	{

	}
}

# endif