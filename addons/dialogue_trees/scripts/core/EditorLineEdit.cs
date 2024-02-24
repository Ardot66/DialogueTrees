# if TOOLS

using Godot;
using System;

namespace Ardot.DialogueTrees;

[Tool]
public partial class EditorLineEdit : LineEdit
{
	private EditorUndoRedoManager _undoRedo;
	private GodotObject _undoRedoContext;
	private string _undoRedoActionName;

	private string _oldText;
	
	[Signal]
	public delegate void EditorLineEditTextChangedUndoRedoEventHandler(EditorLineEdit lineEdit, EditorUndoRedoManager undoRedo, string newText);

	[Signal]
	public delegate void EditorLineEditTextChangedEventHandler(EditorLineEdit lineEdit, string newText);

    public override void _Ready()
    {
		TextSubmitted += OnFunctionNameSubmitted;
		FocusExited += SubmitText;
    }

    ///<summary>Sets up this <c>EditorOptionButton</c>.</summary>
	public void InitializeUndoRedo(EditorUndoRedoManager undoRedo, string undoRedoActionName, GodotObject undoRedoContext = null)
	{
		_undoRedo = undoRedo;
		_undoRedoActionName = undoRedoActionName;
		_undoRedoContext = undoRedoContext;
	}

	///<summary>Sets the initial value of the text that UndoRedo will default to.</summary>
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
		
		_undoRedo.AddDoMethod(this, GodotObject.MethodName.EmitSignal, SignalName.EditorLineEditTextChanged, this, newText);
		_undoRedo.AddDoProperty(this, TextEdit.PropertyName.Text, newText);
		_undoRedo.AddDoProperty(this, PropertyName._oldText, newText);

		_undoRedo.AddUndoMethod(this, GodotObject.MethodName.EmitSignal, SignalName.EditorLineEditTextChanged, this, _oldText);
		_undoRedo.AddUndoProperty(this, TextEdit.PropertyName.Text, _oldText);
		_undoRedo.AddUndoProperty(this, PropertyName._oldText, _oldText);

		EmitSignal(SignalName.EditorLineEditTextChangedUndoRedo, this, _undoRedo, newText);

		_undoRedo.CommitAction();
	}

	private void OnFunctionNameSubmitted(string newText)
	{	
		ReleaseFocus();
	}
}

# endif