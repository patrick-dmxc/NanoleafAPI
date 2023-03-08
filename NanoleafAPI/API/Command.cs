using System.Text.Encodings.Web;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NanoleafAPI
{
    public readonly struct Command
    {
        public readonly object AnonymousType;
        public Command(in object anonymousType)
        {
            this.AnonymousType = anonymousType;
        }
        public override string ToString()
        {
            string commandString = JsonSerializer.Serialize(this.AnonymousType);
            //commandString= commandString.Replace(@"\", "");
            return commandString;
        }
    }
}