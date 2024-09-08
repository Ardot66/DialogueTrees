#if TOOLS
using Ardot.DialogueTrees;
using Godot;
using System;

[Tool]
public partial class DialogueEnumDefinitionEnumValue : MarginContainer
{
	[Export]
	private EditorLineEdit _enumValueLineEdit;
	public EditorLineEdit EnumValueLineEdit {get => _enumValueLineEdit;}

	[Export]
	private ValueButton _removeEnumValueButton;
	public ValueButton RemoveEnumValueButton {get => _removeEnumValueButton;}
}
#endif