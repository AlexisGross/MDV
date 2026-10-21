// 作成: 2026-10-21
// 目的: 例外を端末内のログファイルに記録する(書式の組み立てと、サイズ上限付きの追記)。
//       通信は一切行わない。Partner Center のクラッシュ情報にスタックが無く原因を追えないため、
//       利用者の端末に残った記録を、必要なときに利用者自身に見せてもらうためのもの。
using System.Globalization;
using System.Text;

namespace MDV.Core.Services;

/// <summary>例外の記録を、上限付きのテキストファイルに追記する</summary>
public static class CrashLog
{
    public const string FileName = "crash.log";
    public const string OldFileName = "crash.old.log";

    /// <summary>1ファイルの上限(バイト)。超えたら1世代前へ回すため、合計はおよそこの2倍まで</summary>
    public const long DefaultMaxBytes = 256 * 1024;

    private static readonly object Gate = new();

    // ログ1件分の文字列を組み立てる(日時・発生元・アプリ版・OS版・HRESULT・例外の型/メッセージ/スタック/内側の例外)
    public static string Format(Exception ex, string source, string appVersion, string osVersion, DateTimeOffset when)
    {
        var sb = new StringBuilder();
        sb.AppendLine("==== " + when.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture) + " ====");
        sb.AppendLine("Source: " + source);
        sb.AppendLine("App: " + appVersion);
        sb.AppendLine("OS: " + osVersion);
        sb.AppendLine("HResult: 0x" + ex.HResult.ToString("X8", CultureInfo.InvariantCulture));
        // ToString() は型名・メッセージ・内側の例外・スタックトレースをまとめて含む
        sb.AppendLine(ex.ToString());
        sb.AppendLine();
        return sb.ToString();
    }

    // directory 内のログに追記する。上限を超えていたら先に1世代前(crash.old.log)へ回す
    public static void Append(string directory, string entry, long maxBytes = DefaultMaxBytes)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, FileName);
            var info = new FileInfo(path);
            if (info.Exists && info.Length >= maxBytes)
                File.Move(path, Path.Combine(directory, OldFileName), overwrite: true);

            // 別のMDVウィンドウ(別プロセス)と同時に書くことがあるため、短く数回だけ再試行する
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                    var bytes = new UTF8Encoding(false).GetBytes(entry);
                    fs.Write(bytes, 0, bytes.Length);
                    return;
                }
                catch (IOException) when (attempt < 3)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
