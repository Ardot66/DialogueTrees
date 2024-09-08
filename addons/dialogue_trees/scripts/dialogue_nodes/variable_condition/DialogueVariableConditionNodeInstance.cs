using Godot;
using Godot.Collections;
using Ardot.DialogueTrees.DialogueVariables;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueVariableConditionNodeInstance : DialogueNodeInstance
{
    public DialogueVariableNodeInstance VariableNode => _variableNode;

    private DialogueVariableNodeInstance _variableNode;
    private DialogueVariableConditionInstance _variableConditionInstance;

    public override void Ready(Array data)
    {
        long variableNodeID = data[0].AsInt64();

        if(variableNodeID != -1)
        {
            _variableNode = DialogueTree.GetDialogueNodeInstance<DialogueVariableNodeInstance>(variableNodeID);

            DialogueVariableNodeInstance.VariableInstanceData variableData = _variableNode.GetVariableDataForType(_variableNode.VariableType);

            GodotObject @object = new ();
            ulong instanceID = @object.GetInstanceId();
            @object.SetScript(variableData.VariableConditionInstanceScript);

            _variableConditionInstance = InstanceFromId(instanceID) as DialogueVariableConditionInstance;

            _variableConditionInstance?.Ready(data[1].AsGodotArray());
        }
    }

    public override void RecievePortInput(int portIndex)
    {
        if(_variableNode != null && _variableConditionInstance != null && _variableConditionInstance.RunCondition(_variableNode.VariableValue))
        {
            SendPortOutput(0);
            return;
        }
            
        SendPortOutput(1);
    }
}