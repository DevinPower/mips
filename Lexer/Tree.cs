using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lexer
{
    public class Node<T>
    {
        public T Data { get; private set; }
        public List<Node<T>> Children { get; private set; }

        public Node(T Data)
        {
            Children = new List<Node<T>>();
            this.Data = Data;
        }

        public Node<T> AddChild(T Data)
        {
            Node<T> newNode = new Node<T>(Data);

            if (Data is ASTExpression expression)
            {
                expression.SetTreeRepresentation(newNode as Node<ASTExpression>);
            }

            Children.Add(newNode);
            return newNode;
        }

        public void AddChild(Node<T> Node)
        {
            Children.Add((Node<T>)Node);
        }

        //courtesy of https://stackoverflow.com/questions/1649027/how-do-i-print-out-a-tree-structure
        public void PrintPretty(string indent, bool last)
        {
            Console.Write(indent);
            if (last)
            {
                Console.Write("\\-");
                indent += "  ";
            }
            else
            {
                Console.Write("|-");
                indent += "| ";
            }
            Console.WriteLine(Data.ToString());

            for (int i = 0; i < Children.Count; i++)
                Children[i].PrintPretty(indent, i == Children.Count - 1);
        }
    }
}
