namespace CloudManager.Services.Aws;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.S3;

public sealed class S3Service
{
    private readonly AwsClientFactory factory;

    public S3Service(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // バケット一覧を取得する。
    public async ValueTask<List<S3BucketInfo>> ListBucketsAsync()
    {
        using var s3 = factory.CreateS3Client();
        var response = await s3.ListBucketsAsync();
        return response.Buckets
            .Select(b => new S3BucketInfo(b.BucketName, b.CreationDate.GetValueOrDefault()))
            .ToList();
    }

    // 指定バケットのオブジェクト一覧を取得する(ページング対応)。
    public async ValueTask<List<S3ObjectInfo>> ListObjectsAsync(string bucketName, string? prefix)
    {
        using var s3 = factory.CreateS3Client();
        var result = new List<S3ObjectInfo>();
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                ContinuationToken = continuationToken
            };
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                request.Prefix = prefix;
            }

            var response = await s3.ListObjectsV2Async(request);

            foreach (var obj in response.S3Objects ?? [])
            {
                result.Add(new S3ObjectInfo(
                    obj.Key,
                    obj.Size.GetValueOrDefault(),
                    obj.LastModified.GetValueOrDefault(),
                    obj.StorageClass?.Value ?? "-"));
            }

            continuationToken = response.IsTruncated.GetValueOrDefault() ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return result;
    }

    // ストリームを S3 にアップロードする(TransferUtility + 進捗)。
    public async ValueTask UploadStreamAsync(string bucketName, string key, Stream stream, long totalBytes, IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken)
    {
        using var s3 = factory.CreateS3Client();
        using var transfer = new TransferUtility(s3);
        var request = new TransferUtilityUploadRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = stream,
            AutoCloseStream = false
        };

        if (progress is not null)
        {
            request.UploadProgressEvent += (_, e) =>
                progress.Report(new ProgressUpdate(totalBytes > 0 ? (double)e.TransferredBytes / totalBytes : 0, $"{e.TransferredBytes / 1024}KB / {totalBytes / 1024}KB"));
        }

        await transfer.UploadAsync(
            request,
            cancellationToken);
    }

    // S3 からバイト配列としてダウンロードする(進捗付き)。
    public async ValueTask<byte[]> DownloadBytesAsync(string bucketName, string key, IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken)
    {
        using var s3 = factory.CreateS3Client();
        var getResponse = await s3.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            },
            cancellationToken);

        using var ms = new MemoryStream();
        var totalBytes = getResponse.ContentLength;
        var buffer = new byte[81920];
        int read;
        long transferred = 0;

        await using var responseStream = getResponse.ResponseStream;
        while ((read = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            transferred += read;
            progress?.Report(new ProgressUpdate(totalBytes > 0 ? (double)transferred / totalBytes : 0, $"{transferred / 1024}KB / {totalBytes / 1024}KB"));
        }

        return ms.ToArray();
    }

    // オブジェクトのメタデータを取得する。存在しない場合は null
    public async ValueTask<S3ObjectHead?> HeadObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
    {
        using var s3 = factory.CreateS3Client();
        try
        {
            var response = await s3.GetObjectMetadataAsync(
                new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                },
                cancellationToken);
            return new S3ObjectHead(response.ContentLength, response.Headers.ContentType ?? "application/octet-stream");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // オブジェクトを出力ストリームへそのまま流す(ダウンロード配信用)。
    public async ValueTask DownloadToStreamAsync(string bucketName, string key, Stream destination, CancellationToken cancellationToken = default)
    {
        using var s3 = factory.CreateS3Client();
        using var response = await s3.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            },
            cancellationToken);
        await response.ResponseStream.CopyToAsync(destination, cancellationToken);
    }

    // オブジェクトを削除する。
    public async ValueTask DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
    {
        using var s3 = factory.CreateS3Client();
        await s3.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            },
            cancellationToken);
    }

    // 複数オブジェクトを一括削除する。
    public async ValueTask DeleteObjectsAsync(string bucketName, IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        using var s3 = factory.CreateS3Client();
        var batch = keys.ToList();
        for (var i = 0; i < batch.Count; i += 1000)
        {
            var chunk = batch.Skip(i).Take(1000).Select(k => new KeyVersion { Key = k }).ToList();
            await s3.DeleteObjectsAsync(
                new DeleteObjectsRequest
                {
                    BucketName = bucketName,
                    Objects = chunk
                },
                cancellationToken);
        }
    }

    // 指定プレフィックス配下のオブジェクトを全件削除する(進捗付き)。
    public async ValueTask DeleteObjectsByPrefixAsync(string bucketName, string prefix, IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        using var s3 = factory.CreateS3Client();
        string? continuationToken = null;
        long deleted = 0;
        do
        {
            var listResponse = await s3.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix,
                    ContinuationToken = continuationToken
                },
                cancellationToken);

            if ((listResponse.S3Objects?.Count ?? 0) == 0)
            {
                break;
            }

            var chunk = (listResponse.S3Objects ?? []).Select(o => new KeyVersion { Key = o.Key }).ToList();
            await s3.DeleteObjectsAsync(
                new DeleteObjectsRequest
                {
                    BucketName = bucketName,
                    Objects = chunk
                },
                cancellationToken);

            deleted += chunk.Count;
            progress?.Report(new ProgressUpdate(0, $"{deleted} 件削除済み"));
            continuationToken = listResponse.IsTruncated.GetValueOrDefault() ? listResponse.NextContinuationToken : null;
        }
        while (continuationToken is not null);
    }

    // バケットの公開設定を確認する。
    public async ValueTask<S3PublicAccessReport> GetBucketPublicAccessAsync(string bucketName)
    {
        using var s3 = factory.CreateS3Client();

        var pubAccess = await s3.GetPublicAccessBlockAsync(new GetPublicAccessBlockRequest { BucketName = bucketName });
        var cfg = pubAccess.PublicAccessBlockConfiguration;

        bool? isPolicyPublic = null;
        try
        {
            var policyStatus = await s3.GetBucketPolicyStatusAsync(new GetBucketPolicyStatusRequest { BucketName = bucketName });
            isPolicyPublic = policyStatus.PolicyStatus?.IsPublic;
        }
        catch (AmazonS3Exception)
        {
            /* バケットポリシーなし */
        }

        var blockAll = cfg.BlockPublicAcls.GetValueOrDefault()
            && cfg.IgnorePublicAcls.GetValueOrDefault()
            && cfg.BlockPublicPolicy.GetValueOrDefault()
            && cfg.RestrictPublicBuckets.GetValueOrDefault();
        var verdict = blockAll ? "Private" : isPolicyPublic == true ? "Public" : "Mixed";

        return new S3PublicAccessReport(
            cfg.BlockPublicAcls.GetValueOrDefault(),
            cfg.IgnorePublicAcls.GetValueOrDefault(),
            cfg.BlockPublicPolicy.GetValueOrDefault(),
            cfg.RestrictPublicBuckets.GetValueOrDefault(),
            isPolicyPublic,
            verdict);
    }

    // バージョン一覧を取得する。
    public async ValueTask<List<S3VersionInfo>> ListVersionsAsync(string bucketName, string key)
    {
        using var s3 = factory.CreateS3Client();
        var result = new List<S3VersionInfo>();
        string? keyMarker = null;
        string? versionIdMarker = null;
        do
        {
            var response = await s3.ListVersionsAsync(
                new ListVersionsRequest
                {
                    BucketName = bucketName,
                    Prefix = key,
                    KeyMarker = keyMarker,
                    VersionIdMarker = versionIdMarker
                });
            foreach (var v in (response.Versions ?? []).Where(v => v.Key == key))
            {
                result.Add(new S3VersionInfo(
                    v.VersionId,
                    v.IsLatest.GetValueOrDefault(),
                    v.LastModified.GetValueOrDefault(),
                    v.IsDeleteMarker.GetValueOrDefault() ? 0 : v.Size.GetValueOrDefault(),
                    v.IsDeleteMarker.GetValueOrDefault()));
            }

            if (response.IsTruncated.GetValueOrDefault())
            {
                keyMarker = response.NextKeyMarker;
                versionIdMarker = response.NextVersionIdMarker;
            }
            else
            {
                break;
            }
        }
        while (true);
        return [.. result.OrderByDescending(v => v.LastModified)];
    }

    // バージョンを復元する(CopyObject で同一キーに上書き)。
    public async ValueTask RestoreVersionAsync(string bucketName, string key, string versionId, CancellationToken cancellationToken = default)
    {
        using var s3 = factory.CreateS3Client();
        await s3.CopyObjectAsync(
            new CopyObjectRequest
            {
                SourceBucket = bucketName,
                SourceKey = key,
                SourceVersionId = versionId,
                DestinationBucket = bucketName,
                DestinationKey = key
            },
            cancellationToken);
    }

    // オブジェクトをコピーする。
    public async ValueTask CopyObjectAsync(string srcBucket, string srcKey, string dstBucket, string dstKey, CancellationToken cancellationToken = default)
    {
        using var s3 = factory.CreateS3Client();
        await s3.CopyObjectAsync(
            new CopyObjectRequest
            {
                SourceBucket = srcBucket,
                SourceKey = srcKey,
                DestinationBucket = dstBucket,
                DestinationKey = dstKey
            },
            cancellationToken);
    }

    // オブジェクトを移動する(コピー後削除)。
    public async ValueTask MoveObjectAsync(string srcBucket, string srcKey, string dstBucket, string dstKey, CancellationToken cancellationToken = default)
    {
        await CopyObjectAsync(srcBucket, srcKey, dstBucket, dstKey, cancellationToken);
        await DeleteObjectAsync(srcBucket, srcKey, cancellationToken);
    }

    // ライフサイクルルール一覧を取得する。
    public async ValueTask<List<S3LifecycleRuleInfo>> GetLifecycleAsync(string bucketName)
    {
        using var s3 = factory.CreateS3Client();
        try
        {
            var response = await s3.GetLifecycleConfigurationAsync(bucketName);
            return response.Configuration.Rules
                .Select(r => new S3LifecycleRuleInfo(
                    r.Id ?? string.Empty,
                    r.Filter?.LifecycleFilterPredicate is LifecyclePrefixPredicate p ? p.Prefix : string.Empty,
                    r.Status?.Value ?? string.Empty,
                    r.Transitions?.Select(t => $"{t.Days}d → {t.StorageClass?.Value}").ToList() ?? [],
                    r.Expiration != null ? $"{r.Expiration.Days}d" : null,
                    r.NoncurrentVersionExpiration != null ? $"{r.NoncurrentVersionExpiration.NoncurrentDays}d" : null))
                .ToList();
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchLifecycleConfiguration")
        {
            return [];
        }
    }

    // デリミタ付きでオブジェクト一覧を取得する(フォルダビュー用)。
    public async ValueTask<S3Listing> ListObjectsWithDelimiterAsync(string bucketName, string prefix)
    {
        using var s3 = factory.CreateS3Client();
        var commonPrefixes = new List<string>();
        var objects = new List<S3ObjectInfo>();
        string? continuationToken = null;
        do
        {
            var response = await s3.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix,
                    Delimiter = "/",
                    ContinuationToken = continuationToken
                });
            commonPrefixes.AddRange(response.CommonPrefixes ?? []);
            foreach (var obj in (response.S3Objects ?? []).Where(o => o.Key != prefix))
            {
                objects.Add(new S3ObjectInfo(
                    obj.Key,
                    obj.Size.GetValueOrDefault(),
                    obj.LastModified.GetValueOrDefault(),
                    obj.StorageClass?.Value ?? "-"));
            }

            continuationToken = response.IsTruncated.GetValueOrDefault() ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);
        return new S3Listing(commonPrefixes, objects);
    }
}
