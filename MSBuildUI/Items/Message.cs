using Microsoft.Build.Framework;

namespace MSBuildUI.Items
{
    public class Message
    {
        public BuildEventArgs BuildEventArgs { get; }
        public string FirstLine { get; }

        public Message(BuildEventArgs buildEventArgs)
        {
            BuildEventArgs = buildEventArgs;
            string message = BuildEventArgs.Message?.Trim() ?? "{null}";
            int index = message.IndexOfAny("\r\n".ToCharArray());
            FirstLine = index == -1 ? message : message.Substring(0, index);
        }
    }
}
