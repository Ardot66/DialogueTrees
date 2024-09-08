# if TOOLS

using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

[Tool]
public partial class DialogueFunctionNode : DialogueNode
{
	public string FunctionName = "";

	[Signal]
	public delegate void FunctionNameChangedEventHandler();

	[Export]
	private EditorLineEdit _functionNameEdit;

	public override void _Ready()
	{
		_functionNameEdit.InitializeUndoRedo(GetUndoRedo(), "Set Function Name", GetDialogueTree());
		
		_functionNameEdit.EditorLineEditTextChanged += OnFunctionNameChanged;
	}

	public override void Load(DialogueNodeSaveData data)
	{
		FunctionName = data.General[0].AsString();

		_functionNameEdit.InitializeText(FunctionName);
	}

	public override DialogueNodeSaveData Save() => new (
		new() { FunctionName },
		null
	);

	private void OnFunctionNameChanged(EditorLineEdit lineEdit, string newText)
	{
		FunctionName = newText;
		EmitSignal(SignalName.FunctionNameChanged);
	}
}

# endif
