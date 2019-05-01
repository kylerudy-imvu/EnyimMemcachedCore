using Enyim.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public DateTime DateTime { get; set; }
    }

    //From https://github.com/ZeekoZhu/memcachedcore-stress
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddEnyimMemcached(options => options.AddServer("memcached", 11211));
            services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Debug));
            await Run(services.BuildServiceProvider());
        }

        static async Task TrySingle(IServiceProvider sp)
        {
            using (var scope = sp.CreateScope())
            {
                var memcached = scope.ServiceProvider.GetService<IMemcachedClient>();
                memcached.Add("test", new Foo(), Int32.MaxValue);
                var test = await memcached.GetValueAsync<Foo>("test");
                Console.WriteLine("Single Run: {0}", test.DateTime);
            }
        }

        static async Task RunSync(int cnt, IServiceProvider sp)
        {
            Console.WriteLine("Use Get");
            await TrySingle(sp);
            var sw = Stopwatch.StartNew();
            var obj = new object();
            var errCnt = 0;
            var tasks =
                Enumerable.Range(0, cnt)
                    .Select(i => Task.Run(() =>
                    {
                        using (var scope = sp.CreateScope())
                        {
                            var provider = scope.ServiceProvider;
                            var memcached = provider.GetService<IMemcachedClient>();
                            try
                            {
                                var foo = memcached.Get<Foo>("test");
                                if (foo == null)
                                {
                                    throw new Exception();
                                }
                            }
                            catch (Exception e)
                            {
                                lock (obj)
                                {
                                    errCnt += 1;
                                }

                                //                                Console.WriteLine("Task: {0} Exception: {1}", i, e.GetType().FullName);
                            }
                        }
                    }));
            await Task.WhenAll(tasks);
            sw.Stop();
            Thread.Sleep(TimeSpan.FromSeconds(3));
            await TrySingle(sp);
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Error Cnt: {errCnt}");
            Console.WriteLine($"Avg: {Convert.ToDouble(sw.ElapsedMilliseconds) / Convert.ToDouble(cnt)}ms");
        }

        static async Task RunAsync(int cnt, IServiceProvider sp)
        {
            Console.WriteLine("Use GetValueAsync");
            await TrySingle(sp);
            var sw = Stopwatch.StartNew();
            var obj = new object();
            var errCnt = 0;
            var tasks =
                Enumerable.Range(0, cnt)
                    .Select(i => Task.Run(async () =>
                    {
                        using (var scope = sp.CreateScope())
                        {
                            var provider = scope.ServiceProvider;
                            var memcached = provider.GetService<IMemcachedClient>();
                            try
                            {
                                var foo = await memcached.GetValueAsync<Foo>("test");
                                if (foo == null)
                                {
                                    throw new Exception();
                                }
                            }
                            catch (Exception e)
                            {
                                lock (obj)
                                {
                                    errCnt += 1;
                                }

                                //Console.WriteLine("Task: {0} Exception: {1}", i, e.GetType().FullName);
                            }
                        }
                    }));
            await Task.WhenAll(tasks);
            sw.Stop();
            Thread.Sleep(TimeSpan.FromSeconds(3));
            await TrySingle(sp);
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Error Cnt: {errCnt}");
            Console.WriteLine($"Avg: {Convert.ToDouble(sw.ElapsedMilliseconds) / Convert.ToDouble(cnt)}ms");
        }

        static async Task Run(IServiceProvider sp)
        {
            var cnt = 1000000;
            await RunAsync(cnt, sp);
            await RunSync(cnt, sp);
        }
    }
}
