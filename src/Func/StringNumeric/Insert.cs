namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public string InsertIntoTagFirst(string strFirst, string strLast, string strText, string strInsert)
        {
            int start = strText.IndexOf(strFirst) + strFirst.Length;
            string result = strText.Insert(start, strInsert);
            return result;
        }

        public string InsertIntoTagLast(string strFirst, string strLast, string strText, string strInsert)
        {
            string s = strInsert;
            int start = s.IndexOf(strText) + strFirst.Length;
            int end = s.IndexOf(strText);
            string result = s.Insert(start, strInsert);
            return result;
        }
    }
}