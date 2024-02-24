# if TOOLS

using Godot;
using System;

namespace Ardot.DialogueTrees;

[Tool]
public partial class EditorTextEdit : TextEdit
{
	private string _oldText;

	private EditorUndoRedoManager _undoRedo;
	private GodotObject _undoRedoContext;
	private string _undoRedoActionName;

	[Signal]
	public delegate void EditorTextEditTextChangedEventHandler(EditorTextEdit textEdit, string oldText);

	public override void _Ready()
	{
		FocusExited += SubmitText;
	}

	public void InitializeUndoRedo(EditorUndoRedoManager undoRedo, string undoRedoActionName, GodotObject undoRedoContext = null)
	{
		_undoRedo = undoRedo;
		_undoRedoActionName = undoRedoActionName;
		_undoRedoContext = undoRedoContext;
	}

	public void InitializeText(string text)
	{
		Text = text;
		_oldText = text;
	}

	private void SubmitText()
	{
		string newText = Text;

		if(newText == _oldText)
			return;

		_undoRedo.CreateAction(_undoRedoActionName, UndoRedo.MergeMode.Disable, _undoRedoContext);

		_undoRedo.AddDoMethod(this, GodotObject.MethodName.EmitSignal, SignalName.EditorTextEditTextChanged, this, newText);
		_undoRedo.AddDoProperty(this, TextEdit.PropertyName.Text, newText);
		_undoRedo.AddDoProperty(this, PropertyName._oldText, newText);

		_undoRedo.AddUndoMethod(this, GodotObject.MethodName.EmitSignal, SignalName.EditorTextEditTextChanged, this, _oldText);
		_undoRedo.AddUndoProperty(this, TextEdit.PropertyName.Text, _oldText);
		_undoRedo.AddUndoProperty(this, PropertyName._oldText, _oldText);

		_undoRedo.CommitAction(true);
	}
}

# endif