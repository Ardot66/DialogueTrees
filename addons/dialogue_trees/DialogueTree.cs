using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.Runtime;

[Tool]
[GlobalClass]
[Icon("res://addons/dialogue_trees/icons/dialogue_tree_icon.svg")]
public partial class DialogueTree : Node
{
	///<summary>Called when the conversation is ended by a node in the tree.</summary>
	[Signal]
	public delegate void DialogueEndedEventHandler();

	///<summary>Called when dialogue is output by the tree. Mainly used for printing or displaying dialogue. By default, an empty input has to be given to the tree after it gives an output for it to continue, this is mainly to allow writing output over time. The <c>parameters</c> field can be used for special cases, such as for distinguishing multi-character dialogue.</summary>
	[Signal]
	public delegate void DialogueOutputEventHandler(string dialogue, string character, Array parameters = null);

	[Signal]
	public delegate void TreeDataChangedEventHandler(DialogueTreeData newTreeData);

	private DialogueTreeData _treeData;

	[Export]
	public DialogueTreeData TreeData
	{
		get => _treeData;

		private set
		{
			EmitSignal(SignalName.TreeDataChanged, value);

			_treeData = value;
		}
	}

	private int _currentNode;

	private readonly Stack<DialogueNodeOutputData> _outputStack = new ();
	private readonly List<Variant> _stack = new ();
	private readonly Stack<StackFrame> _stackFrames = new ();

	private readonly struct StackFrame
	{
		public StackFrame (int stackBeginIndex, int beginNode, bool clearingStackFrame)
		{
			StackBeginIndex = stackBeginIndex;
			BeginNode = beginNode;
			ClearingStackFrame = clearingStackFrame;
		}

		/// <summary>
		/// The index in the stack where this stack frame begins.
		/// </summary>
		public readonly int StackBeginIndex;

		/// <summary>
		/// The index of the node at the beginning of this stack frame.
		/// </summary>
		public readonly int BeginNode;
		
		/// <summary>
		/// If true, this stack frame is effectively 'isolated' from all previous stack frames; in that future nodes cannot access variable in previous stack frames.
		/// In practice, this means that if a node's stack frame end is -2 and the stack should be completely erased, then when this stack frame is going to be erased,
		/// it won't. All variables declared after this stack frame will still be destroyed, but the stack frame shall remain.
		/// </summary>
		public readonly bool ClearingStackFrame;
	}

	public void StartDialogue()
	{   
		// if(TreeData == null)
		// {
		// 	EndDialogue();
		// 	return;
		// }

		// DialogueStartNodeInstance startNode = (DialogueStartNodeInstance)GetFirstDialogueNodeOfType(DialogueTreeSettings.GetDialogueNodeData("Srt"));

		// if(startNode == null)
		// {
		// 	EndDialogue();
		// 	return;
		// }

		// FocusedNode = startNode;
		// startNode.Start();
	}

	public void EndDialogue()
	{
		_currentNode = -1;
		EmitSignal(SignalName.DialogueEnded);
	}

	///<summary>Sends an input signal to the focused node in the tree. By default, this can be used to select between options and signal that text has finished writing.</summary>
	public void SendInput(string input = "", params Variant[] parameters)
	{
		FocusedNode?.RecieveDialogueInput(input, parameters);
	}

	public DialogueInputOption[] GetInputOptions()
	{
		return FocusedNode == null ? System.Array.Empty<DialogueInputOption>() : FocusedNode.GetDialogueInputOptions();
	}

	///<summary>Returns the connection to the output <c>port</c> of <c>node</c>. If there is no connection, returns null. the connection follows the same format as <c>Connections</c>.</summary>
	public DialogueTreeData.Connection? GetConnectionToPort(int node, int port)
	{
		if(TreeData == null)
			return null;

		for(int x = 0; x < TreeData.GetConnectionsCount(); x++)
		{
			DialogueTreeData.Connection connection = TreeData.GetConnection(x);

			if(connection.FromNode == node && connection.FromPort == port)
				return connection;
		}

		return null;
	}

	///<summary>Returns the instance with the given index. Only get instances when you actually need them, as they are only instantiated after they are gotten for the first time to increase performance.</summary>
	public T GetDialogueNodeInstance<T>(int index) where T : DialogueNodeInstance => GetDialogueNodeInstance(index) as T;
	
	///<summary>Returns the instance with the given index. Only get instances when you actually need them, as they are only instantiated after they are gotten for the first time to increase performance.</summary>
	public DialogueNodeInstance GetDialogueNodeInstance(int index)
	{
		if(index < 0 || TreeData == null || index >= TreeData.GetNodesCount())
			return null;

		foreach(DialogueNodeInstance instance in _dialogueNodeInstances)
			if(instance.Index == index)
				return instance;

		return InstantiateDialogueNodeInstance(index);
	}

	///<summary>Returns all instantiated <c>DialogueNodeInstances</c>.</summary>
	public DialogueNodeInstance[] GetDialogueNodeInstances()
	{
		return _dialogueNodeInstances.ToArray();
	}

	public override void _Ready()
	{
		DialogueTreeSettings = DialogueTreesSettings.LoadSettings();
	}

	private DialogueNodeInstance GetFirstDialogueNodeOfType(DialogueNodeData dialogueNodeData)
	{
		if(TreeData == null)
			return null;

		for(int x = 0; x < TreeData.GetNodesCount(); x++)
		{
			if(TreeData.GetNodeType(x) == dialogueNodeData.DialogueNodeSaveName)
				return GetDialogueNodeInstance(x);
		}

		return null;
	}

	private DialogueNodeInstance InstantiateDialogueNodeInstance(int index)
	{
		if(TreeData == null)
			return null;

		DialogueNodeData dialogueNodeData = DialogueTreeSettings.GetDialogueNodeData(TreeData.GetNodeType(index));

		if(!dialogueNodeData.TryInstantiateDialogueNodeInstance(out DialogueNodeInstance dialogueNodeInstance))
		{
			GD.PushError($"The DialogueNodeInstance at '{dialogueNodeData.ResourcePath}' is not valid. (either it is null, or it does not inherit from DialogueNodeInstance)");
			return null;
		}

		dialogueNodeInstance.DialogueTree = this;
		dialogueNodeInstance.Index = index;

		_dialogueNodeInstances.Add(dialogueNodeInstance);

		dialogueNodeInstance.Ready(new DialogueNodeSaveData (TreeData.DialogueNodeSaveData[index], TreeData.DialogueNodeReferences[index]));
		return dialogueNodeInstance;
	}
}

public struct DialogueInputOption
{
	public DialogueInputOption(string input, params Variant[] parameters)
	{
		Input = input;   
		Parameters = parameters;
	}

	public string Input;
	public Variant[] Parameters;
	
	public static DialogueInputOption[] ConstructInputOptions(string[] inputs, Variant[][] parameters = null)
	{
		DialogueInputOption[] dialogueInputOptions = new DialogueInputOption[inputs.Length];

		if(parameters == null)
			for(int x = 0; x < inputs.Length; x++)
				dialogueInputOptions[x] = new (inputs[x], null);
		else
		{
			int parametersLength = parameters.Length;

			for(int x = 0; x < inputs.Length; x++)
				dialogueInputOptions[x] = new (inputs[x], parametersLength > x ? parameters[x] : null);
		}

		return dialogueInputOptions;
	}
}
 
