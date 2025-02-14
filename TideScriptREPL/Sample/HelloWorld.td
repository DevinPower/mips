#include stdio.td

string str = ":)";
int[2] args;

args[0] = 777;
args[1] = str;

printf("Hello, world! Your number is %d and your string is %s", args);