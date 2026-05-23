# Token Checker for Windows

タスクバーに Claude Code と Codex の使用率を常時表示する Windows アプリ。

macOS 版 [Token Checker](https://github.com/satonico/Token-Checker) の Windows 移植版。

## 動作要件

- Windows 10 / 11
- .NET 8.0 SDK 以上（ビルドに必要）
- Claude Code CLI（`claude login` 済み）
- Codex CLI（`npm i -g @openai/codex` 後、`codex login` 済み）

どちらか一方のみでも動作する。

## インストール

### 1. .NET 8 SDK をインストール

`dotnet` コマンドが未インストールの場合（`用語 'dotnet' は認識されません` というエラーが出る場合）、先に SDK を入れる。

```powershell
winget install Microsoft.DotNet.SDK.8
```

インストール後、**PowerShell を一度閉じて開き直す**（PATH を反映させるため）。`dotnet --version` で `8.x.x` が表示されれば成功。

> winget が使えない場合は [.NET 8 SDK の公式ダウンロードページ](https://dotnet.microsoft.com/download/dotnet/8.0) からインストーラーを入手する。

### 2. ビルド

```powershell
git clone https://github.com/satonico/Token-Checker-win.git
cd Token-Checker-win
dotnet build TokenChecker.sln -c Release
```

`TokenChecker\bin\Release\net8.0-windows\TokenChecker.exe` を起動する。

## 使い方

1. 事前にターミナルでログインしておく

```powershell
claude login
codex login
```

2. `TokenChecker.exe` を起動するとタスクバー上にウィジェットが表示される
3. ウィジェットをクリックするとポップアップで詳細（使用率・リセット時間・更新間隔設定）が開く

## アンインストール

```powershell
# 自動起動の登録を削除
reg delete "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v TokenChecker /f

# 設定ファイルを削除
Remove-Item "$env:APPDATA\TokenChecker" -Recurse -Force
```

## 免責事項

本ソフトウェアは現状有姿 (as-is) で提供される。利用に起因するいかなる損害についても作者は責任を負わない。
