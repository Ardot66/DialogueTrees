# if TOOLS

using System.Collections.Generic;
using System.Linq;
using Ardot.DialogueTrees.DialogueNodes;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees;

[Tool]
public partial class DialogueSwitchNode : DialogueNodeContainer
{
	[Export]
	private PackedScene _caseTextScene;

	[Export]
	private Label _elseCaseLabel;

	private static Color InputSlotColor {get => Color.FromString("White", default);}
	private static Color OutputSlotColor {get => Color.FromString("White", default);}

	[Export]
	private Button _addCaseButton;

	private Array<DialogueSwitchNodeCaseText> _caseTexts = new ();

	public override void _Ready()
	{
		_addCaseButton.Pressed += OnAddCaseButtonPressed;
	}

	public override DialogueNodeSaveData Save()
	{
		string[] caseTexts = new string[_caseTexts.Count];

		for(int x = 0; x < _caseTexts.Count; x++)
			caseTexts[x] = _caseTexts[x].CaseTextEdit.Text;
			
		return new (
			new Array () {caseTexts},
			null
		);
	}

	public override void Load(DialogueNodeSaveData data)
	{
		string[] caseTexts = data.General[0].AsStringArray();

		for(int x = 0; x < caseTexts.Length; x++)
			InsertCase(InstantiateCaseNode(), x, caseTexts[x]);
	}

	private void InsertCase(DialogueSwitchNodeCaseText caseNode, int index, string caseText = "", RemovedCaseData caseData = null)
	{
		ValueButton removeCaseButton = caseNode.RemoveCaseButton;
		EditorTextEdit caseTextEdit = caseNode.CaseTextEdit;

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

		if(_caseTexts.Count == 0)
		{
			_elseCaseLabel.Visible = true;

			SetSlot(0, true, 0, InputSlotColor, true, 0, OutputSlotColor);
			SetSlot(1, false, 0, InputSlotColor, true, 0, OutputSlotColor);
		}

		_caseTexts.Insert(index, caseNode);
	}

	private RemovedCaseData RemoveCase(DialogueSwitchNodeCaseText caseNode)
	{
		Array<Dictionary> removedConnections = new ();

		if(_caseTexts.Count == 1)
		{
			_elseCaseLabel.Visible = false;

			removedConnections = RemovePort(1).RemovedConnections;

			SetSlot(1, false, 0, InputSlotColor, false, 0, OutputSlotColor);
		}

		caseNode.CaseTextEdit.EditorTextEditTextChanged -= OnCaseTextChanged;
		caseNode.RemoveCaseButton.ValueButtonPressed -= OnRemoveCaseButtonPressed;

		RemovedCaseData removedPortData = new(RemoveControlChild(caseNode, _caseTexts.Count == 1 ? DialogueGraph.SlotType.Any : DialogueGraph.SlotType.OutPort), caseNode.CaseTextEdit.Text);

		removedPortData.RemovedConnections.AddRange(removedConnections);

		_caseTexts.Remove(caseNode);

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
		
		DialogueSwitchNodeCaseText caseNode = InstantiateCaseNode();

		undoRedo.AddDoMethod(this, MethodName.InsertCase, caseNode, _caseTexts.Count, "", default);
		undoRedo.AddUndoMethod(this, MethodName.RemoveCase, caseNode);

		undoRedo.CommitAction();
	}

	private void OnRemoveCaseButtonPressed(Variant value)
	{
		EditorUndoRedoManager undoRedo = GetUndoRedo();
		undoRedo.CreateAction("Remove Switch Case", UndoRedo.MergeMode.Disable, GetDialogueTree());

		DialogueSwitchNodeCaseText caseNode = (DialogueSwitchNodeCaseText)value.AsGodotObject();
		int caseIndex = caseNode.GetIndex();

		RemovedCaseData caseData = RemoveCase(caseNode);

		undoRedo.AddUndoReference(caseNode);

		undoRedo.AddDoMethod(this, MethodName.RemoveCase, caseNode);
		undoRedo.AddUndoMethod(this, MethodName.InsertCase, caseNode, caseIndex, "", caseData);

		undoRedo.CommitAction(false);
	}

	private DialogueSwitchNodeCaseText InstantiateCaseNode()
	{
		return _caseTextScene.Instantiate<DialogueSwitchNodeCaseText>();
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
