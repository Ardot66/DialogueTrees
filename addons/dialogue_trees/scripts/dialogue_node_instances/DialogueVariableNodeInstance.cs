using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

public partial class DialogueVariableNodeInstance : DialogueNodeInstance
{
    public Variant VariableValue
    {
        get => _variableValue;
        set
        {
            EmitSignal(SignalName.VariableValueChanging, this, value);

            _variableValue = value;
        }
    }

    public string VariableName => _variableName;
    public string VariableType => _variableType;
    private Variant VariableDefinition => _variableDefinition; 

    private string _variableName;
    private string _variableType;
    private Variant _variableDefinition;
    private Variant _variableValue;

    ///<summary>Called immediately before the value of this <c>DialogueVariableNodeInstance</c> changes.</summary>
    [Signal]
    public delegate void VariableValueChangingEventHandler(DialogueVariableNodeInstance variableNode, Variant newValue);

    public override void Ready(Array data)
    {
        _variableName = data[0].AsString();
        _variableType = data[1].AsString();
        _variableValue = data[2];
        _variableDefinition = data[3];
    }

    ///<summary>Returns a list of data about how different variables should be defined.</summary>
	public virtual VariableInstanceData[] GetVariableDataList()
	{
		return new VariableInstanceData[]
		{
			new(
				"Enum",
                ResourceLoader.Load<Script>($"{DialogueTreesPlugin.DialogueTreesPluginPath}/scripts/variable_node_instances/DialogueEnumSetterInstance.cs"),
                ResourceLoader.Load<Script>($"{DialogueTreesPlugin.DialogueTreesPluginPath}/scripts/variable_node_instances/DialogueEnumConditionInstance.cs")
			)
		};
	}

    public VariableInstanceData GetVariableDataForType(StringName type)
	{
		foreach(VariableInstanceData data in GetVariableDataList())
			if(data.VariableType == type)
				return data;

		return default;
	}

    public struct VariableInstanceData
	{
		///<summary>Creates a new <c>DialogueVariableInstanceData</c>. See the docs for <c>DialogueVariableInstanceData</c>'s properties for more information on these parameters.</summary>
		public VariableInstanceData(StringName variableType, Script variableSetterInstanceScript, Script variableComparerInstanceScript)
		{
			VariableType = variableType;
			VariableSetterInstanceScript = variableSetterInstanceScript;
			VariableConditionInstanceScript = variableComparerInstanceScript;
		}

		///<summary>The type of this variable.</summary>
		public StringName VariableType;

		///<summary>The script that handles setting and modifying a dialogue variable. The script must inherit from <c>DialogueVariableSetterInstance</c>.</summary>
		public Script VariableSetterInstanceScript;
		///<summary>The scene that handles comparing the dialogue variable to another value. The script must inherit from <c>DialogueVariableConditionInstance</c>.</summary>
		public Script VariableConditionInstanceScript;
	}
}