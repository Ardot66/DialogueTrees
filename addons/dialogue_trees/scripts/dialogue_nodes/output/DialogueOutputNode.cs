# if TOOLS

using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

[Tool]
public partial class DialogueOutputNode : DialogueNode
{
	private const string 
	_characterLineEditPath = "MarginContainer/VBoxContainer/CharacterLineEdit",
	_outputTextEditPath = "MarginContainer/VBoxContainer/OutputTextEdit";

	private EditorLineEdit _characterLineEdit;
	private EditorTextEdit _outputTextEdit;

	public override void _Ready()
	{
		_characterLineEdit = GetNode<EditorLineEdit>(_characterLineEditPath);
		_outputTextEdit = GetNode<EditorTextEdit>(_outputTextEditPath);

		_outputTextEdit.EditorTextEditTextChanged += OnOutputTextChanged;

		_characterLineEdit.InitializeUndoRedo(GetUndoRedo(), "Set Output Character", GetDialogueTree());
		_outputTextEdit.InitializeUndoRedo(GetUndoRedo(), "Set Output Text", GetDialogueTree());
	}

	public override void Load(Array data)
	{
		_outputTextEdit.InitializeText(data[0].AsString());
		_characterLineEdit.InitializeText(data[1].AsString());
	}

	public override Array Save()
	{
		return new()
		{
			_outputTextEdit.Text,
			_characterLineEdit.Text,
		};
	}

	private void OnOutputTextChanged(EditorTextEdit textEdit, string newText)
	{
		SetDeferred(Control.PropertyName.Size, Vector2.Zero);
	}
}

# endif
