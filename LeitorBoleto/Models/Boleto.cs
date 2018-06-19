using Google.Cloud.Vision.V1;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Grpc.Core;
using Grpc.Auth;
using System.Threading.Tasks;

namespace LeitorBoleto.Models
{
    public class Boleto
    {
        private static ImageAnnotatorClient _googleClient;
        
        private static ImageAnnotatorClient GetGoogleClient(string googleCredentialPath)
        {
            if (_googleClient == null)
            {
                var credencialGoogle = GoogleCredential.FromFile(googleCredentialPath);
                var canalApi = new Channel(ImageAnnotatorClient.DefaultEndpoint.Host, credencialGoogle.ToChannelCredentials());
                _googleClient = ImageAnnotatorClient.Create(canalApi);
            }

            return _googleClient;
        }

        public IFormFile Pdf { get; set; }

        public Boleto(IFormFile pdf)
        {
            Pdf = pdf;
        }

        public async Task<string> ObterCodigoBarras(string googleCredentialPath)
        {
            using (var pdfStream = Pdf.OpenReadStream())
            {
                var caminhoImagem = GerarImagem(pdfStream);
                var boleto = "";

                try
                {
                    boleto = await LerImagem(caminhoImagem, googleCredentialPath);
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

        private async Task<string> LerImagem(string caminhoImagem, string googleCredentialPath)
        {
            var boleto = "";
            var client = GetGoogleClient(googleCredentialPath);
            
            var image = await Image.FromFileAsync(caminhoImagem);
            var response = await client.DetectTextAsync(image);

            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                {
                    var valores = ValidarLinhaBoleto(annotation.Description);
                    if (valores.Count > 0)
                        boleto = valores[0].Value;

                    if (!string.IsNullOrEmpty(boleto))
                        break;
                }
            }

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
