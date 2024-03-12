# if TOOLS

using Godot;
using Godot.Collections;
using System.Linq;

namespace Ardot.DialogueTrees;

[Tool]
public partial class EditorOptionButton : OptionButton
{
	[Export]
	public bool SortItems = true;

	public Variant SelectedObject;

	private EditorUndoRedoManager _undoRedo;
	private GodotObject _undoRedoContext;
	private string _undoRedoActionName;

	private GodotObject _getObjectNameObject;
	private StringName _getObjectNameMethodName;

	private int _previousSelectedObjectIndex = -2;

	[Signal]
	public delegate void EditorOptionButtonMidUndoRedoEventHandler(EditorOptionButton optionButton, EditorUndoRedoManager undoRedo, Variant newSelectedObject);

	[Signal]
	public delegate void EditorOptionButtonOptionSelectedEventHandler(EditorOptionButton optionButton, Variant newSelectedObject);

	public override void _Ready()
	{
		ItemSelected += OnOptionSelected;
	}

	///<summary>Sets up this <c>EditorOptionButton</c>'s undo-redo capability.</summary>
	public void InitializeUndoRedo(EditorUndoRedoManager undoRedo, string undoRedoActionName, GodotObject undoRedoContext = null)
	{
		_undoRedo = undoRedo;
		_undoRedoContext = undoRedoContext;
		_undoRedoActionName = undoRedoActionName;
	}

	///<summary>
	///Sets up this <c>EditorOptionButton</c> to get the name of an object for displaying in the UI.
	///The <c>getObjectMethodName</c> parameter must point to a method in <c>GetObjectNameObject</c> that takes a <c>Variant</c> and returns a <c>string</c>.
	///</summary>
	public void InitializeGetObjectName(GodotObject getObjectNameObject, StringName getObjectNameMethodName)
	{
		_getObjectNameObject = getObjectNameObject;
		_getObjectNameMethodName = getObjectNameMethodName;
	}

	public void UpdateOptionsUI(Array<GodotObject> optionObjects)
	{
		UpdateOptionsUI(new Array (optionObjects.Select((obj) => Variant.CreateFrom(obj))));
	}

	///<summary>Updates the options UI. You must call <c>Initialize()</c> first to set up this <c>EditorOptionButton</c>.</summary>
	public void UpdateOptionsUI(Array optionObjects)
	{
		Clear();
		AddItem("None");

		string[] objectNames = new string[optionObjects.Count];

		for(int x = 0; x < objectNames.Length; x++)
			objectNames[x] = _getObjectNameObject.Call(_getObjectNameMethodName, optionObjects[x]).AsString();
	
		Variant[] sortedOptionObjects = optionObjects.ToArray();

		if(SortItems)
			System.Array.Sort(objectNames, sortedOptionObjects);

		for(int x = 0; x < sortedOptionObjects.Length; x++)
		{
			AddItem(objectNames[x]);
			SetItemMetadata(x + 1, sortedOptionObjects[x]);
		}	
			
		int selectedObjectIndex = System.Array.FindIndex(sortedOptionObjects, (v) => v.Obj.Equals(SelectedObject.Obj));

		if(selectedObjectIndex != -1)
		{
			Select(selectedObjectIndex + 1);
			_previousSelectedObjectIndex = selectedObjectIndex + 1;
		}
		else if(Selected != -1)
		{
			Select(-1);
			_previousSelectedObjectIndex = -1;
		}
			
		if(_previousSelectedObjectIndex == -2)
			_previousSelectedObjectIndex = selectedObjectIndex == -1 ? -1 : selectedObjectIndex + 1;

		Size = Vector2.Zero;
	}

	private void OnOptionSelected(long optionIndex)
	{
		int index = (int)optionIndex;

		if(index == 0)
		{
			index = -1;

			Select(index);
		}

		Variant newSelectedObject = index == -1 ? default : GetItemMetadata(index);

		_undoRedo.CreateAction(_undoRedoActionName, UndoRedo.MergeMode.Disable, _undoRedoContext);

		_undoRedo.AddDoMethod(this, GodotObject.MethodName.EmitSignal, SignalName.EditorOptionButtonOptionSelected, this, newSelectedObject);
		_undoRedo.AddDoMethod(this, OptionButton.MethodName.Select, index);
		if(index != -1)
			_undoRedo.AddDoProperty(this, PropertyName.SelectedObject, newSelectedObject);
		_undoRedo.AddDoProperty(this, PropertyName._previousSelectedObjectIndex, index);

		_undoRedo.AddUndoMethod(this, GodotObject.MethodName.EmitSignal, SignalName.EditorOptionButtonOptionSelected, this, SelectedObject);
		_undoRedo.AddUndoProperty(this, PropertyName.SelectedObject, SelectedObject);
		_undoRedo.AddUndoMethod(this, OptionButton.MethodName.Select, _previousSelectedObjectIndex);
		_undoRedo.AddUndoProperty(this, PropertyName._previousSelectedObjectIndex, _previousSelectedObjectIndex);

		EmitSignal(SignalName.EditorOptionButtonMidUndoRedo, this, _undoRedo, newSelectedObject);

		_undoRedo.CommitAction(false);

		EmitSignal(SignalName.EditorOptionButtonOptionSelected, this, newSelectedObject);
		SelectedObject = newSelectedObject;
		_previousSelectedObjectIndex = index;
	} 
}

# endif