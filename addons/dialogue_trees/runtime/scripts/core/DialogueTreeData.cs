using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace Ardot.DialogueTrees.Runtime;

///<summary>Stores all the data about a <c>DialogueTree</c> and its nodes.</summary>
[Tool]
[GlobalClass]
public partial class DialogueTreeData : Resource
{
	public DialogueTreeData()
	{
		DialogueTreesSettings settings = DialogueTreesSettings.Singleton;

		if(settings == null)
			return;

		if(settings.DefaultTree != null)
		{
			_dialogueNodeTypeNames = settings.DefaultTree._dialogueNodeTypeNames;
			_dialogueNodeTypes = settings.DefaultTree._dialogueNodeTypes;
			_stackFrameEnds = settings.DefaultTree._stackFrameEnds;
			_dialogueNodeSaveData = settings.DefaultTree._dialogueNodeSaveData;
			return;
		}
	}
	
	[Export]
	private StringName[] _dialogueNodeTypeNames = System.Array.Empty<StringName>();

	[Export]
	private int[] _dialogueNodeTypes = System.Array.Empty<int>();

	[Export]
	private Array<Godot.Collections.Array> _dialogueNodeSaveData = new ();

	/// <summary>
	/// Specifies what previous stack frame, if any, should be cleared. If a stackFrameEnd is -1, it should be ignored, if it is -2, the stack frame should be completely cleared.
	/// </summary>
	[Export]
	private int[] _stackFrameEnds = System.Array.Empty<int>();

	/// <summary>
	/// List for holding node references.
	/// </summary>
	[Export]
	private Array<Array<int>> _dialogueNodeReferences = new ();

	public IReadOnlyList<Godot.Collections.Array> DialogueNodeSaveData {get =>_dialogueNodeSaveData;} 
	public IReadOnlyList<Array<int>> DialogueNodeReferences {get => _dialogueNodeReferences;}

	public void Clear()
	{
		_dialogueNodeTypeNames = System.Array.Empty<StringName>();
		_dialogueNodeTypes = System.Array.Empty<int>();
		_dialogueNodeSaveData.Clear();
		_dialogueNodeReferences.Clear();
	}

	public void SetValues(StringName[] dialogueNodeTypeNames, int[] dialogueNodeTypes, Array<Godot.Collections.Array> dialogueNodeSaveData, Array<Array<int>> dialogueNodeReferences)
	{
		_dialogueNodeTypeNames = dialogueNodeTypeNames;
		_dialogueNodeTypes = dialogueNodeTypes;
		_dialogueNodeSaveData = dialogueNodeSaveData;
		_dialogueNodeReferences = dialogueNodeReferences;
	}

	public bool IsEmpty()
	{
		return _dialogueNodeTypeNames.Length == 0;
	}

	public int GetNodesCount() => _dialogueNodeTypes.Length;
	public StringName GetNodeType(int nodeIndex) => _dialogueNodeTypeNames[_dialogueNodeTypes[nodeIndex]];

	public bool IsValid()
	{
		int nodeCount = _dialogueNodeTypes.Length;

		bool typesValid = true;
		
		foreach(int type in _dialogueNodeTypes)
			typesValid &= _dialogueNodeTypeNames.Length > type && type >= 0;

		return
		typesValid &&
		_dialogueNodeReferences.Count == nodeCount &&
		_dialogueNodeSaveData.Count == nodeCount && 
		_stackFrameEnds.Length == nodeCount;
	}
}
