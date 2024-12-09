using DynamicValidation;
using System.ComponentModel.DataAnnotations;
using DotNetEnv;

internal class Program
{
    private static void Main(string[] args)
    {
        Env.TraversePath().Load();
        // API Key auslesen
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        // Regeln festlegen
        var rules = new[]
        {
            "Pflichtfelder sind: CompanyName, Street, ZipCode, City, CountryCode, CreditLimit",
            "Adresse muss in Europa sein (EU und Nicht-EU-Staaten).",
            "Das Kreditlimit darf nicht kleiner als 0 sein",
            "Das Kreditlimit darf nicht größer als 1 Million sein",
            "Das Kreditlimit darf nicht größer als 50000 Euro sein, wenn der Kunde aus einem Nicht-EU-Land kommt.",
            "Website Adresse muss eine gültige URI sein, wenn sie gefüllt ist.",
            "Die E-Mail-Adresse muss eine gültige Adresse sein, wenn sie gefüllt ist.",
            "Die Telefonnummer muss ein gültiges Format haben, wenn sie gefüllt ist",
        };
        
        // Code generieren
        var codeGenService = new CodeGenerationService(apiKey);
        var validationCode = codeGenService.GenerateValidationCode<CustomerService>(nameof(CustomerService.AddCustomer), rules);
        //var validationCode = codeGenService.GenerateValidationCode<Customer>(rules);
        var method = codeGenService.CompileValidationMethod(validationCode);

        Console.WriteLine($"\n[CODE]:{validationCode}");

        // Validierung durchführen
        try
        {
            codeGenService.Validate(
                method,
                "Mustermann GmbH",
                "Musterstraße 1",
                "12345",
                "Musterstadt",
                "DE", // NGA 
                "0491725395187",
                "jörg.neumann@neogeeks.de",
                "https://www.mustermann.de",
                1000);
            Console.WriteLine("Validierung 1 erfolgreich!");

            codeGenService.Validate(
                method,
                "Mustermann GmbH",
                "Musterstraße 1",
                "12345",
                "Musterstadt",
                "NO",
                "00491725395187",
                "joerg.neumann@neogeeks.de",
                "https://www.mustermann.de",
                60000); // <- Kreditlimit zu hoch für Nicht-EU-Kunde
            Console.WriteLine("Validierung 2 erfolgreich!");
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Validierungsfehler: {ex.InnerException.Message}");
        }
    }
}
