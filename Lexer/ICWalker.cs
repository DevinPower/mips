using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    //TODO: Delete
    public class ICWalker
    {
        public static string[] GenerateCodeRecursive(Node<ASTExpression> Expressions, CompilationMeta Meta, bool ForceGeneration = false)
        {
            List<string> intermediaryCode = new List<string>();
            Expressions.PostOrderTraversal((x) =>
            {
                //if (x.Data.SkipGeneration) return;

                IntermediaryCodeMeta generatedMeta = x.Data.GenerateCode(Meta);
                foreach (var line in generatedMeta.Code)
                {
                    intermediaryCode.Add(line);
                }
            }, ForceGeneration);

            return intermediaryCode.ToArray();
        }

        public static string GetFirstRegister(string Code)
        {
            return Code.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)[1];
        }
    }
}
