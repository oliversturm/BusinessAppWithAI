public class CustomerService
{
    public void AddCustomer(
        string companyName, 
        string street, 
        string zipcode, 
        string city, 
        string countryCode, 
        string telefon, 
        string emailAdress, 
        string websiteUri, 
        double orderLimit = 0)
    {
        // Logik zum Hinzufügen des Kunden
        // ...

        Console.WriteLine("Kunde erfolgreich hinzugefügt!");
    }
}