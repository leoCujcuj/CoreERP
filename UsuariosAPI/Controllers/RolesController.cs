using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsuariosAPI.DTO;
using UsuariosAPI.Models;

namespace UsuariosAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RolesController(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    /// <summary>
    /// Obtiene la lista completa de roles registrados.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDTO>>> ObtenerRoles()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var result = roles.Select(r => new RoleDTO
        {
            Id = r.Id,
            RoleName = r.Name ?? string.Empty,
            Estado = r.Estado
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Alterna el estado activo/inactivo (toggle) de un rol mediante su nombre.
    /// </summary>
    [HttpPatch("toggle/{roleName}")]
    public async Task<IActionResult> ToggleEstado(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest("El nombre del rol es requerido.");
        }

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return NotFound($"No se encontró el rol '{roleName}'.");
        }

        // Protección para evitar la desactivación del rol crítico del sistema
        if (role.Name != null && role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("El rol 'Admin' es un rol principal del sistema y no puede ser desactivado.");
        }

        role.Estado = !role.Estado;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return Ok(new
        {
            Mensaje = $"El estado del rol '{role.Name}' se ha cambiado a {(role.Estado ? "activo" : "inactivo")}.",
            Rol = new RoleDTO
            {
                Id = role.Id,
                RoleName = role.Name ?? string.Empty,
                Estado = role.Estado
            }
        });
    }

    /// <summary>
    /// Crea un nuevo rol en el sistema.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CrearRol([FromBody] string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest("El nombre del rol es requerido.");
        }

        var exists = await _roleManager.RoleExistsAsync(roleName);
        if (exists)
        {
            return BadRequest($"El rol '{roleName}' ya existe en el sistema.");
        }

        var newRole = new ApplicationRole(roleName)
        {
            Estado = true
        };

        var result = await _roleManager.CreateAsync(newRole);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return Ok(new
        {
            Mensaje = $"Rol '{newRole.Name}' creado exitosamente.",
            Rol = new RoleDTO
            {
                Id = newRole.Id,
                RoleName = newRole.Name ?? string.Empty,
                Estado = newRole.Estado
            }
        });
    }
}
