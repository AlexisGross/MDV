<!-- 作成：2026-10-24 目的：MDV への貢献の手順と、機能を受け入れるかどうかの方針をまとめる -->

# Contributing to MDV

[English](#english) | [日本語](#日本語)

## English

Thank you for your interest in MDV. Issues and pull requests are welcome in English or Japanese.

### What MDV is

MDV is a **read-only Markdown viewer** for Windows 11. It opens a finished Markdown document and shows it as it is, like a PDF viewer. It has no editing features, no network access and no data collection, and it tries to work well without any settings.

To keep it small and simple, we use these questions to decide whether a change belongs in MDV:

1. **Is it for reading?** Editing and file management are out of scope.
2. **Is the document shown incorrectly without it?** Fixes for missing or broken rendering are welcome. Features that are only "nice to have" are usually declined.
3. **Does it keep the promise of no network access and no data collection?**
4. **Does it avoid adding settings or UI elements?** The app should look right with its defaults.
5. **Does an existing feature not work as the menu or documentation says?** That is a bug, and fixes are welcome.

Examples that are out of scope: editing, tabs, a file tree, exporting to PDF or HTML, custom CSS, plugins.

### Before you start

- **Bug fixes, small UX fixes and translations:** you can open a pull request directly.
- **New features or changes to how documents are loaded or shown:** please open an issue first and describe the idea. This avoids work on a pull request that cannot be merged.

### Translations

See [docs/translations/README.md](docs/translations/README.md). You can add a language without changing any code or the manifest.

### Building and testing

Requirements: Windows 11, the .NET 9 SDK, and the Visual Studio "Desktop development with C++" workload (WinUI 3 uses the MSVC tools during the build).

Run these in PowerShell:

```powershell
dotnet build MDV.sln
dotnet test tests/MDV.Core.Tests
```

To run the app: `cd src/MDV.App` and `dotnet run -c Debug`. If MDV from the Microsoft Store is installed on the same PC, `dotnet run` fails because the package ID is the same. See [README.md](README.md#開発時の実行) for the workaround, and do not commit changes to `Package.appxmanifest`.

### Pull requests

- Keep each pull request focused on one topic. Smaller pull requests are easier to review.
- Explain **why** the change is needed, and how you checked it (for example, steps to reproduce in the app).
- Make sure `dotnet test tests/MDV.Core.Tests` passes.

### License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).

---

## 日本語

MDV に関心を持っていただき、ありがとうございます。Issue とプルリクエストは、英語・日本語のどちらでも歓迎します。

### MDV とは

MDV は Windows 11 向けの**閲覧専用 Markdown ビューア**です。PDF ビューアのように、完成した Markdown 文書を開いてそのまま表示します。編集機能・通信・データ収集はなく、設定をしなくても快適に使えることを目指しています。

小さくシンプルに保つため、変更を受け入れるかどうかは次の問いで判断します。

1. **読むための機能か？** 編集やファイル管理は対象外です。
2. **それが無いと、文書が正しく表示されないか？** 表示の欠けや崩れの修正は歓迎します。「あると便利」なだけの機能は、基本的に見送ります。
3. **「通信なし・データ収集なし」の約束を守れるか？**
4. **設定や画面の要素を増やさないか？** 初期状態のまま正しく見えることを優先します。
5. **既存の機能が、メニューや説明のとおりに動かないのか？** それはバグなので、修正を歓迎します。

対象外の例：編集、タブ、ファイルツリー、PDF や HTML への書き出し、独自 CSS、プラグイン。

### 始める前に

- **バグ修正・小さな UX の修正・翻訳：** そのままプルリクエストを送ってください。
- **新しい機能や、文書の読み込み方・表示のしかたを変える変更：** 先に Issue を作って、内容を説明してください。マージできないプルリクエストに時間を使わずに済みます。

### 翻訳

[docs/translations/README.md](docs/translations/README.md) を参照してください。コードやマニフェストを変更せずに言語を追加できます。

### ビルドとテスト

必要なもの：Windows 11、.NET 9 SDK、Visual Studio の「C++ によるデスクトップ開発」ワークロード（WinUI 3 がビルド中に MSVC ツールを使います）。

PowerShell で実行してください。

```powershell
dotnet build MDV.sln
dotnet test tests/MDV.Core.Tests
```

アプリの実行：`cd src/MDV.App` のあと `dotnet run -c Debug`。同じ PC に Microsoft Store 版の MDV が入っていると、パッケージ ID が同じため `dotnet run` が失敗します。回避方法は [README.md](README.md#開発時の実行) を参照してください。`Package.appxmanifest` の変更はコミットしないでください。

### プルリクエスト

- 1 つのプルリクエストには 1 つの話題だけを含めてください。小さいほどレビューしやすくなります。
- 変更が**なぜ**必要か、どう確認したか（例：アプリでの再現手順）を書いてください。
- `dotnet test tests/MDV.Core.Tests` が通ることを確認してください。

### ライセンス

貢献していただいた内容は、[MIT License](LICENSE) のもとで公開されることに同意したものとみなします。
