# if TOOLS

using System.Linq;
using Ardot.DialogueTrees.DialogueVariables;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

[Tool]
public partial class DialogueVariableNode : DialogueNode
{
	private const string 
	_variableNameLineEditPath = "VariableNameLineEdit",
	_typeOptionButtonPath = "TypeOptionButton";

	public StringName VariableName;
	public StringName VariableType;

	public DialogueVariableDefaultValueSetter VariableDefaultValueSetter {get => _variableDefaultValueSetter;}
	public DialogueVariableDefinition VariableDefinition {get => _variableDefinition;}

	private StringName _variableName;
	private StringName _variableType;
	private EditorOptionButton _typeOptionButton;
	private EditorLineEdit _variableNameLineEdit;

	private DialogueVariableDefaultValueSetter _variableDefaultValueSetter;
	private DialogueVariableDefinition _variableDefinition;

	[Signal]
	public delegate void VariableTypeChangedEventHandler();

	[Signal]
	public delegate void VariableTypeChangedUndoRedoEventHandler(EditorUndoRedoManager undoRedo);

	[Signal]
	public delegate void VariableNameChangedEventHandler();

	public override void _Ready()
	{
		_typeOptionButton = GetNode<EditorOptionButton>(_typeOptionButtonPath);
		_variableNameLineEdit = GetNode<EditorLineEdit>(_variableNameLineEditPath);
		
		_typeOptionButton.EditorOptionButtonOptionSelected += OnTypeChanged;
		_typeOptionButton.EditorOptionButtonMidUndoRedo += OnTypeChangedUndoRedo;

		_variableNameLineEdit.EditorLineEditTextChanged += OnVariableNameChanged;

		_typeOptionButton.InitializeUndoRedo(GetUndoRedo(), "Set Variable Type", GetDialogueTree());
		_typeOptionButton.InitializeGetObjectName(this, MethodName.GetTypeName);

		_variableNameLineEdit.InitializeUndoRedo(GetUndoRedo(), "Set Variable Name", GetDialogueTree());

		UpdateTypeOptionButtonUI();
	}

	public override Array Save()
	{
		return new()
		{
			VariableName,
			VariableType,
			_variableDefaultValueSetter == null ? default : _variableDefaultValueSetter.GetValue(),
			_variableDefinition == null ? default : _variableDefinition.GetDefinition(),
		};
	}

	public override void Load(Array data)
	{
		VariableName = data[0].AsStringName();
		_variableNameLineEdit.InitializeText(VariableName);

		VariableType = data[1].AsStringName();
		_typeOptionButton.SelectedObject = VariableType;
		UpdateTypeOptionButtonUI();
		
		LoadVariableUI(InstantiateVariableUI());

		_variableDefinition?.SetDefinition(data[3]);
		_variableDefaultValueSetter?.SetValue(data[2]);
	}

	///<summary>Returns a list of data about how different variables should be defined.</summary>
	public virtual VariableData[] GetVariableDataList()
	{
		return new VariableData[]
		{
			new(
				"Enum",
				ResourceLoader.Load<PackedScene>($"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/dialogue_nodes/subscenes/dialogue_enum_default_value_setter.tscn"),
				ResourceLoader.Load<PackedScene>($"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/dialogue_nodes/subscenes/dialogue_enum_definition.tscn"),
				ResourceLoader.Load<PackedScene>($"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/dialogue_nodes/subscenes/dialogue_enum_setter.tscn"),
				ResourceLoader.Load<PackedScene>($"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/dialogue_nodes/subscenes/dialogue_enum_condition.tscn")
			)
		};
	}

	public VariableData GetVariableDataForType(StringName type)
	{
		foreach(VariableData data in GetVariableDataList())
			if(data.VariableType == type)
				return data;

		return default;
	}

	private Array<Control> UnloadVariableUI()
	{
		Array<Control> removedControls = new()
		{
			_variableDefaultValueSetter,
			_variableDefinition
		};

		if(_variableDefaultValueSetter != null)
			RemoveChild(_variableDefaultValueSetter);

		if (_variableDefinition != null)
			RemoveChild(_variableDefinition);
		
		return removedControls;
	}

