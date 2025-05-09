using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BusinessAppWithAI.Server;

public record SchemaProperty(string Type, string Name);

public record SchemaType(Type Type, string Name, SchemaProperty[] Properties);

public class CodeGenerator {
  const string MODEL_NAME = "gpt-4o";

  // This is often faster than gpt-4o
  // const string MODEL_NAME = "o1-mini";

  const string VALIDATOR_CLASS_NAME = "Validator";

  const string RULES_MARKER = "{{RULES}}";
  const string MODEL_CODE = "{{MODEL}}";

  const string SYSTEM_PROMPT = """
                               You are a developer. You write C# code based on the specifications and rules provided by the user.
                               """;

  const string BASE_PROMPT = $"""
                              Create a static class named '{VALIDATOR_CLASS_NAME}' in C#.
                              The class should contain a static method for each public property of the following schematic type:

                              {MODEL_CODE}

                              Each such method should be named Validate<Property>.
                              The method should accept a parameter of the property's type and return 'string?'.
                              For example, the method for a property "public string Name" would be declared as:

                              public string? ValidateName(string name) ...

                              As a second example, the method for a property "public int Age" would be declared as:

                              public string? ValidateAge(int age) ...

                              The following list contains rules that apply to individual properties of the schema.
                              The field name '_entity' is a special case.

                              {RULES_MARKER}

                              Each of the Validate methods must be implemented such that it checks the validity of the field value according to the rule for the property. If there is no rule for the property, or the validity of the value can be determined without doubt, the method must return 'null'. If the value is not valid, the method must return a short and precise text describing the error condition.
                              Before accessing the value in a validation method, the code must check that the value is not "null" (for reference types).

                              For the special field name '_entity', another method must be generated, named 'ValidateEntity' with the following signature:

                              public string[]? ValidateEntity(Entity entity) ...

                              The type name 'Entity' must be replaced by the name of the previously described schematic type.

                              The implementation of this method must include the validation of all properties by calling the respective Validate<Property> methods, as well as any additional checks specified by the rule for the special field name '_entity'. All validations must always be executed, and any error results must be collected in a result list. If this list contains anything, it is returned as the result of the method; otherwise, the return value is 'null'.

                              Output only code and no descriptive text.
                              The schematic type given above should not be included in the output.
                              Use the directive "#nullable enable" at the beginning of the file.
                              All error messages must be in English, without using technical descriptions like "not null".
                              Use only classes from the namespaces System and System.Text.RegularExpressions in the code.
                              Do NOT use the System.Net.Mail.MailAddress class for email address verification.
                              """;

  const string JS_SYS_PROMPT = """
                               You are a developer. You write JavaScript code based on the specifications and rules provided by the user.
                               """;

  const string CS_CODE = "{{CSCODE}}";

