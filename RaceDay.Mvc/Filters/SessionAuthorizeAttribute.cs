
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Filters
{
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var token = context.HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account", null);
            }
        }
    }

    public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _role;

        public RoleAuthorizeAttribute(string role)
        {
            _role = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var actualRole = context.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(
                context.HttpContext.Session.GetString("Token")))
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account", null);
                return;
            }

            if (actualRole != _role)
            {
                context.Result = new RedirectToActionResult(
                    "Index", "Home", null);
            }
        }
    }
}
