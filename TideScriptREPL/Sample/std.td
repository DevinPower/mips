//----------------------//                                                       
//  StandardStrings.td  //               //-------------------//               
//  Author:Devin Power  //               //Feel free to modify//               
//  LastUpdate:1-13-25  //               //-------------------//               
//----------------------//                                                       

string newLine = "
";

function print(string Value){
     |Ori $v0, $zero, 4
     |Syscall
}

function str_len(string Value){
     |Ori $t0, $zero, 1
     |Ori $v0, $zero, 0

     |start_str_len: 
     |LB $t1, 0($a0)
     |Beq $zero, $t1, exit_str_len
     |Addi $v0, $v0, 1
     |Addi $a0, $a0, 1
     |J start_str_len
     |exit_str_len:
     |Move $t8, $v0
}

function read_str(string StrPointer, int Length){
     |Ori $v0, $zero, 8
     |Syscall
}

string readString = "XXXXXXXXXX";

read_str(readString, 10);
print(newLine);
print("--------------------");
print(newLine);
print(readString);