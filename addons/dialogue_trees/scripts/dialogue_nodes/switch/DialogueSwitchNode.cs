# if TOOLS

using System.Linq;
using Ardot.DialogueTrees.DialogueNodes;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees;

[Tool]
public partial class DialogueSwitchNode : DialogueNodeContainer
{
	private const string 
	_caseTextPath = $"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/dialogue_nodes/subscenes/case_text.tscn",
	_elseCaseLabelPath = "Label",
	_caseTextEditPath = "HBoxContainer/TextEdit",
	_addCaseButtonPath = "MarginContainer/AddCaseButton",
	_removeCaseButtonPath = "HBoxContainer/Button";

	private const int 
	_extraChildrenCount = 2;

	private static Color InputSlotColor {get => Color.FromString("White", default);}
	private static Color OutputSlotColor {get => Color.FromString("White", default);}

	private Button _addCaseButton;
	private int _caseCount;

	public override void _Ready()
	{
		_addCaseButton = GetNode<Button>(_addCaseButtonPath);
 
		_addCaseButton.Pressed += OnAddCaseButtonPressed;
	}

	public override Array Save()
	{
		Array<Node> children = GetChildren();

		string[] caseTexts = new string[children.Count - _extraChildrenCount];

		for(int x = 0; x < children.Count - _extraChildrenCount; x++)
			caseTexts[x] = children[x].GetNode<TextEdit>(_caseTextEditPath).Text;

		return new () 
		{
			caseTexts
		};
	}

	public override void Load(Array data)
	{
		string[] caseTexts = data[0].AsStringArray();

		for(int x = 0; x < caseTexts.Length; x++)
			InsertCase(InstantiateCaseNode(), x, caseTexts[x]);
	}

	private void InsertCase(Control caseNode, int index, string caseText = "", RemovedCaseData caseData = null)
	{
		ValueButton removeCaseButton = caseNode.GetNode<ValueButton>(_removeCaseButtonPath);
		EditorTextEdit caseTextEdit = caseNode.GetNode<EditorTextEdit>(_caseTextEditPath);

		if(caseData == null)
		{
			caseTextEdit.InitializeUndoRedo(GetUndoRedo(), "Set Switch Case", GetDialogueTree());
			caseTextEdit.InitializeText(caseText);
		}

		caseTextEdit.EditorTextEditTextChanged += OnCaseTextChanged;

		removeCaseButton.Value = caseNode;
		removeCaseButton.ValueButtonPressed += OnRemoveCaseButtonPressed;

		InsertControlChild(caseNode, index, DialogueGraph.SlotType.OutPort);

		if(caseData != null)
		{
			AddPort(index, caseData, DialogueGraph.SlotType.OutPort);
			caseTextEdit.InitializeText(caseData.RemovedCaseText);
		}

		if(_caseCount == 0)
		{
			Label elseCaseLabel = GetNode<Label>(_elseCaseLabelPath);
			elseCaseLabel.Visible = true;

			SetSlot(0, true, 0, InputSlotColor, true, 0, OutputSlotColor);
			SetSlot(1, false, 0, InputSlotColor, true, 0, OutputSlotColor);
		}

		_caseCount++;
	}

	private RemovedCaseData RemoveCase(Control caseNode)
	{
		Array<Dictionary> removedConnections = new ();

		if(_caseCount == 1)
		{
			Label elseCaseLabel = GetNode<Label>(_elseCaseLabelPath);
			elseCaseLabel.Visible = false;

			removedConnections = RemovePort(1).RemovedConnections;

			SetSlot(1, false, 0, InputSlotColor, false, 0, OutputSlotColor);
		}

		ValueButton removeCaseButton = caseNode.GetNode<ValueButton>(_removeCaseButtonPath);
		EditorTextEdit caseTextEdit = caseNode.GetNode<EditorTextEdit>(_caseTextEditPath);

		caseTextEdit.EditorTextEditTextChanged -= OnCaseTextChanged;
		removeCaseButton.ValueButtonPressed -= OnRemoveCaseButtonPressed;

		RemovedCaseData removedPortData = new(RemoveControlChild(caseNode, _caseCount == 1 ? DialogueGraph.SlotType.Any : DialogueGraph.SlotType.OutPort), caseTextEdit.Text);

		removedPortData.RemovedConnections.AddRange(removedConnections);

		_caseCount--;

		return removedPortData;
	}

	private void OnCaseTextChanged(EditorTextEdit textEdit, string newText)
	{
		SetDeferred(Control.PropertyName.Size, Vector2.Zero);
	}

	private void OnAddCaseButtonPressed()
	{
		EditorUndoRedoManager undoRedo = GetUndoRedo();
		undoRedo.CreateAction("Add Switch Case", UndoRedo.MergeMode.Disable, GetDialogueTree());
		
		Control caseNode = InstantiateCaseNode();

		undoRedo.AddDoMethod(this, MethodName.InsertCase, caseNode, _caseCount, "", default);
		undoRedo.AddUndoMethod(this, MethodName.RemoveCase, caseNode);

		undoRedo.CommitAction();
	}

	private void OnRemoveCaseButtonPressed(Variant value)
	{
		EditorUndoRedoManager undoRedo = GetUndoRedo();
		undoRedo.CreateAction("Remove Switch Case", UndoRedo.MergeMode.Disable, GetDialogueTree());

		Control caseNode = (Control)value.AsGodotObject();
		int caseIndex = caseNode.GetIndex();

		RemovedCaseData caseData = RemoveCase(caseNode);

		undoRedo.AddUndoReference(caseNode);

		undoRedo.AddDoMethod(this, MethodName.RemoveCase, caseNode);
		undoRedo.AddUndoMethod(this, MethodName.InsertCase, caseNode, caseIndex, "", caseData);

		undoRedo.CommitAction(false);
	}

	private static Control InstantiateCaseNode()
	{
		return ResourceLoader.Load<PackedScene>(_caseTextPath).Instantiate<Control>();
	}

	public partial class RemovedCaseData : RemovedPortData
	{	
		public RemovedCaseData(RemovedPortData portData, string removedCaseText) : base(portData.RemovedConnections, portData.PortEnabledLeft, portData.PortEnabledRight, portData.PortColorLeft, portData.PortColorRight)
		{
			RemovedCaseText = removedCaseText;
		}

		public string RemovedCaseText;
	}
}

# endif
