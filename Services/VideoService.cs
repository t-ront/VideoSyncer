using System.IO;
using VideoSyncer.Models;
using VideoSyncer.Services.FFmpeg;
using VideoSyncer.Utilities;

namespace VideoSyncer.Services
{
    /// <summary>
    /// 動画処理クラス
    /// </summary>
    internal sealed class VideoService
    {
        /// <summary>
        /// FFmpeg実行ファイルパス
        /// </summary>
        private readonly string _ffmpegExePath;

        /// <summary>
        /// FFprobe実行ファイルパス
        /// </summary>
        private readonly string _ffprobeExePath;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        internal VideoService()
        {
            var baseDir = AppContext.BaseDirectory;
            _ffmpegExePath = Path.Combine(baseDir, "Tools", "ffmpeg.exe");
            _ffprobeExePath = Path.Combine(baseDir, "Tools", "ffprobe.exe");
        }

        /// <summary>
        /// 動画を生成する
        /// </summary>
        /// <param name="videos">出力元動画パス</param>
        /// <param name="offsets">オフセット</param>
        /// <param name="outputFile">出力ファイルパス</param>
        /// <param name="encodeOptions">エンコードオプション</param>
        /// <param name="targetWidth">出力動画幅</param>
        /// <param name="targetHeight">出力動画高</param>
        /// <param name="progress">進捗状況</param>
        /// <param name="statusProgress">ステータス進捗</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        internal void GenerateSplitVideo(string[] videos, double[] offsets, string outputFile, string encodeOptions, int targetWidth, int targetHeight, IProgress<string> progress, IProgress<(double Percentage, TimeSpan? Eta)> statusProgress, CancellationToken cancellationToken)
        {
            if (!File.Exists(_ffmpegExePath))
            {
                progress.Report("エラー: FFmpeg実行ファイルが見つかりません");
                return;
            }

            var minOffset = offsets.Min();
            var delays = offsets.Select(o => o - minOffset).ToArray();
            var config = GridPlacement.GetLayoutConfig(videos.Length, targetWidth, targetHeight);
            var arguments = FFmpegCommandBuilder.BuildCommand(videos, delays, config, encodeOptions, outputFile, targetWidth, targetHeight);

            var totalDurationSeconds = GetVideoDuration(videos[0], progress, cancellationToken).TotalSeconds;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                FFmpegRunner.Run(_ffmpegExePath, arguments, totalDurationSeconds, progress, statusProgress, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                File.Delete(outputFile);
                throw;
            }
        }

        /// <summary>
        /// 動画の長さを取得する
        /// </summary>
        /// <param name="filePath">動画ファイルパス</param>
        /// <param name="progress">進捗状況</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>動画長</returns>
        private TimeSpan GetVideoDuration(string filePath, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_ffprobeExePath))
            {
                return TimeSpan.Zero;
            }

            var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"";
            var (output, _) = ExternalProcess.RunCommandWithResult(_ffprobeExePath, args, 1, progress, cancellationToken);
            return double.TryParse(output.Trim(), System.Globalization.CultureInfo.InvariantCulture, out var sec)
                ? TimeSpan.FromSeconds(sec)
                : TimeSpan.Zero;
        }
    }
}