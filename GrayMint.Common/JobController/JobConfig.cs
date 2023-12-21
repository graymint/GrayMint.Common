﻿namespace GrayMint.Common.JobController;

public class JobConfig
{
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(30);
    public string? Name { get; init; }
}