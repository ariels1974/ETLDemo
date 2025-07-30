using ETLDemoGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ETLDemoGateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KafkaController : ControllerBase
    {
        private readonly KafkaProducerService _producerService;

        public KafkaController(KafkaProducerService producerService)
        {
            _producerService = producerService;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromBody] string message)
        {
            await _producerService.ProduceAsync(message);
            return Ok("Message published to Kafka.");
        }
    }
}
