using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using VideoSyncer.ViewModels;

namespace VideoSyncer
{
    /// <summary>
    /// MainWindow処理クラス
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// ドラッグ開始位置
        /// </summary>
        private Point _dragStartPoint;

        /// <summary>
        /// ViewModelプロパティ
        /// </summary>
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 同期対象動画ドラッグ開始位置記録処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TargetVideoListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        /// <summary>
        /// 同期対象動画ドラッグ開始時処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TargetVideoListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                var listBoxItem = FindAncestor<System.Windows.Controls.ListBoxItem>((DependencyObject)e.OriginalSource);
                if (listBoxItem != null)
                {
                    _ = DragDrop.DoDragDrop(listBoxItem, (string)listBoxItem.DataContext, DragDropEffects.Move);
                }
            }
        }

        /// <summary>
        /// 同期対象動画ドラッグアンドドロップ時処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TargetVideoListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    ViewModel.AddTargetVideo(file);
                }
            }
            else if (e.Data.GetDataPresent(typeof(string)))
            {
                var droppedData = (string)e.Data.GetData(typeof(string));
                var targetItem = FindAncestor<System.Windows.Controls.ListBoxItem>((DependencyObject)e.OriginalSource);
                var targetIndex = targetItem == null
                    ? ViewModel.TargetVideos.Count - 1
                    : TargetVideoListBox.ItemContainerGenerator.IndexFromContainer(targetItem);
                if (targetIndex < 0)
                {
                    targetIndex = ViewModel.TargetVideos.Count - 1;
                }

                var removeIndex = ViewModel.TargetVideos.IndexOf(droppedData);
                if (removeIndex >= 0 && targetIndex >= 0 && removeIndex != targetIndex)
                {
                    ViewModel.TargetVideos.RemoveAt(removeIndex);
                    ViewModel.TargetVideos.Insert(targetIndex, droppedData);
                    ViewModel.SelectedTargetIndex = targetIndex;
                }
            }
        }

        /// <summary>
        /// 親要素検索処理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="current"></param>
        /// <returns></returns>
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            } while (current != null);
            return null;
        }
    }
}