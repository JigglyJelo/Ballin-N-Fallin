using Godot;
using System;

public abstract partial class GridMenu : Menu2D{
    protected int selectionX = 1;
    protected int selectionY = 1;
    protected int totalSelectionsX;
    protected int totalSelectionsY;
    protected override void MenuUp(){
        if(selectionY > 1) selectionY--;
		else selectionY = totalSelectionsY;
		UpdateSelectionVisual();
		joystickTimer = 0;
        Set2DSelection();
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
    }
    protected override void MenuDown(){
        if(selectionY < totalSelectionsY) selectionY++;
		else selectionY = 1;
		UpdateSelectionVisual();
		joystickTimer = 0;
        Set2DSelection();
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
    }
    protected override void MenuLeft(){
        if(selectionX > 1) selectionX--;
		else selectionX = totalSelectionsX;
		UpdateSelectionVisual();
		joystickTimer = 0;
        Set2DSelection();
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
    }
    protected override void MenuRight(){
        if(selectionX < totalSelectionsX) selectionX++;
		else selectionX = 1;
		UpdateSelectionVisual();
		joystickTimer = 0;
        Set2DSelection();
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
    }
    //Sets the Menu2D's selection based off selectionX and selectionY
    protected void Set2DSelection(){
        Selection = selectionX + (selectionY * (totalSelectionsX-1));
    }

    protected void MenuChoose(int selectionX,int selectionY){
        MenuChoose(selectionX + (selectionY * (totalSelectionsX-1)));
        SFX.Play("Confirm");
    }
}