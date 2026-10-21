// 作成: 2026-10-21
// 目的: 端末内クラッシュログ(CrashLog)の書式と、上限付き追記(ローテーション)を検査する
using MDV.Core.Services;
using Xunit;

public class CrashLogTests
{
    // 例外を実際に投げて捕まえ、スタックトレースを持つ例外を作る
    private static Exception Thrown(Exception ex)
    {
        try { throw ex; }
        catch (Exception caught) { return caught; }
    }

    [Fact]
    public void Format_includes_type_message_stack_versions_and_time()
    {
        var ex = Thrown(new InvalidOperationException("壊れた", new IOException("内側")));
        var when = new DateTimeOffset(2026, 10, 21, 9, 30, 0, TimeSpan.FromHours(9));

        var text = CrashLog.Format(ex, "Application.UnhandledException", "1.0.7.0", "10.0.26200.0", when);

        Assert.Contains("2026-10-21T09:30:00", text);
        Assert.Contains("Application.UnhandledException", text);
        Assert.Contains("1.0.7.0", text);
        Assert.Contains("10.0.26200.0", text);
        Assert.Contains("System.InvalidOperationException", text);
        Assert.Contains("壊れた", text);
        Assert.Contains(nameof(Thrown), text);          // スタックトレース
        Assert.Contains("System.IO.IOException", text);  // 内側の例外
        Assert.Contains("内側", text);
    }

    [Fact]
    public void Format_includes_hresult()
    {
        var ex = new System.Runtime.InteropServices.COMException("com", unchecked((int)0x80004005));
        var text = CrashLog.Format(ex, "test", "1", "1", DateTimeOffset.Now);
        Assert.Contains("0x80004005", text);
    }

    [Fact]
    public void Append_creates_directory_and_appends()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mdv-crashlog-" + Guid.NewGuid());
        try
        {
            CrashLog.Append(dir, "first");
            CrashLog.Append(dir, "second");
            var text = File.ReadAllText(Path.Combine(dir, CrashLog.FileName));
            Assert.Contains("first", text);
            Assert.Contains("second", text);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Append_rotates_when_over_limit_so_total_size_is_bounded()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mdv-crashlog-" + Guid.NewGuid());
        try
        {
            var entry = new string('x', 300);
            for (int i = 0; i < 20; i++)
                CrashLog.Append(dir, entry, maxBytes: 1000);

            var current = new FileInfo(Path.Combine(dir, CrashLog.FileName));
            var old = new FileInfo(Path.Combine(dir, CrashLog.OldFileName));
            Assert.True(old.Exists);
            // 現在のファイルも1世代前も、上限+1件分を超えない
            Assert.True(current.Length <= 1000 + 400, $"current={current.Length}");
            Assert.True(old.Length <= 1000 + 400, $"old={old.Length}");
            Assert.Equal(2, Directory.GetFiles(dir).Length);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
