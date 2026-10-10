using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// In-memory user store (for demo/testing). Replace with a DB in production.
var users = new List<User>
{
    new User(Guid.NewGuid(), "Alice", "alice@example.com"),
    new User(Guid.NewGuid(), "Bob", "bob@example.com")
};

// Helper validation functions
static bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);

static bool IsValidEmail(string? email)
{
    if (string.IsNullOrWhiteSpace(email)) return false;
    try
    {
        // Use MailAddress to validate format
        var _ = new MailAddress(email);
        return true;
    }
    catch
    {
        return false;
    }
}

// CRUD endpoints for users

// GET /users - list all users
app.MapGet("/users", () => Results.Ok(users));

// GET /users/{id} - get user by id
app.MapGet("/users/{id:guid}", (Guid id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
}).WithName("GetUserById");

// POST /users - create new user
app.MapPost("/users", (UserCreateDto dto) =>
{
    // Validate input
    if (IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name is required and cannot be empty." });
    if (!IsValidEmail(dto.Email))
        return Results.BadRequest(new { error = "Email is required and must be a valid email address." });

    var user = new User(Guid.NewGuid(), dto.Name.Trim(), dto.Email.Trim());
    users.Add(user);
    return Results.CreatedAtRoute("GetUserById", new { id = user.Id }, user);
});

// PUT /users/{id} - update existing user
app.MapPut("/users/{id:guid}", (Guid id, UserUpdateDto dto) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    // Validate provided fields
    if (dto.Name is not null)
    {
        if (IsNullOrWhiteSpace(dto.Name))
            return Results.BadRequest(new { error = "If provided, Name cannot be empty." });
        user.Name = dto.Name.Trim();
    }

    if (dto.Email is not null)
    {
        if (!IsValidEmail(dto.Email))
            return Results.BadRequest(new { error = "If provided, Email must be a valid email address." });
        user.Email = dto.Email.Trim();
    }

    return Results.NoContent();
});

// DELETE /users/{id} - remove user by id
app.MapDelete("/users/{id:guid}", (Guid id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();
    users.Remove(user);
    return Results.NoContent();
});

// (Removed example WeatherForecast endpoint - not relevant to user management API)

app.Run();

record User(Guid Id, string Name, string Email)
{
    public Guid Id { get; init; } = Id;
    public string Name { get; set; } = Name;
    public string Email { get; set; } = Email;
}

record UserCreateDto(string Name, string Email);

record UserUpdateDto(string? Name, string? Email);

// (WeatherForecast record removed)
