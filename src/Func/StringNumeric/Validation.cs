using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public bool CheckStringArrayForValue(string strValue, string[] strArray)
        {
            if (strArray.Contains(strValue))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsValidEmail(string strIn)
        {
            // Return true if strIn is in valid e-mail format.
            return Regex.IsMatch(strIn, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        public bool CompareObj(object obj1, object obj2)
        {
            if (obj1 == obj2)
            {
                return true;
            }
            else
            {
                return true;
            }
        }

        public bool IsString(object obj)
        {
            if (obj is string)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsNumeric(object obj)
        {
            if (obj is int)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Only Letters:

        public bool IsAllLetters(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsLetter(c))
                    return false;
            }
            return true;
        }

        // Only Numbers:

        public bool IsAllDigits(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

        // Only Numbers Or Letters:

        public bool IsAllLettersOrDigits(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsLetterOrDigit(c))
                    return false;
            }
            return true;
        }

        // Only Numbers Or Letters Or Underscores:

        public bool IsAllLettersOrDigitsOrUnderscores(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }
            return true;
        }
    }
}