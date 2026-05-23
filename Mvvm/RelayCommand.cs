using System.Windows.Input;

namespace VideoSyncer.Mvvm
{
    /// <summary>
    /// ICommandインターフェース簡易実装クラス
    /// </summary>
    /// <param name="execute"></param>
    /// <param name="canExecute"></param>
    public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        /// <summary>
        /// 実行コマンド保持フィールド
        /// </summary>
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        /// <summary>
        /// コマンド実行可否変化時イベントハンドラー保持フィールド
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add     => CommandManager.RequerySuggested += value;
            remove  => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// 実行可能かどうかを判断する
        /// </summary>
        /// <param name="parameter">コマンドに渡すオプションパラメーター</param>
        /// <returns>実行可能可否</returns>
        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        /// <summary>
        /// 指定されたパラメーターでコマンドを実行する
        /// </summary>
        /// <param name="parameter">コマンドに渡すオプションパラメーター</param>
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}