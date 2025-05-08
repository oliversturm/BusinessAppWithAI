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
        Console.WriteLine("Customer added successfully");
    }
}