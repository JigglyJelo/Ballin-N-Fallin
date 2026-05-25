using Godot;
using System;
using System.Collections.Generic;

public abstract partial class TeamMode : Mode, IModeStartEvent{
    public Color TeamAColor, TeamBColor;
    public static string WinningTeam = "";
	public static string[] Teams = new string[Game.TotalPlayers];
    public static int TeamAPlayerCount, TeamBPlayerCount;

    public override void _Ready(){
        base._Ready();
        //Reset static variables
        WinningTeam = "";
        AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        isScoreMode = false;
    }

    public virtual void OnModeStart(){
        if(Game.TotalPlayers != 2){
			TeamAColor = Game.TeamColors[0];
			TeamBColor = Game.TeamColors[1];
		}else{
			if(Game.Players[0].Team.Equals("A")){
				TeamAColor = Game.Players[0].PlayerColor;
				TeamBColor = Game.Players[1].PlayerColor;
			}else if(Game.Players[0].Team.Equals("B")){
				TeamAColor = Game.Players[1].PlayerColor;
				TeamBColor = Game.Players[0].PlayerColor;
			}
		}
        TeamAPlayerCount = 0;
        TeamBPlayerCount = 0;
        foreach(Player player in Game.Players){
			switch(player.Team){
				case "A": TeamAPlayerCount++; break;
				case "B": TeamBPlayerCount++; break;
			}
		}
    }

    //Give points to players on winning team
    protected override void SetPoints(){
        foreach(Player player in Game.Players){
            if(player.Team.Equals(WinningTeam)) Positions[player.Id - 1] = 1;
            else Positions[player.Id - 1] = (byte)Game.TotalPlayers;
        }
    }

    //Randomly sets Teams to be even if player count is even else team A will have one extra player
    public static void SetTeams(){
        Teams = new string[Game.TotalPlayers];
		if(Game.TotalPlayers != 2){
            int aTeamCount = 0;
            int bTeamCount = 0;
            int maxPlayersPerTeam = Game.TotalPlayers / 2;
            if(Tour.IsTour){
                string[] placementTeams = new string[Game.TotalPlayers];
                int[] sortedScores = new int[Game.TotalPlayers];
                for(int i = 0; i < Game.TotalPlayers; i++){
                    sortedScores[i] = Tour.PlayerScores[i];
                }
                
                
                Array.Sort(sortedScores, (a, b) => b.CompareTo(a)); // Sort directly in descending order
                // Assign team placements based on number of players
                switch(Game.TotalPlayers){
                    case 1:
                        placementTeams = new string[] { "A" };
                        break;
                    case 2:
                        placementTeams = new string[] { "A", "B" };
                        break;
                    case 3:
                        placementTeams = new string[] { "A", "B", "B" };
                        break;
                    case 4:
                        placementTeams = new string[] { "A", "B", "B", "A" };
                        break;
                    case 5:
                        placementTeams = new string[] { "A", "B", Game.Random.Next(0, 2) == 0 ? "A" : "B", "B", "A" };
                        break;
                    case 6:
                        placementTeams = new string[] { "A", "B", "B", "A", "B", "A" };
                        break;
                    case 7:
                        placementTeams = new string[] { "A", "B", Game.Random.Next(0, 2) == 0 ? "A" : "B", "B", "A", "B", "A" };
                        break;
                    case 8:
                        placementTeams = new string[] { "A", "B", "A", "B", "B", "A", "B", "A" };
                        break;
                }

                // Ensure Teams array is properly initialized
                if(Teams == null || Teams.Length != Game.TotalPlayers){
                    Teams = new string[Game.TotalPlayers];
                }

                // Use a dictionary to ensure correct mapping of scores to placements
                Dictionary<int, Queue<string>> scoreToTeamMapping = new Dictionary<int, Queue<string>>();
                for(int i = 0; i < Game.TotalPlayers; i++){
                    if(!scoreToTeamMapping.ContainsKey(sortedScores[i])){
                        scoreToTeamMapping[sortedScores[i]] = new Queue<string>();
                    }
                    scoreToTeamMapping[sortedScores[i]].Enqueue(placementTeams[i]);
                }

                // Assign teams while preserving order
                for(int i = 0; i < Game.TotalPlayers; i++){
                    Teams[i] = scoreToTeamMapping[Tour.PlayerScores[i]].Dequeue();
                }

            }else{
                for(int i = 0; i < Game.TotalPlayers; i++){
                    if (aTeamCount < maxPlayersPerTeam && Game.Random.Next(0,2) == 0){
                        Teams[i] = "A";
                        aTeamCount++;
                    }else if (bTeamCount < maxPlayersPerTeam){
                        Teams[i] = "B";
                        bTeamCount++;
                    }else{
                        Teams[i] = "A";
                        aTeamCount++;
                    }
                }
            }
        }else{
            if(Game.Random.Next(0,2) == 0){
                Teams[0] = "A";
                Teams[1] = "B";
            }else{
                Teams[0] = "B";
                Teams[1] = "A";
            }
        }
        if(Online.IsHost()) TeamSportsMode.ModeNode.Rpc(nameof(TeamSportsMode.SetTeams),Teams);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SetTeams(string[] teams){
        Teams = teams;
    }

    public static bool IsTeamMode(){
        return ModeNode is TeamMode;
    }
}