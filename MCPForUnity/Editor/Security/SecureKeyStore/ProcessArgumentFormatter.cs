using System.Text;

namespace com.tgs.mcpforunity.editor.Security
{
    internal static class ProcessArgumentFormatter
    {
        internal static string Join(params string[] arguments)
        {
            var result = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(Quote(argument));
            }

            return result.ToString();
        }

        private static string Quote(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            var result = new StringBuilder();
            result.Append('"');
            int backslashCount = 0;
            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '"')
                {
                    result.Append('\\', backslashCount * 2 + 1);
                    result.Append('"');
                    backslashCount = 0;
                    continue;
                }

                result.Append('\\', backslashCount);
                result.Append(character);
                backslashCount = 0;
            }

            result.Append('\\', backslashCount * 2);
            result.Append('"');
            return result.ToString();
        }
    }
}
