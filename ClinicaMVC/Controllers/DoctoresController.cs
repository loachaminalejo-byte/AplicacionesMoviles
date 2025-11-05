using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ClinicaMVC.Models;

namespace ClinicaMVC.Controllers
{
    public class DoctoresController : Controller
    {
        // URL base de tu API Spring
        private readonly string apiBaseUrl = "http://localhost:8080/api/doctor";
        private readonly HttpClient _http;

        public DoctoresController(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient();
        }

        // ========== 1) LISTAR DOCTORES ==========
        public async Task<IActionResult> Index()
        {
            var doctores = await GetDoctoresFromApi();
            return View(doctores);
        }

        private async Task<List<Doctor>> GetDoctoresFromApi()
        {
            try
            {
                var response = await _http.GetAsync(apiBaseUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var data = JsonSerializer.Deserialize<List<Doctor>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return data ?? new List<Doctor>();
            }
            catch
            {
                // si falla la API devolvemos lista vacía para que la página cargue igual
                return new List<Doctor>();
            }
        }

        // ========== 2) CREAR DOCTOR DESDE AJAX ==========
        // Este método lo llama el fetch() del Index.cshtml
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JsonElement bodyDelFront)
        {
            try
            {
                // bodyDelFront es lo que mandamos desde el navegador:
                // {
                //   nombreDoctor: "...",
                //   apellidosDoctor: "...",
                //   emailDoctor: "...",
                //   edadDoctor: 33,
                //   estadoDoctor: "ACTIVO",
                //   fkIdEspecialidad: { idEspecialidad: 1 }
                // }

                // 1. Serializarlo tal cual SIN CAMBIARLE LOS NOMBRES
                //    Esto es CLAVE: la API Java espera justo esos nombres.
                var jsonBody = JsonSerializer.Serialize(bodyDelFront, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // <- MUY IMPORTANTE
                });

                // 2. Preparar el body para el POST a la API
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // 3. Llamar a la API Spring con el mismo JSON
                var response = await _http.PostAsync(apiBaseUrl, content);

                // 4. Si la API respondió con error (400,500...), devolvemos ese error al front
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();

                    // Para depuración en consola del navegador
                    return BadRequest(new
                    {
                        ok = false,
                        message = "Error al crear en API",
                        detail = errorText
                    });
                }

                // 5. Si todo ok, volvemos a pedir la lista actualizada
                var doctoresActualizados = await GetDoctoresFromApi();

                return Ok(new
                {
                    ok = true,
                    doctores = doctoresActualizados
                });
            }
            catch (Exception ex)
            {
                // Si ni siquiera pudimos llamar a la API o se cayó algo raro:
                return BadRequest(new
                {
                    ok = false,
                    message = "Excepción en MVC al crear",
                    detail = ex.Message
                });
            }
        }
    }
}
