using System.Text;
using System.Text.RegularExpressions;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public static string TruncateString(string source, int length)
        {
            if (source.Length > length)
            {
                source = source.Substring(0, length);
            }
            return source;
        }

        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c >= '0' && c <= '9' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string CleanupTags(string strTagStart, string strTagEnd, string strText)
        {
            return Regex.Replace(strText, strTagStart + ".*?" + strTagEnd, string.Empty);
        }
        public static string SimpleReplace(string strText, string strTag, string strReplace)
        {
            return strText.Replace(strTag, strReplace);
        }
        public static string SimpleReplaceTag(string strText, string strTagName, string strReplace)
        {
            return strText.Replace($"[${strTagName}$]", strReplace);
        }
        public static string UpperCaseFirstCharacter(string text)
        {
            return Regex.Replace(text, "^[a-z]", m => m.Value.ToUpper());
        }
        public static string BinaryToText(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
    }
}