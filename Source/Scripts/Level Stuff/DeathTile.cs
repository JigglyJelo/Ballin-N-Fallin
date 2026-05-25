using Godot;
using System.Linq;

public partial class DeathTile : Area2D{
    [Export]
	private string team;

    public override void _Ready(){
        if(team == null) team = "";
    }
    
    public void _on_area_2d_body_entered(PhysicsBody2D body){
        if(body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
            if(Online.IsHost() || player.TicksToIgnore != 0){
                Death.KillPlayer(player,Death.DeathCause.Offscreen);
            }
        }else if(body.IsInGroup("Ball")){
            if(Online.IsHost()){
                if(Game.CurrentMode == Mode.GameMode.Soccer){
                    (Mode.ModeNode as TeamSportsMode).Rpc(nameof(TeamSportsMode.PointScored),team);
                }else body.CallDeferred("queue_free");
            }
        }else if(body.IsInGroup("Item Box")){
            if(Online.IsHost()){
                ItemBox itemBox = body.GetParent() as ItemBox;
                itemBox.Creator.Rpc(nameof(itemBox.Creator.RemoveItemBox));
            }
		}else if(body.IsInGroup("Crown")){
            (Mode.ModeNode as CTK).SpawnCrown();
            body.CallDeferred("queue_free");
        }else if(body.IsInGroup("Item")){
            if(Online.IsHost()){
                byte key = ItemSynchronizer.SyncNode.SpawnedItems.FirstOrDefault(x => x.Value == body.GetParent()).Key;
                ItemSynchronizer.SyncNode.Rpc(nameof(ItemSynchronizer.SyncNode.RemoveItem),key);//byte.Parse(body.GetParent().Name.ToString().Substring(11))
            }
        }else if(body.IsInGroup("Trash")){
            if(Game.CurrentMode == Mode.GameMode.Survival && Game.CurrentLevelName.Contains("Trash Compactor - ") && Online.IsHost()){
                TrashCompactor trashCompactor = Mode.ModeNode.GetNode<TrashCompactor>("Level/TrashSpawner");
                byte key = trashCompactor.SpawnedTrash.FirstOrDefault(x => x.Value.Rb == body).Key;
                trashCompactor.Rpc(nameof(trashCompactor.RemoveTrash),key);
            }
        }else if(!(body is StaticBody2D)){
            if(Game.CurrentMode == Mode.GameMode.Survival && Game.CurrentLevelName.Contains("Trash Compactor - ")){
                body.CallDeferred("queue_free");
            }else body.CallDeferred("queue_free");
        }
    }
}