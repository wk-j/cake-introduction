
namespace HelloLib.Tests
{
    public class HelloTests
    {
        [Fact]
        public void ShouldEqualTo1() {
            Assert.Equal(1, 1);
        }

        [Fact]
        public void ShouldSayEnglish() {
            var hello = new Hello().SayHello("English");
            Assert.Equal("Hello", hello);
        }

        [Fact]
        public void ShouldSayGerman() {
            var hello = new Hello().SayHello("German");
            Assert.Equal("Hallo", hello);
        }
    }
}