
namespace HelloLib
{
    public class Hello
    {
        public string SayHello(string lang) {
            var dict = new Dictionary<string, string> {
                { "Thai", "สวัสดี" },
                { "German", "Hallo" },
                { "English", "Hello" },
            };
            var ok = dict.TryGetValue(lang, out var value);
            return ok ? value : "...";
        }
    }
}