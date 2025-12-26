using System;

namespace FileManagment.Utils
{
    public static class PathHelper
    {
        public static string GetFileName(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return string.Empty;

            int lastSeparatorIndex = -1;

            for (int i = 0; i < fullPath.Length; i++)
            {
                if (fullPath[i] == '\\' || fullPath[i] == '/')
                {
                    lastSeparatorIndex = i;
                }
            }

            if (lastSeparatorIndex == -1) return fullPath;

            int fileNameLength = fullPath.Length - (lastSeparatorIndex + 1);
            char[] fileNameChars = new char[fileNameLength];

            for (int i = 0; i < fileNameLength; i++)
            {
                fileNameChars[i] = fullPath[lastSeparatorIndex + 1 + i];
            }

            return new string(fileNameChars);
        }
    }
}
