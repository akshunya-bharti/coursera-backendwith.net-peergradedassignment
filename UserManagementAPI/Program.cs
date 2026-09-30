using System;
using System.Collections.Generic;
using System.Linq;

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
    var user = new User(Guid.NewGuid(), dto.Name, dto.Email);
    users.Add(user);
    return Results.CreatedAtRoute("GetUserById", new { id = user.Id }, user);
});

// PUT /users/{id} - update existing user
app.MapPut("/users/{id:guid}", (Guid id, UserUpdateDto dto) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();
    // update fields
    user.Name = dto.Name ?? user.Name;
    user.Email = dto.Email ?? user.Email;
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
