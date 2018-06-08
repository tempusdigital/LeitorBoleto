using Microsoft.AspNetCore.Mvc;
using LeitorBoleto.Models;

namespace LeitorBoleto.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(new BoletoViewModel());
        }
    }
}