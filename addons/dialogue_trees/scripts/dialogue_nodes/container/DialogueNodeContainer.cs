#if TOOLS
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueNodes;

[Tool]
public abstract partial class DialogueNodeContainer : DialogueNode
{
    ///<summary>Inserts a control as a child of this <c>DialogueNode</c>. This fuction will automatically move connections to keep them connected to their original control.</summary>
    ///<param name = 'affectSlots'>What types of connections and slots should be moved when the new control is inserted.</param>
	///<param name = 'removedControlData'>Data about this control when it was removed by <c>RemoveControlChild</c>. (Mainly for use with UndoRedo)</param>
    public void InsertControlChild(Control control, int childIndex, DialogueGraph.SlotType affectSlots = DialogueGraph.SlotType.Any, RemovedPortData removedControlData = null)
	{	
		AddChild(control);
		MoveChild(control, childIndex);

		int childCount = GetChildCount();
		
		for(int x = childCount - 1; x > childIndex; x--)
		{
			int oldSlot = x - 1;

			DialogueGraph.SwitchPortConnections(Name, oldSlot, x, affectSlots);

			if(affectSlots.HasFlag(DialogueGraph.SlotType.InPort))
			{
				SetSlotEnabledLeft(x, IsSlotEnabledLeft(oldSlot));
				SetSlotColorLeft(x, GetSlotColorLeft(oldSlot));
			}
			
			if(affectSlots.HasFlag(DialogueGraph.SlotType.OutPort))
			{
				SetSlotEnabledRight(x, IsSlotEnabledRight(oldSlot));
				SetSlotColorRight(x, GetSlotColorRight(oldSlot));
			}
		}

		if(removedControlData != null)
			AddPort(childIndex, removedControlData, affectSlots);
	}

	///<summary>Removes a child control of this <c>DialogueNode</c>. This fuction will automatically move and delete connections to keep them connected to their original control.</summary>
	///<param name = 'affectSlots'>What types of connections and slots should be moved and deleted when the control is removed.</param>
	///<returns>Data about the removed control. (This is to allow for UndoRedo use)</returns>
	public RemovedPortData RemoveControlChild(Control control, DialogueGraph.SlotType affectSlots = DialogueGraph.SlotType.Any)
	{
		int childIndex = control.GetIndex();

		RemovedPortData removedControlData = RemovePort(childIndex, affectSlots);

		int childCount = GetChildCount();

		for(int x = childIndex; x < childCount - 1; x++)
		{
			int oldSlot = x + 1;

			DialogueGraph.SwitchPortConnections(Name, oldSlot, x, affectSlots);

			if(affectSlots.HasFlag(DialogueGraph.SlotType.InPort))
			{
				SetSlotEnabledLeft(x, IsSlotEnabledLeft(oldSlot));
				SetSlotColorLeft(x, GetSlotColorLeft(oldSlot));
			}
			
			if(affectSlots.HasFlag(DialogueGraph.SlotType.OutPort))
			{
				SetSlotEnabledRight(x, IsSlotEnabledRight(oldSlot));
				SetSlotColorRight(x, GetSlotColorRight(oldSlot));
			}
		}

		//Would just call RemoveChild, but that causes a mysterious error
		CallDeferred(Node.MethodName.RemoveChild, control);
		SetDeferred(Control.PropertyName.Size, Vector2.Zero);

		return removedControlData;
	}

	public void AddPort(int index, RemovedPortData portData, DialogueGraph.SlotType affectSlots = DialogueGraph.SlotType.Any)
	{
		foreach(Dictionary con in portData.RemovedConnections)
			DialogueGraph.ConnectNode(con);
		
		if(affectSlots.HasFlag(DialogueGraph.SlotType.InPort))
		{
			SetSlotEnabledLeft(index, portData.PortEnabledLeft);
			SetSlotColorLeft(index, portData.PortColorLeft);
		}
		
		if(affectSlots.HasFlag(DialogueGraph.SlotType.OutPort))
		{
			SetSlotEnabledRight(index, portData.PortEnabledRight);
			SetSlotColorRight(index, portData.PortColorRight);
		}
	}

	public RemovedPortData RemovePort(int index, DialogueGraph.SlotType affectSlots = DialogueGraph.SlotType.Any)
	{
		RemovedPortData removedPortData = new ();

		foreach(Dictionary con in DialogueGraph.GetConnectionsToPort(Name, index, affectSlots))
		{
			removedPortData.RemovedConnections.Add(con);
			DialogueGraph.DisconnectNode(con);
		}

		if(affectSlots.HasFlag(DialogueGraph.SlotType.InPort))
		{
			removedPortData.PortEnabledLeft = IsSlotEnabledLeft(index);
			removedPortData.PortColorLeft = GetSlotColorLeft(index);
		}

		if(affectSlots.HasFlag(DialogueGraph.SlotType.OutPort))
		{
			removedPortData.PortEnabledRight = IsSlotEnabledRight(index); 
			removedPortData.PortColorRight = GetSlotColorRight(index);
		}

		return removedPortData;
	}

	public partial class RemovedPortData : GodotObject
	{
		public RemovedPortData()
		{

		}

		public RemovedPortData(Array<Dictionary> removedConnections, bool portEnabledLeft, bool portEnabledRight, Color portColorLeft, Color portColorRight)
		{
			RemovedConnections = removedConnections;
			PortEnabledLeft = portEnabledLeft;
			PortEnabledRight = portEnabledRight;
			PortColorLeft = portColorLeft;
			PortColorRight = portColorRight;
		}

		public Array<Dictionary> RemovedConnections = new (); 
		public bool PortEnabledLeft; 
		public bool PortEnabledRight;
		public Color PortColorLeft; 
		public Color PortColorRight;
	}
}
#endif