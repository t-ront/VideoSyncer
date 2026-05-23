using System.Diagnostics;
using System.Text;

namespace VideoSyncer.Utilities
{
    /// <summary>
    /// 外部プロセスクラス
    /// </summary>
    internal sealed class ExternalProcess
    {
        /// <summary>
        /// 外部プロセスを実行し、標準出力を取得する
        /// </summary>
        /// <param name="programPath">外部プロセスパス</param>
        /// <param name="commandOption">コマンドオプション</param>
        /// <param name="timeOutSpanMinutes">タイムアウト時間(分)</param>
        /// <param name="logger">ロガー</param>
        /// <returns>標準出力文字列、実行に失敗した場合は空文字</returns>
        internal static (string standardOutput, string standardError) RunCommandWithResult(
            string programPath,
            string commandOption,
            int timeOutSpanMinutes,
            IProgress<string>? logger = null,
            CancellationToken cancellationToken = default)
        {
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            var success = RunBaseMethod(
                programPath,
                commandOption,
                timeOutSpanMinutes,
                (data) => outputBuilder.AppendLine(data),
                (error) => errorBuilder.AppendLine(error),
                logger,
                cancellationToken);

            return success
                ? (outputBuilder.ToString(), errorBuilder.ToString())
                : (string.Empty, errorBuilder.ToString());
        }

        /// <summary>
        /// 外部プロセス実行共通ロジック
        /// </summary>
        /// <param name="programPath">外部プロセスパス</param>
        /// <param name="commandOption">コマンドオプション</param>
        /// <param name="timeOutSpanMinutes">タイムアウト時間(分)</param>
        /// <param name="onOutput">標準出力時処理メソッド</param>
        /// <param name="onError">エラー出力時処理メソッド</param>
        /// <param name="logger">ロガー</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>実行成否</returns>
        internal static bool RunBaseMethod(
            string programPath,
            string commandOption,
            int timeOutSpanMinutes,
            Action<string>? onOutput = null,
            Action<string>? onError = null,
            IProgress<string>? logger = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var psInfo = BuildProcessStartInfo(programPath, commandOption);

                using var process = Process.Start(psInfo);
                if (process == null)
                {
                    logger?.Report($"エラー: プロセスの起動に失敗しました ({programPath})");
                    return false;
                }

                using var registration = cancellationToken.Register(() =>
                {
                    try
                    {
                        if(!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch
                    {

                    }
                });

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        onOutput?.Invoke(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        onError?.Invoke(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (timeOutSpanMinutes > 0)
                {
                    var timeOutSpan = timeOutSpanMinutes * 60 * 1000;
                    if (!process.WaitForExit(timeOutSpan))
                    {
                        logger?.Report($"エラー: プロセスがタイムアウトしました。強制終了します。({timeOutSpanMinutes}分)");
                        process.Kill();
                        return false;
                    }
                }
                else
                {
                    process.WaitForExit();
                }
                cancellationToken.ThrowIfCancellationRequested();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger?.Report("エラー発生! 外部プロセスの実行中に例外が発生しました。");
                logger?.Report(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 外部プロセス開始情報を構築する
        /// </summary>
        /// <param name="programPath">外部プロセスパス</param>
        /// <param name="commandOption">コマンドオプション</param>
        /// <returns>外部プロセス開始情報</returns>
        private static ProcessStartInfo BuildProcessStartInfo(string programPath, string commandOption)
        {
            return new ProcessStartInfo()
            {
                FileName = programPath,
                Arguments = commandOption,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }
    }
}