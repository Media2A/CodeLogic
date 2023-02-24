using System;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        // Password generator

        public string CreateGuid()
        {
            return Guid.NewGuid().ToString();
        }

        public string CreateRandomPassword(int passwordLength, bool allowSpecialChar)
        {
            string allowedChars;
            if (allowSpecialChar)
            {
                allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?_-";
            }
            else
            {
                allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
            }

            char[] chars = new char[passwordLength];
            Random rd = new Random();

            for (int i = 0; i < passwordLength; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }
    }
}