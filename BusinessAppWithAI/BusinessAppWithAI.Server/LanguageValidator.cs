using System.Reflection;
using System.Text.Json;

namespace BusinessAppWithAI.Server;

public record Rule(string Field, string RuleText);

public record ValidationInput(string Field, string Value);

public record ValidationResult(bool Valid, string? Message);

public class Validator(SchemaType schemaType, Dictionary<string, MethodInfo> validators) {
  public string? ValidateField(string field, string value, string targetType) {
    ArgumentNullException.ThrowIfNull(field);
    ArgumentNullException.ThrowIfNull(targetType);

    if (validators.TryGetValue(field, out var validator)) {
      var conversionType = Type.GetType(targetType);
      if (conversionType == null) {
        throw new Exception($"Cannot find type '{targetType}'");
      }

      object? typedValue;
      if (value != null) {
        typedValue = Convert.ChangeType(value, conversionType);
        if (typedValue == null) {
          throw new Exception($"Cannot convert '{value}' (type '{value.GetType().FullName}') to type '{targetType}'");
        }
      }
      else
        typedValue = null;

      return (string?)validator.Invoke(null, [typedValue]);
    }

    return null;
  }

  public string[]? ValidateEntity(string jsonEntity) {
    if (validators.TryGetValue("_entity", out var validator)) {
      var entity = JsonSerializer.Deserialize(jsonEntity, schemaType.Type,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
      if (entity == null) {
        throw new Exception($"Cannot deserialize JSON entity '{jsonEntity}'");
      }

      return (string[]?)validator.Invoke(null, [entity]);
    }

    return null;
  }
}

public interface ILanguageValidator {
  void SetRules(Rule[] rules);
  string GetJavaScript();
  ValidationResult ValidateField(ValidationInput input);
}

public class LanguageValidator : ILanguageValidator {
  public LanguageValidator() {
    schemaType = CodeGenerator.GetModelSchema<BusinessObject>();
    var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (string.IsNullOrWhiteSpace(openAIApiKey)) {
      throw new Exception("OPENAI_API_KEY is not set");
    }

    generator = new CodeGenerator(openAIApiKey, schemaType);
  }

  private readonly Dictionary<string, string> rules = new();
  private readonly SchemaType schemaType;
  private readonly CodeGenerator generator;

  public void SetRules(Rule[] rules) {
    foreach (Rule rule in rules) {
      this.rules[rule.Field] = rule.RuleText;
    }

    generator.UpdateValidator(AllValidationRules());
  }

  public string GetJavaScript() {
    return generator.GetJavaScript();
  }

  string MakeCSharpFieldName(string jsonFieldName) {
    if (string.IsNullOrEmpty(jsonFieldName))
      return jsonFieldName;
    return char.ToUpper(jsonFieldName[0]) + jsonFieldName[1..];
  }

  IEnumerable<string> AllValidationRules() {
    foreach (var rule in rules) {
      yield return $"Field '{MakeCSharpFieldName(rule.Key)}': {rule.Value}";
    }
  }

  public ValidationResult ValidateField(ValidationInput input) {
    var validator = generator.Validator;
    if (validator == null) {
      return new ValidationResult(true, null);
    }

    if (input.Field == "_entity") {
      var result = validator.ValidateEntity(input.Value);
      return result switch
      {
        null => new ValidationResult(true, null),
        _ => new ValidationResult(false, string.Join("\n", result))
      };
    }
    else {
      var csharpFieldName = MakeCSharpFieldName(input.Field);
      var result = validator.ValidateField(csharpFieldName, input.Value,
        schemaType.Properties.First(p => p.Name == csharpFieldName).Type);
      return new ValidationResult(result == null, result);
    }
  }
}