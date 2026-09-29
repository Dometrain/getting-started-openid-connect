using Newtonsoft.Json;

namespace OidcDemoApp.Extensions;

public static class StringExtensions
{
    public static string BeautifyJson(this string str)
    {
        var obj = JsonConvert.DeserializeObject(str);
        string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
        return json;
    }
}
