using VideoSyncer.Utilities;

namespace VideoSyncer.Services.FFmpeg
{
    /// <summary>
    /// FFmpeg実行クラス
    /// </summary>
    internal sealed class FFmpegRunner
    {
        /// <summary>
        /// FFmpegを実行する
        /// </summary>
        /// <param name="ffmpegExePath">ffmpeg実行ファイルパス</param>
        /// <param name="arguments">コマンドライン引数</param>
        /// <param name="durationSeconds">再生時間(秒)</param>
        /// <param name="logger">ロガー</param>
        /// <param name="statusProgress">ステータス進捗</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        internal static void Run(string ffmpegExePath, string arguments, double durationSeconds, IProgress<string> logger, IProgress<(double Percentage, TimeSpan? Eta)> statusProgress, CancellationToken cancellationToken)
        {
            var currentProcessedSeconds = 0.0;
            var currentSpeed = 1.0;

            _ = ExternalProcess.RunBaseMethod(
                programPath: ffmpegExePath,
                commandOption: arguments,
                timeOutSpanMinutes: 0,
                onOutput: null,
                onError: (data) => ParseProgress(data, durationSeconds, ref currentProcessedSeconds, ref currentSpeed, statusProgress),
                logger: logger,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// 処理状況を解析する
        /// </summary>
        /// <param name="data"></param>
        /// <param name="durationSeconds">再生時間(秒)</param>
        /// <param name="currentProcessedSeconds">処理済み時間(秒)</param>
        /// <param name="currentSpeed">現処理速度</param>
        /// <param name="statusProgress">処理状況</param>
        private static void ParseProgress(string data, double durationSeconds, ref double currentProcessedSeconds, ref double currentSpeed, IProgress<(double, TimeSpan?)> statusProgress)
        {
            if (data.StartsWith("out_time=") && TimeSpan.TryParse(data.AsSpan(9), out TimeSpan currentTime))
            {
                currentProcessedSeconds = currentTime.TotalSeconds;
                if (durationSeconds > 0)
                {
                    var percentage = Math.Clamp(currentProcessedSeconds / durationSeconds * 100, 0, 100);
                    TimeSpan? eta = currentSpeed > 0 ? TimeSpan.FromSeconds((durationSeconds - currentProcessedSeconds) / currentSpeed) : null;
                    statusProgress?.Report((percentage, eta));
                }
            }
            else if (data.StartsWith("speed=") && double.TryParse(data[6..].TrimEnd('x', ' '), System.Globalization.CultureInfo.InvariantCulture, out var speedValue))
            {
                currentSpeed = speedValue;
            }
        }
    }
}