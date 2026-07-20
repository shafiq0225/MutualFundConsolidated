using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MutualFund.Investment.API.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected string CurrentUserId =>
            User.FindFirstValue("sub") ?? string.Empty;

        protected string CurrentUserRole =>
            User.FindFirstValue("role") ?? string.Empty;

        protected string CurrentUserName =>
            $"{User.FindFirstValue("firstName")} " +
            $"{User.FindFirstValue("lastName")}".Trim();

        protected bool IsAdmin =>
            CurrentUserRole == "Admin";

        protected bool IsEmployee =>
            CurrentUserRole == "Employee";

        protected bool CanViewAllOrdersData =>
            IsAdmin || IsEmployee || User.HasClaim("permissions", "order.view");

        protected bool CanViewAllPortfolioData =>
            IsAdmin || IsEmployee || User.HasClaim("permissions", "investor.view");
    }
}