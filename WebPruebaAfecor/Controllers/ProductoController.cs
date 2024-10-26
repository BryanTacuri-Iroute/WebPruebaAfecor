using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebPruebaAfecor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WebPruebaAfecor.Controllers
{
    [Authorize]
    public class ProductoController : Controller
    {
        private readonly DbpruebaContext _dbContext;
        private readonly ILogger<ProductoController> _logger;

        public ProductoController(DbpruebaContext dbContext, ILogger<ProductoController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: Producto/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var productos = await _dbContext.Productos
                                                 .Where(p => p.Estado != "I") // Mostrar solo productos activos
                                                 .AsNoTracking()
                                                 .ToListAsync();
                return View(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de productos.");
                return StatusCode(500, "Ocurrió un error al cargar los productos.");
            }
        }

        // GET: Producto/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Producto/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Producto producto)
        {
            if (!ModelState.IsValid)
            {
                return View(producto);
            }

            try
            {
                _dbContext.Productos.Add(producto);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Producto {Nombre} creado exitosamente.", producto.Nombre);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el producto {Nombre}.", producto.Nombre);
                ModelState.AddModelError("", "Ocurrió un error al crear el producto. Inténtalo nuevamente.");
                return View(producto);
            }
        }

        // GET: Producto/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var producto = await _dbContext.Productos.FindAsync(id);
            if (producto == null || producto.Estado == "I")
            {
                _logger.LogWarning("Producto con ID {Id} no encontrado o está inactivo.", id);
                return NotFound("Producto no encontrado o inactivo.");
            }
            return View(producto);
        }

        // POST: Producto/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Producto producto)
        {
            if (id != producto.IdProducto)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(producto);
            }

            try
            {
                var usuarioModificacion = User.FindFirstValue(ClaimTypes.Name) ?? "System";

                // Actualizar los campos adicionales
                producto.Estado = "M";
                producto.FechaModificacion = DateTime.UtcNow; // Fecha actual en UTC
                producto.UsuarioModificacion = usuarioModificacion;

                // Marcar el objeto como modificado y guardar los cambios
                _dbContext.Entry(producto).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Producto {Nombre} (ID: {Id}) actualizado exitosamente.", producto.Nombre, id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await ProductoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Error de concurrencia al actualizar el producto {Nombre} (ID: {Id}).", producto.Nombre, id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar el producto {Nombre} (ID: {Id}).", producto.Nombre, id);
                ModelState.AddModelError("", "Ocurrió un error al actualizar el producto. Inténtalo nuevamente.");
                return View(producto);
            }
        }

        // GET: Producto/Eliminar/5
        public async Task<IActionResult> Eliminar(int id)
        {
            var producto = await _dbContext.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.IdProducto == id && p.Estado == "A");
            if (producto == null)
            {
                _logger.LogWarning("Producto con ID {Id} no encontrado o ya está inactivo.", id);
                return NotFound("Producto no encontrado o ya inactivo.");
            }
            return View(producto);
        }

        // POST: Producto/Eliminar/5 (Cambia el estado a "Inactivo")
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var producto = await _dbContext.Productos.FindAsync(id);
            if (producto == null)
            {
                _logger.LogWarning("Producto con ID {Id} no encontrado para eliminación.", id);
                return NotFound("Producto no encontrado.");
            }

            try
            {
                // Cambiar el estado del producto en lugar de eliminarlo
                producto.Estado = "I";

                // Usar una transacción para garantizar consistencia
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                _dbContext.Productos.Update(producto);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Producto con ID {Id} marcado como inactivo exitosamente.", id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar como inactivo el producto con ID {Id}.", id);
                ModelState.AddModelError("", "Ocurrió un error al intentar eliminar el producto.");
                return View(producto);
            }
        }

        private async Task<bool> ProductoExists(int id)
        {
            return await _dbContext.Productos.AnyAsync(e => e.IdProducto == id && e.Estado != "E");
        }
    }
}
