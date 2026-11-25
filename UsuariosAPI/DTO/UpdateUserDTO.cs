using System;

namespace UsuariosAPI.DTO;

public class UpdateUserDTO
{
    public string? Nombre { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? Estado { get; set; }
    public string? NuevoRol { get; set; }
}
