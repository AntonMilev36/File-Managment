using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FileManagment.FileSystem;

namespace FileManagment.Commands
{
    public interface ICommand
    {
        void Execute(string[] args, Context storage);
    }
}