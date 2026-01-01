using System;
using System.IO;
using FileManagment.Commands;
using FileManagment.Commands.BaseCommands;
using FileManagment.Commands.DirectoryCommands;
using FileManagment.Commands.FileCommands;
using FileManagment.FileSystem;
using FileManagment.Utils;

namespace FileManagment
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Context storage = new Context();

            if (args.Length > 0)
            {
                ExecuteCommand(args, storage);
            }
            else
            {
                RunInteractiveLoop(storage);
            }
        }

        // Handle interactive sessions
        private static void RunInteractiveLoop(Context storage)
        {
            Console.WriteLine("Virtual File System Shell. Type 'exit' to quit.");

            while (true)
            {
                Console.Write($"ID:{storage.CurrentFolderID} > ");
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
                    break;

                string[] commandArgs = CommandsParser.ManualParse(input);
                if (commandArgs.Length > 0)
                {
                    ExecuteCommand(commandArgs, storage);
                }
            }
        }

        // For Debug
        private static void ExecuteCommand(string[] args, Context storage)
        {
            string command = args[0].ToLower();
            string[] commandArgs = new string[args.Length - 1];

            Array.Copy(args, 1, commandArgs, 0, args.Length - 1);

            try
            {
                ICommand? commandInstance = GetCommandInstance(command);

                if (commandInstance != null)
                {
                    Console.WriteLine($"Executing command: {command}...");
                    commandInstance.Execute(commandArgs, storage);
                    Console.WriteLine("Command executed successfully.");
                }
                else
                {
                    Console.WriteLine($"Error: Unknown command '{command}'.");
                    ShowUsage();
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Error: File not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

        private static ICommand? GetCommandInstance(string command)
        {
            return command switch
            {
                "ls" => new LsCommand(),
                "cpin" => new CpinCommand(),
                "cpout" => new CpoutCommand(),
                "rm" => new RmCommand(),
                "md" => new MdCommand(),
                "cd" => new CdCommand(),
                "rd" => new RdCommand(),
                _ => null
            };
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage: fs <command> [arguments...]");
            Console.WriteLine("Available commands:");
            Console.WriteLine("  ls                                - List current directory contents.");
            Console.WriteLine("  cpin <source_path> <target_name>  - Copy file into container.");
            Console.WriteLine("  rm <file_name>                    - Remove file from container.");
            Console.WriteLine("  cpout <source_name> <target_path> - Copy file out of container.");
            Console.WriteLine("  md <dir_name>                     - Make directory.");
            Console.WriteLine("  cd <dir_name | .. | >            - Change directory.");
            Console.WriteLine("  rd <dir_name>                     - Remove directory.");
        }
    }
}