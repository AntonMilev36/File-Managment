using FileManagment.FileSystem;
using System;

namespace FileManagment.Commands.DirectoryCommands
{
    public class CdCommand : ICommand
    {
        public void Execute(string[] args, Context storage)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("cd expects 1 argument: cd <dirname>");
                return;
            }

            string dirName = args[0];

            try
            {
                storage.ChangeDirectory(dirName);
                Console.WriteLine($"Successfully moved to '{dirName}' directory.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}