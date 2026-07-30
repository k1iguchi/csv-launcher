# Security Policy

## Supported Versions

このリポジトリは最新の main/develop ブランチのみを対象にメンテナンスします。

## Reporting a Vulnerability

脆弱性やシークレット漏えいを発見した場合は、公開 Issue ではなく非公開チャネルで報告してください。

推奨手順:

1. 影響範囲を簡潔にまとめる
2. 再現手順を記載する
3. 秘匿情報はマスクして共有する

## Secret Handling

- `credentials.json` や `client_secret*.json` はコミット禁止
- `CsvLauncher/EmbeddedGoogleOAuth.cs` はビルド時生成物として扱い、公開リポジトリでは保持しない運用を推奨
- シークレット公開時は即時ローテーションする
