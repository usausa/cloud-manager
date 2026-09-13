# CloudManager

AWS リソースを参照・操作する Blazor Server 製の管理 Web UI。
`~/.aws/credentials` / `~/.aws/config` のプロファイルを切り替えながら、EC2 / RDS / S3 などの状態確認と起動・停止などの操作、cron によるジョブ実行ができる。

## 構成

| プロジェクト | 役割 |
| --- | --- |
| `src/CloudManager.Core` | AWS サービス操作 (`Services/Aws`)、ジョブ定義・実行履歴の永続化 (`Accessors`, `Services`)、モデル |
| `src/CloudManager.Host` | Blazor Server ホスト。画面 (`Components`)、ジョブスケジューラ (`Infrastructure/Jobs`, `Workers`)、S3 ダウンロード API (`Endpoints`) |
| `tests/CloudManager.UnitTests` | 単体テスト (xUnit v3 / bUnit) |
| `tests/CloudManager.IntegrationTests` | ホスト起動を伴う統合テスト (SQLite 実体を使用) |
| `tests/CloudManager.E2ETests` | Playwright によるブラウザテスト |

## 前提条件

- .NET 10 SDK
- `~/.aws/credentials` / `~/.aws/config` に認証情報とリージョンが設定済みであること
- E2E テストを実行する場合は Playwright のブラウザ (`pwsh tests/CloudManager.E2ETests/bin/Debug/net10.0/playwright.ps1 install`)

### ~/.aws/credentials

```ini
[default]
aws_access_key_id = AKIAIOSFODNN7EXAMPLE
aws_secret_access_key = wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
```

### ~/.aws/config

```ini
[default]
region = ap-northeast-1
```

## 起動

```bash
dotnet run --project src/CloudManager.Host
```

`http://localhost:8080` で開く。

## 設定 (`appsettings.json`)

| キー | 説明 |
| --- | --- |
| `ConnectionStrings:Default` | ジョブ定義・実行履歴を保存する SQLite の接続文字列 (テーブルは起動時に作成) |
| `Aws:DefaultProfile` | 起動時に選択するプロファイル。画面の「設定」で切り替え可能 |
| `Aws:DefaultRegion` | 起動時に選択するリージョン |
| `Job:LogRetentionCountPerJob` | ジョブごとに保持する実行履歴の件数 |
| `Log` / `Profiler` / `Serilog` | HTTP ログ・SQL ログ・ログ出力先 |

## 対応サービス

| サービス | 参照 | 操作 |
| --- | --- | --- |
| EC2 | ✅ | 起動 / 停止 / 再起動 / 終了 / SSM Run Command |
| EBS | ✅ | アタッチ / デタッチ / スナップショット |
| ECS | ✅ | 希望タスク数変更 / 強制再デプロイ / タスク一覧 |
| Lambda | ✅ | 実行 / 環境変数 / エイリアス / 同時実行数 / DLQ |
| ECR | ✅ | イメージ一覧 / 削除 |
| S3 | ✅ | アップロード / ダウンロード / 削除 / プレビュー / コピー・移動 / バージョン / ライフサイクル / パブリックアクセス |
| RDS | ✅ | 起動 / 停止 / 再起動 / スナップショット作成・復元・削除 / パラメータグループ / イベント / Aurora フェイルオーバー |
| DynamoDB | ✅ | スキャン / PITR / TTL |
| VPC | ✅ | — |
| Elastic IP | ✅ | 関連付け / 解除 |
| CloudFront | ✅ | キャッシュ無効化 |
| ELB | ✅ | ターゲットヘルス |
| Route 53 | ✅ | レコード追加 / 変更 / 削除 |
| ACM | ✅ | 証明書詳細 |
| API Gateway | ✅ | デプロイ |
| EventBridge | ✅ | ルール有効化 / 無効化 |
| SQS | ✅ | メッセージ送受信 / パージ |
| SNS | ✅ | メッセージ発行 |
| CloudWatch | ✅ | メトリクス / アラーム |
| CloudWatch Logs | ✅ | ログイベント / Logs Insights |
| SSM Parameter Store | ✅ | 値の参照 / 編集 |
| Secrets Manager | ✅ | 値の参照 |
| Cognito | ✅ | パスワードリセット |
| Cost | — | Pricing API による概算 |

## ジョブ

EC2 / RDS の起動・停止、ECS の希望タスク数変更、Lambda 実行、CloudFront キャッシュ無効化を cron 式で定期実行できる。
定義は SQLite に保存され、起動時にスケジューラへ登録される。実行結果は「実行履歴」から確認できる。

## テスト

各テストプロジェクトは Microsoft.Testing.Platform の実行ファイルとして動作する。

```bash
dotnet run --project tests/CloudManager.UnitTests
dotnet run --project tests/CloudManager.IntegrationTests
dotnet run --project tests/CloudManager.E2ETests
```

統合テスト・E2E テストは存在しないプロファイル名で起動するため AWS へは接続しない。
