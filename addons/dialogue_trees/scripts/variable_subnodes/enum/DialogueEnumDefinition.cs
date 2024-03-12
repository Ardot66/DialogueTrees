# if TOOLS

using System.Linq;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

[Tool]
public partial class DialogueEnumDefinition : DialogueVariableDefinition
{
	private const string
	_enumValueTextScenePath = $"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/dialogue_nodes/subscenes/enum_value.tscn", 
	_enumValueLineEditPath = "HBoxContainer/EnumValueLineEdit",
	_removeEnumValueButtonPath = "HBoxContainer/RemoveEnumValueButton",
	_addEnumValueButtonPath = "AddEnumValueButton";

	private const int
	_postEnumValuesChildCount = 1,
	_preEnumValuesChildCount = 1;

	public string[] EnumValues {get => _enumValues;}

	private string[] _enumValues = System.Array.Empty<string>();

	private Button _addEnumValueButton;

	private int _valuesCount;

	[Signal]
	public delegate void EnumDefinitionChangingUndoRedoEventHandler(string[] newEnumDefinition, EditorUndoRedoManager undoRedo);

	[Signal]
	public delegate void EnumDefinitionChangingEventHandler(string[] newEnumDefinition);

	public override void _Ready()
	{
		_addEnumValueButton = GetNode<Button>(_addEnumValueButtonPath);
		_addEnumValueButton.Pressed += OnAddEnumValueButtonPressed;
	}

	public override Variant GetDefinition()
	{
		return _enumValues;
	}

	public override void SetDefinition(Variant definition)
	{
		string[] newEnumValues = definition.AsStringArray();

		for(int x = 0; x < newEnumValues.Length; x++)
		{
			Control enumValue = ResourceLoader.Load<PackedScene>(_enumValueTextScenePath).Instantiate<Control>();

			AddEnumValue(enumValue, x + _preEnumValuesChildCount, newEnumValues[x], false);
		}

		EmitSignal(SignalName.EnumDefinitionChanging, newEnumValues);

		_enumValues = newEnumValues;
	}

	private void AddEnumValue(Control enumValue, int index, string valueName = null, bool signalChange = true)
	{	
		AddChild(enumValue);
		MoveChild(enumValue, index);

		ValueButton removeEnumValueButton = enumValue.GetNode<ValueButton>(_removeEnumValueButtonPath);
		EditorLineEdit enumValueLineEdit = enumValue.GetNode<EditorLineEdit>(_enumValueLineEditPath);

		removeEnumValueButton.Value = enumValue;
		removeEnumValueButton.ValueButtonPressed += OnRemoveEnumValueButtonPressed;

		enumValueLineEdit.EditorLineEditTextChangedUndoRedo += OnEnumValueTextEditTextChangedUndoRedo;
		enumValueLineEdit.EditorLineEditTextChanged += OnEnumValueChanged;
		enumValueLineEdit.InitializeUndoRedo(VariableNode.GetUndoRedo(), "Set Enum Value Name", VariableNode.GetDialogueTree());

		if(valueName != null)
			enumValueLineEdit.InitializeText(valueName);

		_valuesCount++;

		VariableNode.Size = Vector2.Zero;

		if(signalChange)
			UpdateEnumValues();
	}

	private string RemoveEnumValue(Control enumValue, bool signalChange = true)
	{
		if(!HasNode(enumValue.Name.ToString()))
			return null;

		ValueButton removeEnumValueButton = enumValue.GetNode<ValueButton>(_removeEnumValueButtonPath);

		removeEnumValueButton.ValueButtonPressed -= OnRemoveEnumValueButtonPressed;
		RemoveChild(enumValue);

		EditorLineEdit enumValueLineEdit = enumValue.GetNode<EditorLineEdit>(_enumValueLineEditPath);

		_valuesCount--;

		VariableNode.Size = Vector2.Zero;

		if(signalChange)
			UpdateEnumValues();

		return enumValueLineEdit.Text;
	}

	private void UpdateEnumValues()
	{
		string[] newEnumValues = new string[_valuesCount];

		int childCount = GetChildCount();

		for(int x = _preEnumValuesChildCount; x < childCount - _postEnumValuesChildCount; x++)
			newEnumValues[x - _preEnumValuesChildCount] = GetChild(x).GetNode<EditorLineEdit>(_enumValueLineEditPath).Text;
		
		EmitSignal(SignalName.EnumDefinitionChanging, newEnumValues);

		_enumValues = newEnumValues;
	}

	private void OnAddEnumValueButtonPressed()
	{
		EditorUndoRedoManager undoRedo  = VariableNode.GetUndoRedo();

		undoRedo.CreateAction("Add Enum Value", UndoRedo.MergeMode.Disable, VariableNode.GetDialogueTree());

		Control enumValue = ResourceLoader.Load<PackedScene>(_enumValueTextScenePath).Instantiate<Control>();

		undoRedo.AddDoReference(enumValue);
		undoRedo.AddDoMethod(this, MethodName.AddEnumValue, enumValue, _valuesCount + _preEnumValuesChildCount, default, true);
		undoRedo.AddUndoMethod(this, MethodName.RemoveEnumValue, enumValue, true);

		EmitSignal(SignalName.EnumDefinitionChangingUndoRedo, _enumValues.Append("").ToArray(), undoRedo);

		undoRedo.CommitAction();
	}	

	private void OnRemoveEnumValueButtonPressed(Variant value)
	{
		Control enumValue = (Control)value.AsGodotObject();
		int enumValueIndex = enumValue.GetIndex();

		EditorUndoRedoManager undoRedo  = VariableNode.GetUndoRedo();

		undoRedo.CreateAction("Remove Enum Value", UndoRedo.MergeMode.Disable, VariableNode.GetDialogueTree());

		string[] newEnumValues = new string[_enumValues.Length - 1];
		
		for(int x = 0, y = 0; x < _enumValues.Length; x++, y++)
		{
			if(x == enumValueIndex - _preEnumValuesChildCount)
			{
				y--;
				continue;
			}
				
			newEnumValues[y] = _enumValues[x];
		}

		EmitSignal(SignalName.EnumDefinitionChangingUndoRedo, newEnumValues, undoRedo);

		string removedEnumValueText = RemoveEnumValue(enumValue);

		undoRedo.AddDoMethod(this, MethodName.RemoveEnumValue, enumValue, true);
		undoRedo.AddUndoMethod(this, MethodName.AddEnumValue, enumValue, enumValueIndex, removedEnumValueText, true);
		undoRedo.AddUndoReference(enumValue);

		undoRedo.CommitAction();
	}

	private void OnEnumValueChanged(EditorLineEdit lineEdit, string newText)
	{
		CallDeferred(MethodName.UpdateEnumValues);
	}

	private void OnEnumValueTextEditTextChangedUndoRedo(EditorLineEdit lineEdit, EditorUndoRedoManager undoRedo, string newText)
	{
		string[] newEnumValues = new string[_enumValues.Length];
		_enumValues.CopyTo(newEnumValues, 0);

		newEnumValues[lineEdit.GetParent().GetParent().GetIndex() - _preEnumValuesChildCount] = newText;

		EmitSignal(SignalName.EnumDefinitionChangingUndoRedo, newEnumValues, undoRedo);
	}
}

# endif
