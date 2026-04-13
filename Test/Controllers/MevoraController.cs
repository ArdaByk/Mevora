using Mevora;
using Microsoft.AspNetCore.Mvc;
using Test.Features.Commands;
using Test.Features.Events;
using Test.Features.Ping;

namespace Test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MevoraController : ControllerBase
{
    private readonly IMevoraDispatcher _dispatcher;

    public MevoraController(IMevoraDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("ping")]
    public async Task<IActionResult> Ping([FromBody] PingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dispatcher.DispatchAsync(request, cancellationToken);
            return Ok(new { Data = result });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors });
        }
    }

    [HttpPost("log")]
    public async Task<IActionResult> Log([FromBody] PrintLogCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await _dispatcher.DispatchAsync(command, cancellationToken);
            return Ok(new { Message = "Log command executed successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Errors = ex.Errors });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser([FromBody] UserRegisteredEvent evt, CancellationToken cancellationToken)
    {
        // Event publish example
        await _dispatcher.PublishAsync(evt, cancellationToken);
        return Ok(new { Message = "User registered event published successfully" });
    }
}
