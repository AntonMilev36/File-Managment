using System;

namespace FileManagment.Commands
{
    public class LsCommand : ICommand
    {
        public void Execute(string[] args, Builder storage)
        {
            Console.WriteLine("Contents of container:");
            Console.WriteLine("{0,-32} {1,10} {2,10}", "Name", "Size", "Type");
            Console.WriteLine(new string('-', 55));

            bool found = false;
            foreach (var record in storage.ListCurrentDirectory())
            {
                Console.WriteLine("{0,-32} {1,10}B {2,10}", record.Name, record.Size, record.Type);
                found = true;
            }

            if (!found) Console.WriteLine("(Directory is empty)");
        }
    }
}