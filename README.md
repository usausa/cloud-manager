# CloudManager

A web console for browsing and operating AWS resources from your local AWS profiles.
Switch between profiles and regions, inspect EC2 / RDS / S3 and other services, run day-to-day operations, and schedule recurring jobs with cron.

![Dashboard](docs/images/dashboard.png)

## ✨ Features

| Service | View | Operations |
| --- | --- | --- |
| EC2 | ✅ | Start / Stop / Reboot / Terminate / SSM Run Command |
| EBS | ✅ | Attach / Detach / Create snapshot |
| ECS | ✅ | Change desired count / Force new deployment / List tasks |
| Lambda | ✅ | Invoke / Environment variables / Aliases / Concurrency / DLQ |
| ECR | ✅ | List images / Delete image |
| S3 | ✅ | Upload / Download / Delete / Preview / Copy & move / Versions / Lifecycle / Public access |
| RDS | ✅ | Start / Stop / Reboot / Snapshots (create, restore, delete) / Parameter groups / Events / Aurora failover |
| DynamoDB | ✅ | Scan / PITR / TTL |
| VPC | ✅ | Subnets / Security groups / Route tables / Internet gateways / NAT gateways |
| Elastic IP | ✅ | Associate / Disassociate |
| CloudFront | ✅ | Invalidate cache |
| ELB | ✅ | Target health |
| Route 53 | ✅ | Add / Update / Delete records |
| ACM | ✅ | Certificate details |
| API Gateway | ✅ | Deploy |
| EventBridge | ✅ | Enable / Disable rules |
| SQS | ✅ | Send / Receive / Purge |
| SNS | ✅ | Publish |
| CloudWatch | ✅ | Metrics / Alarms |
| CloudWatch Logs | ✅ | Log events / Logs Insights |
| SSM Parameter Store | ✅ | View / Edit values |
| Secrets Manager | ✅ | View values |
| Cognito | ✅ | Reset password |
| Cost | — | Estimates from the Pricing API |

## 🔐 Requirements

CloudManager uses the AWS profiles on the machine it runs on. Configure `~/.aws/credentials` and `~/.aws/config` before use.

```ini
# ~/.aws/credentials
[default]
aws_access_key_id = AKIAIOSFODNN7EXAMPLE
aws_secret_access_key = wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
```

```ini
# ~/.aws/config
[default]
region = ap-northeast-1
```

## 🧭 Usage

### 🔄 Profile and region

The dashboard shows the active profile and region. Open **Settings** to switch to any profile found in `~/.aws`; the change applies immediately to every page.

### 🖥️ Browsing and operating resources

Each service has its own page reachable from the navigation menu or the dashboard. Lists can be filtered and refreshed, and row actions open dialogs for operations.
Destructive operations (terminate, delete, force failover, ...) ask for confirmation and, where appropriate, require the resource identifier to be typed in. Long-running operations show progress while the service reaches the target state.

### 📦 S3

Buckets and objects can be browsed by prefix. Objects can be uploaded from the browser, downloaded, previewed (images and text), copied or moved, and their versions restored. Bucket-level lifecycle rules and public access settings are available from the bucket list.

### ⏰ Scheduled jobs

**Jobs** lets you schedule recurring operations with a cron expression:

- Start / Stop / Reboot an EC2 instance
- Start / Stop an RDS instance
- Change the desired count of an ECS service
- Invoke a Lambda function
- Invalidate a CloudFront distribution

Each job runs with its own profile and region, and the cron expression can be evaluated in UTC or local time. Jobs can also be executed immediately, and every run is recorded in **Job History** with its result and error details.

## ⚙️ Configuration

| Key | Description |
| --- | --- |
| `Aws:DefaultProfile` | Profile selected at startup |
| `Aws:DefaultRegion` | Region selected at startup |
| `Job:LogRetentionCountPerJob` | Number of history entries kept per job |
| `ConnectionStrings:Default` | SQLite database that stores job definitions and history (created automatically) |
