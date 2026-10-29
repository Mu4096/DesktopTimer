# DesktopTimer

デスクトップの隅に常駐し、ボタン1回で開始できるタイマー（Windows専用。自分用だが、友達に zip で渡すこともできる）。

## 事前準備

1. .NET 10 SDK をインストール（PowerShell）

   ```
   winget install Microsoft.DotNet.SDK.10
   ```

   インストール後、新しいターミナルで `dotnet --version` が `10.x` を返すことを確認。

2. Git がインストールされていること（Claude Code の利用に必要）

## 普段使いの起動方法

開発用の `bin\Debug` から起動すると、起動中は exe がロックされてビルドできず、自動起動にも Debug ビルドが登録されてしまう。
普段使いは Release 版を専用フォルダに出力し、そこから起動する。

1. 動いているタイマーを終了する（タイマーの右クリック →「終了」、またはトレイメニューの「終了」）
2. Release 版を出力する（このフォルダで PowerShell を開いて実行）

   ```
   dotnet publish src/DesktopTimer -c Release -o "$env:LOCALAPPDATA\Programs\DesktopTimer"
   ```

3. 出力された exe を一度起動する

   ```
   & "$env:LOCALAPPDATA\Programs\DesktopTimer\DesktopTimer.exe"
   ```

   起動時に自動起動の登録先がこの exe になり、以後は Windows へのサインイン時に自動で起動する
   （自動起動は設定画面の「Windows の起動時に自動で起動する」で ON/OFF）。
   手動で起動したい場合は、この exe をスタートにピン留めするか、デスクトップにショートカットを作る。

- 設定と追加した音源は exe の場所に関係なく `%APPDATA%\DesktopTimer\` に保存されるので、出力先を変えても引き継がれる
- 実行には .NET 10 ランタイムが必要（.NET 10 SDK を入れていれば含まれている）

### コードを変更した後の更新

上の手順 1〜2 をやり直す（タイマーが起動中だと exe を上書きできない）。

## 他の PC で使う

.NET ランタイムを含めた版を出力し、フォルダごと持っていく（持っていった先の PC に .NET を入れる必要はない）。

1. このフォルダで PowerShell を開いて実行する（出力先はリポジトリの外にしておく。中に出すと git の未追跡ファイルになる）

   ```
   dotnet publish src/DesktopTimer -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o C:\work\creative\DesktopTimer-portable
   ```

   普段使いの exe とは出力先が別なので、動いているタイマーを終了する必要はない。

2. 出力された `DesktopTimer.exe` を USB メモリやクラウドドライブで持っていき、相手の PC の `%LOCALAPPDATA%\Programs\DesktopTimer` に置く
3. `DesktopTimer.exe` を一度起動する（自動起動の登録先がこの exe になる）

- 出力されるのは `DesktopTimer.exe`（.NET ランタイムを含むため約 170MB）と `DesktopTimer.pdb`。`.pdb` はデバッグ用なので持っていかなくても動く
- 初回起動で SmartScreen の警告（「Windows によって PC が保護されました」）が出たら、「詳細情報」→「実行」で進める
- 設定と音源も引き継ぐ場合は、両方の PC でタイマーを終了してから `%APPDATA%\DesktopTimer\` を相手の PC の同じ場所へコピーする。モニター構成が違うと、ウィンドウ位置は画面内に収まるよう調整される
- 更新するときは、相手の PC でタイマーを終了してから手順 1〜2 をやり直す

## 友達に渡す

`tools/make-dist.ps1` で配布用の zip を作る。中身は .NET ランタイムを含めた `DesktopTimer.exe`（圧縮済み）と、受け取った人向けの説明書 `はじめにお読みください.txt`（原本は `tools/dist/`）。

1. このフォルダで PowerShell を開いて実行する

   ```
   powershell -ExecutionPolicy Bypass -File tools\make-dist.ps1
   ```

   リポジトリの隣の `DesktopTimer-dist\DesktopTimer_YYYY-MM-DD.zip`（約 70MB）ができる。出力先は `-OutDir` で変えられる。
   普段使いの exe とは出力先が別なので、動いているタイマーを終了する必要はない。

2. zip を LINE やクラウドドライブなどで渡す。展開・起動・削除の手順は説明書に書いてある

- 署名をしていないので、相手の PC では初回起動時に SmartScreen の警告が出る（説明書に進め方を書いてある）
- 操作や設定項目を変えたら、説明書の該当箇所も合わせて直す

## 開発中の実行

```
dotnet build
dotnet test
dotnet run --project src/DesktopTimer -- --short
```

- `--short` は Debug ビルド専用の動作確認用オプション。プリセットと警告色の時間を「分」ではなく「秒」として扱う（10分 → 10秒）
- 多重起動防止があるため、普段使いのタイマーが起動していると `dotnet run` 側はすぐ終了する。確認前に普段使いの方を終了しておく

## 基本操作

| 操作 | 方法 |
|---|---|
| 開始 | プリセットボタンをクリック、または `Ctrl+Alt+1` / `Ctrl+Alt+2` |
| 時間を指定して開始 | 右クリック →「タイマー（時間指定）...」。時・分・秒を入れて Enter（↑↓キー・ホイールで増減、Esc で閉じる） |
| アラーム | 右クリック →「アラーム（時間指定）...」。時・分（24時間制）を入れて Enter。過ぎた時刻なら翌日に鳴る。待っている間は残り時間を表示し、キャンセルはタイマーと同じ |
| カウントアップ | 右クリック →「カウントアップ開始」。止めるときは右クリック →「停止」、`Ctrl+Alt+0`、または本体を1秒長押し。止めた時間はクリックで閉じるまで表示 |
| キャンセル / 時間切れの解除 | 右クリック →「キャンセル」、`Ctrl+Alt+0`、トレイメニュー、本体を1秒長押し（中央に出るゲージが一周したらキャンセル）。時間切れ中はクリックでも解除 |
| 移動 | 上端のグリップ（⋮⋮⋮⋮）をドラッグ |
| サイズ変更 | マウスを乗せると出る右下のハンドルをドラッグ、または設定画面のスライダー |
| 設定 | グリップ右端の ⚙、右クリック →「設定...」、トレイメニュー |
| 表示 / 非表示 | トレイアイコンのダブルクリック、またはトレイメニュー |

ホットキーは設定画面で変更できる。

## Claude Code での開始手順

1. この zip を任意の場所に解凍
2. 解凍した `DesktopTimer` フォルダでターミナルを開き、`claude` を起動
3. 最初のメッセージとして次を送る：

   ```
   CLAUDE.md と docs/spec.md を読んで、ステップ1から実装を始めて
   ```

以降は Claude Code がステップごとに確認を求めてくるので、動作確認して OK を返す。
v1 の実装は完了済み。以降の変更内容は `docs/changelog/` に1変更1ファイルで記録する（運用ルールは `CLAUDE.md`）。

## 構成

| ファイル | 内容 |
|---|---|
| `src/DesktopTimer/` | アプリ本体（C# / WPF / .NET 10） |
| `tests/DesktopTimer.Tests/` | 単体テスト（xUnit） |
| `DesktopTimer.slnx` | ソリューション |
| `CLAUDE.md` | Claude Code への作業指示（自動で読み込まれる） |
| `docs/spec.md` | 仕様書 |
| `docs/changelog/` | 変更ログ（初回実装の記録は `2026-10-13_初回実装.md`） |
| `.claude/settings.json` | ビルド・実行・git コマンドの事前許可 |
