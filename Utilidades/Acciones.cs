using Microsoft.Extensions.Configuration;

namespace tesisAPI.Utilidades
{
    public class Acciones
    {

        public static readonly HashSet<string> TesisModosValidos;
        public static readonly HashSet<string> DesaModosValidos;
        public static readonly HashSet<string> TareaModosValidos;
        public static readonly HashSet<string> AutorModosValidos;
        public static readonly HashSet<string> ComentarioModosValidos;
        public static readonly HashSet<string> AsesorModosValidos;
        public static readonly HashSet<string> LineasModosValidos;







        static Acciones()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            TesisModosValidos = new HashSet<string>(
                configuration.GetSection("Acciones:TesisModosValidos").Get<string[]>() ?? []
            );

            DesaModosValidos = new HashSet<string>(
                configuration.GetSection("Acciones:TesisDesaModosValidos").Get<string[]>() ?? []
            );

            TareaModosValidos = new HashSet<string>(
                configuration.GetSection("Acciones:TareaModosValidos").Get<string[]>() ?? []
            );

            AsesorModosValidos = new HashSet<string>(
                configuration.GetSection("Acciones:AsesorModosValidos").Get<string[]>() ?? []
            );

            ComentarioModosValidos = new HashSet<string>(
               configuration.GetSection("Acciones:ComentarioModosValidos").Get<string[]>() ?? []
           );

            LineasModosValidos = new HashSet<string>(
             configuration.GetSection("Acciones:LineasModosValidos").Get<string[]>() ?? []
         );





        }
    }
}
