namespace tesisAPI.Services
{
    public class ArchivoService
    {
        private readonly IConfiguration _config;

        public ArchivoService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SubirArchivosPendientes()
        {
            var rutaInterna = _config["UploadConfig:RutaInterna"];
            var rutaExterna = _config["UploadConfig:RutaExterna"];
            var urlApiExterna = _config["UploadConfig:UrlApiExterna"];
            var token = _config["UploadConfig:Token"];

            var anio = DateTime.Now.Year.ToString();
            var carpetaAnio = Path.Combine(rutaInterna, anio);

            if (!Directory.Exists(carpetaAnio)) return;

            var archivos = Directory.GetFiles(carpetaAnio);

            foreach (var archivoPath in archivos)
            {
                var nombreArchivo = Path.GetFileName(archivoPath);

                try
                {
                    // 👉 Extraer el usuario desde el nombre del archivo (antes del primer '-')
                    var usuario = nombreArchivo.Split('-')[0];

                    var archivoBytes = await System.IO.File.ReadAllBytesAsync(archivoPath);

                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                    using var form = new MultipartFormDataContent();

                    form.Add(new StringContent(usuario), "usuario");
                    form.Add(new StringContent($"{rutaExterna}/{anio}"), "ruta");
                    form.Add(new StringContent(token), "token");
                    form.Add(new StringContent(nombreArchivo), "nombre_archivo");

                    var streamContent = new ByteArrayContent(archivoBytes);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    form.Add(streamContent, "file1", nombreArchivo);
                    Console.WriteLine($"➡️ Enviando POST a: {urlApiExterna}");
                    Console.WriteLine($"usuario: {usuario}");
                    Console.WriteLine($"ruta: {rutaExterna}/{anio}");
                    Console.WriteLine($"token: {token}");
                    Console.WriteLine($"archivo: {nombreArchivo} ({archivoBytes.Length} bytes)");

                    var response = await client.PostAsync(urlApiExterna, form);
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Contenido de la respuesta: {responseContent}");

                    // Eliminar archivo tras subir
                    File.Delete(archivoPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error subiendo archivo {nombreArchivo}: {ex.Message}");
                }
            }
        }
    }

}