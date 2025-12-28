using FileManagment.FileSystem;
using System;

namespace FileManagment.Commands.FileCommands
{
    public class CpinCommand : ICommand
    {
        public void Execute(string[] args, Context storage)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("cpin expects 2 arguments: cpin <source_path> <target_name>");
                return;
            }

            string sourcePath = args[0];
            string targetName = args[1];

            try
            {
                storage.WriteFile(sourcePath, targetName);
                Console.WriteLine($"File '{sourcePath}' successfully copied as '{targetName}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cpin: {ex.Message}");
            }
        }
    }
}