UPDATE JobDefinition
SET Name = /*@ name */'',
    Description = /*@ description */'',
    ProfileName = /*@ profileName */'',
    RegionName = /*@ regionName */'',
    ServiceType = /*@ serviceType */'',
    Operation = /*@ operation */'',
    ParametersJson = /*@ parametersJson */'',
    CronExpression = /*@ cronExpression */'',
    CronTimeZone = /*@ cronTimeZone */'',
    IsEnabled = /*@ isEnabled */1,
    UpdatedAt = /*@ updatedAt */''
WHERE Id = /*@ id */0
