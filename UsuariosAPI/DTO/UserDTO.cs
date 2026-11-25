using System;
using System.Collections.Generic;

namespace UsuariosAPI.DTO;

public class UserDTO
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}
