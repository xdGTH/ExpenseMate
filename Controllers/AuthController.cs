using ExpenseMate.Data;
using ExpenseMate.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

[Route("/[controller]")]
public class AuthController : Controller
{
    private readonly ApplicationDbContext _db;

    public AuthController(ApplicationDbContext context)
    {
        _db = context;
    }

    [HttpGet]
    [Route("/[action]")]
    public IActionResult Register() => View();

    [HttpPost]
    [Route("/[action]")]
    public async Task<IActionResult> Register(string email, string password, string fullname)
    {
        if (_db.Users.Any(u => u.Email == email))
        {
            ModelState.AddModelError("", "Email already registered.");
            return View();
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User { Email = email, PasswordHash = hash, FullName = fullname };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return RedirectToAction("Login");
    }

    [HttpGet]
    [Route("/[action]")]
    [Route("/")]
    public IActionResult Login() => View();

    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid login");
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction("Index", "Home");
    }

    [Route("/[controller]/[action]")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/");
    }
}
