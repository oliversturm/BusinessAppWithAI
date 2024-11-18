using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using OpenAI.Chat;
using System.Reflection;
using System.Text;

namespace BusinessAppWithAI.Server;

public record SchemaProperty(string Type, string Name);

public record SchemaType(string Name, SchemaProperty[] Properties);

public class Validator(Dictionary<string, MethodInfo> validators) {
  string? ValidateField(string field, string value, string targetType) {
    if (validators.TryGetValue(field, out var validator)) {
      object typedValue = Convert.ChangeType(value, Type.GetType(targetType));
      return (string?)validator.Invoke(null, [typedValue]);
    }

    return null;
  }
}

public class CodeGenerator {
  const string MODEL_NAME = "gpt-4o";

  const string VALIDATOR_CLASS_NAME = "Validator";

  const string RULES_MARKER = "{{RULES}}";
  const string MODEL_CODE = "{{MODEL}}";

  const string SYSTEM_PROMPT = """
                               Du bist ein Entwickler. Du schreibst C#-Code auf Basis der Vorgaben und Regeln des Benutzers.
                               """;

  const string BASE_PROMPT = $"""
                              Erstelle eine statische Klasse mit dem Namen '{VALIDATOR_CLASS_NAME}' in C#.
                              Die Klasse sollte je eine statische Methode enthalten für jede public-Property des
                              folgenden schematischen Typs:

                              {MODEL_CODE}

                              Jede solche Methode sollte den Namen Validate<Property> haben.
                              Die Methode sollte einen Parameter vom Typ der Property entgegennehmen und 'string?' zurückgeben.
                              Zum Beispiel wäre die Methode für eine Property "public string Name" so deklariert:
                              public string? ValidateName(string name) ...
                              Als zweites Beispiel wäre die Methode für eine Property "public int Age" so deklariert:
                              public string? ValidateAge(int age) ...

                              Die folgende Liste enthält Regeln, die sich auf einzelne Properties des Schemas beziehen.
                              Der Feldname '_entity' ist ein Sonderfall.

                              {RULES_MARKER}

                              Jeder der Validate-Methoden muss nun so implementiert werden, dass sie entsprechend der Regel
                              fǔr die Property die Validität des Feldwertes prüft. Sollte es keine Regel für die Property
                              geben, oder die Validität des Wertes kann zweifelsfrei festgestellt werden, muss die Methode
                              'null' zurückgeben. Wenn die Validität nicht gegeben ist, muss ein Text zurückgegeben werden,
                              der den Fehlerzustand kurz und präzise auf deutsch beschreibt.

                              Für den besonderen Feldnamen '_entity' muss eine weitere Methode generiert werden, mit 
                              dem Namen 'ValidateEntity' und folgender Signatur: 

                              public string[]? ValidateEntity(Entity entity) ...

                              Der Typname 'Entity' muss dabei durch den Namen des zuvor beschriebenen schematischen
                              Typs ersetzt werden.

                              Die Implementation dieser Methode muss die Validierung aller Properties durch Aufruf
                              der jeweiligen Validate<Property>-Methoden beinhalten, sowie weitere Prüfungen, wie sie
                              durch die Regel für den besonderen Feldnamen '_entity' vorgegeben sind. Es müssen
                              immer alle Validierungen ausgeführt werden, und etwaige Fehlerresultate werden in der 
                              Resultatsliste gesammelt. Wenn diese Liste letztlich etwas enthält, wird sie als
                              Result zurückgegeben, andernfalls ist der Rückgabewert 'null'.

                              Gib ausschließlich Code und keinerlei Beschreibungstext aus.
                              Gib alle Fehlertexte in deutsch aus.
                              Verwende im Code nur Klassen aus den Namespaces System und System.Text.RegularExpressions.
                              Verwende NICHT die Klasse System.Net.Mail.MailAddress zur Verifikation von Email-Adressen.
                              """;

  private readonly string apiKey;
  private readonly SchemaType schemaType;
  private readonly string modelCode;

  public CodeGenerator(string openAIApiKey, SchemaType schemaType) {
    ArgumentNullException.ThrowIfNull(openAIApiKey);
    ArgumentNullException.ThrowIfNull(schemaType);
    apiKey = openAIApiKey;
    this.schemaType = schemaType;
    modelCode = GenerateModelCode();
  }

  string GenerateModelCode() {
    var sb = new StringBuilder();
    sb.AppendLine($$"""public class {{schemaType.Name}} {""");

    foreach (var property in schemaType.Properties) {
      sb.AppendLine($$"""  public {{property.Type}} {{property.Name}} { get; }""");
    }

    sb.AppendLine("}");
    return sb.ToString();
  }

  public void UpdateValidator() {
  }

  string GenerateCode(IEnumerable<string> validationRules) {
    ArgumentNullException.ThrowIfNull(schemaType);
    ArgumentNullException.ThrowIfNull(validationRules);

    string rulesString = "- " + string.Join(", \n- ", validationRules);
    var userPrompt = BASE_PROMPT
      .Replace(MODEL_CODE, "\n" + modelCode + "\n")
      .Replace(RULES_MARKER, "\n" + rulesString + "\n");

#if DEBUG
    Console.WriteLine($"[PROMPT]:\n{userPrompt}");
#endif
    ChatClient client = new(model: MODEL_NAME, apiKey: apiKey);
    ChatCompletion completion = client.CompleteChat(
      [
        new SystemChatMessage(SYSTEM_PROMPT),
        new UserChatMessage(userPrompt),
      ],
      new ChatCompletionOptions { Temperature = 0, });

    var code = completion.Content[0].Text;
    var index = code.IndexOf(@"```csharp");
    if (index > -1) {
      var last = code.IndexOf(@"```", index + 9);
      if (last > -1) {
        code = code.Substring(index + 9, last - index - 9);
      }
      else {
        code = code.Substring(index + 9, code.Length - index - 9);
      }
    }

    return code;
  }

  MethodInfo? CompileValidationCode(string code) {
    ArgumentNullException.ThrowIfNull(code);

    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

    string assemblyName = Path.GetRandomFileName();
    var references = AppDomain.CurrentDomain.GetAssemblies()
      .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
      .Select(a => MetadataReference.CreateFromFile(a.Location))
      .Cast<MetadataReference>().ToList();
    references.Add(
      MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).Assembly.Location));

    CSharpCompilation compilation = CSharpCompilation.Create(
      assemblyName,
      new[] { syntaxTree },
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    MethodInfo method = null;

    using var ms = new MemoryStream();
    EmitResult result = compilation.Emit(ms);

    if (!result.Success) {
      foreach (Diagnostic diagnostic in result.Diagnostics) {
        Console.WriteLine(diagnostic.ToString());
      }
    }
    else {
      ms.Seek(0, SeekOrigin.Begin);
      Assembly assembly = Assembly.Load(ms.ToArray());
      Type type = assembly.GetType(VALIDATOR_CLASS_NAME);
      method = type.GetMethod(VALIDATION_METHOD_NAME);
    }

    return method;
  }

  public void Validate(MethodInfo method, params object[] validationInput) {
    ArgumentNullException.ThrowIfNull(method);
    ArgumentNullException.ThrowIfNull(validationInput);
    try {
      method.Invoke(null, validationInput);
    }
    catch (TargetInvocationException ex) {
      throw new ValidationException("Validation error. See inner exception for details.", ex.InnerException);
    }
  }

  public static SchemaType GetModelSchema<T>() where T : class {
    var t = typeof(T);
    var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(
      p => new SchemaProperty(p.PropertyType.Name, p.Name)).ToArray();
    return new SchemaType(t.Name, properties);
  }
}