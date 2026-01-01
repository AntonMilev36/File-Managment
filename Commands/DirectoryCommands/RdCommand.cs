using FileManagment.FileSystem;
using System;

namespace FileManagment.Commands.DirectoryCommands
{
    public class RdCommand : ICommand
    {
        public void Execute(string[] args, Context storage)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("rd expects 1 argument: rd <dirname>");
                return;
            }

            string dirName = args[0];

            try
            {
                storage.RemoveDirectory(dirName);
                Console.WriteLine($"Directory '{dirName}' deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}