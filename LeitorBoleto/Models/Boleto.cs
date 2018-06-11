using ImageMagick;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.RegularExpressions;
using System;
using Tesseract;

namespace LeitorBoleto.Models
{
    public class Boleto
    {
        public IFormFile Pdf { get; set; }

        public Boleto(IFormFile pdf)
        {
            Pdf = pdf;
        }

        public string ObterCodigoBarras(string googleCredentialPath)
        {
            using (var pdfStream = Pdf.OpenReadStream())
            {
                var caminhoImagem = GerarImagem(pdfStream);
                var boleto = "";

                try
                {
                    boleto = LerImagem(caminhoImagem);
                }
                finally
                {
                    RemoverArquivo(caminhoImagem);
                }

                return boleto;
            }
        }

        private string GerarImagem(Stream stream)
        {
            MagickNET.SetGhostscriptDirectory(Directory.GetCurrentDirectory());
            MagickNET.SetTempDirectory(Path.GetTempPath());

            var settings = new MagickReadSettings
            {
                Density = new Density(300, 300)
            };

            using (var images = new MagickImageCollection())
            {
                images.Read(stream, settings);

                using (var imagem = images.AppendHorizontally())
                {
                    var caminhoArquivo = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");
                    imagem.Format = MagickFormat.Png;
                    imagem.Write(caminhoArquivo);

                    return caminhoArquivo;
                }
            }
        }

        private string LerImagem(string caminhoArquivo)
        {
            using (var engine = new TesseractEngine(Directory.GetCurrentDirectory() + "\\tessdata", "hin", EngineMode.CubeOnly))
            {
                engine.SetVariable("load_system_dawg", false);
                engine.SetVariable("load_freq_dawg", false);

                using (var img = Pix.LoadFromFile(caminhoArquivo))
                {
                    img.ConvertRGBToGray();

                    using (var page = engine.Process(img))
                    {
                        var valores = ValidarLinhaBoleto(page.GetText());

                        if (valores.Count > 0)
                            return valores[0].Value;
                    }
                }
            }

            return "";
        }

        private void RemoverArquivo(string imagem)
        {
            File.Delete(imagem);
        }

        private MatchCollection ValidarLinhaBoleto(string input)
        {
            return Regex.Matches(input, @"\d{5}(\.)?\d{5}(\s)?\d{5}(\.)?\d{6}(\s)?\d{5}(\.)?\d{6}(\s)?\d(\s)?\d{14}");
        }
    }
}
