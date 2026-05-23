using System.Globalization;
using VideoSyncer.Models;

namespace VideoSyncer.Services.FFmpeg
{
    /// <summary>
    /// FFmpegコマンド構築クラス
    /// </summary>
    internal sealed class FFmpegCommandBuilder
    {
        /// <summary>
        /// FFmpegコマンドを構築する
        /// </summary>
        /// <param name="videos">入力動画パス配列</param>
        /// <param name="delays">同期動画遅延時間</param>
        /// <param name="config">動画配置設定</param>
        /// <param name="encodeOptions">エンコード設定</param>
        /// <param name="outputFile">出力ファイルパス</param>
        /// <param name="targetWidth">出力動画幅</param>
        /// <param name="targetHeight">出力動画高</param>
        /// <returns>構築済みFFmpegコマンド</returns>
        internal static string BuildCommand(string[] videos, double[] delays, GridConfig config, string encodeOptions, string outputFile, int targetWidth, int targetHeight)
        {
            var culture = CultureInfo.InvariantCulture;
            var inputArgs = string.Join(" ", videos.Select(v => $"-i \"{v}\""));
            var filterComplex = string.Empty;

            for (var i = 0; i < videos.Length; i++)
            {
                var tpad = delays[i] > 0 ? $",tpad=start_duration={delays[i].ToString("F3", culture)}:start_mode=clone" : "";
                filterComplex += $"[{i}:v]setpts=PTS-STARTPTS,fps=30,scale={config.VideoWidth}:{config.VideoHeight},format=yuv420p{tpad}[v{i}_ready]; ";
            }

            var xstackInputs = string.Join(string.Empty, Enumerable.Range(0, videos.Length).Select(i => $"[v{i}_ready]"));
            var layoutParts = string.Join("|", config.Placements.Select(p => $"{p.X}_{p.Y}"));

            filterComplex += $"{xstackInputs}xstack=inputs={videos.Length}:layout={layoutParts}:fill=black[outv_stack]; ";
            filterComplex += $"[outv_stack]pad={targetWidth}:{targetHeight}:(ow-iw)/2:(oh-ih)/2:color=black[outv]; ";

            var audioDelayMs = (int)(delays[0] * 1000);
            filterComplex += audioDelayMs > 0
                ? $"[0:a]adelay=delays={audioDelayMs}:all=1[outa]"
                : $"[0:a]anull[outa]";

            return $"{inputArgs} -filter_complex \"{filterComplex}\" -map \"[outv]\" -map \"[outa]\" {encodeOptions} -c:a aac -shortest -progress pipe:2 -y \"{outputFile}\"";
        }
    }
}