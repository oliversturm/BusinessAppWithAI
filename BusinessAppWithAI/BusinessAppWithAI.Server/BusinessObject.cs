using FluentValidation;

namespace BusinessAppWithAI.Server;

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