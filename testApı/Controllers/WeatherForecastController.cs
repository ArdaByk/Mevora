using Mevora;
using Microsoft.AspNetCore.Mvc;

namespace testApı.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
      private readonly IMevoraDispatcher _dispatcher;

        public WeatherForecastController(IMevoraDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var res = await _dispatcher.DispatchAsync(new GetUserByIdQuery());

            return res;
        }

        [HttpGet("public")]
        public async Task Publsih()
        {
            await _dispatcher.PublishAsync(new UserRegisteredMessage());
        }

        [HttpGet("val")]
        public async Task Val()
        {
            var res = _dispatcher.DispatchAsync(new TestQuery { Name = "fdfdsf"});
        }
    }
}
