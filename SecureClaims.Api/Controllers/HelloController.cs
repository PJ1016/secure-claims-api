using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureClaims.Api.Services;

namespace SecureClaims.Api.Controllers
{
    [ApiController]
    [Route("api/hello")]
    [Authorize(Policy = "ApiScope")]
    public class HelloController : ControllerBase
    {
        private readonly IHelloService _helloService;

        public HelloController(IHelloService helloService)
        {
            _helloService = helloService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_helloService.GetMessage());
        }
    }
}