	private void LoadVariableUI(Array<Control> variableUI)
	{
		_variableDefaultValueSetter = (DialogueVariableDefaultValueSetter)variableUI[0];
		_variableDefinition = (DialogueVariableDefinition)variableUI[1];

		if(_variableDefaultValueSetter == null)
			return;
		
		_variableDefaultValueSetter.VariableNode = this;

		AddChild(_variableDefaultValueSetter);
		MoveChild(_variableDefaultValueSetter, 5);

		if(_variableDefinition == null)
			return;

		_variableDefinition.VariableNode = this;

		AddChild(_variableDefinition);
		MoveChild(_variableDefinition, 6);	
	}

	private void UpdateTypeOptionButtonUI()
	{
		_typeOptionButton.UpdateOptionsUI(new Array (GetVariableDataList().Select((s) => Variant.CreateFrom(s.VariableType))));
	}

	private void OnTypeChangedUndoRedo(EditorOptionButton optionButton, EditorUndoRedoManager undoRedo, Variant newSelectedObject)
	{
		VariableType = newSelectedObject.AsString();

		Array<Control> removedControls = UnloadVariableUI();

		Array<Control> newControls = InstantiateVariableUI();
		LoadVariableUI(newControls);

		foreach(Control control in newControls)
			if(control != null)
				undoRedo.AddDoReference(control);

		undoRedo.AddDoMethod(this, MethodName.UnloadVariableUI);
		undoRedo.AddDoMethod(this, MethodName.LoadVariableUI, newControls);
		undoRedo.AddDoProperty(this, Control.PropertyName.Size, Vector2.Zero);
		undoRedo.AddUndoMethod(this, MethodName.UnloadVariableUI);
		undoRedo.AddUndoMethod(this, MethodName.LoadVariableUI, removedControls);
		undoRedo.AddUndoProperty(this, Control.PropertyName.Size, Vector2.Zero);

		foreach(Control control in removedControls)
			if(control != null)
				undoRedo.AddUndoReference(control);

		EmitSignal(SignalName.VariableTypeChangedUndoRedo, undoRedo);

		Size = Vector2.Zero;
	}

	private void OnTypeChanged(EditorOptionButton optionButton, Variant newSelectedObject)
	{
		VariableType = newSelectedObject.AsString();

		EmitSignal(SignalName.VariableTypeChanged);
	}

	private void OnVariableNameChanged(EditorLineEdit lineEdit, string newVariableName)
	{
		VariableName = newVariableName;

		EmitSignal(SignalName.VariableNameChanged);
	}

	private Array<Control> InstantiateVariableUI()
	{
		VariableData variableData = GetVariableDataForType(VariableType);

		return new()
		{
			variableData.VariableDefaultValueSetter?.Instantiate<DialogueVariableDefaultValueSetter>(),
			variableData.VariableDefinition?.Instantiate<DialogueVariableDefinition>(),
		};
	}

	private static string GetTypeName(Variant type)
	{
		return type.AsString();
	}

	public struct VariableData
	{
		///<summary>Creates a new <c>DialogueVariableData</c>. See the docs for <c>DialogueVariableData</c>'s properties for more information on these parameters.</summary>
		public VariableData(StringName variableType, PackedScene variableDefaultValueSetter, PackedScene variableDefinition, PackedScene variableSetter, PackedScene variableComparer)
		{
			VariableType = variableType;
			VariableDefaultValueSetter = variableDefaultValueSetter;
			VariableDefinition = variableDefinition;
			VariableSetter = variableSetter;
			VariableCondition = variableComparer;
		}

		///<summary>The type of this variable.</summary>
		public StringName VariableType;

		///<summary>The scene that handles setting the default value of a dialogue variable. The root node must inherit from <c>DialogueVariableDefaultValueSetter</c>.</summary>
		public PackedScene VariableDefaultValueSetter;
		///<summary>The scene that handles defining the parameters of a dialogue variable (such as an enum's states). The root node must inherit from <c>DialogueVariableDefinition</c>. <br/>
		///<b>Note:</b> Setting this value is optional, as not all variables need a definition.</summary>
		public PackedScene VariableDefinition;
		///<summary>The scene that handles setting and modifying a dialogue variable. The root node must inherit from <c>DialogueVariableSetter</c>.</summary>
		public PackedScene VariableSetter;
		///<summary>The scene that handles comparing the dialogue variable to another value. The root node must inherit from <c>DialogueVariableCondition</c>.</summary>
		public PackedScene VariableCondition;
	}
}

# endif
