# if TOOLS

using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

[Tool]
public partial class DialogueEnumDefinition : DialogueVariableDefinition
{
	[Export]
	private PackedScene _enumValueTextScene;

	[Export]
	private int _preEnumValuesChildCount;

	public string[] EnumValues {get => _enumValues;}

	private string[] _enumValues = System.Array.Empty<string>();

	[Export]
	private Button _addEnumValueButton;

	private readonly List<DialogueEnumDefinitionEnumValue> _enumValueSetters = new ();

	[Signal]
	public delegate void EnumDefinitionChangingUndoRedoEventHandler(string[] newEnumDefinition, EditorUndoRedoManager undoRedo);

	[Signal]
	public delegate void EnumDefinitionChangingEventHandler(string[] newEnumDefinition);

	public override void _Ready()
	{
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
			DialogueEnumDefinitionEnumValue enumValue = _enumValueTextScene.Instantiate<DialogueEnumDefinitionEnumValue>();

			AddEnumValue(enumValue, x, newEnumValues[x], false);
		}

		EmitSignal(SignalName.EnumDefinitionChanging, newEnumValues);

		_enumValues = newEnumValues;
	}

	private void AddEnumValue(DialogueEnumDefinitionEnumValue enumValue, int index, string valueName = null, bool signalChange = true)
	{	
		AddChild(enumValue);
		MoveChild(enumValue, index + _preEnumValuesChildCount);

		ValueButton removeEnumValueButton = enumValue.RemoveEnumValueButton;
		EditorLineEdit enumValueLineEdit = enumValue.EnumValueLineEdit;

		removeEnumValueButton.Value = enumValue;
		removeEnumValueButton.ValueButtonPressed += OnRemoveEnumValueButtonPressed;

		enumValueLineEdit.EditorLineEditTextChangedUndoRedo += OnEnumValueTextEditTextChangedUndoRedo;
		enumValueLineEdit.EditorLineEditTextChanged += OnEnumValueChanged;
		enumValueLineEdit.InitializeUndoRedo(VariableNode.GetUndoRedo(), "Set Enum Value Name", VariableNode.GetDialogueTree());

		if(valueName != null)
			enumValueLineEdit.InitializeText(valueName);

		_enumValueSetters.Insert(index, enumValue);

		VariableNode.Size = Vector2.Zero;

		if(signalChange)
			UpdateEnumValues();
	}

	private string RemoveEnumValue(DialogueEnumDefinitionEnumValue enumValue, bool signalChange = true)
	{
		if(!HasNode(enumValue.Name.ToString()))
			return null;

		enumValue.RemoveEnumValueButton.ValueButtonPressed -= OnRemoveEnumValueButtonPressed;
		RemoveChild(enumValue);

		_enumValueSetters.Remove(enumValue);

		VariableNode.Size = Vector2.Zero;

		if(signalChange)
			UpdateEnumValues();

		return enumValue.EnumValueLineEdit.Text;
	}

	private void UpdateEnumValues()
	{
		string[] newEnumValues = new string[_enumValueSetters.Count];

		for(int x = 0; x < _enumValueSetters.Count; x++)
			newEnumValues[x] = _enumValueSetters[x].EnumValueLineEdit.Text;
		
		EmitSignal(SignalName.EnumDefinitionChanging, newEnumValues);

		_enumValues = newEnumValues;
	}

	private void OnAddEnumValueButtonPressed()
	{
		EditorUndoRedoManager undoRedo  = VariableNode.GetUndoRedo();

		undoRedo.CreateAction("Add Enum Value", UndoRedo.MergeMode.Disable, VariableNode.GetDialogueTree());

		DialogueEnumDefinitionEnumValue enumValue = _enumValueTextScene.Instantiate<DialogueEnumDefinitionEnumValue>();

		undoRedo.AddDoReference(enumValue);
		undoRedo.AddDoMethod(this, MethodName.AddEnumValue, enumValue, _enumValueSetters.Count, default, true);
		undoRedo.AddUndoMethod(this, MethodName.RemoveEnumValue, enumValue, true);

		EmitSignal(SignalName.EnumDefinitionChangingUndoRedo, _enumValues.Append("").ToArray(), undoRedo);

		undoRedo.CommitAction();
	}	

	private void OnRemoveEnumValueButtonPressed(Variant value)
	{
		DialogueEnumDefinitionEnumValue enumValue = (DialogueEnumDefinitionEnumValue)value.AsGodotObject();
		int enumValueIndex = _enumValueSetters.IndexOf(enumValue);

		EditorUndoRedoManager undoRedo  = VariableNode.GetUndoRedo();

		undoRedo.CreateAction("Remove Enum Value", UndoRedo.MergeMode.Disable, VariableNode.GetDialogueTree());

		string[] newEnumValues = new string[_enumValues.Length - 1];
		
		for(int x = 0, y = 0; x < _enumValues.Length; x++, y++)
		{
			if(x == enumValueIndex)
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

		newEnumValues[_enumValueSetters.IndexOf(lineEdit.GetParent().GetParent<DialogueEnumDefinitionEnumValue>())] = newText;

		EmitSignal(SignalName.EnumDefinitionChangingUndoRedo, newEnumValues, undoRedo);
	}
}

# endif
