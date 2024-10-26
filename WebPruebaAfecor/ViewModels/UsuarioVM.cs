using System;
using System.Collections.Generic;

namespace WebPruebaAfecor.ViewModels;

public partial class UsuarioVM
{

    public required string Nombre { get; set; }

    public required string Correo { get; set; }

    public required string  Clave { get; set; }

    public required string? ConfirmarClave { get; set; }


}
