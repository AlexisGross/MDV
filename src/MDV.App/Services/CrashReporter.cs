// 作成: 2026-10-21
// 目的: 未処理の例外と、画面で扱ったエラーを端末内のログ(ApplicationData の LocalFolder\Logs)に記録する。
//       通信は一切しない(「データ収集なし」の方針)。利用者は About の「ログフォルダを開く」から中身を確認できる。
using System.Runtime.InteropServices;
using MDV.Core.Services;
using Windows.Storage;

namespace MDV.App.Services;

/// <summary>例外を端末内のログファイルに記録する</summary>
public static class CrashReporter
{
    private static string? _logFolder;
    private static string? _appVersion;
    // 同じ例外が複数のハンドラ(Application と AppDomain)に届いたとき、二重に書かないための記憶
    private static Exception? _lastRecorded;

    /// <summary>ログを置くフォルダ。パッケージ実行では LocalState\Logs、非パッケージ実行では %LOCALAPPDATA%\MDV\Logs</summary>
    public static string LogFolder => _logFolder ??= ResolveLogFolder();

    // 起動直後に呼ぶ。3種類の未処理例外を受けて記録する(記録のみで、例外の扱い自体は変えない)
    public static void Install(Microsoft.UI.Xaml.Application app)
    {
        // クラッシュ時に ApplicationData へ触れて失敗しないよう、パスと版は先に求めておく
        _ = LogFolder;
        _ = AppVersion;

        app.UnhandledException += (s, e) =>
        {
            // XAML由来の例外は e.Message の方が詳しいことがあるため、異なる場合は発生元に添える
            var source = "Application.UnhandledException";
            if (!string.IsNullOrEmpty(e.Message) && e.Message != e.Exception?.Message)
                source += " (" + e.Message + ")";
            if (e.Exception != null) Record(e.Exception, source);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Record(ex, "AppDomain.UnhandledException" + (e.IsTerminating ? " (terminating)" : ""));
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
            Record(e.Exception, "TaskScheduler.UnobservedTaskException");
    }

    // 例外を1件記録する。記録の失敗でさらに例外を起こさないよう、すべて握りつぶす
    public static void Record(Exception ex, string source)
    {
        try
        {
            if (ReferenceEquals(Interlocked.Exchange(ref _lastRecorded, ex), ex)) return;
            var os = $"{Environment.OSVersion.Version} {RuntimeInformation.ProcessArchitecture}";
            CrashLog.Append(LogFolder, CrashLog.Format(ex, source, AppVersion, os, DateTimeOffset.Now));
        }
        catch
        {
            // ログを書けなくても本来の処理には影響させない
        }
    }

    // ログフォルダをエクスプローラーで開く(まだ記録が無くてもフォルダは作って開く)
    public static async Task OpenLogFolderAsync()
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var folder = await StorageFolder.GetFolderFromPathAsync(LogFolder);
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }
        catch (Exception ex)
        {
            Record(ex, "OpenLogFolder");
        }
    }

    private static string AppVersion => _appVersion ??= ResolveAppVersion();

    // アプリの版をパッケージ情報から取得する(非パッケージ実行時はアセンブリ情報)
    private static string ResolveAppVersion()
    {
        try
        {
            var v = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        }
        catch
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        }
    }

    // ログフォルダのパスを決める(パッケージ実行なら LocalFolder 配下)
    private static string ResolveLogFolder()
    {
        try
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, "Logs");
        }
        catch
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MDV", "Logs");
        }
    }
}
