using Godot;
using System;

public abstract class TransformItem : Item{
    public bool Activated = false;
    protected float transformTime;
    protected float timer = 0;

    public TransformItem(Player player,ItemEnum itemType,float transformTime): base(player,itemType){
        if(Player.Item != null && Player.Item is TransformItem) Player.Visuals.TransformBar.Value = 100;
        this.transformTime = transformTime;
    }

    public void TransformItemTimer(float delta){
        if(Activated){
            timer += delta;
            Player.Visuals.TransformBar.Value = (1 - (timer/transformTime)) * 100;
            if(timer >= transformTime){
                Player.Visuals.TransformBar.Value = 0;
                Player.ResetTransformation();
                Player.Item = null;
            }
        }
    }
    public abstract void SetTransformation();
}