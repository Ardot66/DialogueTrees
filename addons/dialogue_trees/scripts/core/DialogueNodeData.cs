using Godot;
using Godot.Collections;
using System;

namespace Ardot.DialogueTrees;

[Tool]
[GlobalClass]
public partial class DialogueNodeData : Resource
{
    
    private StringName _dialogueNodeName;

    [Export]
    public StringName DialogueNodeName
    {
        get => _dialogueNodeName;

        set
        {
            _dialogueNodeName = value;
            dialogueNodeCollection?.NotifyCollectionModified();
        }
    }

    private StringName _dialogueNodeSaveName;

    [Export]
    public StringName DialogueNodeSaveName
    {
        get => _dialogueNodeSaveName;

        set
        {
            _dialogueNodeSaveName = value;
            dialogueNodeCollection?.NotifyCollectionModified();
        }
    }

    private PackedScene _dialogueNodeScene;

    [Export]
    public PackedScene DialogueNodeScene
    {
        get => _dialogueNodeScene;
        set
        {
            _dialogueNodeScene = value;
            dialogueNodeCollection?.NotifyCollectionModified();
        }
    }

    private Script _instanceScript;
    [Export]
    public Script InstanceScript
    {
        get => _instanceScript;
        set
        {
            _instanceScript = value;
            dialogueNodeCollection?.NotifyCollectionModified();
        }
    }

    [ExportGroup("Tooltip")]
    [Export(PropertyHint.MultilineText)]
    public string DialogueNodeTooltip;

    [ExportGroup("Advanced")]
    [Export]
    private int _nodeLimit = -1;
    
    public int NodeLimit
    {
        get => _nodeLimit == -1 ? int.MaxValue : _nodeLimit;
        set => _nodeLimit = value;
    }

    [Export]
    public bool CanBeDeleted = true;

    [Export]
    public bool IncludeInAddNodeMenu = true;

    [Export]
    public int IncludeInNewTrees = 0;

    public DialogueTreeSettings dialogueNodeCollection;

    public bool IsValid()
    {
        return DialogueNodeScene != null && InstanceScript != null && !string.IsNullOrEmpty(DialogueNodeName) && !string.IsNullOrEmpty(DialogueNodeSaveName);
    }
    
    public bool TryInstantiateDialogueNode(out DialogueNode dialogueNode)
    {
        dialogueNode = null;

        if(!IsValid())
            return false;

        dialogueNode = DialogueNodeScene.Instantiate() as DialogueNode;

        return dialogueNode != null;
    }

    public bool TryInstantiateDialogueNodeInstance(out DialogueNodeInstance dialogueNodeInstance)
    {
        dialogueNodeInstance = null;

        if(!IsValid())
            return false;

		GodotObject @object = new ();
		ulong instanceID = @object.GetInstanceId();
		@object.SetScript(InstanceScript);

		dialogueNodeInstance = InstanceFromId(instanceID) as DialogueNodeInstance;

		return dialogueNodeInstance != null;
    }
}