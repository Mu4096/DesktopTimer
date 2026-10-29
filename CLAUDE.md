# DesktopTimer — Claude Code 向け作業指示

## このリポジトリについて

Windows 専用のデスクトップ常駐タイマー（C# / WPF / .NET 10）。v1 の実装（仕様書 14章のステップ1〜10）は完了済みで、現在は**保守・機能追加フェーズ**。

- 仕様の正は `docs/spec.md`。変更に関係する章は作業前に必ず読む
- 変更の履歴は `docs/changelog/` にある。セッション開始時は最新数件を読んで直近の経緯を把握する
- 初回実装の設計判断・未解決事項は `docs/changelog/2026-10-13_初回実装.md` にまとまっている。既存コードの「なぜこうなっているか」はまずここを探す

## 変更の進め方

1. 依頼内容を確認し、仕様と矛盾する・仕様に記載のない挙動を決める必要がある場合は、**実装前にユーザーに確認**する
2. 実装する
3. `dotnet build` が警告・エラーなしで通ること、`dotnet test` が全件合格することを確認
4. `docs/changelog/` に変更ログを新規作成する（下記「変更ログ」）
5. 仕様が変わった場合は `docs/spec.md` も更新する（ログだけに書いて仕様書と食い違わせない）
6. ユーザーに動作確認してほしい点を箇条書きで伝える
7. ユーザーの確認が取れたら git commit（メッセージは日本語、`<種別>: 内容` の形式。種別は `feat` / `fix` / `refactor` / `docs` など）。ログファイルも同じコミットに含める

大きな変更は複数ステップに分け、ステップごとに 3〜7 を繰り返してよい（ログは1変更につき1ファイルにまとめ、ステップごとに追記する）。

## 変更ログ（docs/changelog/）

- **変更ごとに新しいファイルを作る**。既存のログファイルは書き換えない（誤記の訂正を除く）
- ファイル名：`YYYY-MM-DD_変更内容.md`（日付は作業日、変更内容は日本語で短く。例：`2026-10-20_一時停止機能の追加.md`）
  - 同じ日に同名になる場合は末尾に `_2` などを付ける
  - ファイル名に使えない文字（`\ / : * ? " < > |`）と空白は使わない
- 書式：

```markdown
# 変更内容のタイトル

- 日付：YYYY-MM-DD
- 種別：機能追加 / 不具合修正 / 改善 / リファクタリング / ドキュメント
- 関連する仕様書の章：（例：4.5, 10.1。なければ「なし」）

## 背景・目的

なぜ変更したか（ユーザーの要望・不具合の症状など）

## 変更内容

- 何をどう変えたか（主なファイル・クラス名を含める）
- 仕様に記載のない判断をした場合はその旨と理由

## 確認

- ビルド・テストの結果、自動確認した内容、ユーザーに確認してもらった内容
- 未確認・残課題があれば明記
```

## プロジェクト構成

```
DesktopTimer.slnx
src/DesktopTimer/
  App.xaml(.cs)            起動・多重起動防止・トレイ・表示制御
  Themes/                  配色・フォント（Theme）、共通コントロール（Controls）、タイマー本体（TimerStyles）
  Views/                   TimerWindow, SettingsWindow
  ViewModels/              状態遷移と表示用プロパティ
  Services/                TimerService, SoundService, HotkeyService,
                           SettingsService, FullscreenWatcher, MonitorHelper,
                           AutoStartService, TrayIconService など
  Models/                  AppSettings, TimerState など
  Interop/                 Win32 P/Invoke 定義
  Assets/                  アイコン、埋め込みビープWAV
tests/DesktopTimer.Tests/  xUnit（TimerService・時間書式・配置計算・全画面判定）
tools/make-dist.ps1        友達配布用 zip の作成（説明書の原本は tools/dist/）
docs/spec.md               仕様書（正）
docs/changelog/            変更ログ（1変更1ファイル）
README.md                  使い方・ビルド方法
```

## コーディング方針

- MVVM は軽量に。外部 MVVM フレームワークは導入しない（必要なら CommunityToolkit.Mvvm のみ可）
- 追加パッケージは NAudio 以外は原則使わない。必要になったらユーザーに確認
- Win32 API 呼び出しは `Interop/` に集約。計算ロジックは純粋関数に分離してテストを書く（PlacementCalculator, FullscreenWatcher.IsFullscreen と同様）
- 残り時間は開始時刻との差分で計算する（仕様書 11章）。タイマーイベントの回数を数えない
- ユーザー向け文字列は日本語
- 設定・音源の保存先は `%APPDATA%\DesktopTimer\`。設定項目を追加したら `AppSettings.Normalize()` の補正と、古い settings.json がそのまま読めることを確認する

## ビルド・実行

```
dotnet build
dotnet test
dotnet run --project src/DesktopTimer
```

- Debug ビルドで `--short` を付けて起動すると、プリセット・警告閾値を「秒」として扱う（動作確認用）
- 自動テストは状態遷移・時間計算・配置計算などのロジックにのみ書けば十分。UI は手動確認とする

## 動作確認時の注意

- ユーザーのタイマーが起動中だと、確認用インスタンスは多重起動防止で即終了する。自動操作の前にウィンドウが本当に確認用インスタンスのものか確かめ、見つからない場合は操作せず中断する
- 自動起動（HKCU\...\Run）や `%APPDATA%\DesktopTimer\settings.json` を確認で書き換えた場合は、終了後に確認前の状態へ戻す
