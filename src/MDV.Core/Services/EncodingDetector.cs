using System.Text;
using MDV.Core.Models;

namespace MDV.Core.Services;

/// <summary>バイト列からテキストエンコードを判定する</summary>
public static class EncodingDetector
{
    private static bool _registered;

    // UTF-8でないファイルの読み込みに使う、東アジアの2バイト文字のコードページと表示名
    private static readonly Dictionary<int, string> LegacyCodePages = new()
    {
        [932] = "Shift_JIS",   // 日本語
        [936] = "GBK",         // 中国語(簡体字)
        [949] = "EUC-KR",      // 韓国語(CP949)
        [950] = "Big5",        // 中国語(繁体字)
    };

    /// <summary>Shift_JIS(932)等を使うためのプロバイダ登録。起動時に1回呼ぶ。</summary>
    public static void RegisterProviders()
    {
        if (_registered) return;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _registered = true;
    }

    /// <summary>利用者の地域設定(CurrentCulture)の既定コードページを手がかりに判定する</summary>
    public static TextEncodingInfo Detect(byte[] bytes) =>
        Detect(bytes, System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage);

    /// <param name="ansiCodePage">利用者の地域の既定コードページ(例: 日本語 932、韓国語 949、英語 1252)</param>
    public static TextEncodingInfo Detect(byte[] bytes, int ansiCodePage)
    {
        // 1. BOM優先
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new TextEncodingInfo(new UTF8Encoding(true), "UTF-8 (BOM)");
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return new TextEncodingInfo(Encoding.Unicode, "UTF-16 LE");
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return new TextEncodingInfo(Encoding.BigEndianUnicode, "UTF-16 BE");

        // 2. UTF-8として厳密デコードを試行
        if (IsValidUtf8(bytes))
            return new TextEncodingInfo(new UTF8Encoding(false), "UTF-8");

        // 3. 失敗なら、利用者の地域が東アジアの2バイト文字のコードページ(日本語・中国語・韓国語)であればそれで読む。
        //    それ以外の地域では従来どおり Shift_JIS とみなす。
        //    (例えば韓国語の地域で Shift_JIS 固定にすると、EUC-KR の文書がすべて文字化けする。
        //     EUC-KR のハングルの多くは Shift_JIS としても有効なバイト列のため、内容からの判別はしない)
        RegisterProviders();
        var codePage = LegacyCodePages.ContainsKey(ansiCodePage) ? ansiCodePage : 932;
        return new TextEncodingInfo(Encoding.GetEncoding(codePage), LegacyCodePages[codePage]);
    }

    private static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            var strict = new UTF8Encoding(false, throwOnInvalidBytes: true);
            strict.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException) { return false; }
    }
}
