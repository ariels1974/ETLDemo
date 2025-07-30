using ETLDemo.Grpc;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;

namespace ETLDemoGateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SimpleMessageController : ControllerBase
    {
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] string message)
        {
            // Replace with your gRPC server address
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new SimpleMessageService.SimpleMessageServiceClient(channel);
            var request = new SimpleMessageRequest { Message = message };
            var reply = await client.SendMessageAsync(request);
            return Ok(reply.Response);
        }
    }
}
