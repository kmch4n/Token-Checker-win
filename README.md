# Token Checker for Windows

Claude Code と Codex CLI の使用率をタスクバー上にリアルタイム表示する Windows アプリ。

---

## 動作要件

| 要件 | バージョン |
|------|-----------|
| OS | Windows 10 / 11 |
| .NET | 8.0 以上（SDK または Runtime） |
| Claude Code CLI | `claude login` 済み |
| Codex CLI（任意） | `npm i -g @openai/codex` でインストール済み |

---

## 機能

### タスクバーウィジェット
- Claude / Codex の 5 時間使用率をバーグラフでタスクバー上に常時表示
- 使用率に応じて色が変化（緑 → 黄 → 赤）
- 左端・右端への配置切替対応
- マルチモニター対応（モニターごとにウィジェットを表示）

### トレイアイコン
- 使用率をアイコンのバーグラフで可視化
- ホバーでツールチップに数値表示
- 右クリックメニューから操作可能

### 詳細ポップアップ（クリックで開閉）
- Claude・Codex それぞれの 5 時間・週次使用率と進捗バーを表示
- リセットまでの残り時間を表示（1 分以内は「まもなくリセット」）
- 画面外クリックで自動クローズ

### その他
| 機能 | 説明 |
|------|------|
| 自動ポーリング | 2 分 / 5 分 / 10 分の間隔で自動更新 |
| ログイン時起動 | レジストリ経由で自動起動の ON/OFF |
| 再ログイン | ポップアップ内のボタンから `claude auth login` / `codex login` を起動 |
| ポップアップ透明度 | 75% / 55% / 35% / 15% / 不透明 から選択 |
| 多重起動防止 | Mutex により同一インスタンスを 1 つのみ許可 |

---

## ビルド方法

```powershell
cd D:\Token-Checker-win
dotnet build TokenChecker.sln -c Release
```

出力先: `TokenChecker\bin\Release\net8.0-windows\TokenChecker.exe`

### 単一ファイルに発行する場合

```powershell
dotnet publish TokenChecker\TokenChecker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\
```

出力先: `publish\TokenChecker.exe`（.NET ランタイムを同梱した単体 exe）

---

## 使い方

1. `TokenChecker.exe` を起動するとタスクバー上にウィジェットが表示される
2. ウィジェットまたはトレイアイコンをクリックすると詳細ポップアップが開く
3. ポップアップ内の各種設定はリアルタイムで反映・保存される

### トレイアイコン右クリックメニュー

| 項目 | 動作 |
|------|------|
| 詳細を表示/非表示 | ポップアップの開閉 |
| 今すぐ更新 | 即時データ取得 |
| モニター切替 | 表示モニターを順番に切替 |
| 終了 | アプリを終了 |

---

## 設定ファイル

設定は `%AppData%\TokenChecker\` 以下に自動保存されます。

| ファイル | 内容 |
|---------|------|
| `settings.json` | ポーリング間隔・ウィジェット配置・透明度・モニター |
| `claude-usage-cache.json` | 最後に取得した Claude 使用率のキャッシュ |
| `claude-polling-state.json` | レート制限後の再試行タイミング管理 |

---

## トークン取得の仕組み

### Claude

以下の順で OAuth アクセストークンを取得します。

1. **Windows 資格情報マネージャー**（`Claude Code-credentials` / `Claude Code-credentials/{username}` / `Claude Code/{username}`）
2. **ファイルフォールバック**
   - `%USERPROFILE%\.claude\.credentials.json`
   - `%USERPROFILE%\.claude\credentials.json`
   - `%AppData%\Claude\credentials.json`

取得したトークンで `https://api.anthropic.com/api/oauth/usage` を呼び出し、使用率を取得します。

### Codex

以下の順で `codex` 実行ファイルを探し、`codex app-server` を子プロセスとして起動。JSON-RPC (`account/rateLimits/read`) で使用率を取得します。

1. `%AppData%\npm\codex.cmd`
2. `%ProgramFiles%\nodejs\codex.cmd`
3. PATH 上の `codex`（拡張子 `.exe` / `.cmd` / `.bat` のみ許可）

---

## macOS 版との主な違い

| macOS | Windows |
|-------|---------|
| Keychain | Windows 資格情報マネージャー + ファイルフォールバック |
| MenuBarExtra | タスクバーウィジェット（WPF） + NotifyIcon |
| SwiftUI popover | WPF ポップアップウィンドウ |
| SMAppService | `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` |
| osascript → Terminal | `cmd.exe /k` |
| SF Symbols | Unicode 文字（✦ ▶） |

---

## アーキテクチャ

```
TokenChecker/
├── App.xaml.cs                  # エントリポイント・トレイ・ウィジェット管理
├── Models/
│   ├── Usage.cs                 # RateLimit / UsageSnapshot データモデル
│   └── DomainError.cs           # エラー種別
├── Providers/
│   ├── ClaudeUsageProvider.cs   # Claude 使用率取得
│   └── CodexUsageProvider.cs    # Codex 使用率取得
├── Services/
│   ├── AnthropicUsageApiClient.cs  # Anthropic API クライアント
│   ├── CodexAppServerClient.cs     # Codex JSON-RPC クライアント
│   ├── WindowsTokenSource.cs       # 資格情報マネージャー / ファイル読み取り
│   └── StartupManager.cs           # スタートアップ登録（レジストリ）
├── ViewModels/
│   └── UsageViewModel.cs        # ポーリング・設定管理
├── Views/
│   ├── TaskbarWidget.xaml       # タスクバーウィジェット
│   ├── UsagePopupWindow.xaml    # 詳細ポップアップ
│   └── LoginWindow.xaml         # 再ログインダイアログ
└── Utilities/
    ├── PollingInterval.cs       # ポーリング間隔定義
    ├── PopupTransparency.cs     # 透明度定義
    ├── WidgetPlacement.cs       # ウィジェット配置定義
    ├── TaskbarPosition.cs       # タスクバー位置検出
    ├── TrayIconRenderer.cs      # トレイアイコン描画
    └── WindowEffects.cs         # ウィンドウ効果
```
