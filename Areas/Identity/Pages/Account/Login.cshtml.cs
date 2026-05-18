// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Control_flota.Models.Login;
using Microsoft.EntityFrameworkCore; 
using Control_flota.Data; // ← Agregar esto (si necesitas acceso a DbContext)

namespace Control_flota.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly RoleManager<IdentityRole> _roleManager; 
        private readonly ApplicationDbContext _context; // ← Opcional: para consultar directamente

        //Constructor 
        public LoginModel(
            SignInManager<Usuario> signInManager,
            UserManager<Usuario> userManager, 
            ILogger<LoginModel> logger,
            RoleManager<IdentityRole> roleManager, 
            ApplicationDbContext context) 
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _roleManager = roleManager; // ← Agregar
            _context = context; // ← Agregar
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    
                    
                    
                    // Obtener el usuario
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    
                    if (user != null)
                    {
                        // Obtener los roles del usuario
                        var roles = await _userManager.GetRolesAsync(user);
                        
                        // Verificar si tiene algún rol
                        if (roles.Any())
                        {
                            var primerRol = roles.First();
                            
                            // 🔴 Redirigir según el NOMBRE del rol
                            if (primerRol == "Administrador")
                            {
                                return LocalRedirect("/Admin/Index");
                            }
                            else if (primerRol == "Conductor")
                            {
                                return LocalRedirect("/conductor/index");
                            }
                        }
                    }
                    
                    // Si no tiene rol o no coincide, va al home normal
                    return LocalRedirect(returnUrl);
                    
                    // 🔴 ========== FIN DE LA PARTE AGREGADA ==========
                }
                
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
