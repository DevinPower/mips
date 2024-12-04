﻿using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
