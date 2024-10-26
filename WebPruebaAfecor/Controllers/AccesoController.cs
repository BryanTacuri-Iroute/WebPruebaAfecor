using Microsoft.AspNetCore.Mvc;
using WebPruebaAfecor.Models;
using WebPruebaAfecor.ViewModels;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebPruebaAfecor.Custom;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebPruebaAfecor.Services;

namespace WebPruebaAfecor.Controllers
{
    public class AccesoController : Controller
    {
        private readonly DbpruebaContext _dbContext;
        private readonly Utilidades _utilidades;
        private readonly AuthService _authService;
        private readonly ILogger<AccesoController> _logger;

        public AccesoController(DbpruebaContext dbContext, Utilidades utilidades, AuthService authService, ILogger<AccesoController> logger)
        {
            _dbContext = dbContext;
            _utilidades = utilidades;
            _authService = authService;
            _logger = logger;
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Registrarse()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Registrarse(UsuarioVM modelo)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Mensaje"] = "Por favor completa todos los campos correctamente.";
                return View(modelo);
            }

            if (modelo.Clave != modelo.ConfirmarClave)
            {
                ViewData["Mensaje"] = "Las contraseñas no coinciden.";
                return View(modelo);
            }

            var usuarioExistente = await _dbContext.Usuarios
                                    .FirstOrDefaultAsync(u => u.Correo == modelo.Correo);
            if (usuarioExistente != null)
            {
                ViewData["Mensaje"] = "El correo ya está registrado. Usa uno diferente.";
                return View(modelo);
            }

            var claveEncriptada = _utilidades.EncriptarSHA256(modelo.Clave);

            var usuario = new Usuario
            {
                Nombre = modelo.Nombre,
                Correo = modelo.Correo,
                Clave = claveEncriptada
            };

            try
            {
                await _dbContext.Usuarios.AddAsync(usuario);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Login", "Acceso");
            }
            catch (Exception ex)
            {
                // Registrar el error (para un registro detallado, utiliza un ILogger)
                ViewData["Mensaje"] = "Ocurrió un error al crear el usuario. Intenta nuevamente.";
                return View(modelo);
            }
        }


        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated) RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Login(LoginVM modelo)
        {
            if (string.IsNullOrEmpty(modelo.Correo) || string.IsNullOrEmpty(modelo.Clave))
            {
                ViewData["Mensaje"] = "Por favor completa todos los campos.";
                return View(modelo);
            }

            try
            {
                var claveEncriptada = _utilidades.EncriptarSHA256(modelo.Clave);
                var usuario = await _dbContext.Usuarios
                                    .FirstOrDefaultAsync(u => u.Correo == modelo.Correo && u.Clave == claveEncriptada);

                if (usuario == null)
                {
                    ViewData["Mensaje"] = "Correo o contraseña incorrectos.";
                    _logger.LogWarning("Intento de inicio de sesión fallido para el correo {Correo}", modelo.Correo);
                    return View(modelo);
                }

                await _authService.SignInAsync(usuario); // Usa el servicio de autenticación


                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de inicio de sesión.");
                ViewData["Mensaje"] = "Ocurrió un error al procesar el inicio de sesión. Intenta nuevamente.";
                return View(modelo);
            }
        }
    }
}


