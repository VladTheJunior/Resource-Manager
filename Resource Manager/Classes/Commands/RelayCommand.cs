using System;
using System.Windows.Input;

namespace Resource_Manager.Classes.Commands
{
    public class RelayCommand<T> : ICommand
    {
        private Action<string> openFile;


        public RelayCommand(Action<string> openFile)
        {
            this.openFile = openFile;
        }

        public void Execute(object parameter)
        {
            openFile(parameter.ToString());
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
