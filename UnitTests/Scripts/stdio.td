function void print(string Value)
{
    |Ori $v0, $zero, 4
    |Syscall
}

function void readString(string Value, int Length)
{
    |Ori $v0, $zero, 8
    |Syscall
}

function void printNum(int Value){
     |Ori $v0, $zero, 1
     |Syscall
}

function void printFloat(float Value){
     |Ori $v0, $zero, 2
     |Syscall
}

function void printIntFloat(int Value){
     |Ori $v0, $zero, 2
     |Syscall
}

function void printChar(int Value){
     |Ori $v0, $zero, 11
     |Syscall
}

function int getTime(){
     |Ori $v0, $zero, 18
     |Syscall
}

function int str_len(string Value)
{
     int length = 0;
     while (Value[length] > 0)
     {
          length += 1;
     }
     return length;
}

function void printf(string InputString, int[] Args)
{
     int i = 0;
     int currentArg = 0;
     int string_length = str_len(InputString);

     while (i < string_length)
     {
          if (InputString[i] == 37)
          {
               if (InputString[i + 1] == 100)
               {
                    printNum(Args[currentArg]);
                    currentArg += 1;
                    i += 2;
               }
               if (InputString[i + 1] == 102)
               {
                    printIntFloat(Args[currentArg]);
                    currentArg += 1;
                    i += 2;
               }
               elseif (InputString[i + 1] == 115)
               {
                    print(Args[currentArg]);
                    currentArg += 1;
                    i += 2;
               }
               elseif (InputString[i + 1] == 99)
               {
                    printChar(Args[currentArg]);
                    currentArg += 1;
                    i += 2;
               }
               else
               {
                    printChar(InputString[i]);
                    i += 1;
               }
          }
          else
          {
               printChar(InputString[i]);
               i += 1;
          }
     }
     return 0;
}