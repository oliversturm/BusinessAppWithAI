using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IValidator<BusinessObject>, BusinessObjectValidator>();

var app = builder.Build();

app.MapPost("/receiver", async (IValidator<BusinessObject> validator, BusinessObject businessObject) => {
  var result = await validator.ValidateAsync(businessObject);
  if (!result.IsValid) {
    return Results.ValidationProblem(result.ToDictionary());
  }

  return Results.Ok(businessObject);
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