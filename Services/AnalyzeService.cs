using System.IO;
using System.Text.Json;
using VideoSyncer.Utilities;

namespace VideoSyncer.Services
{
    /// <summary>
    /// 解析クラス
    /// </summary>
    internal sealed class AnalyzeService
    {
        /// <summary>
        /// Python実行ファイルパス
        /// </summary>
        private readonly string _pythonExePath;

        /// <summary>
        /// Pythonスクリプトファイルパス
        /// </summary>
        private readonly string _pythonScriptPath;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        internal AnalyzeService()
        {
            var baseDir = AppContext.BaseDirectory;
            _pythonExePath = Path.Combine(baseDir, "Tools", "python", "python.exe");
            _pythonScriptPath = Path.Combine(baseDir, "Utilities", "audio_sync.py");
        }

        /// <summary>
        /// 音声同期解析を行う
        /// </summary>
        /// <param name="videos">解析対象動画ファイルパスリスト</param>
        /// <param name="progress">進行状況通知インターフェース</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>同期元との動画時間差異リスト</returns>
        internal double[] AnalyzeAudioSync(string[] videos, IProgress<string> progress, CancellationToken cancellationToken)
        {
            if (!File.Exists(_pythonExePath))
            {
                progress.Report("エラー: Python実行ファイルが見つかりません - " + _pythonExePath);
            }

            if (!File.Exists(_pythonScriptPath))
            {
                progress.Report("エラー: Pythonスクリプトが見つかりません - " + _pythonScriptPath);
            }

            var arguments = $"\"{_pythonScriptPath}\" " + string.Join(" ", videos.Select(v => $"\"{v}\""));
            var (output, error) = ExternalProcess.RunCommandWithResult(_pythonExePath, arguments, 10, progress, cancellationToken);
            if (string.IsNullOrEmpty(output))
            {
                progress.Report("Pythonプロセスの起動、または実行に失敗しました。");
                if (!string.IsNullOrEmpty(error))
                {
                    progress.Report($"詳細: {error}");
                }
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<double[]>(output) ?? [];
            }
            catch
            {
                progress.Report("Pythonからの出力の解析に失敗しました: " + output);
                if (!string.IsNullOrEmpty(error))
                {
                    progress.Report($"エラー出力: {error}");
                }
                return [];
            }
        }
    }
}