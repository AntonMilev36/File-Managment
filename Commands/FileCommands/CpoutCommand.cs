using FileManagment.FileSystem;
using System;

namespace FileManagment.Commands.FileCommands
{
    public class CpoutCommand : ICommand
    {
        public void Execute(string[] args, Context storage)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("cpout expects 2 arguments: cpout <source_name_in_container> <destination_path_on_disk>");
                return;
            }

            string sourceName = args[0];
            string destinationPath = args[1];

            try
            {
                storage.ReadFile(sourceName, destinationPath);
                Console.WriteLine($"Successfully exported '{sourceName}' to '{destinationPath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cpout: {ex.Message}");
            }
        }
    }
}
