using BCrypt.Net;
using DeckDuel2.Configuration;
using DeckDuel2.Data;
using DeckDuel2.Domain;
using DeckDuel2.DTOs;
using DeckDuel2.Extensions;
using DeckDuel2.Hubs;
using DeckDuel2.Models;
using DeckDuel2.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing connection string: DefaultConnection");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60);
    }));

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), $"{JwtOptions.SectionName}:SigningKey is required.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing configuration section: Jwt");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("Missing configuration value: Jwt:SigningKey");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // Only read query token for SignalR hub requests
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/games"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddOptions<AzureOpenAIOptions>()
    .Bind(builder.Configuration.GetSection(AzureOpenAIOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint)
                && !string.IsNullOrWhiteSpace(o.ApiKey)
                && !string.IsNullOrWhiteSpace(o.DeploymentName),
        "AzureOpenAI Endpoint, ApiKey and DeploymentName are all required.")
    .ValidateOnStart();

builder.Services.AddScoped<AIService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IGameRealtimeNotifier, GameRealtimeNotifier>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5174"
                        , "http://localhost:5173"
                        , "https://witty-hill-0ad3d2b1e.2.azurestaticapps.net"
                        , "http://witty-hill-0ad3d2b1e.2.azurestaticapps.net")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // important for SignalR from browser
    });
});

builder.Services.AddAuthorization();
builder.Services.AddOpenApi(options => options.AddBearerSecurityScheme());


// Register repository
builder.Services.AddScoped<IDeckRepository, DeckRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IGameService, GameService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();


//Configure endpoints

app.MapGet("/getSharedDeckNames", async (IDeckRepository deckRepo) =>
{
    var decks = await deckRepo.GetSharedDeckNamesAsync();
    return TypedResults.Ok(decks);
}).WithName("GetSharedDeckNames");


app.MapPost("/registerUser", async (UserDto userDto, IUserService userService) =>
{
    var result = await userService.RegisterUserAsync(userDto);
    
    if (!result.Success)
        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );

    return TypedResults.Created();
}).WithName("RegisterUser");

app.MapPost("/login", async (UserDto login, IUserService userService, IUserRepository userRepo) =>
{
    var result = await userService.LoginAsync(login);

    if (!result.Success)
    {
        if (result.ErrorType == DDError.Unauthorized)
            return Results.Unauthorized();

        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    var user = await userRepo.GetUserByUsernameAsync(login.Username);
    if (user == null)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        token = result.Value,
        inGameName = user.InGameName
    });
});

app.MapPost("/decks", async (GenerateDeckRequestDto request, ClaimsPrincipal user, IDeckService deckService, IUserRepository userRepo) =>
{
    var validationResults = new List<ValidationResult>();
    var isValid = Validator.TryValidateObject(
        request,
        new ValidationContext(request),
        validationResults,
        validateAllProperties: true);

    if (!isValid)
    {
        var errors = validationResults
            .SelectMany(v => v.MemberNames.DefaultIfEmpty(string.Empty), (v, member) => new { member, v.ErrorMessage })
            .GroupBy(x => x.member)
            .ToDictionary(
                g => string.IsNullOrWhiteSpace(g.Key) ? "deckPrompt" : g.Key,
                g => g.Select(x => x.ErrorMessage ?? "Invalid value.").ToArray());

        return Results.ValidationProblem(errors);
    }

    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await deckService.GenerateDeckAsync(request.DeckPrompt, userEntity.Id);

    if (!result.Success)
        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest);

    return Results.Ok(result.Value);
}).WithName("GenerateDeck").RequireAuthorization();

app.MapGet("/decks", async (ClaimsPrincipal user, IDeckService deckService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await deckService.GetDecksAsync(userEntity.Id);
    
    if (!result.Success)
        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );

    return Results.Ok(result.Value);
}).WithName("GetDecks").RequireAuthorization();

app.MapGet("/decks/{deckId:int}/cards", async (int deckId, ClaimsPrincipal user, IDeckService deckService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await deckService.GetDeckCardsAsync(deckId, userEntity.Id);
    
    if (!result.Success)
        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );

    return Results.Ok(result.Value);
}).WithName("GetDeckCards").RequireAuthorization();

