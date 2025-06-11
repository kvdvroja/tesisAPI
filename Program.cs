using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using tesisAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuración JWT basada en Authentication.SecretForKey
var config = builder.Configuration;
var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddHostedService<HostedService>();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IJwtService, JwtService>();
//Externo
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

//CORS
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.AllowAnyOrigin()    // <-- AQUÍ VA
                  .AllowAnyHeader()
                  .AllowAnyMethod();
            //Si después quieres restringir (no todo el mundo) solo cambias:
            /*.WithOrigins("https://tudominio.com", "https://otrodominio.com")*/

        });
});
//CORS

//Creado
builder.Services.AddScoped<PlantillaService>();
builder.Services.AddScoped<LineaInvService>();
builder.Services.AddScoped<CursoProgService>();
builder.Services.AddScoped<TesisService>();
builder.Services.AddScoped<ArchivoService>();
builder.Services.AddScoped<TesisDesaService>();
builder.Services.AddScoped<TareaService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();


builder.Services.AddScoped<OConnection>();

//Config Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer", // <-- esto es clave
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Pegue el token sin 'Bearer'. Swagger lo agregará automáticamente."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

//CORS
app.UseCors(MyAllowSpecificOrigins);
//CORS

//Middleware para el control de errores
app.UseMiddleware<tesisAPI.Middlewares.ErrorHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    //Comentado debido a que esto muestra el backstrace del error
    //app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();  
}

app.UseStaticFiles(); // habilita carga de archivos

app.UseHttpsRedirection();

app.UseAuthentication(); // importante para validar el JWT
app.UseAuthorization();

app.MapControllers();

app.Run();
