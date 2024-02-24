using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueActions;

[Tool]
[GlobalClass]
public partial class SetPropertyAction : DialogueAction
{
	public class CustomPropertyName
	{
		public static readonly StringName 
		Property = "Property",
		Value = "Value";
	}

	public NodePath TargetedNode
	{
		get => _targetedNode;
		set
		{
			_targetedNode = value;
			NotifyPropertyListChanged();
		}
	}

	public StringName Property => _property;
	public Variant Value => _value;

	private NodePath _targetedNode;

	private StringName _property;

	private Variant _value = default;

	public override void Invoke(params Variant[] parameters)
	{
		if(!HasNode(TargetedNode))
			return;

		Node targetedNode = GetNode(TargetedNode);

		targetedNode.Set(_property, _value);
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		List<string> targetedNodePropertyNames = new ();

		Dictionary targetedProperty = null;

		if(HasNode(TargetedNode))
		{
			List<Dictionary> targetedNodeProperties = GetTargetedNodePropertyList();

			foreach(Dictionary property in targetedNodeProperties)
			{
				StringName name = property["name"].AsStringName();

				targetedNodePropertyNames.Add(name);

				if(name == _property)
					targetedProperty = property;
			}
		}
			
		return new()
		{
			new()
			{
				{"name", PropertyName.TargetedNode},
				{"type", (int)Variant.Type.NodePath},
			},
			new()
			{
				{"name", CustomPropertyName.Property},
				{"type", (int)Variant.Type.Int},
				{"usage", (int)PropertyUsageFlags.Editor},
				{"hint", (int)PropertyHint.Enum},
				{"hint_string", string.Join(',', targetedNodePropertyNames)}
			},
			new()
			{
				{"name", CustomPropertyName.Value},
				{"type", targetedProperty != null ? targetedProperty["type"] : 0},
				{"usage", (int)PropertyUsageFlags.Editor},
				{"hint", targetedProperty != null ? targetedProperty["hint"] : 0},
				{"hint_string", targetedProperty != null ? targetedProperty["hint_string"] : ""},
			},
			new()
			{
				{"name", PropertyName._property},
				{"type", (int)Variant.Type.StringName},
				{"usage", (int)PropertyUsageFlags.Storage}
			},
			new()
			{
				{"name", PropertyName._value},
				{"type", targetedProperty != null ? targetedProperty["type"] : 0},
				{"usage", (int)PropertyUsageFlags.Storage}
			}
		};
	}

	public override Variant _Get(StringName property)
	{
		if(!HasNode(TargetedNode))
			return default;

		List<Dictionary> targetedNodeProperties = GetTargetedNodePropertyList();

		if(property == CustomPropertyName.Property)
		{
			for(int x = 0; x < targetedNodeProperties.Count; x++)
			{
				if(targetedNodeProperties[x]["name"].AsStringName() == _property)
					return x;
			}
		}
		else if(property == CustomPropertyName.Value)
			return _value;

		return default;
	}

	public override bool _Set(StringName property, Variant value)
	{
		if(!HasNode(TargetedNode))
			return false;

		List<Dictionary> targetedNodeProperties = GetTargetedNodePropertyList();

		if(property == CustomPropertyName.Property)
		{
			Dictionary targetedProperty = targetedNodeProperties[value.AsInt32()];

			_property = targetedProperty["name"].AsStringName();

            _value = targetedProperty["type"].AsInt32() switch
            {
                (int)Variant.Type.String => "",
                _ => default,
            };

            NotifyPropertyListChanged();
		}
		else if(property == CustomPropertyName.Value)
			_value = value;
		else
			return false;

		return true;
	}

	private List<Dictionary> GetTargetedNodePropertyList()
	{
		if(!HasNode(TargetedNode))
			return null;

		Array<Dictionary> targetedNodeRawProperties = GetNode(TargetedNode).GetPropertyList();
		List<Dictionary> targetedNodeProperties = new ();

		foreach(Dictionary property in targetedNodeRawProperties)
		{
			//Check if usage is any of these usages
			const int usages = (int)(PropertyUsageFlags.Category | PropertyUsageFlags.Group);

			int propertyUsages = property["usage"].AsInt32();
			int propertyType = property["type"].AsInt32();

			if((propertyUsages & usages) != 0 || propertyType == (int)Variant.Type.Nil)
				continue;

			targetedNodeProperties.Add(property);
		}

		targetedNodeProperties.Sort((a, b) => a["name"].AsString().CompareTo(b["name"].AsString()));

		return targetedNodeProperties;
	}
}
