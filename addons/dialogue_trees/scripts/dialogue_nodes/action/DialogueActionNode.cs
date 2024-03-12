# if TOOLS

using Godot;
using Godot.Collections;
using Ardot.DialogueTrees.DialogueActions;
using System.Linq;

namespace Ardot.DialogueTrees.DialogueNodes;

[Tool]
public partial class DialogueActionNode : DialogueNode
{
	private const string
	_actionSelectButtonPath = "MarginContainer/ActionSelectButton";

	private Array<GodotObject> _avaliableActions = new ();

	private EditorOptionButton _actionSelectButton;
	private DialogueTree _dialogueTree;

	public override void _Ready()
	{
		_dialogueTree = GetDialogueTree();

		if(_dialogueTree == null)
			return;
		
		_actionSelectButton = GetNode<EditorOptionButton>(_actionSelectButtonPath);
		_actionSelectButton.InitializeUndoRedo(GetUndoRedo(), "Set Connected Action", GetDialogueTree());
		_actionSelectButton.InitializeGetObjectName(this, MethodName.GetActionName);

		_dialogueTree.ChildEnteredTree += OnChildEnteredDialogueTree;
		_dialogueTree.ChildExitingTree += OnChildExitingDialogueTree;
	}

	public override void GraphReady()
	{
		UpdateActionsList();
	}

	public override void Load(Array data)
	{
		_actionSelectButton.SelectedObject = _dialogueTree.GetNodeOrNull<DialogueAction>(data[0].AsNodePath()); 
	}

	public override Array Save()
	{
		return new()
		{
			_actionSelectButton.SelectedObject.VariantType != Variant.Type.Nil ? _dialogueTree.GetPathTo((Node)_actionSelectButton.SelectedObject) : null
		};
	}

	private void UpdateActionsList()
	{
		foreach(Node child in _dialogueTree.GetChildren())
			if(!_avaliableActions.Contains(child))
				AddActionNode(child, false);

		_actionSelectButton.UpdateOptionsUI(_avaliableActions);
	}

	private void OnChildEnteredDialogueTree(Node child)
	{
		AddActionNode(child, true);
	}

	private void AddActionNode(Node node, bool updateUI)
	{
		if(node is DialogueAction actionNode)
		{
			_avaliableActions.Add(actionNode);

			actionNode.Renamed += OnActionNodeRenamed;

			if(updateUI)
				_actionSelectButton.UpdateOptionsUI(_avaliableActions);
		}
	}

	private void OnChildExitingDialogueTree(Node child)
	{
		if(child is DialogueAction actionNode)
		{
			_avaliableActions.Remove(actionNode);

			actionNode.Renamed -= OnActionNodeRenamed;

			_actionSelectButton.UpdateOptionsUI(_avaliableActions);
		}
	}

	private void OnActionNodeRenamed()
	{
		_actionSelectButton.UpdateOptionsUI(_avaliableActions);
	}

	private string GetActionName(Variant action)
	{
		return ((Node)action).Name;
	}
}

# endif
