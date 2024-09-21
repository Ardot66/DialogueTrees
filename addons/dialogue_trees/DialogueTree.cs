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

	/// <summary>
	/// Called when the dialogue is paused by a node.
	/// </summary>
	[Signal]
	public delegate void DialoguePausedEventHandler();

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

	private DialogueNodeOutputData _currentOutputData;

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
		StringName startNodeSaveName = DialogueTreesSettings.Singleton.StartNodeSaveName;
		DialogueNodeServer startNodeServer = DialogueTreesServer.Singleton.GetNodeServer(startNodeSaveName);

		int startNodeIndex = -1;

		for(int x = 0; x < TreeData.GetNodesCount(); x++)
		{
			if(TreeData.GetNodeType(x) == startNodeSaveName)
			{
				startNodeIndex = x;
				break;
			}
		}

		startNodeServer.RecieveInput(this, startNodeIndex, default, default);
		ExecuteDialogue();
	}

	public void PushToOutputStack(DialogueNodeOutputData outputData)
	{
		_outputStack.Push(outputData);
	}

	public void PushToDataStack(Variant data)
	{
		_stack.Add(data);
	}

	/// <summary>
	/// Pulls a value from the stack. Indexes are reversed, so 0 is the most recent item on the stack, while the stack's count is the oldest item.
	/// </summary>
	public Variant GetStackValue(int index)
	{
		return _stack[^index];
	}

	public void ContinueDialogue(Variant parameters)
	{
		DialogueNodeServer nodeServer = DialogueTreesServer.Singleton.GetNodeServer(_currentOutputData.DestinationDialogueTree.TreeData.GetNodeType(_currentOutputData.DestinationIndex));
		nodeServer.DialogueContinued(_currentOutputData.DestinationDialogueTree, _currentOutputData.DestinationIndex, _currentOutputData.DestinationDialogueTree.TreeData.GetNodeSaveData(_currentOutputData.DestinationIndex), _currentOutputData.Data, parameters);

		ExecuteDialogue();
	}

	private void ExecuteDialogue()
	{
		while(_outputStack.Count > 0)
		{
			DialogueNodeOutputData outputData = _outputStack.Pop();
			_currentOutputData = outputData;

			DialogueNodeServer nodeServer = DialogueTreesServer.Singleton.GetNodeServer(outputData.DestinationDialogueTree.TreeData.GetNodeType(outputData.DestinationIndex));
			bool continueExecution = nodeServer.RecieveInput(outputData.DestinationDialogueTree, outputData.DestinationIndex, outputData.DestinationDialogueTree.TreeData.GetNodeSaveData(outputData.DestinationIndex), outputData.Data);

			if(!continueExecution)
			{
				EmitSignal(SignalName.DialoguePaused);
				break;
			}

			int stackFrameEnds = outputData.DestinationDialogueTree.TreeData.GetNodeStackFrameEnds(outputData.DestinationIndex);

			StackFrame clearStackFrame = new (-1, -1, false);

			if(stackFrameEnds == -2)
			{
				while(_stackFrames.Count > 0)
				{
					clearStackFrame = _stackFrames.Peek();

					if(clearStackFrame.ClearingStackFrame)
						break;

					_stackFrames.Pop();
				}
			}
			else if (stackFrameEnds != -1)
			{
				while(_stackFrames.Count > 0)
				{
					StackFrame stackFrame = _stackFrames.Pop();

					if(stackFrame.BeginNode != stackFrameEnds)
						continue;

					clearStackFrame = stackFrame;
				}
			}

			if(clearStackFrame.StackBeginIndex != -1)
				_stack.RemoveRange(_stack.Count - clearStackFrame.StackBeginIndex, clearStackFrame.StackBeginIndex);
		}

		if(_outputStack.Count == 0)
			EmitSignal(SignalName.DialogueEnded);
	}
}
