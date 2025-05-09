#nullable enable

using System;
using System.Text.RegularExpressions;

public static class Validator
{
    public static string? ValidateName(string name)
    {
        if (name == null) return "Name is required.";
        if (name.Length < 3) return "Name must be at least three characters long.";
        return null;
    }

    public static string? ValidateAge(int age)
    {
        if (age < 1 || age > 120) return "Age must be between 1 and 120.";
        return null;
    }

    public static string? ValidateEmail(string email)
    {
        if (email == null) return "Email is required.";
        var emailPattern = @"^[^@\s]+@((oliversturm\.com)|(neogeeks\.de))$";
        if (!Regex.IsMatch(email, emailPattern)) return "Email must be a valid address at oliversturm.com or neogeeks.de.";
        return null;
    }

    public static string[]? ValidateEntity(BusinessAppWithAI.Server.BusinessObject entity)
    {
        var errors = new System.Collections.Generic.List<string>();

        var nameError = ValidateName(entity.Name);
        if (nameError != null) errors.Add(nameError);

        var ageError = ValidateAge(entity.Age);
        if (ageError != null) errors.Add(ageError);

        var emailError = ValidateEmail(entity.Email);
        if (emailError != null) errors.Add(emailError);

        if (entity.Age < 70 && (entity.Name == "Percival" || entity.Name == "Ethelreda"))
        {
            errors.Add("People under 70 must not be named Percival or Ethelreda.");
        }

        return errors.Count > 0 ? errors.ToArray() : null;
    }
}