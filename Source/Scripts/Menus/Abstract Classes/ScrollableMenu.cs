using Godot;
using System.Collections.Generic;

/// <summary>
/// An abstract menu class that extends <see cref="VerticalMenu"/> to support automatic vertical scrolling.
/// Ideal for long lists like inventories, leaderboards, or expansive options menus.
/// </summary>
public abstract partial class ScrollableMenu : VerticalMenu{
	/// <summary>The maximum number of items visible on the screen at once before the menu needs to scroll.</summary>
	[Export] public int VisibleItems = 8;
	/// <summary>The parent node containing all selectable items. This node physically moves to simulate scrolling.</summary>
	protected Node2D selectionsContainer;
	private float baseContainerY;
	private int visibleWindowStart = 0;
	private Tween scrollTween;
	
	// C# native memory to track the starting positions of any dynamically generated labels
	private Dictionary<ulong, float> basePositions = new Dictionary<ulong, float>();

	public override void _Ready(){
		base._Ready();
		selectionsContainer = GetNodeOrNull<Node2D>("Selections");
		if(selectionsContainer != null){
			baseContainerY = selectionsContainer.Position.Y;
			if(Selections == null || Selections.Count == 0) Selections = selectionsContainer.GetChildren();
			totalSelections = Selections.Count;
		}
	}

	protected override void UpdateSelectionVisual(){
		base.UpdateSelectionVisual();

		if(selectionsContainer == null || Selections == null || Selections.Count == 0) return;

		// --- THE C# DICTIONARY FIX ---
		// Check the Instance ID of the current labels. If we haven't seen them before, record their starting Y.
		for(int i = 0; i < Selections.Count; i++){
			Node node = Selections[i];
			ulong id = node.GetInstanceId();
			
			if(!basePositions.ContainsKey(id)){
				if(node is Node2D n2) basePositions.Add(id, n2.Position.Y);
				else if(node is Control c) basePositions.Add(id, c.Position.Y);
			}
		}

		int currentIndex = Selection - 1;

		//--- WINDOW LOGIC ---
		if(currentIndex < visibleWindowStart) visibleWindowStart = currentIndex;
		else if(currentIndex >= visibleWindowStart + VisibleItems) visibleWindowStart = currentIndex - VisibleItems + 1;

		//--- SCROLLING LOGIC ---
		// Read from the C# dictionary using the Instance IDs
		ulong firstItemId = Selections[0].GetInstanceId();
		ulong targetItemId = Selections[visibleWindowStart].GetInstanceId();
		
		float firstItemY = basePositions[firstItemId];
		float targetTopY = basePositions[targetItemId];
		float offset = targetTopY - firstItemY;
		
		if(scrollTween != null && scrollTween.IsValid()) scrollTween.Kill(); 
		
		scrollTween = CreateTween();
		scrollTween.SetParallel(true); 
		
		for(int i = 0; i < Selections.Count; i++){
			Node node = Selections[i];
			float baseY = basePositions[node.GetInstanceId()];
			float targetY = baseY - offset;
			
			if(node is Node2D n2) {
				scrollTween.TweenProperty(n2, "position:y", targetY, 0.15f);
			} 
			else if(node is Control c) {
				scrollTween.TweenProperty(c, "position:y", targetY, 0.15f);
			}
		}
	}

	private float GetItemLocalY(int index){
		if(index < 0 || index >= Selections.Count) return 0;
		if(Selections[index] is Node2D node2D) return node2D.Position.Y;
		else if(Selections[index] is Control control) return control.Position.Y;
		return 0;
	}
}