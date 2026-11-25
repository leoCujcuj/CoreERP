using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsuariosAPI.DTO;
using UsuariosAPI.Models;

namespace UsuariosAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuariosController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UsuariosController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Ver lista de todos los usuarios registrados.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDTO>>> ObtenerUsuarios()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserDTO>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDTO
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Nombre = user.Nombre,
                Estado = user.Estado,
                FechaCreacion = user.FechaCreacion,
                Roles = roles
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Ver perfil del usuario autenticado actual.
    /// </summary>
    [HttpGet("perfil")]
    public async Task<ActionResult<UserDTO>> VerPerfil()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("No se pudo identificar al usuario autenticado.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDTO
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Nombre = user.Nombre,
            Estado = user.Estado,
            FechaCreacion = user.FechaCreacion,
            Roles = roles
        });
    }

    /// <summary>
    /// Ver perfil de un usuario específico por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDTO>> VerPerfilPorId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("El identificador de usuario es requerido.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound($"No se encontró ningún usuario con el ID '{id}'.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDTO
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Nombre = user.Nombre,
            Estado = user.Estado,
            FechaCreacion = user.FechaCreacion,
            Roles = roles
        });
    }

    /// <summary>
    /// Agregar un usuario al sistema desde la aplicación (creado por un administrador u otro usuario autorizado).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDTO>> AgregarUsuario([FromBody] RegisterDTO model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest("El correo electrónico y la contraseña son requeridos.");
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return BadRequest("El correo electrónico ya se encuentra registrado.");
        }

        var roleName = string.IsNullOrWhiteSpace(model.Rol) ? "User" : model.Rol;
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            return BadRequest($"El rol '{roleName}' no existe en el sistema.");
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            Nombre = model.Nombre,
            Estado = true,
            FechaCreacion = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, roleName);

        var userDto = new UserDTO
        {
            Id = user.Id,
            Email = user.Email,
            Nombre = user.Nombre,
            Estado = user.Estado,
            FechaCreacion = user.FechaCreacion,
            Roles = new List<string> { roleName }
        };

        return CreatedAtAction(nameof(VerPerfilPorId), new { id = user.Id }, userDto);
    }

    /// <summary>
    /// Actualizar los datos de un usuario (nombre, teléfono, estado o rol).
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarUsuario(string id, [FromBody] UpdateUserDTO model)
    {
        if (string.IsNullOrWhiteSpace(id) || model == null)
        {
            return BadRequest("Datos de solicitud inválidos.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound($"No se encontró ningún usuario con el ID '{id}'.");
        }

        if (!string.IsNullOrWhiteSpace(model.Nombre))
        {
            user.Nombre = model.Nombre;
        }

        if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            user.PhoneNumber = model.PhoneNumber;
        }

        if (model.Estado.HasValue)
        {
            user.Estado = model.Estado.Value;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(updateResult.Errors.Select(e => e.Description));
        }

        if (!string.IsNullOrWhiteSpace(model.NuevoRol))
        {
            var roleExists = await _roleManager.RoleExistsAsync(model.NuevoRol);
            if (!roleExists)
            {
                return BadRequest($"El rol '{model.NuevoRol}' no existe en el sistema.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.NuevoRol);
        }

        return Ok(new { Mensaje = "Usuario actualizado exitosamente." });
    }

    /// <summary>
    /// Cambiar la contraseña de un usuario específico por su ID.
    /// </summary>
    [HttpPost("{id}/cambiar-password")]
    public async Task<IActionResult> CambiarPassword(string id, [FromBody] ChangePasswordDTO model)
    {
        if (string.IsNullOrWhiteSpace(id) || model == null)
        {
            return BadRequest("Datos de solicitud inválidos.");
        }

        if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
        {
            return BadRequest("La contraseña actual y la nueva contraseña son requeridas.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound($"No se encontró ningún usuario con el ID '{id}'.");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return Ok(new { Mensaje = "Contraseña actualizada exitosamente." });
    }

    /// <summary>
    /// Cambiar la contraseña del usuario actualmente autenticado.
    /// </summary>
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPasswordPropio([FromBody] ChangePasswordDTO model)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("No se pudo identificar al usuario autenticado.");
        }

        return await CambiarPassword(userId, model);
    }
}
