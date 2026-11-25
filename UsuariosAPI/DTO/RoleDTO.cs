using System;

namespace UsuariosAPI.DTO;

public class RoleDTO
{
    public string Id { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
}
