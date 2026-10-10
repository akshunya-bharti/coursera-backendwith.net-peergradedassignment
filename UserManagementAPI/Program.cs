using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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

// In-memory thread-safe user store (for demo/testing). Replace with a DB in production.
var users = new ConcurrentDictionary<Guid, User>();
{
    var u1 = new User(Guid.NewGuid(), "Alice", "alice@example.com");
    var u2 = new User(Guid.NewGuid(), "Bob", "bob@example.com");
    users[u1.Id] = u1;
    users[u2.Id] = u2;
}

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

// GET /users - list all users (paged optional)
app.MapGet("/users", (int? page, int? pageSize) =>
{
    // Snapshot values to avoid enumerating a changing collection
    var snapshot = users.Values.ToArray();
    if (page is null && pageSize is null)
        return Results.Ok(snapshot);

    var p = Math.Max(1, page ?? 1);
    var ps = Math.Clamp(pageSize ?? 50, 1, 100);
    var items = snapshot.Skip((p - 1) * ps).Take(ps).ToArray();
    return Results.Ok(new { page = p, pageSize = ps, total = snapshot.Length, items });
});

// GET /users/{id} - get user by id
app.MapGet("/users/{id:guid}", (Guid id) =>
{
    return users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound(new { error = "User not found." });
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
    var added = users.TryAdd(user.Id, user);
    if (!added)
        return Results.Problem(detail: "Failed to add user.", statusCode: 500);

    return Results.CreatedAtRoute("GetUserById", new { id = user.Id }, user);
});

// PUT /users/{id} - update existing user
app.MapPut("/users/{id:guid}", (Guid id, UserUpdateDto dto) =>
{
    // Ensure user exists
    if (!users.TryGetValue(id, out var existing))
        return Results.NotFound();

    // Validate provided fields
    if (dto.Name is not null && IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "If provided, Name cannot be empty." });
    if (dto.Email is not null && !IsValidEmail(dto.Email))
        return Results.BadRequest(new { error = "If provided, Email must be a valid email address." });

    var newName = dto.Name is not null ? dto.Name.Trim() : existing.Name;
    var newEmail = dto.Email is not null ? dto.Email.Trim() : existing.Email;

    var updated = new User(existing.Id, newName, newEmail);

    // Try update with optimistic concurrency; fallback to set on failure
    var updatedSuccessfully = users.TryUpdate(id, updated, existing);
    if (!updatedSuccessfully)
    {
        // last-resort overwrite
        users[id] = updated;
    }

    return Results.NoContent();
});

// DELETE /users/{id} - remove user by id
app.MapDelete("/users/{id:guid}", (Guid id) =>
{
    var removed = users.TryRemove(id, out var _);
    return removed ? Results.NoContent() : Results.NotFound();
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
