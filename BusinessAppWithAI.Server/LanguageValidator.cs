public record Rule(string Field, string RuleText);

public record ValidationInput(string Field, string Value);

public record ValidationResult(bool Valid, string? Message);

public interface ILanguageValidator {
  void SetRule(Rule rule);
  ValidationResult ValidateField(ValidationInput input);
}

public class LanguageValidator : ILanguageValidator {
  private Dictionary<string, string> rules = new();

  public void SetRule(Rule rule) {
    rules[rule.Field] = rule.RuleText;
  }
}