using MediatR;
using Mevora;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using testAPI.queries;

namespace testAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        public IMevoraDispatcher _mevoraDispatcher;
        public IMediator _mediatr;

        public WeatherForecastController(IMevoraDispatcher mevoraDispatcher, IMediator mediatr)
        {
            _mevoraDispatcher = mevoraDispatcher;
            _mediatr = mediatr;
        }

        [HttpGet]
        public async Task<string[]> Get([FromQuery] MevoraGetAllDataQuery? getAllDataQuery)
        {
            Stopwatch stp = new Stopwatch();
            stp.Start();

            string[] response = await _mevoraDispatcher.DispatchAsync(getAllDataQuery);

            stp.Stop();
            var a = stp.Elapsed;
            return response;
        }
        [HttpGet("mediatr")]
        public async Task<string[]> GetMediatr([FromQuery] MediatrGetAllDataQuery? getAllDataQuery)
        {
            Stopwatch stp = new Stopwatch();
            stp.Start();
            string[] response = await _mediatr.Send(getAllDataQuery);

            stp.Stop();
            var a = stp.Elapsed;
            return response;
        }
        [HttpGet("benchmark")]
        public async Task<string> Benchmark()
        {
            var getAllDataQuery = new MevoraGetAllDataQuery();
            var mediatrQuery = new MediatrGetAllDataQuery();

            // --- Warm-up ---
            await _mevoraDispatcher.DispatchAsync(getAllDataQuery);
            await _mediatr.Send(mediatrQuery);

            int iterations = 100; // kaç kez test edeceðimiz
            TimeSpan mevoraTime = TimeSpan.Zero;
            TimeSpan mediatrTime = TimeSpan.Zero;

            // --- Mevora Test ---
            for (int i = 0; i < iterations; i++)
            {
                var sw = Stopwatch.StartNew();
                await _mevoraDispatcher.DispatchAsync(getAllDataQuery);
                sw.Stop();
                mevoraTime += sw.Elapsed;
            }

            // --- Mediatr Test ---
            for (int i = 0; i < iterations; i++)
            {
                var sw = Stopwatch.StartNew();
                await _mediatr.Send(mediatrQuery);
                sw.Stop();
                mediatrTime += sw.Elapsed;
            }

            double mevoraAvgMs = mevoraTime.TotalMilliseconds / iterations;
            double mediatrAvgMs = mediatrTime.TotalMilliseconds / iterations;

            return $"Mevora Avg: {mevoraAvgMs} ms, Mediatr Avg: {mediatrAvgMs} ms";
        }

    }
}
