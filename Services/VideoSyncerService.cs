using System.IO;

namespace VideoSyncer.Services
{
    /// <summary>
    /// 動画同期処理クラス
    /// </summary>
    internal sealed class VideoSyncerService
    {
        /// <summary>
        /// 動画の生成を実行する
        /// </summary>
        /// <param name="inputVideos">入力動画パス</param>
        /// <param name="outputFile">出力ファイルパス</param>
        /// <param name="encodeOptions">エンコードオプション</param>
        /// <param name="targetWidth">出力動画幅</param>
        /// <param name="targetHeight">出力動画高</param>
        /// <param name="progress">進捗状況</param>
        /// <param name="statusProgress">ステータス進捗</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        internal static void Execute(string[] inputVideos, string outputFile, string encodeOptions, int targetWidth, int targetHeight, IProgress<string> progress, IProgress<(double Percentage, TimeSpan? Eta)> statusProgress, CancellationToken cancellationToken)
        {
            var outputDirectory = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                _ = Directory.CreateDirectory(outputDirectory);
            }

            foreach (var video in inputVideos)
            {
                if (!File.Exists(video))
                {
                    progress.Report("エラー: 動画ファイルが見つかりません - " + video);
                    return;
                }
            }

            progress.Report("音声の同期を分析中...");
            var offsets = new AnalyzeService().AnalyzeAudioSync(inputVideos, progress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (offsets == null || offsets.Length.Equals(0))
            {
                progress.Report("エラー: 音声の分析に失敗しました。");
                return;
            }
            progress.Report("分析完了!");

            progress.Report("動画の生成中...");
            new VideoService().GenerateSplitVideo(inputVideos, offsets, outputFile, encodeOptions, targetWidth, targetHeight, progress, statusProgress, cancellationToken);
            progress.Report("動画の作成が完了しました: " + outputFile);
        }
    }
}