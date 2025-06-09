using ExpenseMate.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ExpenseMate.Controllers
{
    public class HomeController : Controller
    {
        [Route("/home/index")]
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
