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
        public Node<T> Parent { get; set; }

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

            newNode.Parent = this;

            Children.Add(newNode);
            return newNode;
        }

        public void AddChild(Node<T> Node)
        {
            if (Node.Parent != null)
                Node.Parent.Children.Remove(Node);

            Node.Parent = this;
            Children.Add((Node<T>)Node);
        }

        //courtesy of https://stackoverflow.com/questions/1649027/how-do-i-print-out-a-tree-structure
        public void PrintPretty(string indent, bool last)
        {
            ConsoleColor startColor = Console.ForegroundColor;

            if (Data is ASTExpression expr)
            {
                if (IsSkippedInCodeGen())
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                {
                    if (expr.SkipChildGeneration)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    if (Parent == null || expr.TreeRepresentation == null) Console.ForegroundColor = ConsoleColor.Gray;
                }           
            }

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

            Console.ForegroundColor = startColor;
        }

        //TODO: Maybe just take type T instead of Node<T>?
        public void PostOrderTraversal(Action<Node<T>> Action, bool ForceGeneration = false)
        {
            foreach (var child in Children)
            {
                child.PostOrderTraversal(Action, ForceGeneration);
            }

            if (ForceGeneration || !IsSkippedInCodeGen())
            {
                Action(this);
                return;
            }

            Console.WriteLine($"Skipped a gen for {Data.ToString()}");
        }

        bool IsSkippedInCodeGen()
        {
            if (Data is ASTExpression currentNodeData)
            {
                if (currentNodeData.SkipGeneration) return true;
            }
            
            if (Parent is Node<ASTExpression> initialParent)
            {
                int levels = 0;
                Node<ASTExpression> parentExpression = initialParent;
                while (parentExpression != null)
                {
                    if (parentExpression.Data != null)
                    {
                        if (parentExpression.Data.SkipChildGeneration)
                        {
                            return true;
                        }
                    }
                    levels++;
                    parentExpression = parentExpression.Parent;
                }
            }

            return false;
        }
    }
}
