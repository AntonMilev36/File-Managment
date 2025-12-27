using System;

namespace FileManagment.Commands
{
    public class MdCommand : ICommand
    {
        public void Execute(string[] args, Builder storage)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("md expects 1 argument: md <dirname>");
                return;
            }

            string dirName = args[0];

            try
            {
                storage.MakeDirectory(dirName);
                Console.WriteLine($"Directory '{dirName}' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}