using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JwtAuthDemo.Models;
using JwtAuthDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<UserService>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/register", (User user, UserService userService) =>
{
    if (userService.GetUser(user.Username) != null)
    {
        return Results.Conflict("User already exists");
    }

    userService.AddUser(user);
    return Results.Ok();
})
.WithName("Register")
.WithTags("Auth");

app.MapPost("/login", (LoginRequest request, UserService userService, JwtService jwt) =>
{
    if (userService.VerifyPassword(request.Username, request.Password))
    {
        var user = userService.GetUser(request.Username)!;
        var token = jwt.GenerateToken(user.Username, user.Role);
        return Results.Ok(new { Token = token });
    }

    return Results.Unauthorized();
})
.WithName("Login")
.WithTags("Auth");

app.MapGet("/public", () => "This is a public endpoint.")
    .WithName("Public")
    .WithTags("Test");

app.MapGet("/protected", [Microsoft.AspNetCore.Authorization.Authorize] () =>
{
    return Results.Ok("You are authorized!");
})
.WithName("Protected")
.WithTags("Test");

app.MapGet("/admin", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")] () =>
{
    return Results.Ok("Welcome, Admin!");
})
.WithName("Admin")
.WithTags("Test");

app.Run();

public record LoginRequest(string Username, string Password);

