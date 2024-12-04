using Lexer;

namespace UnitTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TreeTest()
        {
            Node<char> CharTreeRoot = new Node<char>('R');

            CharTreeRoot.AddChild('L');
            CharTreeRoot.AddChild('R');

            CharTreeRoot.AddChild('1');
            CharTreeRoot.AddChild('2');
            CharTreeRoot.AddChild('3');
            CharTreeRoot.AddChild('4');

            Assert.Pass();
        }
    }
}