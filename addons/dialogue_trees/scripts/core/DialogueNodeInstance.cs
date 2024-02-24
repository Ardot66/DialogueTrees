using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees;

public abstract partial class DialogueNodeInstance : GodotObject
{
	///<summary>The <c>DialogueTree</c> that this instance is connected to.</summary>
	public DialogueTree DialogueTree;

	///<summary>The child index of this <c>DialogueNodeInstance</c>'s respective <c>DialogueNode</c>. Can be used in certain functions, e.g. <c>GetConnectionsToPort(Index, myPort)</c></summary>
	public int Index;

	///<summary>Called when this instance is created, passes the data saved by this <c>DialogueNodeInstance</c>'s respective <c>DialogueNode</c>.</summary>
	public virtual void Ready(Array data)
	{

	}

	///<summary>Called when this node recieves an input signal from another node.</summary>
	public virtual void RecievePortInput(int portIndex)
	{

	}

	///<summary>Called when dialogue input is given to the tree and this is the focused node.</summary>
	public virtual void RecieveDialogueInput(string dialogue, Variant[] parameters)
	{

	}

	///<summary>Returns all possible dialogue input options.</summary>
	public virtual DialogueInputOption[] GetDialogueInputOptions()
	{
		return System.Array.Empty<DialogueInputOption>();
	}

	///<summary>Send a dialogue output to the tree. Mainly used for printing or displaying dialogue.</summary>
	public void SendDialogueOutput(string dialogue, string character, params Variant[] parameters)
	{
		DialogueTree.EmitSignal(DialogueTree.SignalName.DialogueOutput, dialogue, character, new Array (parameters));
	}

	///<summary>Send an output signal to a port. This will activate the node that is connected to that port, or end the conversation if no node is connected.</summary>
	public void SendPortOutput(int portIndex)
	{
		DialogueTreeData.Connection? portConnection = DialogueTree.GetConnectionToPort(Index, portIndex);

		if(portConnection == null)
		{
			DialogueTree.EndDialogue();
			return;
		}

		DialogueNodeInstance connectedNode = DialogueTree.GetDialogueNodeInstance(portConnection.Value.ToNode);
		DialogueTree.FocusedNode = connectedNode;
		connectedNode.RecievePortInput(portConnection.Value.ToPort);
	}
}
