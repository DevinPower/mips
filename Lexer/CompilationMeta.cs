namespace Lexer
{
    public class VariableMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public VariableMeta(string Name, string Type)
        {
            this.Name = Name;
            this.Type = Type;
        }
    }

    public class CompilationMeta
    {
        List<VariableMeta> Variables = new List<VariableMeta>();

        public void AddVariable(string Variable, string Type)
        {
            Variables.Add(new VariableMeta(Variable, Type));
        }
    }
}