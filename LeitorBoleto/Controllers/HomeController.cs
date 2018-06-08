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

        public IActionResult Error()
        {
            return View("Index", new BoletoViewModel() { MensagemErro = "Ops! Houve uma falha, por favor, tente novamente." });
        }
    }
}