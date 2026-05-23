using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using VideoSyncer.Mvvm;
using VideoSyncer.Services;

namespace VideoSyncer.ViewModels
{
    /// <summary>
    /// MainWindow VMクラス
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region 画面プロパティ
        /// <summary>
        /// 同期元動画パス
        /// </summary>
        public string BaseVideo { get; set => SetProperty(ref field, value); } = string.Empty;

        /// <summary>
        /// 同期対象動画パスリスト
        /// </summary>
        public ObservableCollection<string> TargetVideos { get; } = [];

        /// <summary>
        /// 選択された同期対象動画のインデックス
        /// </summary>
        public int SelectedTargetIndex { get; set => SetProperty(ref field, value); } = -1;

        /// <summary>
        /// 出力フォルダパス
        /// </summary>
        public string OutputDir { get; set => SetProperty(ref field, value); } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        /// <summary>
        /// ログテキスト
        /// </summary>
        public string LogText { get; set => SetProperty(ref field, value); } = string.Empty;

        /// <summary>
        /// 進捗状況
        /// </summary>
        public double ProgressValue { get; set => SetProperty(ref field, value); } = 0;

        /// <summary>
        /// 進捗テキスト
        /// </summary>
        public string ProgressText { get; set => SetProperty(ref field, value); } = "待機中...";

        /// <summary>
        /// 進捗インジケーターローディングフラグ
        /// </summary>
        public bool IsIndeterminate { get; set => SetProperty(ref field, value); } = false;

        /// <summary>
        /// 実行中フラグ
        /// </summary>
        public bool IsExecuting { get; set => SetProperty(ref field, value); } = false;

        /// <summary>
        /// CPUエンコードフラグ
        /// </summary>
        public bool IsCpuSelected { get; set; } = true;

        /// <summary>
        /// NVIDIA GPUエンコードフラグ
        /// </summary>
        public bool IsNvidiaSelected { get; set; }

        /// <summary>
        /// AMD GPUエンコードフラグ
        /// </summary>
        public bool IsAmdSelected { get; set; }

        /// <summary>
        /// Intel GPUエンコードフラグ
        /// </summary>
        public bool IsIntelSelected { get; set; }

        /// <summary>
        /// 解像度設定 (0:4K, 1:FHD, 2:HD, 3:SD)
        /// </summary>
        public int ResolutionIndex { get; set; } = 1;

        /// <summary>
        /// 処理キャンセル用トークン
        /// </summary>
        private CancellationTokenSource? _cts;
        #endregion

