
namespace HelloApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var hello = new Hello();
            
            Console.WriteLine(hello.SayHello("German"));
            Console.WriteLine(hello.SayHello("English"));
            Console.WriteLine(hello.SayHello("Thai"));
        }
    }
}
