# if TOOLS

using Ardot.DialogueTrees.DialogueNodes;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

[Tool]
public partial class DialogueVariableConditionNode : DialogueNode
{
	private const string
	_ConditionContainerPath = "VBoxContainer",
	_variableOptionButtonPath = "VBoxContainer/HBoxContainer/VariableOptionButton";

	public DialogueVariableNode VariableNode {get => _variableNode;}

	private DialogueVariableNode _variableNode;
	private DialogueVariableCondition _variableCondition;
	private Control _ConditionContainer;
	private Array<GodotObject> _variableNodes = new ();
	private EditorOptionButton _variableOptionButton;

	private int _connectedVariableIndex = -1;
	private Array _variableConditionSaveData = null;

	[Signal]
	public delegate void VariableNodeChangedEventHandler();

	public override void _Ready()
	{
		_ConditionContainer = GetNode<Control>(_ConditionContainerPath);
		_variableOptionButton = GetNode<EditorOptionButton>(_variableOptionButtonPath);
		_variableOptionButton.InitializeUndoRedo(GetUndoRedo(), "Set Condition Variable", GetDialogueTree());
		_variableOptionButton.InitializeGetObjectName(this, MethodName.GetVariableOptionName);

		_variableOptionButton.EditorOptionButtonMidUndoRedo += OnVariableNodeChangedUndoRedo;
		_variableOptionButton.EditorOptionButtonOptionSelected += OnVariableNodeChanged;

		DialogueGraph.ChildEnteredTree += OnChildEnteredGraph;
		DialogueGraph.DialogueNodeRemovedUndoRedo += OnDialogueNodeRemoved;
		DialogueGraph.ChildExitingTree += OnChildExitingGraph;
	}

	public override void GraphReady()
	{
		if(_connectedVariableIndex != -1)
		{
			SetVariableNode(DialogueGraph.GetChildOrNull<DialogueVariableNode>(_connectedVariableIndex));

			_variableOptionButton.SelectedObject = VariableNode;
			AddVariableConditionUI(InstantiateVariableCondition());

			_variableCondition?.Load(_variableConditionSaveData);
			_variableConditionSaveData = null;
		}

		foreach(Node child in DialogueGraph.GetChildren())
			AddVariableNode(child, false);

		_variableOptionButton.UpdateOptionsUI(_variableNodes);
	}

	public override Array Save()
	{
		return new() 
		{
			_variableOptionButton.SelectedObject.VariantType != Variant.Type.Nil ? ((Node)_variableOptionButton.SelectedObject).GetIndex() : -1,
			_variableCondition?.Save()
		};
	}

	public override void Load(Array data)
	{
		_connectedVariableIndex = data[0].AsInt32();
		_variableConditionSaveData = data[1].AsGodotArray();
	}

	private void OnChildEnteredGraph(Node child)
	{
		AddVariableNode(child, true);
	}

	private void AddVariableNode(Node node, bool updateUI)
	{
		if(node is DialogueVariableNode variableNode && !_variableNodes.Contains(variableNode))
		{
			_variableNodes.Add(variableNode);

			variableNode.VariableNameChanged += OnVariableNodeNameChanged;

			if(updateUI)
				_variableOptionButton.UpdateOptionsUI(_variableNodes);
		}
	}

	private void OnChildExitingGraph(Node child)
	{
		if(child is DialogueVariableNode variableNode)
		{
			_variableNodes.Remove(variableNode);

			variableNode.VariableNameChanged -= OnVariableNodeNameChanged;

			_variableOptionButton.UpdateOptionsUI(_variableNodes);
		}
	}

	private void OnDialogueNodeRemoved(DialogueNode dialogueNode, EditorUndoRedoManager undoRedo)
	{
		if(dialogueNode is DialogueVariableNode variableNode && variableNode == VariableNode)
		{
			_variableNode = null;

			ReloadVariableUIUndoRedo(undoRedo);
		}
	}

	private void AddVariableConditionUI(DialogueVariableCondition Condition)
	{
		_variableCondition = Condition;

		if(Condition == null)
			return;

		Condition.VariableNode = VariableNode;

		_ConditionContainer.AddChild(Condition);
	}

	private DialogueVariableCondition RemoveVariableConditionUI()
	{	
		if(_variableCondition == null)
			return null;

		DialogueVariableCondition variableCondition = _variableCondition;

		_ConditionContainer.RemoveChild(variableCondition);
		return variableCondition;
	}

	private static string GetVariableOptionName(Variant variableNode)
	{
		return ((DialogueVariableNode)variableNode).VariableName;
	}

	private void OnVariableNodeNameChanged()
	{
	 	_variableOptionButton.CallDeferred(EditorOptionButton.MethodName.UpdateOptionsUI, _variableNodes);
	}

	private void OnVariableNodeChangedUndoRedo(EditorOptionButton optionButton, EditorUndoRedoManager undoRedo, Variant selectedOption)
	{
		SetVariableNode((DialogueVariableNode)selectedOption);

		ReloadVariableUIUndoRedo(undoRedo);
	}

	private void ReloadVariableUIUndoRedo(EditorUndoRedoManager undoRedo)
	{
		DialogueVariableCondition oldCondition = RemoveVariableConditionUI();
		DialogueVariableCondition newCondition = InstantiateVariableCondition();

		AddVariableConditionUI(newCondition);

		if(newCondition != null)
			undoRedo.AddDoReference(newCondition);

		undoRedo.AddDoMethod(this, MethodName.RemoveVariableConditionUI);
		undoRedo.AddDoMethod(this, MethodName.AddVariableConditionUI, newCondition);
		undoRedo.AddDoProperty(this, Control.PropertyName.Size, Vector2.Zero);
		undoRedo.AddUndoMethod(this, MethodName.RemoveVariableConditionUI);
		undoRedo.AddUndoMethod(this, MethodName.AddVariableConditionUI, oldCondition);
		undoRedo.AddUndoProperty(this, Control.PropertyName.Size, Vector2.Zero);

		if(oldCondition != null)
			undoRedo.AddUndoReference(oldCondition);

		Size = Vector2.Zero;
	}

	private void OnVariableNodeChanged(EditorOptionButton optionButton, Variant selectedOption)
	{
		SetVariableNode((DialogueVariableNode)selectedOption);

		EmitSignal(SignalName.VariableNodeChanged);
	}

	private DialogueVariableCondition InstantiateVariableCondition()
	{
		if(_variableNode == null)
			return null;

		DialogueVariableNode.VariableData variableData = _variableNode.GetVariableDataForType(_variableNode.VariableType);

		return variableData.VariableCondition?.Instantiate<DialogueVariableCondition>();
	}

	private void SetVariableNode(DialogueVariableNode variableNode)
	{
		if(_variableNode != null)
			_variableNode.VariableTypeChangedUndoRedo -= ReloadVariableUIUndoRedo;

		_variableNode = variableNode;

		if(_variableNode != null)
			_variableNode.VariableTypeChangedUndoRedo += ReloadVariableUIUndoRedo;
	}
}

# endif
