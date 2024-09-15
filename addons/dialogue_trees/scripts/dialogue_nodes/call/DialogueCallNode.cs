# if TOOLS

using System.Linq;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

[Tool]
public partial class DialogueCallNode : DialogueNode
{
	private Array<GodotObject> _avaliableFunctions = new ();

	[Export]
	private EditorOptionButton _functionSelectButton;

	private int _connectedFunctionIndex = -1;

	public override void _Ready()
	{
		_functionSelectButton.InitializeUndoRedo(GetUndoRedo(), "Set Connected Function", GetDialogueTree());
		_functionSelectButton.InitializeGetObjectName(this, MethodName.GetFunctionName);
		
		DialogueGraph.ChildEnteredTree += OnChildEnteredGraph;
		DialogueGraph.ChildExitingTree += OnChildExitingGraph;

		if(_connectedFunctionIndex != -1)
		 	_functionSelectButton.SelectedObject = DialogueGraph.DialogueNodes[_connectedFunctionIndex];

		UpdateFunctionsList();
	}

	public override void Load(DialogueNodeSaveData data)
	{	
		_connectedFunctionIndex = data.References[0];
	}

	public override DialogueNodeSaveData Save() => new(
		null,
		new Array<int>() {_functionSelectButton.SelectedObject.VariantType == Variant.Type.Object ? DialogueGraph.GetDialogueNodeIndex((DialogueNode)_functionSelectButton.SelectedObject) : -1}
	);

	private void UpdateFunctionsList()
	{
		foreach(Node child in DialogueGraph.GetChildren())
			if(!_avaliableFunctions.Contains(child))
				AddFunctionNode(child, false);

		_functionSelectButton.UpdateOptionsUI(_avaliableFunctions);
	}

	private void OnChildEnteredGraph(Node child)
	{
		AddFunctionNode(child, true);
	}

	private void AddFunctionNode(Node node, bool updateUI)
	{
		if(node is DialogueFunctionNode functionNode)
		{
			_avaliableFunctions.Add(functionNode);

			functionNode.FunctionNameChanged += OnFunctionNodeNameChanged;

			if(updateUI)
				_functionSelectButton.UpdateOptionsUI(_avaliableFunctions);
		}
	}

	private void OnChildExitingGraph(Node child)
	{
		if(child is DialogueFunctionNode functionNode)
		{
			_avaliableFunctions.Remove(functionNode);

			functionNode.FunctionNameChanged -= OnFunctionNodeNameChanged;

			_functionSelectButton.UpdateOptionsUI(_avaliableFunctions);
		}
	}

	private void OnFunctionNodeNameChanged()
	{
	 	_functionSelectButton.CallDeferred(EditorOptionButton.MethodName.UpdateOptionsUI, _avaliableFunctions);
	}

	private string GetFunctionName(Variant function)
	{
		return ((DialogueFunctionNode)function).FunctionName;
	}
}

# endif
