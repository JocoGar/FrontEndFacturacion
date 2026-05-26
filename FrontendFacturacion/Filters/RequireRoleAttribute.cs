using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FrontendFacturacion.Filters
{
    public class RequireRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _rolesPermitidos;

        public RequireRoleAttribute(params string[] rolesPermitidos)
        {
            _rolesPermitidos = rolesPermitidos;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var usuarioDpi = context.HttpContext.Session.GetString("UsuarioDpi");
            var usuarioRol = context.HttpContext.Session.GetString("UsuarioRol") ?? "";

            if (string.IsNullOrWhiteSpace(usuarioDpi))
            {
                context.Result = new RedirectToActionResult("Login", "Cuenta", null);
                return;
            }

            var rolNormalizado = usuarioRol.Trim().ToLower();

            var tienePermiso = _rolesPermitidos.Any(rol =>
                rolNormalizado == rol.Trim().ToLower()
            );

            if (!tienePermiso)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}