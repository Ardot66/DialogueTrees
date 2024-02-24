# if TOOLS

using System.Linq;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

[Tool]
public partial class DialogueEnumSetter : DialogueVariableSetter
{
	private const string
	_enumOptionButtonPath = "EnumOptionButton";	

	private EditorOptionButton _enumOptionButton;
	private DialogueEnumDefinition _enumDefinition;

	public override void _Ready()
	{
		_enumOptionButton = GetNode<EditorOptionButton>(_enumOptionButtonPath);
		_enumOptionButton.InitializeUndoRedo(VariableNode.GetUndoRedo(), "Set Enum Value Setter", VariableNode.GetDialogueTree());
		_enumOptionButton.InitializeGetObjectName(VariableNode.VariableDefaultValueSetter, DialogueEnumDefaultValueSetter.MethodName.GetEnumName);
		
		_enumDefinition = (DialogueEnumDefinition)VariableNode.VariableDefinition;
		_enumDefinition.EnumDefinitionChangingUndoRedo += OnEnumDefinitionChanging;

		UpdateEnumOptionButtonUI(_enumDefinition.EnumValues);
	}

	public override Array Save()
	{
		return new()
		{
			_enumOptionButton.SelectedObject
		};
	}	

	public override void Load(Array data)
	{
		_enumOptionButton.SelectedObject = data[0];
		UpdateEnumOptionButtonUI(_enumDefinition.EnumValues);
	}

	private void OnEnumDefinitionChanging(string[] newDefinition, EditorUndoRedoManager undoRedo)
	{
		string[] oldDefinition = _enumDefinition.EnumValues;
		int selectedObjectIndex;

		if(newDefinition.Length == oldDefinition.Length)
			selectedObjectIndex = System.Array.IndexOf(oldDefinition, _enumOptionButton.SelectedObject.AsString());
		else	
			selectedObjectIndex = System.Array.IndexOf(newDefinition, _enumOptionButton.SelectedObject.AsString());
		
		Variant newSelectedObject = selectedObjectIndex == -1 ? default : newDefinition[selectedObjectIndex];

		undoRedo.AddDoProperty(_enumOptionButton, EditorOptionButton.PropertyName.SelectedObject, newSelectedObject);
		undoRedo.AddDoMethod(this, MethodName.UpdateEnumOptionButtonUI, newDefinition);
		undoRedo.AddUndoProperty(_enumOptionButton, EditorOptionButton.PropertyName.SelectedObject, _enumOptionButton.SelectedObject);
		undoRedo.AddUndoMethod(this, MethodName.UpdateEnumOptionButtonUI, oldDefinition);
	}

	private void UpdateEnumOptionButtonUI(string[] enumDefinition)
	{
		_enumOptionButton.UpdateOptionsUI(new Array(enumDefinition.Select((a) => Variant.CreateFrom(a))));
	}

	public static string GetEnumName(Variant value)
	{
		return value.AsString();
	}
}

# endif
