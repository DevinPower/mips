#include stdio.td

class Player{
    string Name;
    int Score;

    function void Print(){
        print(Name);
        printNum(Score);
    }
}

function Player PlayerMaker(){
    Player player = new Player();
    player.Name = "Devin";
    player.Score = 150;
    player.Print();
}

PlayerMaker();