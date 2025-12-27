using System;
using System.IO;
using FileManagment.Commands;

namespace FileManagment
{
    public class Program
    {
        private const string ContainerFileName = "fs_container.bin";

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            string command = args[0].ToLower();
            string[] commandArgs = new string[args.Length - 1];

            Array.Copy(args, 1, commandArgs, 0, args.Length - 1);

            try
            {
                // Intialize the container
                Builder storage = new Builder(ContainerFileName);

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

            // Keep the console open, to see the result
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
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

                // Later implementation

                // "cd" => new CdCommand(),
                // "rd" => new RdCommand(),
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