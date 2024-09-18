using Godot;
namespace Ardot.DialogueTrees.Runtime;

[Tool]
[GlobalClass]
public partial class DialogueNodeData : Resource
{
    [Export]
    private StringName _dialogueNodeName;
    public StringName DialogueNodeName {get => _dialogueNodeName;}

    private StringName _dialogueNodeSaveName;
    public StringName DialogueNodeSaveName {get => _dialogueNodeSaveName;}
    
    private PackedScene _dialogueNodeSingletonScene;
    public PackedScene DialogueNodeSingletonScene {get => _dialogueNodeSingletonScene;}

#if TOOLS
    [Export]
    private PackedScene _dialogueNodeScene;
    public PackedScene DialogueNodeScene {get => _dialogueNodeScene;}
#endif

    [ExportGroup("Tooltip")]
    [Export(PropertyHint.MultilineText)]
    private string _dialogueNodeTooltip;
    public string DialogueNodeTooltip {get => _dialogueNodeTooltip;}

    [ExportGroup("Advanced")]
    [Export]
    private int _nodeLimit = -1;
    
    public int NodeLimit { get => _nodeLimit == -1 ? int.MaxValue : _nodeLimit; }

    [Export]
    private bool _canBeDeleted = true;
    public bool CanBeDeleted {get => _canBeDeleted;}

    [Export]
    private bool _includeInAddNodeMenu = true;
    public bool IncludeInAddNodeMenu {get => _includeInAddNodeMenu;}

    [Export]
    private int _includeInNewTrees = 0;
    public int IncludeInNewTrees {get => _includeInNewTrees;}

    public bool IsValid()
    {
        return DialogueNodeScene != null && _dialogueNodeSingletonScene != null && !string.IsNullOrEmpty(DialogueNodeName) && !string.IsNullOrEmpty(DialogueNodeSaveName);
    }

    # if TOOLS
    
    public bool TryInstantiateDialogueNode(out Editor.DialogueNode dialogueNode)
    {
        dialogueNode = null;

        if(!IsValid())
            return false;
        
        dialogueNode = DialogueNodeScene.Instantiate() as Editor.DialogueNode;
        return dialogueNode != null;
    }

    # endif
}