        #region コマンド設定
        public RelayCommand SelectBaseVideoCommand { get; }
        public RelayCommand AddTargetVideosCommand { get; }
        public RelayCommand RemoveTargetVideoCommand { get; }
        public RelayCommand ClearTargetVideosCommand { get; }
        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }
        public RelayCommand SelectOutputDirCommand { get; }
        public RelayCommand OpenOutputDirCommand { get; }
        public RelayCommand ExecuteCommand { get; }
        public RelayCommand CancelCommand { get; }
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindowViewModel()
        {
            SelectBaseVideoCommand      = new RelayCommand(_ => SelectBaseVideo());
            AddTargetVideosCommand      = new RelayCommand(_ => AddTargetVideos());
            RemoveTargetVideoCommand    = new RelayCommand(_ => RemoveTargetVideo());
            ClearTargetVideosCommand    = new RelayCommand(_ => TargetVideos.Clear());
            MoveUpCommand               = new RelayCommand(_ => MoveUp());
            MoveDownCommand             = new RelayCommand(_ => MoveDown());
            SelectOutputDirCommand      = new RelayCommand(_ => SelectOutputDir());
            OpenOutputDirCommand        = new RelayCommand(_ => OpenOutputDir());
            ExecuteCommand              = new RelayCommand(async _ => await ExecuteAsync(), _ => !IsExecuting);
            CancelCommand               = new RelayCommand(_ => Cancel());
        }

        /// <summary>
        /// 同期元動画選択処理
        /// </summary>
        private void SelectBaseVideo()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "動画ファイル|*.mp4;*.mkv;*.avi;*.mov|すべてのファイル|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                BaseVideo = dialog.FileName;
                if (TargetVideos.Contains(BaseVideo))
                {
                    _ = TargetVideos.Remove(BaseVideo);
                }
            }
        }

        /// <summary>
        /// 同期対象動画追加処理
        /// </summary>
        /// <param name="path"></param>
        public void AddTargetVideo(string path)
        {
            if (path == BaseVideo)
            {
                _ = MessageBox.Show("同期元動画と同じ動画は追加できません。", "注意", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!TargetVideos.Contains(path))
            {
                TargetVideos.Add(path);
            }
        }

        /// <summary>
        /// 同期対象動画削除処理
        /// </summary>
        private void RemoveTargetVideo()
        {
            if (SelectedTargetIndex >= 0)
            {
                TargetVideos.RemoveAt(SelectedTargetIndex);
            }
        }

        /// <summary>
        /// 同期対象動画複数追加処理
        /// </summary>
        private void AddTargetVideos()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "動画ファイル|*.mp4;*.mkv;*.avi;*.mov|すべてのファイル|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    AddTargetVideo(file);
                }
            }
        }

        /// <summary>
        /// 同期対象動画順序上げ処理
        /// </summary>
        private void MoveUp()
        {
            if (SelectedTargetIndex > 0)
            {
                var index = SelectedTargetIndex;
                var item = TargetVideos[index];
                TargetVideos.RemoveAt(index);
                TargetVideos.Insert(index - 1, item);
                SelectedTargetIndex = index - 1;
            }
        }

        /// <summary>
        /// 同期対象動画順序下げ処理
        /// </summary>
        private void MoveDown()
        {
            if (SelectedTargetIndex >= 0 && SelectedTargetIndex < TargetVideos.Count - 1)
            {
                var index = SelectedTargetIndex;
                var item = TargetVideos[index];
                TargetVideos.RemoveAt(index);
                TargetVideos.Insert(index + 1, item);
                SelectedTargetIndex = index + 1;
            }
        }

        /// <summary>
        /// 出力フォルダ選択処理
        /// </summary>
        private void SelectOutputDir()
        {
            var dialog = new OpenFolderDialog
            {
                InitialDirectory = OutputDir
            };
            if (dialog.ShowDialog() == true)
            {
                OutputDir = dialog.FolderName;
            }
        }

        /// <summary>
        /// 出力フォルダ開く処理
        /// </summary>
        private void OpenOutputDir()
        {
            if (Directory.Exists(OutputDir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = OutputDir,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("指定されたフォルダがまだ存在しません。\n一度動画を出力するか、フォルダを変更してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 処理を実行する
        /// </summary>
        /// <returns></returns>
        private async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(BaseVideo) || !File.Exists(BaseVideo))
            {
                _ = MessageBox.Show("同期元動画を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (TargetVideos.Count.Equals(0))
            {
                _ = MessageBox.Show("同期対象動画を追加してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsExecuting = true;
            LogText = string.Empty;
            ProgressValue = 0;
            IsIndeterminate = true;
            ProgressText = "音声の分析中...";

            var progress = new Progress<string>(msg => LogText += msg + Environment.NewLine);
            var statusProgress = new Progress<(double Percentage, TimeSpan? Eta)>(status =>
            {
                IsIndeterminate = false;
                ProgressValue = status.Percentage;
                ProgressText = status.Eta.HasValue
                    ? $"生成中... {status.Percentage:F1}％ (残り約 {(int)status.Eta.Value.TotalMinutes}分 {status.Eta.Value.Seconds}秒)"
                    : $"生成中... {status.Percentage:F1}％ (残り時間計算中...)";
            });

            var encodeOptions = GetEncodeOptions();
            var (targetWidth, targetHeight) = GetResolution();
            var inputVideos = new string[TargetVideos.Count + 1];
            inputVideos[0] = BaseVideo;
            for (var i = 0; i < TargetVideos.Count; i++)
            {
                inputVideos[i + 1] = TargetVideos[i];
            }

            var outputFile = Path.Combine(OutputDir, $"output_{DateTime.Now:yyyyMMddHHmmss}.mp4");

            _cts = new CancellationTokenSource();

            try
            {
                await Task.Run(() => VideoSyncerService.Execute(inputVideos, outputFile, encodeOptions, targetWidth, targetHeight, progress, statusProgress, _cts.Token), _cts.Token);
                ProgressValue = 100;
                ProgressText = "完了しました!";
                MessageBox.Show($"完了しました!\n\n出力先:\n{outputFile}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                IsIndeterminate = false;
                ProgressText = "キャンセルされました";
                MessageBox.Show("処理がキャンセルされました", "キャンセル", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                IsIndeterminate = false;
                ProgressText = "エラーが発生しました";
                MessageBox.Show($"エラー:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsExecuting = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// 画面選択からエンコードオプションを取得する
        /// </summary>
        /// <returns>取得したエンコードオプション</returns>
        private string GetEncodeOptions()
        {
            return true switch
            {
                _ when IsNvidiaSelected => "-vcodec h264_nvenc -profile:v high -g 150 -b:v 0 -cq 25",
                _ when IsAmdSelected    => "-vcodec h264_amf -profile:v high -g 150 -b:v 0 -cq 25",
                _ when IsIntelSelected  => "-vcodec h264_qsv -cq 25",
                _                       => "-vcodec h264"
            };
        }

        /// <summary>
        /// 画面選択から解像度を取得する
        /// </summary>
        /// <returns></returns>
        private (int Width, int Height) GetResolution()
        {
            return ResolutionIndex switch
            {
                0 => (3840, 2160),
                1 => (1920, 1080),
                2 => (1280, 720),
                3 => (848, 480),
                _ => (1920, 1080)
            };
        }

        /// <summary>
        /// 処理のキャンセルを要求する
        /// </summary>
        private void Cancel()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                LogText += "キャンセルを要求しています...\n";
                _cts.Cancel();
            }
        }
    }
}