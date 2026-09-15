using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Messenger.Server.Data;
using Messenger.Server.Hubs;
using Messenger.Shared.Dtos;
using Messenger.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    o.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/chathub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

var passwordHasher = new PasswordHasher<User>();

// POST /register
app.MapPost("/register", async (RegisterRequest req, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Username == req.Username))
        return Results.Conflict("Username already taken.");

    var user = new User { Id = Guid.NewGuid(), Username = req.Username, CreatedAt = DateTime.UtcNow };
    user.PasswordHash = passwordHasher.HashPassword(user, req.Password);
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// POST /login
app.MapPost("/login", async (LoginRequest req, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
    if (user is null)
        return Results.Unauthorized();

    if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});

// GET /users?search=query
app.MapGet("/users", async (ClaimsPrincipal principal, AppDbContext db, string? search) =>
{
    if (string.IsNullOrWhiteSpace(search))
        return Results.Ok(Array.Empty<UserDto>());

    var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var users = await db.Users
        .Where(u => u.Id != userId && u.Username.Contains(search))
        .Select(u => new UserDto { Id = u.Id, Username = u.Username })
        .ToListAsync();
    return Results.Ok(users);
}).RequireAuthorization();

// GET /chats
app.MapGet("/chats", async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var chatIds = await db.ChatMembers
        .Where(cm => cm.UserId == userId)
        .Select(cm => cm.ChatId)
        .ToListAsync();

    var result = new List<ChatDto>();
    foreach (var chatId in chatIds)
    {
        var otherUsername = await db.ChatMembers
            .Where(cm => cm.ChatId == chatId && cm.UserId != userId)
            .Join(db.Users, cm => cm.UserId, u => u.Id, (cm, u) => u.Username)
            .FirstOrDefaultAsync();

        var lastMsg = await db.Messages
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.SentAt)
            .Select(m => new { m.Text, m.SentAt })
            .FirstOrDefaultAsync();

        result.Add(new ChatDto
        {
            Id = chatId,
            OtherUserName = otherUsername ?? "",
            LastMessageText = lastMsg?.Text,
            LastMessageAt = lastMsg?.SentAt
        });
    }

    return Results.Ok(result);
}).RequireAuthorization();

// POST /chats/private
app.MapPost("/chats/private", async (CreatePrivateChatRequest req, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var myChats = await db.ChatMembers
        .Where(cm => cm.UserId == userId)
        .Select(cm => cm.ChatId)
        .ToListAsync();

    var theirChats = await db.ChatMembers
        .Where(cm => cm.UserId == req.OtherUserId)
        .Select(cm => cm.ChatId)
        .ToListAsync();

    var sharedIds = myChats.Intersect(theirChats).ToList();

    Guid? existingChatId = null;
    foreach (var id in sharedIds)
    {
        var count = await db.ChatMembers.CountAsync(cm => cm.ChatId == id);
        if (count == 2) { existingChatId = id; break; }
    }

    if (existingChatId.HasValue)
    {
        var lastMsg = await db.Messages
            .Where(m => m.ChatId == existingChatId.Value)
            .OrderByDescending(m => m.SentAt)
            .Select(m => new { m.Text, m.SentAt })
            .FirstOrDefaultAsync();
        var other = await db.Users.FindAsync(req.OtherUserId);
        return Results.Ok(new ChatDto
        {
            Id = existingChatId.Value,
            OtherUserName = other?.Username ?? "",
            LastMessageText = lastMsg?.Text,
            LastMessageAt = lastMsg?.SentAt
        });
    }

    var chat = new Chat { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
    db.Chats.Add(chat);
    db.ChatMembers.Add(new ChatMember { ChatId = chat.Id, UserId = userId });
    db.ChatMembers.Add(new ChatMember { ChatId = chat.Id, UserId = req.OtherUserId });
    await db.SaveChangesAsync();

    var otherUser = await db.Users.FindAsync(req.OtherUserId);
    return Results.Ok(new ChatDto
    {
        Id = chat.Id,
        OtherUserName = otherUser?.Username ?? "",
        LastMessageText = null,
        LastMessageAt = null
    });
}).RequireAuthorization();

app.MapHub<ChatHub>("/chathub");

app.Run();
