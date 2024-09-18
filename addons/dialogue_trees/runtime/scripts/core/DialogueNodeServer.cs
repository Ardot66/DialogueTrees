using Godot;
namespace Ardot.DialogueTrees.Runtime;

/// <summary>
/// Special node created during runtime that gives functionality to runtime dialogue nodes.
/// </summary>
public abstract partial class DialogueNodeServer : Node
{	
	/// <summary>
	/// Function for handling when a node recieves an input.<br/>
	/// Output should be handled by pushing DialogueNodeOutputData to the dialogue tree output stack.
	/// </summary>
	/// <param name="dialogueTree">The dialogue tree that the node that is recieving input is a part of.</param>
	/// <param name="index">The index of the node that is recieving input.</param>
	/// <param name="saveData">The save data of the node that is recieving input.</param>
	/// <param name="inputData">The data that has been passed from the outputting node to the node that is recieving input.</param>
	public abstract void InputRecieved(DialogueTree dialogueTree, int index, DialogueNodeSaveData saveData, Variant inputData);
}
