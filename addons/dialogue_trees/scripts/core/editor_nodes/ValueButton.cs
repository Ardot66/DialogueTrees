# if TOOLS

using Godot;
using System;

namespace Ardot.DialogueTrees;

[Tool]
public partial class ValueButton : Button
{
	public Variant Value;

	[Signal]
	public delegate void ValueButtonPressedEventHandler(Variant Value);

	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private void OnPressed()
	{
		EmitSignal(SignalName.ValueButtonPressed, Value);
	}
}

# endif
