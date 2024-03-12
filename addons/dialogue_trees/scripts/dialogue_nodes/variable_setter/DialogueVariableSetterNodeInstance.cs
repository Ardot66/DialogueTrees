using Godot;
using Godot.Collections;
using Ardot.DialogueTrees.DialogueVariables;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueVariableSetterNodeInstance : DialogueNodeInstance
{
    public DialogueVariableNodeInstance VariableNode => _variableNode;

    private DialogueVariableNodeInstance _variableNode;
    private DialogueVariableSetterInstance _variableSetterInstance;

    public override void Ready(Array data)
    {
        int variableNodeIndex = data[0].AsInt32();

        if(variableNodeIndex != -1)
        {
            _variableNode = DialogueTree.GetDialogueNodeInstance<DialogueVariableNodeInstance>(variableNodeIndex);

            DialogueVariableNodeInstance.VariableInstanceData variableData = _variableNode.GetVariableDataForType(_variableNode.VariableType);

            GodotObject @object = new ();
            ulong instanceID = @object.GetInstanceId();
            @object.SetScript(variableData.VariableSetterInstanceScript);

            _variableSetterInstance = InstanceFromId(instanceID) as DialogueVariableSetterInstance;

            _variableSetterInstance?.Ready(data[1].AsGodotArray());
        }
    }

    public override void RecievePortInput(int portIndex)
    {
        if(_variableNode != null && _variableSetterInstance != null)
            _variableNode.VariableValue = _variableSetterInstance.SetVariable(_variableNode.VariableValue);
        
        SendPortOutput(0);
    }
}