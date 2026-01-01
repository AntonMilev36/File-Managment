using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagment.Utils
{
    public static class CommandsParser
    {
        public static string[] ManualParse(string input)
        {
            int wordCount = 0;
            bool inWord = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != ' ' && !inWord)
                {
                    wordCount++;
                    inWord = true;
                }
                else if (input[i] == ' ')
                {
                    inWord = false;
                }
            }

            if (wordCount == 0) return new string[0];

            string[] results = new string[wordCount];
            int currentResultIndex = 0;
            string currentWord = "";
            inWord = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != ' ')
                {
                    currentWord += input[i];
                    inWord = true;
                }
                else if (inWord)
                {
                    results[currentResultIndex++] = currentWord;
                    currentWord = "";
                    inWord = false;
                }
            }

            if (inWord)
            {
                results[currentResultIndex] = currentWord;
            }

            return results;
        }
    }
}
