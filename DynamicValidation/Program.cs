using DynamicValidation;
using System.ComponentModel.DataAnnotations;
using DotNetEnv;

internal class Program
{
    private static void Main(string[] args)
    {
        Env.TraversePath().Load();
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        var rules = new[]
        {
            "Required fields are: CompanyName, Street, ZipCode, City, CountryCode, CreditLimit",
            "The address must be in Europe (EU and non-EU countries).",
            "The credit limit must not be less than 0",
            "The credit limit must not be greater than 1 million",
            "The credit limit must not be greater than 50,000 euros if the customer is from a non-EU country.",
            "Website address must be a valid URI if provided.",
            "The email address must be valid if provided.",
            "The phone number must have a valid format if provided",
        };
        
        var codeGenService = new CodeGenerationService(apiKey);
        var validationCode = codeGenService.GenerateValidationCode<CustomerService>(nameof(CustomerService.AddCustomer), rules);
        var method = codeGenService.CompileValidationMethod(validationCode);

        Console.WriteLine($"\n[CODE]:{validationCode}");

        try
        {
            codeGenService.Validate(
                method,
                "Société Générale",
                "29 Boulevard Haussmann",
                "75009",
                "Paris",
                "FR", // France (EU)
                "+33123456789",
                "jean.dupont@societegenerale.fr",
                "https://www.societegenerale.fr",
                1000);
            Console.WriteLine("Validation 1 successful!");
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Validation 1 error: {ex.InnerException.Message}");
        }

        try
        {
            codeGenService.Validate(
                method,
                "Nordmann AS",
                "Karl Johans gate 1",
                "0154",
                "Oslo",
                "NO", // Norway (Non-EU)
                "+4740123456",
                "ola.nordmann@nordmann.no",
                "https://www.nordmann.no",
                60000); // <- Credit limit too high for non-EU customer
            Console.WriteLine("Validation 2 successful!");
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Validation 2 error: {ex.InnerException.Message}");
        }
    }
}
