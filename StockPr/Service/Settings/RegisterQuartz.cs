using Quartz;
using StockPr.Jobs;

namespace StockPr.Service.Settings
{
    public static class RegisterQuartz
    {
        public static void AddQuartzJobs(this IServiceCollection services)
        {
            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();

                // 1. BaoCaoPhanTichJob: Every 15 minutes
                var bcptKey = new JobKey("BaoCaoPhanTichJob");
                q.AddJob<BaoCaoPhanTichJob>(opts => opts.WithIdentity(bcptKey));
                q.AddTrigger(opts => opts
                    .ForJob(bcptKey)
                    .WithIdentity("BaoCaoPhanTichJob-trigger")
                    .WithCronSchedule("0 0/15 * * * ?"));

                // 2. F319ScoutJob: Every 5 minutes (shifted)
                var f319Key = new JobKey("F319ScoutJob");
                q.AddJob<F319ScoutJob>(opts => opts.WithIdentity(f319Key));
                q.AddTrigger(opts => opts
                    .ForJob(f319Key)
                    .WithIdentity("F319ScoutJob-trigger")
                    .WithCronSchedule("0 5/5 * * * ?"));

                // 3. NewsCrawlerJob: Every 30 minutes (shifted)
                var newsKey = new JobKey("NewsCrawlerJob");
                q.AddJob<NewsCrawlerJob>(opts => opts.WithIdentity(newsKey));
                q.AddTrigger(opts => opts
                    .ForJob(newsKey)
                    .WithIdentity("NewsCrawlerJob-trigger")
                    .WithCronSchedule("0 10/15 * * * ?"));

                // 4. PortfolioJob: Monthly 1, at 8:00
                var portfolioKey = new JobKey("PortfolioJob");
                q.AddJob<PortfolioJob>(opts => opts.WithIdentity(portfolioKey));
                q.AddTrigger(opts => opts
                    .ForJob(portfolioKey)
                    .WithIdentity("PortfolioJob-trigger")
                    .WithCronSchedule("0 0 8 1 * ?"));

                //// 5. AnalysisRealtimeJob: 9:15-11:30, 13:15-14:30 (Every 15m)
                //var realtimeKey = new JobKey("AnalysisRealtimeJob");
                //q.AddJob<AnalysisRealtimeJob>(opts => opts.WithIdentity(realtimeKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(realtimeKey)
                //    .WithIdentity("AnalysisRealtimeJob-trigger")
                //    .WithCronSchedule("0 15,30,45 9,10,13,14 ? * MON-FRI"));
                //q.AddTrigger(opts => opts
                //    .ForJob(realtimeKey)
                //    .WithIdentity("AnalysisRealtimeJob-morning-trigger")
                //    .WithCronSchedule("0 0,15,30 11 ? * MON-FRI"));

                //// 6. EODStatsJob: 15:00
                //var eodKey = new JobKey("EODStatsJob");
                //q.AddJob<EODStatsJob>(opts => opts.WithIdentity(eodKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(eodKey)
                //    .WithIdentity("EODStatsJob-trigger")
                //    .WithCronSchedule("0 0 15 ? * MON-FRI"));

                //// 7. MorningSetupJob: 8:00 (Trade days)
                //var morningKey = new JobKey("MorningSetupJob");
                //q.AddJob<MorningSetupJob>(opts => opts.WithIdentity(morningKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(morningKey)
                //    .WithIdentity("MorningSetupJob-trigger")
                //    .WithCronSchedule("0 0 8 ? * MON-FRI"));

                //// 8. EPSRankJob: 23:00
                //var epsKey = new JobKey("EPSRankJob");
                //q.AddJob<EPSRankJob>(opts => opts.WithIdentity(epsKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(epsKey)
                //    .WithIdentity("EPSRankJob-trigger")
                //    .WithCronSchedule("0 0 23 * * ?"));

                //// 9. TraceGiaJob: 9h, 13h, 17h
                //var traceKey = new JobKey("TraceGiaJob");
                //q.AddJob<TraceGiaJob>(opts => opts.WithIdentity(traceKey).StoreDurably());
                //q.AddTrigger(opts => opts.ForJob(traceKey).WithIdentity("TraceGiaJob-9h-trigger").UsingJobData("IsEndOfDay", false).WithCronSchedule("0 0 9 * * ?"));
                //q.AddTrigger(opts => opts.ForJob(traceKey).WithIdentity("TraceGiaJob-13h-trigger").UsingJobData("IsEndOfDay", false).WithCronSchedule("0 0 13 * * ?"));
                //q.AddTrigger(opts => opts.ForJob(traceKey).WithIdentity("TraceGiaJob-17h-trigger").UsingJobData("IsEndOfDay", true).WithCronSchedule("0 0 17 * * ?"));

                //// 10. TongCucThongKeJob: Weekly Saturday 9:00
                //var tctkKey = new JobKey("TongCucThongKeJob");
                //q.AddJob<TongCucThongKeJob>(opts => opts.WithIdentity(tctkKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(tctkKey)
                //    .WithIdentity("TongCucThongKeJob-trigger")
                //    .WithCronSchedule("0 0 9 ? * SAT"));

                //// 11. TuDoanhJob: Evening stats every 30m
                //var tudoanhKey = new JobKey("TuDoanhJob");
                //q.AddJob<TuDoanhJob>(opts => opts.WithIdentity(tudoanhKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(tudoanhKey)
                //    .WithIdentity("TuDoanhJob-trigger")
                //    .WithCronSchedule("0 0/30 19-23 ? * MON-FRI"));

                //// 12. ChartStatsJob: Evening stats every 30m
                //var chartStatsKey = new JobKey("ChartStatsJob");
                //q.AddJob<ChartStatsJob>(opts => opts.WithIdentity(chartStatsKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(chartStatsKey)
                //    .WithIdentity("ChartStatsJob-trigger")
                //    .WithCronSchedule("0 10/30 19-23 ? * MON-FRI"));

                //// 13. Chart4UJob: Evening stats every 30m
                //var chart4uKey = new JobKey("Chart4UJob");
                //q.AddJob<Chart4UJob>(opts => opts.WithIdentity(chart4uKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(chart4uKey)
                //    .WithIdentity("Chart4UJob-trigger")
                //    .WithCronSchedule("0 20/30 19-23 ? * MON-FRI"));

                //// 14. ForexMorningJob: 11:30 Trade days
                //var forexKey = new JobKey("ForexMorningJob");
                //q.AddJob<ForexMorningJob>(opts => opts.WithIdentity(forexKey));
                //q.AddTrigger(opts => opts
                //    .ForJob(forexKey)
                //    .WithIdentity("ForexMorningJob-trigger")
                //    .WithCronSchedule("0 30 11 ? * MON-FRI"));
            });

            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        }
    }
}
