#include stdio.td

class Player{
    string Name;
    int Score;

    function void Print(){
        print(Name);
        printNum(Score);
    }

    function void Increase(int amount){
          Score += amount;
    }
}

function Player PlayerMaker(){
    Player player = new Player();
    player.Name = "Devin";
    player.Score = 140;
    player.Increase(10);
    player.Print();
    return player;
}

PlayerMaker();