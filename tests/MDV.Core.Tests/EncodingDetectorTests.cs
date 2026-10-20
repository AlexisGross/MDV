using System.Linq;
using System.Text;
using MDV.Core.Services;
using Xunit;

public class EncodingDetectorTests
{
    public EncodingDetectorTests() => EncodingDetector.RegisterProviders();

    [Fact] public void Utf8_bom()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("あ")).ToArray();
        var r = EncodingDetector.Detect(bytes);
        Assert.Equal("UTF-8 (BOM)", r.DisplayName);
        Assert.Equal("あ", r.Encoding.GetString(StripBom(bytes, r)));
    }

    [Fact] public void Utf8_no_bom()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello あ");
        var r = EncodingDetector.Detect(bytes);
        Assert.Equal("UTF-8", r.DisplayName);
    }

    [Fact] public void Utf16_le_bom()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("あ")).ToArray();
        Assert.Equal("UTF-16 LE", EncodingDetector.Detect(bytes).DisplayName);
    }

    [Fact] public void ShiftJis_fallback()
    {
        EncodingDetector.RegisterProviders();
        var sjis = Encoding.GetEncoding(932);
        var bytes = sjis.GetBytes("日本語テスト");
        var r = EncodingDetector.Detect(bytes, 932);
        Assert.Equal("Shift_JIS", r.DisplayName);
        Assert.Equal("日本語テスト", r.Encoding.GetString(bytes));
    }

    // 東アジア以外の地域(例: 英語 1252)では従来どおり Shift_JIS とみなす
    [Fact] public void Non_cjk_locale_keeps_shift_jis()
    {
        var bytes = Encoding.GetEncoding(932).GetBytes("日本語テスト");
        var r = EncodingDetector.Detect(bytes, 1252);
        Assert.Equal("Shift_JIS", r.DisplayName);
        Assert.Equal("日本語テスト", r.Encoding.GetString(bytes));
    }

    // 韓国語の地域では EUC-KR(CP949)として読む
    [Fact] public void Korean_locale_reads_euc_kr()
    {
        var bytes = Encoding.GetEncoding(949).GetBytes("한글 문서입니다");
        var r = EncodingDetector.Detect(bytes, 949);
        Assert.Equal("EUC-KR", r.DisplayName);
        Assert.Equal("한글 문서입니다", r.Encoding.GetString(bytes));
    }

    [Theory]
    [InlineData(936, "GBK", "简体中文文档")]
    [InlineData(950, "Big5", "繁體中文文件")]
    public void Chinese_locales_read_their_code_page(int codePage, string name, string text)
    {
        var bytes = Encoding.GetEncoding(codePage).GetBytes(text);
        var r = EncodingDetector.Detect(bytes, codePage);
        Assert.Equal(name, r.DisplayName);
        Assert.Equal(text, r.Encoding.GetString(bytes));
    }

    [Fact] public void Pure_ascii_is_utf8()
    {
        var r = EncodingDetector.Detect(Encoding.ASCII.GetBytes("plain text"));
        Assert.Equal("UTF-8", r.DisplayName);
    }

    private static byte[] StripBom(byte[] b, MDV.Core.Models.TextEncodingInfo r)
    {
        var pre = r.Encoding.GetPreamble();
        return b.Length >= pre.Length && pre.Length > 0 ? b[pre.Length..] : b;
    }
}
