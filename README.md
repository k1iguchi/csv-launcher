# CSV Launcher

Windows 向けの CSV ランチャーです。
.csv を Google Drive にアップロードして Google スプレッドシートとして開きます。
CSV パスを省略して起動した場合は、空のスプレッドシートを新規作成して開きます。

## 要件

- Windows 10 / 11
- .NET SDK 8.x（ビルド時）
- Google アカウント

## ビルド

1. リポジトリ直下に credentials.json を配置
2. scripts/build.ps1 を実行

```powershell
.\scripts\build.ps1
```

成果物:
- dist/CsvLauncher.exe
- dist/setup.ps1
- dist/CsvLauncher-<Version>.zip
- dist/LICENSE
- dist/THIRD_PARTY_NOTICES.md

## 実行

```cmd
CsvLauncher.exe [--folder-id=<drive-folder-id>] [<csv-path>]
```

- csv-path 指定あり: CSV をスプレッドシート変換アップロードして開く
- csv-path 指定なし: 空のスプレッドシートを作成して開く

例:

```cmd
CsvLauncher.exe --folder-id=<drive-folder-id> sample.csv
```

```cmd
CsvLauncher.exe
```

## 関連付け

登録:

```powershell
.\dist\setup.ps1
```

解除:

```powershell
.\dist\setup.ps1 -Uninstall
```

## ドキュメント

- ビルド手順: [docs/ビルド手順書.md](docs/ビルド手順書.md)
- 要件定義: [docs/要件定義書.md](docs/要件定義書.md)
- 詳細仕様: [docs/詳細仕様書.md](docs/詳細仕様書.md)

## 注意

- credentials.json はコミットしないこと
- CsvLauncher/EmbeddedGoogleOAuth.cs は scripts/build.ps1 実行時に再生成される

## ライセンス

このプロジェクトは Apache License 2.0 で公開しています。

- ライセンス本文: [LICENSE](LICENSE)
- サードパーティライセンス: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)
