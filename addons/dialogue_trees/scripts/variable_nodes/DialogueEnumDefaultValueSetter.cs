# if TOOLS

using System.Linq;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

[Tool]
public partial class DialogueEnumDefaultValueSetter : DialogueVariableDefaultValueSetter
{
	private const string
	_enumOptionButtonPath = "EnumOptionButton";

	private EditorOptionButton _enumOptionButton;
	private DialogueEnumDefinition _definitionNode;
	
	public override void _Ready()
	{
		_enumOptionButton = GetNode<EditorOptionButton>(_enumOptionButtonPath);
		_definitionNode = (DialogueEnumDefinition)VariableNode.VariableDefinition;

		_definitionNode.EnumDefinitionChangingUndoRedo += OnEnumDefinitionChanging;

		_enumOptionButton.InitializeUndoRedo(VariableNode.GetUndoRedo(), "Set Enum Value", VariableNode.GetDialogueTree());
		_enumOptionButton.InitializeGetObjectName(this, MethodName.GetEnumName);

		UpdateEnumOptionButtonUI(_definitionNode.EnumValues);
	}

	public override Variant GetValue()
	{
		return _enumOptionButton.SelectedObject.AsString();
	}

   	public override void SetValue(Variant variant)
	{
		_enumOptionButton.SelectedObject = variant;
		UpdateEnumOptionButtonUI(_definitionNode.EnumValues);
	}

	private void OnEnumDefinitionChanging(string[] newDefinition, EditorUndoRedoManager undoRedo)
	{
		string selectedEnum = _enumOptionButton.SelectedObject.AsString();
		string[] oldDefinition = _definitionNode.EnumValues;
		int selectedObjectIndex = -1;

		if(_enumOptionButton.SelectedObject.VariantType != Variant.Type.Nil)
		{
			if(newDefinition.Length == oldDefinition.Length)
				selectedObjectIndex = System.Array.IndexOf(oldDefinition, selectedEnum);
			else	
				selectedObjectIndex = System.Array.IndexOf(newDefinition, selectedEnum);
		}
		
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

	private static string GetEnumName(Variant type)
	{
		return type.AsString();
	}
}

# endif
