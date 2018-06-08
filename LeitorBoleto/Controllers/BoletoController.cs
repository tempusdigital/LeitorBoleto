using LeitorBoleto.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeitorBoleto.Controllers
{
    public class BoletoController : Controller
    {
        [HttpPost("GerarBoleto")]
        public IActionResult Post(IFormFile file)
        {
            if (file is null)
                return View("/Views/Home/Index.cshtml", new BoletoViewModel { LinhaBoleto = "" });

            try
            {
                var boleto = new Boleto(file).ObterCodigoBarras();
                return View("/Views/Home/Index.cshtml", new BoletoViewModel { LinhaBoleto = string.IsNullOrWhiteSpace(boleto) ? "Não foi possível obter a linha digitável" : boleto });
            }
            catch (Exception ex)
            {
                return View("/Views/Home/Index.cshtml", new BoletoViewModel { LinhaBoleto = "", MensagemErro = "Ops! Houve uma falha: " + ex.Message  });
            }

        }
    }
}