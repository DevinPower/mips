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
        public Node<T> Left { get; private set; }
        public Node<T> Right { get; private set; }

        public Node(T Data)
        {
            this.Data = Data;
        }

        public void AddChild(T Data)
        {

        }

        public void Exec(Action action)
        {
            Left.Exec(action);
            action();
            Right.Exec(action);
        }
    }
}
