namespace CloudManager.Host.Models.Forms;

using FluentValidation;

using Mofucat.JobScheduler;

public sealed class JobFormValidator : AbstractValidator<JobForm>
{
    public JobFormValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("ジョブ名を入力してください。");
        RuleFor(static x => x.ProfileName)
            .NotEmpty().WithMessage("プロファイルを入力してください。");
        RuleFor(static x => x.RegionName)
            .NotEmpty().WithMessage("リージョンを入力してください。");
        RuleFor(static x => x.Operation)
            .Must(static (form, operation) => JobOperationCatalog.ForService(form.ServiceType).Contains(operation))
            .WithMessage("サービスに対応した操作を選択してください。");
        RuleFor(static x => x.CronExpression)
            .NotEmpty().WithMessage("Cron 式を入力してください。")
            .Must(BeValidCron).WithMessage("Cron 式の形式が正しくありません。");

        RuleFor(static x => x.InstanceId)
            .NotEmpty().WithMessage("EC2 インスタンス ID を入力してください。")
            .When(static x => x.Operation is JobOperation.Ec2Start or JobOperation.Ec2Stop or JobOperation.Ec2Reboot);
        RuleFor(static x => x.DbInstanceId)
            .NotEmpty().WithMessage("RDS DB インスタンス ID を入力してください。")
            .When(static x => x.Operation is JobOperation.RdsStart or JobOperation.RdsStop);
        RuleFor(static x => x.Cluster)
            .NotEmpty().WithMessage("ECS クラスター名を入力してください。")
            .When(static x => x.Operation == JobOperation.EcsUpdateDesiredCount);
        RuleFor(static x => x.ServiceName)
            .NotEmpty().WithMessage("ECS サービス名を入力してください。")
            .When(static x => x.Operation == JobOperation.EcsUpdateDesiredCount);
        RuleFor(static x => x.DesiredCount)
            .GreaterThanOrEqualTo(0).WithMessage("希望タスク数は 0 以上で入力してください。")
            .When(static x => x.Operation == JobOperation.EcsUpdateDesiredCount);
        RuleFor(static x => x.FunctionName)
            .NotEmpty().WithMessage("Lambda 関数名を入力してください。")
            .When(static x => x.Operation == JobOperation.LambdaInvoke);
        RuleFor(static x => x.DistributionId)
            .NotEmpty().WithMessage("ディストリビューション ID を入力してください。")
            .When(static x => x.Operation == JobOperation.CloudFrontInvalidate);
        RuleFor(static x => x.Paths)
            .NotEmpty().WithMessage("パスを入力してください。")
            .When(static x => x.Operation == JobOperation.CloudFrontInvalidate);
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<JobForm>.CreateWithOptions((JobForm)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
    };

    private static bool BeValidCron(string expression)
    {
        try
        {
            _ = CronExpression.Parse(expression);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return false;
        }
    }
}