  const string JS_PROMPT = $"""
                            Here is the code for a validation class in C#:

                            {CS_CODE}

                            Create an equivalent implementation in JavaScript, encapsulated in an IIFE structure that produces an object with the methods implemented in C#.
                            Implement the functions separately from the return statement, and don't use "this".
                            Precisely adopt the logic of the implementation.
                            Use undefined instead of null.
                            Use shorthand for checks on undefined, truthy or falsy, without === or !==.
                            Use variable names starting with a lowercase letter, including for the properties of the object.
                            Output only the JavaScript code, no explanations or comments.
                            Output only the IIFE structure, without assignment to a variable.
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
    sb.AppendLine($$"""public class {{schemaType.Type.FullName}} {""");

    foreach (var property in schemaType.Properties) {
      sb.AppendLine($$"""  public {{property.Type}} {{property.Name}} { get; }""");
    }

    sb.AppendLine("}");
    return sb.ToString();
  }

  public Validator? Validator { get; private set; }

  public void UpdateValidator(IEnumerable<string> validationRules) {
    var code = GenerateCsharpCode(validationRules);
    var assembly = CompileValidationCode(code);
    if (assembly != null) {
      var validators = GetValidatorMethods(assembly);
      this.Validator = new Validator(schemaType, validators);
    }
    else {
      this.Validator = null;
    }
  }

  string GenerateCode(string systemPrompt, string userPrompt, string resultCodeShortcut) {
#if DEBUG
    Console.WriteLine($"[PROMPT]:\n{userPrompt}");

    var stopwatch = Stopwatch.StartNew();
#endif
    // Use this for OpenAI connection
    ChatClient client = new(model: MODEL_NAME, apiKey: apiKey);

    // Use this for connection to local model, e.g. LM Studio
    // ChatClient client = new(
    //   "model", // model name -- doesn't matter if just one is loaded, but must be non-empty!
    //   new ApiKeyCredential("key"), // key also can't be empty even if it's not needed
    //   new OpenAIClientOptions() { Endpoint = new Uri("http://localhost:1234/v1") });
    ChatCompletion completion = client.CompleteChat(
      [
        new SystemChatMessage(systemPrompt),
        new UserChatMessage(userPrompt),
        // for o1-preview or o1-mini, combine the two prompts
        //new UserChatMessage(systemPrompt + userPrompt),
      ],
      new ChatCompletionOptions { Temperature = 0, }
    );

    var code = completion.Content[0].Text;
    var match = Regex.Match(code, ".*```" + resultCodeShortcut + "(.*?)\n```.*", RegexOptions.Singleline);
    var processedCode = match.Success ? match.Groups[1].Value : code;
    this.lastGeneratedCode = processedCode;
#if DEBUG
    stopwatch.Stop();
    Console.WriteLine($"[CODE]:\n{processedCode}");
    Console.WriteLine($"[TIME]: {stopwatch.ElapsedMilliseconds}ms");
#endif
    return processedCode;
  }

  string GenerateCsharpCode(IEnumerable<string> validationRules) {
    ArgumentNullException.ThrowIfNull(validationRules);

    string rulesString = "- " + string.Join("\n- ", validationRules);
    var userPrompt = BASE_PROMPT
      .Replace(MODEL_CODE, "\n" + modelCode + "\n")
      .Replace(RULES_MARKER, "\n" + rulesString + "\n");

    return GenerateCode(SYSTEM_PROMPT, userPrompt, "csharp");
  }

  string? lastGeneratedCode;

  public string GetJavaScript() {
    if (lastGeneratedCode == null) {
      return "";
    }

    var userPrompt = JS_PROMPT
      .Replace(CS_CODE, "\n" + lastGeneratedCode + "\n");

    return GenerateCode(JS_SYS_PROMPT, userPrompt, "javascript");
  }

  Assembly? CompileValidationCode(string code) {
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
      [syntaxTree],
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    using var ms = new MemoryStream();
    EmitResult result = compilation.Emit(ms);

    if (!result.Success) {
      foreach (Diagnostic diagnostic in result.Diagnostics) {
        Console.WriteLine(diagnostic.ToString());
      }

      return null;
    }
    else {
      ms.Seek(0, SeekOrigin.Begin);
      return Assembly.Load(ms.ToArray());
    }
  }

  Dictionary<string, MethodInfo> GetValidatorMethods(Assembly assembly) {
    Type? type = assembly.GetType(VALIDATOR_CLASS_NAME);
    if (type == null) {
      throw new Exception($"Type '{VALIDATOR_CLASS_NAME}' not found in assembly.");
    }

    var result = new Dictionary<string, MethodInfo>();
    foreach (var property in schemaType.Properties) {
      string methodName = $"Validate{property.Name}";
      var method = type.GetMethod(methodName);
      if (method != null) {
        result.Add(property.Name, method);
      }
      else {
        throw new Exception($"Method '{methodName}' not found in assembly.");
      }
    }

    var entityMethod = type.GetMethod("ValidateEntity");
    if (entityMethod != null) {
      result.Add("_entity", entityMethod);
    }
    else {
      throw new Exception($"Method 'ValidateEntity' not found in assembly.");
    }

    return result;
  }

  public static SchemaType GetModelSchema<T>() where T : class {
    var t = typeof(T);
    var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
      .Where(p => p.PropertyType.FullName != null)
      .Select(p => new SchemaProperty(p.PropertyType.FullName!, p.Name)).ToArray();
    return new SchemaType(t, t.Name, properties);
  }
}