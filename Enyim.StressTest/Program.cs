using Enyim.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Enyim.StressTest
{

    class Foo
    {
        public int[] Numbers { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
    }

    //From https://github.com/ZeekoZhu/memcachedcore-stress
    class Program
    {
        private static IMemcachedClient _memcachedClient;
        private static int _runTimes = 100000;
        private static readonly string _cacheKey = "enyim-stress-test";
        private static ILogger _logger;

        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(_ => _.AddJsonFile("appsettings.json", true))
                .ConfigureLogging(_ => _.AddConsole())
                .ConfigureServices(_ => _.AddEnyimMemcached())
                .Build();

            _memcachedClient = host.Services.GetRequiredService<IMemcachedClient>();
            _logger = host.Services.GetRequiredService<ILogger<Program>>();
            var runTimes = host.Services.GetRequiredService<IConfiguration>().GetValue<int?>("RunTimes");
            _runTimes = runTimes.HasValue ? runTimes.Value : _runTimes;

            await Run();
        }

        static async Task TrySingle()
        {
            await _memcachedClient.SetAsync(_cacheKey, new Foo(), 36000);
            var test = await _memcachedClient.GetValueAsync<Foo>(_cacheKey);
            Console.WriteLine("Single Run: {0}", test.DateTime);
        }

        static async Task RunSync(int cnt)
        {
            Console.WriteLine("Use Get");
            await TrySingle();
            var sw = Stopwatch.StartNew();
            var errCnt = 0;
            var tasks = Enumerable.Range(0, cnt)
                    .Select(i => Task.Run(() =>
                    {

                        try
                        {
                            var foo = _memcachedClient.Get<Foo>(_cacheKey);
                            if (foo == null)
                            {
                                Interlocked.Increment(ref errCnt);
                            }
                        }
                        catch (Exception e)
                        {
                            Interlocked.Increment(ref errCnt);
                        }
                    }));
            await Task.WhenAll(tasks);
            sw.Stop();

            Thread.Sleep(TimeSpan.FromSeconds(3));
            await TrySingle();
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Error Cnt: {errCnt}");
            Console.WriteLine($"Avg: {Convert.ToDouble(sw.ElapsedMilliseconds) / Convert.ToDouble(cnt)}ms");
        }

        static async Task RunAsync(int cnt)
        {
            Console.WriteLine("Use GetValueAsync");
            await TrySingle();
            var sw = Stopwatch.StartNew();
            var obj = new object();
            var errCnt = 0;
            var tasks = Enumerable.Range(0, cnt)
                    .Select(i => Task.Run(async () =>
                    {
                        try
                        {
                            var foo = await _memcachedClient.GetValueAsync<Foo>(_cacheKey);
                            if (foo == null)
                            {
                                _logger.LogError("GetValueAsync return null");
                                Interlocked.Increment(ref errCnt);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Exception on GetValueAsync");
                            Interlocked.Increment(ref errCnt);
                        }
                    }));
            await Task.WhenAll(tasks);
            sw.Stop();
            Thread.Sleep(TimeSpan.FromSeconds(3));
            await TrySingle();
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Error Cnt: {errCnt}");
            Console.WriteLine($"Avg: {Convert.ToDouble(sw.ElapsedMilliseconds) / Convert.ToDouble(cnt)}ms");
        }

        static async Task Run()
        {
            var cnt = _runTimes;
            await RunAsync(cnt);
            //await RunSync(cnt);
        }
    }
}
