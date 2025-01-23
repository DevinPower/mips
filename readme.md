# Overview
### This project is 

- A virtual CPU based on MIPS-1 architecture
- A high level programming language 
- A compiler from the programming language to assembly to machine code

The purpose here was to dive into how CPUs and compilers work. I started from the CPU and built upwards on the stack. This was attempted using the least amount of guidance possible, but [Crafting Interpreters](https://craftinginterpreters.com/) was a huge source of information when stuck.

Still a WIP, some features to add, some bugs to work out. Mainly the need to add in floats.

### Programming Language Features

#### Types & Arrays
```
int x = 1;
string msg = "hello, world!";
int[3] numbers;
```

#### Functions
```
function foo(string bar){
    //Do stuff
}
```

#### Inline Assembly
Use a single pipe to denote inline machine instructions. Note that arguments are stored sequentially from $a0-$a4 and Syscall 4 (print string) will print the string starting at address memory $a0.
```
function print(string Value){
    |Ori $v0, $zero, 4
    |Syscall
}
```

#### Operators
Not equals, modulo, less/greater than or equals, increment and decrement operator to be implemented
```
"=", "==", "<", ">",
"+", "-", "*", "/", "+=", "-=", "*=", "/=",
"||", "&&"
```

#### Control Statements
```
if (1 > 2){
    //Do stuff
}else{
    //Other stuff
}

while (i < 10){
    //Do stuff
}
```

#### Other Stuff
Return statements for functions, comments.