using System.IO;
using Newtonsoft.Json;


var repoText = File.ReadAllText(args[0]);
var changelogText = File.ReadAllText(args[1]);

dynamic repo = JsonConvert.DeserializeObject(repoText);
repo[0].Changelog = changelogText;

var serializer = new Newtonsoft.Json.JsonSerializer();
serializer.Formatting = Formatting.Indented;
using (var sw = new StreamWriter(args[0]))
{
    using (var writer = new JsonTextWriter(sw))
    {
        writer.Indentation = 4;
        serializer.Serialize(writer, repo);
    }
}
