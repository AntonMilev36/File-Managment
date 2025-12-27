using System;

namespace FileManagment.Commands
{
    public class RmCommand : ICommand
    {
        public void Execute(string[] args, Builder storage)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("rm expects 1 argument: rm <filename>");
                return;
            }

            string fileName = args[0];

            try
            {
                storage.RemoveFile(fileName);
                Console.WriteLine($"Successfully removed '{fileName}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}