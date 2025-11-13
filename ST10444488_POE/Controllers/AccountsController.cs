using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST10444488_POE.Data;
using ST10444488_POE.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ST10444488_POE.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ST10444488_POEContext _context;

        public AccountsController(ST10444488_POEContext context)
        {
            _context = context;
        }

        public IActionResult Register() => View();
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string email, string phoneNumber, string role)
        {
            var exists = await _context.Users.AnyAsync(u => u.Username == username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View();
            }

            var user = new User
            {
                Username = username,
                PasswordHash = Hash(password),
                Email = email,
                PhoneNumber = phoneNumber,
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var hash = Hash(password);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == hash);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Role", user.Role);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid login.";
            return View();
        }

        private string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

}