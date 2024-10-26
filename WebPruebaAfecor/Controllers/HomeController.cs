using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebPruebaAfecor.Models;
using WebPruebaAfecor.Services;
using WebPruebaAfecor.ViewModels;

namespace WebPruebaAfecor.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AuthService _authService;

        public HomeController(ILogger<HomeController> logger, AuthService authService)
        {
            _logger = logger;
            _authService = authService;

        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Salir()
        {
            await _authService.SignOutAsync();
            return RedirectToAction("Login", "Acceso");

        }
    }
}
