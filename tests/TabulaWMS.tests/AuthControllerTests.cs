using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TabulaWMS.Controllers;
using TabulaWMS.Data;
using TabulaWMS.Models;
using TabulaWMS.Services;
using TabulaWMS.DTOs;

public class AuthControllerTests
{
    private readonly TabulaContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenService _tokenService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // Setup an isolated Database for testing
        var options = new DbContextOptionsBuilder<TabulaContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Fresh DB per test
            .Options;

        _context = new TabulaContext(options);

        // Setup Identity services wired to the In-Memory DB
        var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<User, IdentityRole, TabulaContext>(_context);
        var idOptions = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<User>();
        var userManagerLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<User>>.Instance;

        // Setup UserManager
        _userManager = new UserManager<User>(
            userStore, idOptions, passwordHasher, null!, null!, null!, null!, null!, userManagerLogger);

        // Setup SignInManager mock dependencies (SignInManager requires HttpContextAccessor and IUserClaimsPrincipalFactory)
        var contextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        var claimsFactory = new UserClaimsPrincipalFactory<User>(_userManager, Microsoft.Extensions.Options.Options.Create(new IdentityOptions()));
        var signInManagerlogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SignInManager<User>>.Instance;
        _signInManager = new SignInManager<User>(_userManager, contextAccessor, claimsFactory, null!, signInManagerlogger, null!, null!);

        // Setup TokenService
        var inMemorySettings = new Dictionary<string, string> {
            {"JwtSettings:Key", "SuperSecretLongKeyThatNoOneWillEverGuess12345_OmgThisThingRequiresALotOfCharacters!"},
            {"JwtSettings:Issuer", "TabulaWMS"},
            {"JwtSettings:Audience", "TabulaWMSClient"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _tokenService = new TokenService(configuration);

        // 4. Instantiate the Controller
        _controller = new AuthController(_userManager, _signInManager, _tokenService);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange: Create a test user in the in-memory database
        var user = new User { UserName = "test@warehouse.com", Email = "test@warehouse.com", Name = "Test User", Role = "Operator" };
        await _userManager.CreateAsync(user, "SecurePassword123!");

        var loginDto = new LoginDto { Email = "test@warehouse.com", Password = "SecurePassword123!" };

        // Act: Hit the login endpoint
        var result = await _controller.Login(loginDto);

        // Assert: Verify we get an Ok response containing a token
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Optional: Reflection or dynamic check to ensure token exists in the anonymous object
        var tokenProperty = okResult.Value.GetType().GetProperty("token");
        var tokenValue = tokenProperty?.GetValue(okResult.Value, null) as string;

        Assert.False(string.IsNullOrEmpty(tokenValue));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange: Create a test user
        var user = new User { UserName = "test@warehouse.com", Email = "test@warehouse.com", Name = "Test User", Role = "Operator" };
        await _userManager.CreateAsync(user, "SecurePassword123!");

        var loginDto = new LoginDto { Email = "test@warehouse.com", Password = "WrongPassword!" };

        // Act: Hit login with the wrong password
        var result = await _controller.Login(loginDto);

        // Assert: Verify it returns 401 Unauthorized
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange: Use an email that was never registered in the in-memory DB
        var loginDto = new LoginDto { Email = "ghost@warehouse.com", Password = "SomePassword123!" };

        // Act: Attempt to login
        var result = await _controller.Login(loginDto);

        // Assert: Verify it returns 401 Unauthorized (and doesn't crash with a 500 error)
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}