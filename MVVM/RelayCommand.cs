using System.Windows.Input;

namespace rex.MVVM
{
    internal class RelayCommand : ICommand
    {
        private readonly Func<object?, Task>? executeAsync;
        private readonly Action<object?>? execute;
        private readonly Func<object?, bool>? canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            this.canExecute = canExecute;
        }

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute is null || canExecute(parameter);
        }

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    if (executeAsync != null)
                        await executeAsync(parameter);
                    else if (execute != null)
                        execute?.Invoke(parameter);
                    else
                        throw new Exception("No executor defined for command.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to execute command: {ex.Message}");
                }
            }
        }
    }
}
