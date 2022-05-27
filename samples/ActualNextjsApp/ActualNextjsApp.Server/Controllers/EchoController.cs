using Microsoft.AspNetCore.Mvc;

namespace ActualNextjsApp.Server.Controllers
{
    public class EchoController : Controller
    {
        [Route("/api/echo")]
        public string Echo()
        {
            return "Hello world";
        }
    }
}
