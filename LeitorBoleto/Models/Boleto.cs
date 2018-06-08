using Google.Cloud.Vision.V1;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace LeitorBoleto.Models
{
    public class Boleto
    {
        public IFormFile Pdf { get; set; }

        public Boleto(IFormFile pdf)
        {
            Pdf = pdf;
        }

        public string ObterCodigoBarras()
        {
            using (var pdfStream = Pdf.OpenReadStream())
            {
                var imagem = GerarImagem(pdfStream);
                return LerImagem(imagem);
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
                    var temp = Path.GetTempPath();
                    var nomeArquivo = Guid.NewGuid().ToString() + ".png";
                    imagem.Format = MagickFormat.Png;
                    imagem.Write(temp + nomeArquivo);

                    return nomeArquivo;
                }
            }
        }

        private string LerImagem(string nomeArquivo)
        {
            var boleto = "";
            var imagem = Directory.GetFiles(Path.GetTempPath(), nomeArquivo);
            var client = ImageAnnotatorClient.Create();
            var image = Image.FromFile(imagem[0]);
            var response = client.DetectText(image);

            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                {
                    boleto = ValidarLinhaBoleto(annotation.Description).FirstOrDefault()?.Value;

                    if (!string.IsNullOrEmpty(boleto))
                        break;
                }
            }

            RemoverArquivo(imagem[0]);

            return boleto;
        }

        private void RemoverArquivo(string imagem)
        {
            File.Delete(imagem);
        }

        private MatchCollection ValidarLinhaBoleto(string input)
        {
            return Regex.Matches(input, @"\d{5}\.\d{5} \d{5}\.\d{6} \d{5}\.\d{6} \d \d{14}");
        }
    }
}
