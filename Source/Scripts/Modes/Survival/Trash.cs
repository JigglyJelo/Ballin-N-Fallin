using Godot;

public partial class Trash : Node{
	public InterpolatedBody Rb;
	public override void _Ready(){
		Rb = GetNode<InterpolatedBody>("RigidBody2D");
	}
}