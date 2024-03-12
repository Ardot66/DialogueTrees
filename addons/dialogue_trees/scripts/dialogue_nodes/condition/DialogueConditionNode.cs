# if TOOLS

using Godot;
using Godot.Collections;
using Ardot.DialogueTrees.DialogueConditions;

namespace Ardot.DialogueTrees.DialogueNodes;

[Tool]
public partial class DialogueConditionNode : DialogueNode
{
	private const string
	_conditionSelectButtonPath = "HBoxContainer/ConditionSelectButton";

	private Array<GodotObject> _avaliableConditions = new ();

	private EditorOptionButton _conditionSelectButton;
	private DialogueTree _dialogueTree;

	public override void _Ready()
	{
		base._Ready();

		_dialogueTree = GetDialogueTree();

		if(_dialogueTree == null)
			return;
		
		_conditionSelectButton = GetNode<EditorOptionButton>(_conditionSelectButtonPath);
		_conditionSelectButton.InitializeUndoRedo(GetUndoRedo(), "Set Connected Condition", GetDialogueTree());
		_conditionSelectButton.InitializeGetObjectName(this, MethodName.GetConditionName);

		_dialogueTree.ChildEnteredTree += OnChildEnteredDialogueTree;
		_dialogueTree.ChildExitingTree += OnChildExitingDialogueTree;
	}

	public override void GraphReady()
	{
		UpdateConditionsList();
	}

	public override void Load(Array data)
	{
		_conditionSelectButton.SelectedObject = _dialogueTree.GetNodeOrNull<DialogueCondition>(data[0].AsNodePath()); 
	}

	public override Array Save()
	{
		return new()
		{
			_conditionSelectButton.SelectedObject.VariantType != Variant.Type.Nil ? _dialogueTree.GetPathTo((Node)_conditionSelectButton.SelectedObject) : null
		};
	}

	private void UpdateConditionsList()
	{
		foreach(Node child in _dialogueTree.GetChildren())
			if(!_avaliableConditions.Contains(child))
				AddConditionNode(child, false);

		_conditionSelectButton.UpdateOptionsUI(_avaliableConditions);
	}

	private void OnChildEnteredDialogueTree(Node child)
	{
		AddConditionNode(child, true);
	}

	private void AddConditionNode(Node node, bool updateUI)
	{
		if(node is DialogueCondition ConditionNode)
		{
			_avaliableConditions.Add(ConditionNode);

			ConditionNode.Renamed += OnConditionNodeRenamed;

			if(updateUI)
				_conditionSelectButton.UpdateOptionsUI(_avaliableConditions);
		}
	}

	private void OnChildExitingDialogueTree(Node child)
	{
		if(child is DialogueCondition ConditionNode)
		{
			_avaliableConditions.Remove(ConditionNode);

			ConditionNode.Renamed -= OnConditionNodeRenamed;

			_conditionSelectButton.UpdateOptionsUI(_avaliableConditions);
		}
	}

	private void OnConditionNodeRenamed()
	{
		_conditionSelectButton.UpdateOptionsUI(_avaliableConditions);
	}

	private string GetConditionName(Variant action)
	{
		return ((Node)action).Name;
	}
}

# endif
