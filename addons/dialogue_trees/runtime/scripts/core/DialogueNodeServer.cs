using Godot;
namespace Ardot.DialogueTrees.Runtime;

/// <summary>
/// Special node created during runtime that gives functionality to runtime dialogue nodes.
/// </summary>
public abstract partial class DialogueNodeServer : Node
{	
	/// <summary>
	/// Function for handling when a node recieves an input.<para/>
	/// Output should be handled by pushing DialogueNodeOutputData to the dialogue tree output stack. <para/>
	/// Should return true if execution should continue, and false to temporarily end the dialogue and wait for something to call ContinueDialogue in the DialogueTree.
	/// When ContinueDialogue is called, the AwaitEnded function in this server is also called.
	/// </summary>
	/// <param name="dialogueTree">The dialogue tree that the node that is recieving input is a part of.</param>
	/// <param name="index">The index of the node that is recieving input.</param>
	/// <param name="saveData">The save data of the node that is recieving input.</param>
	/// <param name="inputData">The data that has been passed from the outputting node to the node that is recieving input.</param>
	public abstract bool RecieveInput(DialogueTree dialogueTree, int index, DialogueNodeSaveData saveData, Variant inputData);

	/// <summary>
	/// Called when ContinueDialogue is called in a DialogueTree.<para/>
	/// For information on other parameters, see RecieveInput.
	/// </summary>
	/// <param name="parameters">Optional parameters passed by whatever called ContinueDialogue.</param>
	public virtual void DialogueContinued(DialogueTree dialogueTree, int index, DialogueNodeSaveData saveData, Variant parameters)
	{

	}
}
