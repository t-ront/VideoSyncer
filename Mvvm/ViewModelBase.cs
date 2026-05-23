using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoSyncer.Mvvm
{
    /// <summary>
    /// ViewModel基底クラス
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// プロパティ変更通知イベント
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// プロパティを設定する
        /// </summary>
        /// <typeparam name="T">プロパティ型</typeparam>
        /// <param name="storage">プロパティ格納場所</param>
        /// <param name="value">設定値</param>
        /// <param name="propertyName">プロパティ名</param>
        /// <returns>プロパティが変更された場合はtrue、それ以外はfalse</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// プロパティ変更通知時動作
        /// </summary>
        /// <param name="propertyName">プロパティ名</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}