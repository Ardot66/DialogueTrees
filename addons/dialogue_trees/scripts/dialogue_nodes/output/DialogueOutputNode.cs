# if TOOLS

using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

[Tool]
public partial class DialogueOutputNode : DialogueNode
{
	[Export]
	private EditorLineEdit _characterLineEdit;

	[Export]
	private EditorTextEdit _outputTextEdit;

	public override void _Ready()
	{
		_outputTextEdit.EditorTextEditTextChanged += OnOutputTextChanged;

		_characterLineEdit.InitializeUndoRedo(GetUndoRedo(), "Set Output Character", GetDialogueTree());
		_outputTextEdit.InitializeUndoRedo(GetUndoRedo(), "Set Output Text", GetDialogueTree());
	}

	public override void Load(DialogueNodeSaveData data)
	{
		_outputTextEdit.InitializeText(data.General[0].AsString());
		_characterLineEdit.InitializeText(data.General[1].AsString());
	}

	public override DialogueNodeSaveData Save() => new (
		new()
		{
			_outputTextEdit.Text,
			_characterLineEdit.Text,
		},
		null
	);

	private void OnOutputTextChanged(EditorTextEdit textEdit, string newText)
	{
		SetDeferred(Control.PropertyName.Size, Vector2.Zero);
	}
}

# endif
