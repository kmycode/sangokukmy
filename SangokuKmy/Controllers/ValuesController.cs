using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SangokuKmy.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ValuesController : ControllerBase
  {
    private readonly ILogger<ValuesController> logger;

    public ValuesController(ILogger<ValuesController> logger) {
      this.logger = logger;
    }

    // GET api/values
    [HttpGet]
    public ActionResult<IEnumerable<string>> Get()
    {
      this.logger.LogDebug("Hello, log!");
      return new string[] { "value1", "value2" };
    }

    // GET api/values/5
    [HttpGet("{id}")]
    public ActionResult<string> Get(int id)
    {
      return "value";
    }

    // POST api/values
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/values/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/values/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }

    // GET api/values/streaming
    [HttpGet("streaming")]
    public async Task StreamingTest()
    {
      try
      {
        // HTTPヘッダを設定する
        this.Response.Headers.Add("Content-Type", "text/event-stream; charset=UTF-8");

        // メッセージを送信する
        await this.Response.WriteAsync("data: こんにちは！\n");

        // 3秒待つ
        await Task.Delay(1000 * 3);

        // もう一度メッセージを送信
        await this.Response.WriteAsync(@"{""typeId"":4,""id"":100,""message"":""てすと"",""type"":{""id"":6,""text"":""支配"",""color"":""blue""},""date"":{""year"":2018,""month"":12,""day"":1,""hours"":12,""minutes"":0,""seconds"":0}}
");
        await this.Response.WriteAsync("data: きゅうりでつっこむぞ\n");

        // 3秒待つ
        await Task.Delay(1000 * 3);
      }
      catch (Exception ex)
      {

      }
    }
  }
}