app.MapGet("/games/open", async (ClaimsPrincipal user, IGameService gameService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await gameService.GetOpenGamesAsync(userEntity.Id);
    if (!result.Success)
        return Results.Problem(title: result.ErrorType.ToString(), detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

    return Results.Ok(result.Value);
}).WithName("GetOpenGames").RequireAuthorization();

app.MapGet("/games/active", async (ClaimsPrincipal user, IGameService gameService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await gameService.GetActiveGamesAsync(userEntity.Id);
    if (!result.Success)
        return Results.Problem(title: result.ErrorType.ToString(), detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

    return Results.Ok(result.Value);
}).WithName("GetActiveGames").RequireAuthorization();


app.MapPost("/games", async (CreateGameDto dto, ClaimsPrincipal user, IGameService gameService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await gameService.CreateGameAsync(dto.DeckId, userEntity.Id);

    if (!result.Success)
        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );

    var location = $"/games/{result.Value.GameId}";
    return Results.Created(location, result.Value);
}).WithName("CreateGame").RequireAuthorization();

app.MapPost("/games/{gameId:int}/start", async (int gameId, ClaimsPrincipal user, IGameService gameService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await gameService.StartGameAsync(gameId, userEntity.Id);

    if (!result.Success)
        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );

    return Results.Ok(result.Value);
}).WithName("StartGame").RequireAuthorization();



app.MapPost("/games/{gameId:int}/players", async (int gameId, ClaimsPrincipal user, IGameService gameService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await gameService.JoinGameAsync(gameId, userEntity.Id);

    if (!result.Success)
    {
        if (result.ErrorType == DDError.NotFound)
            return Results.NotFound(result.Error);

        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    var location = $"/games/{gameId}/players/{result.Value.Id}";
    return Results.Created(location, result.Value);
}).WithName("JoinGame").RequireAuthorization();



app.MapGet("/games/usergames/{userGameId:int}/card", async (int userGameId, ClaimsPrincipal user, IGameService gameService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await gameService.GetCurrentHandTopCardAsync(userGameId, userEntity.Id);

    if (!result.Success)
    {
        if (result.ErrorType == DDError.NotFound) return Results.NotFound(result.Error);
        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    return Results.Ok(result.Value);
}).WithName("GetCurrentHandTopCard").RequireAuthorization();

app.MapPost("/games/usergames/{userGameId:int}/turns", async (int userGameId, TakeTurnDto dto, ClaimsPrincipal user, IGameService gameService, IGameRepository gameRepo, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var userGame = await gameRepo.GetUserGameByIdAsync(userGameId);
    if (userGame == null) return Results.NotFound("UserGame not found.");

    // Ensure caller can only play for their own userGame
    if (userGame.UserId != userEntity.Id) return Results.Unauthorized();

    var result = await gameService.TakeTurnAsync(userGame.GameId, userEntity.Id, dto.CategoryTypeId);

    if (!result.Success)
    {
        if (result.ErrorType == DDError.NotFound) return Results.NotFound(result.Error);
        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    return Results.Ok(result.Value);
}).WithName("TakeTurn").RequireAuthorization();

app.MapGet("/games/usergames/{userGameId:int}/status", async (int userGameId, ClaimsPrincipal user, IGameService gameService, IUserRepository userRepo) =>
{
    var userEntity = await user.GetAuthenticatedUserAsync(userRepo);
    if (userEntity == null) return Results.Unauthorized();

    var result = await gameService.GetCurrentGameStatusAsync(userGameId, userEntity.Id);

    if (!result.Success)
    {
        if (result.ErrorType == DDError.NotFound) return Results.NotFound(result.Error);
        if (result.ErrorType == DDError.NotOwner) return Results.Unauthorized();

        return Results.Problem(
            title: result.ErrorType.ToString(),
            detail: result.Error,
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    return Results.Ok(result.Value);
}).WithName("GetCurrentGameStatus").RequireAuthorization();

app.MapHub<GameHub>("/hubs/games");

app.Run();
