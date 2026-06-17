using Godot;
/// <summary>
/// An abstract menu class that extends <see cref="VerticalMenu"/> to support automatic vertical scrolling.
/// Ideal for long lists like inventories, leaderboards, or expansive options menus.
/// </summary>
public abstract partial class ScrollableMenu : VerticalMenu{
    /// <summary>The maximum number of items visible on the screen at once before the menu needs to scroll.</summary>
    [Export] public int VisibleItems = 5;
    /// <summary>The parent node containing all selectable items. This node physically moves to simulate scrolling.</summary>
    protected Node2D selectionsContainer;
    private float baseContainerY;
    private int visibleWindowStart = 0;
    private Tween scrollTween;

    public override void _Ready(){
        base._Ready();
        selectionsContainer = GetNodeOrNull<Node2D>("Selections");
        if(selectionsContainer != null){
            //Store the original Y position so we have a reliable anchor to calculate off of
            baseContainerY = selectionsContainer.Position.Y;
            //Ensure Selections list and totalSelections are populated for VerticalMenu's math
            if(Selections == null || Selections.Count == 0) Selections = selectionsContainer.GetChildren();
            totalSelections = Selections.Count;
        }
    }

    protected override void UpdateSelectionVisual(){
        //Call the base VerticalMenu method to handle the text scaling, colors, and Z-indexing
        base.UpdateSelectionVisual();

        if(selectionsContainer == null || Selections == null || Selections.Count == 0) return;

        //Our Selection variable is 1-indexed (1 to totalSelections), so we convert to a 0-indexed array
        int currentIndex = Selection - 1;

        //--- WINDOW LOGIC ---
        //Check if the current selection has moved outside our visible screen window

        //Moving UP past the top of the window
        if(currentIndex < visibleWindowStart) visibleWindowStart = currentIndex;
        //Moving DOWN past the bottom of the window
        else if(currentIndex >= visibleWindowStart + VisibleItems) visibleWindowStart = currentIndex - VisibleItems + 1;

        //--- SCROLLING LOGIC ---
        //Calculate how far down the list the new "top" item is compared to the very first item.
        float firstItemY = GetItemLocalY(0);
        float targetTopY = GetItemLocalY(visibleWindowStart);
        float offset = targetTopY - firstItemY;
        
        //Move the container up by that offset
        float targetContainerY = baseContainerY - offset;

        //Smoothly tween the container to the new position
        if(scrollTween != null && scrollTween.IsValid()) scrollTween.Kill(); //Prevent tweens from fighting if the user scrolls very fast
        
        scrollTween = CreateTween();
        //Matching the 0.15f duration from the VerticalMenu visual scale tween
        scrollTween.TweenProperty(selectionsContainer,"position:y",targetContainerY,0.15f); 
    }

    //Helper to safely get the Y position whether you're using Node2D or Control nodes
    private float GetItemLocalY(int index){
        if(index < 0 || index >= Selections.Count) return 0;
        if(Selections[index] is Node2D node2D) return node2D.Position.Y;
        else if(Selections[index] is Control control) return control.Position.Y;
        return 0;
    }
}