using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IValidator<BusinessObject>, BusinessObjectValidator>();

builder.Services.AddCors(options => {
  options.AddDefaultPolicy(builder => {
    builder.WithOrigins("http://localhost:51655")
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});

var app = builder.Build();
app.UseCors();

app.MapPost("/receiver", async (IValidator<BusinessObject> validator, BusinessObject businessObject) => {
  var result = await validator.ValidateAsync(businessObject);
  if (!result.IsValid) {
    return Results.ValidationProblem(result.ToDictionary());
  }

  return Results.Ok(businessObject);
});

app.MapPost("/api/configureRule", async (Rule rule) => {
  Console.WriteLine($"Setting rule for '{rule.Field}': '{rule.RuleText}'");
  return Results.Ok(rule);
});

app.MapPost("/api/validate", async (ValidationInput input) => {
  Console.WriteLine($"Validating field '{input.Field}' with value '{input.Value}'");
  return Results.Ok(new ValidationResult(true, null));
});

app.Run();

public record BusinessObject(string Name, int Age, string Email);

public class BusinessObjectValidator : AbstractValidator<BusinessObject> {
  public BusinessObjectValidator() {
    RuleFor(x => x.Name).NotEmpty();
    RuleFor(x => x.Age).InclusiveBetween(0, 120);
    RuleFor(x => x.Email).EmailAddress().Must(BelongToValidDomain);
  }

  bool BelongToValidDomain(string email) {
    string[] validDomains = ["neogeeks.de", "oliversturm.com"];

    var domain = email.Split("@")[1];
    return validDomains.Contains(domain);
  }
}

public record Rule(string Field, string RuleText);

public record ValidationInput(string Field, string Value);

public record ValidationResult(bool Valid, string? Message);