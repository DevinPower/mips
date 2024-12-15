using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    //TODO: Delete
    public class ICWalker
    {
        public static void Walk(Node<ASTExpression> Root, CompilationMeta CompilationMeta)
        {
            
        }

        static void HandleNode(Node<ASTExpression> Node, CompilationMeta Meta)
        {
            foreach (var child in Node.Children)
            {
                //child.PostOrderTraversal(Meta);
            }

            //do stuff
        }
    }
}
