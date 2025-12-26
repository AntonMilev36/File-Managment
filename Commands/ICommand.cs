using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagment;

namespace FileManagment.Commands
{
    public interface ICommand
    {
        void Execute(string[] args, Builder storage);
    }
}