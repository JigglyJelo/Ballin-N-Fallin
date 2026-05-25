public partial class Miscellaneous : Mode{
	public override void _Ready(){
		base._Ready();
		Game.CurrentMode = GameMode.Miscellaneous;
	}

	public override Item GiveItem(Player player){
                const float MAX_COMEBACK_LUCK = 60;
                Item item;
                float luck = Game.Random.Next(1,101);
                luck += GetComebackLuck(player) * MAX_COMEBACK_LUCK;
                if(luck > 90) item = new Wings(player);
                else if(luck > 80) item = new BowlingBall(player);
                else if(luck > 70) item = new Moon(player);
                else if(luck > 60) item = new BigFungus(player);
                else if(luck > 50) item = new SmallBall(player);
                else if(luck > 40) item = new Booll(player);
                else if(luck > 30) item = new Inverter(player);
                else if (luck > 27) item = new StopSign(player,3);
                else if (luck > 24) item = new StopSign(player,2);
                else if(luck > 20) item = new StopSign(player,1);
                else if (luck > 17) item = new Pepper(player,3);
                else if (luck > 14) item = new Pepper(player,2);
                else if(luck > 10) item = new Pepper(player,1);
                
                else if (luck > 7) item = new Ball(player,3);
                else if (luck > 4) item = new Ball(player,2);
                else item = new Ball(player,1);
                return item;
	}

	protected override void SetPoints(){}
}