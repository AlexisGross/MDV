using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Globalization;

namespace MDV.App.Services;

/// <summary>
/// コードから使うUI文字列を Strings/&lt;言語&gt;/Resources.resw から取得する。
/// 言語は [表示]→[言語] の指定、未指定ならWindowsの表示言語で決まり、
/// 該当する翻訳が無ければ既定言語(en-US)になる。
/// XAML側の文字列は x:Uid で同じリソースから読み込まれる。
/// </summary>
public static partial class Localizer
{
    private static ResourceLoader? _loader;

    public static string Get(string key)
    {
        try
        {
            _loader ??= new ResourceLoader();
            var value = _loader.GetString(key);
            // キーが見つからない場合も画面が空欄にならないよう、キー名をそのまま返す
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }

    /// <summary>
    /// 翻訳が用意されている言語(BCP-47タグ)の一覧。Strings/ 以下のフォルダ名から
    /// ビルド時に生成される(MDV.App.csproj の MdvGenerateLanguageList)ため、言語を追加してもコードの変更は不要。
    /// </summary>
    public static IReadOnlyList<string> AvailableLanguages() => BuiltInLanguages;

    // PrimaryLanguageOverride の扱い(ドキュメントの指針に従う):
    // - パッケージ実行では指定がOS側に保存されるため、ユーザーが [表示]→[言語] で選んだときだけ指定する
    // - 非パッケージ実行では保存されないため、起動のたびにアプリ設定の言語を指定し直す
    // - 指定する値はアプリに翻訳がある言語だけにする
    // 「システムに合わせる」は指定を解除することで実現する(未指定ならWindowsの言語設定から選ばれる)。

    /// <summary>
    /// 起動時、ウィンドウ生成前に呼ぶ。アプリ設定の言語(空文字は「システムに合わせる」)と実際の指定を合わせる。
    /// パッケージ実行では、OSに保存された指定が設定と食い違うときだけ直す(通常は何もしない)。
    /// 以前のバージョンが起動のたびにWindowsの第一言語を指定していたため、その指定もここで解除される。
    /// </summary>
    public static void ApplySavedLanguage(string? language)
    {
        try
        {
            var wanted = IsAvailable(language) ? language! : "";
            // 保存されている指定は Windows.Globalization 側で読む
            // (Windows App SDK 側の getter は新しいプロセスでは常に空文字を返すため、比較に使えない)
            if (IsPackaged && SameLanguage(Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride, wanted))
                return;
            SetOverride(wanted);
        }
        catch
        {
            // 指定できない環境では既定の言語選択に任せる
        }
    }

    /// <summary>[表示]→[言語] で選んだ言語を指定する(空文字は「システムに合わせる」)。反映はアプリの再起動後。</summary>
    public static void SetLanguage(string language)
    {
        try { SetOverride(IsAvailable(language) ? language : ""); }
        catch { /* 指定できない環境では既定の言語選択に任せる */ }
    }

    private static void SetOverride(string language)
    {
        if (language.Length > 0)
        {
            ApplicationLanguages.PrimaryLanguageOverride = language;
            return;
        }
        // 指定の解除。Windows App SDK 側は空文字を受け付けない(E_INVALIDARG)ため、
        // パッケージ実行時はその元である Windows.Globalization 側で解除する。
        // 非パッケージ実行では指定が保存されないので、何も指定しなければよい。
        if (IsPackaged)
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "";
    }

    // OSは指定を "ja-JP" → "ja" のように短くして保存することがあるため、言語部分が一致すれば同じとみなす
    private static bool SameLanguage(string saved, string wanted) =>
        string.Equals(saved, wanted, StringComparison.OrdinalIgnoreCase) ||
        (saved.Length > 0 && wanted.StartsWith(saved + "-", StringComparison.OrdinalIgnoreCase));

    private static bool IsAvailable(string? language) =>
        !string.IsNullOrEmpty(language) &&
        AvailableLanguages().Any(l => string.Equals(l, language, StringComparison.OrdinalIgnoreCase));

    private static bool IsPackaged
    {
        get
        {
            try { return Windows.ApplicationModel.Package.Current != null; }
            catch { return false; }
        }
    }

    /// <summary>
    /// 言語タグをその言語自身の名称で返す(例: ko-KR → 한국어, en-US → English)。
    /// 同じ言語の別地域(例: pt-BR と pt-PT)が同梱されている場合だけ地域名も付ける。
    /// </summary>
    public static string NativeName(string tag)
    {
        try
        {
            var lang = new Windows.Globalization.Language(tag);
            var baseTag = string.IsNullOrEmpty(lang.Script)
                ? tag.Split('-')[0]
                : $"{tag.Split('-')[0]}-{lang.Script}";
            var sameBase = AvailableLanguages().Count(l =>
                l.StartsWith(baseTag + "-", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(l, baseTag, StringComparison.OrdinalIgnoreCase));
            return sameBase > 1 ? lang.NativeName : new Windows.Globalization.Language(baseTag).NativeName;
        }
        catch { return tag; }
    }
}
