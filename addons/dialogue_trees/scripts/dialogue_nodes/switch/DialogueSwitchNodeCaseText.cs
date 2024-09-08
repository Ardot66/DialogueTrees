# if TOOLS
using Godot;
using System;
using Ardot.DialogueTrees;

[Tool]
public partial class DialogueSwitchNodeCaseText : MarginContainer
{
	[Export]
	private EditorTextEdit _caseTextEdit;
	public EditorTextEdit CaseTextEdit {get => _caseTextEdit;}

	[Export]
	private ValueButton _removeCaseButton;
	public ValueButton RemoveCaseButton {get => _removeCaseButton;}
}
#endif
