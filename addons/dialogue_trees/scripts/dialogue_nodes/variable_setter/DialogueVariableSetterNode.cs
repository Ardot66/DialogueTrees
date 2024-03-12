# if TOOLS

using Ardot.DialogueTrees.DialogueNodes;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

[Tool]
public partial class DialogueVariableSetterNode : DialogueNode
{
	private const string
	_setterContainerPath = "VBoxContainer",
	_variableOptionButtonPath = "VBoxContainer/HBoxContainer/VariableOptionButton";

	public DialogueVariableNode VariableNode {get => _variableNode;}

	private DialogueVariableNode _variableNode;
	private DialogueVariableSetter _variableSetter;
	private Control _setterContainer;
	private Array<GodotObject> _variableNodes = new ();
	private EditorOptionButton _variableOptionButton;

	private int _connectedVariableIndex = -1;
	private Array _variableSetterSaveData = null;

	[Signal]
	public delegate void VariableNodeChangedEventHandler();

	public override void _Ready()
	{
		_setterContainer = GetNode<Control>(_setterContainerPath);
		_variableOptionButton = GetNode<EditorOptionButton>(_variableOptionButtonPath);
		_variableOptionButton.InitializeUndoRedo(GetUndoRedo(), "Set Setter Variable", GetDialogueTree());
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
			AddVariableSetterUI(InstantiateVariableSetter());

			_variableSetter?.Load(_variableSetterSaveData);
			_variableSetterSaveData = null;
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
			_variableSetter?.Save()
		};
	}

	public override void Load(Array data)
	{
		_connectedVariableIndex = data[0].AsInt32();
		_variableSetterSaveData = data[1].AsGodotArray();
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

	private void AddVariableSetterUI(DialogueVariableSetter setter)
	{
		_variableSetter = setter;

		if(setter == null)
			return;

		setter.VariableNode = VariableNode;

		_setterContainer.AddChild(setter);
	}

	private DialogueVariableSetter RemoveVariableSetterUI()
	{	
		if(_variableSetter == null)
			return null;

		DialogueVariableSetter variableSetter = _variableSetter;

		_setterContainer.RemoveChild(variableSetter);
		return variableSetter;
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
		DialogueVariableSetter oldSetter = RemoveVariableSetterUI();
		DialogueVariableSetter newSetter = InstantiateVariableSetter();

		AddVariableSetterUI(newSetter);

		if(newSetter != null)
			undoRedo.AddDoReference(newSetter);

		undoRedo.AddDoMethod(this, MethodName.RemoveVariableSetterUI);
		undoRedo.AddDoMethod(this, MethodName.AddVariableSetterUI, newSetter);
		undoRedo.AddDoProperty(this, Control.PropertyName.Size, Vector2.Zero);
		undoRedo.AddUndoMethod(this, MethodName.RemoveVariableSetterUI);
		undoRedo.AddUndoMethod(this, MethodName.AddVariableSetterUI, oldSetter);
		undoRedo.AddUndoProperty(this, Control.PropertyName.Size, Vector2.Zero);

		if(oldSetter != null)
			undoRedo.AddUndoReference(oldSetter);

		Size = Vector2.Zero;
	}

	private void OnVariableNodeChanged(EditorOptionButton optionButton, Variant selectedOption)
	{
		SetVariableNode((DialogueVariableNode)selectedOption);

		EmitSignal(SignalName.VariableNodeChanged);
	}

	private DialogueVariableSetter InstantiateVariableSetter()
	{
		if(_variableNode == null)
			return null;

		DialogueVariableNode.VariableData variableData = _variableNode.GetVariableDataForType(_variableNode.VariableType);

		return variableData.VariableSetter?.Instantiate<DialogueVariableSetter>();
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
