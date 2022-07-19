using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BlazorAuthDemo.Server.Models;
using BlazorAuthDemo.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace BlazorAuthDemo.Server.Api;

[ApiController]
[Route("/api/[controller]/[action]")]
[Authorize]
public class AuthenticationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;

    public AuthenticationController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UserSignIn model)
    {
        // TODO: check if user already exists and validate password 

        ApplicationUser user = new() { Email = model.Email, UserName = model.Email };
        user.PasswordHash = this.userManager.PasswordHasher.HashPassword(user, model.Password);

        IdentityResult result = await this.userManager.CreateAsync(user);

        if (!result.Succeeded)
            throw new NotImplementedException();

        return base.Ok();
    }

    [HttpPost]
    [AllowAnonymous]
    [Produces(typeof(UserSignInResult))]
    public async Task<IActionResult> SignIn([FromBody] UserSignIn model)
    {
        UserSignInResult result = await this.SignInAsync(model);

        return result.Success
            ? base.Ok(result)
            : base.BadRequest(result);
    }

    // TODO: move to service class
    private async Task<UserSignInResult> SignInAsync(UserSignIn model)
    {
        ApplicationUser? user = await this.userManager.FindByEmailAsync(model.Email);
        user ??= await this.userManager.FindByNameAsync(model.Email);

        // we only "check" whether the credentials are correct here, we do not use the SignInManager
        // to sign in as this will add an auth cookie which we don't need as we are using a JWT
        SignInResult result = user is not null
            ? await this.signInManager.CheckPasswordSignInAsync(user, model.Password, false)
            : SignInResult.Failed;

        if (!result.Succeeded)
            return new UserSignInResult(false, "E-mail address or password is incorrect.");

        string token = await this.GetTokenStringAsync(user!);

        UserSignInResult signInResult = new(true, Token: token);
        return signInResult;
    }

    // TODO: move to service class
    internal async Task<string> GetTokenStringAsync(ApplicationUser user)
    {
        List<Claim> claims = (await this.userManager.GetClaimsAsync(user)).ToList();

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes("af1097912a99481f839b0490b875eaf1"));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);
        DateTime expiry = DateTime.Now.AddDays(Convert.ToInt32(1));

        JwtSecurityToken token = new("https://localhost", "https://localhost", claims, expires: expiry, signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
