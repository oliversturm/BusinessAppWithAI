using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using OpenAI.Chat;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace DynamicValidation
{
    internal class CodeGenerationService
    {
        const string MODEL_NAME = "gpt-4o";

        const string METHOD_TEXT = "Web-API-Funktion";
        const string MODEL_TEXT = "'Data Transfer Object'-Klasse";
        const string VALIDATOR_CLASS_NAME = "Validator";
        const string VALIDATION_METHOD_NAME = "ValidateParameters";
        const string TYPE_MARKER = "{{TYPE}}";
        const string CODE_MARKER = "{{CODE}}";
        const string RULES_MARKER = "{{RULES}}";
        const string SYSTEM_PROMPT = """
            Du bist ein Entwickler. Du schreibst C#-Code auf Basis der Vorgaben und Regeln des Benutzers.
            """;
        const string BASE_PROMPT = $"""
            Erstelle eine Methode zur Validierung aller Eingabeparameter einer {TYPE_MARKER} in C# Code.
            Deklariere die Methode innerhalb einer Klasse mit dem Namen '{VALIDATOR_CLASS_NAME}'.
            Die Methode soll statisch deklariert werden und den Namen '{VALIDATION_METHOD_NAME}' tragen.
            Ist ein Parameter nicht valide, soll eine entsprechende Exception ausgelöst werden.
            Der Aufbau der {TYPE_MARKER} sieht wie folgt aus:
            {CODE_MARKER}
            Berücksichtige bei der Validierung bitte die folgenden Regeln:
            {RULES_MARKER}
            Gib ausschließlich Code und keinerlei Beschreibungstext aus.
            Gib die Fehlertexte in deutsch aus.
            Verwende im Code nur Klassen aus den Namespaces System und System.Text.RegularExpressions.
            Verwende NICHT die Klasse System.Net.Mail.MailAddress zur Verifikation der Email-Adresse.
            """;
        private readonly string apiKey;

        public CodeGenerationService(string openAIApiKey)
        {
            ArgumentNullException.ThrowIfNull(openAIApiKey);
            apiKey = openAIApiKey;
        }

        public string GenerateValidationCode<T>(IEnumerable<string> validationRules) where T : class
        {
            var signature = SignatureExtractor.GetModelSignature<T>();
            var prompt = BASE_PROMPT.Replace(TYPE_MARKER, MODEL_TEXT);
            return GenerateCode(prompt, signature, validationRules);
        }

        public string GenerateValidationCode<T>(string methodName, IEnumerable<string> validationRules) where T : class
        {
            var signature = SignatureExtractor.GetMethodSignature<T>(methodName);
            var prompt = BASE_PROMPT.Replace(TYPE_MARKER, METHOD_TEXT);
            return GenerateCode(prompt, signature, validationRules);
        }

        public string GenerateCode(string basePrompt, string signature, IEnumerable<string> validationRules)
        {
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentNullException.ThrowIfNull(validationRules);
            
            string rulesString = "- " + string.Join(", \n- ", validationRules);
            var userPrompt = basePrompt
                .Replace(CODE_MARKER, "\n" + signature + "\n")
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
            new ChatCompletionOptions
            {
                Temperature = 0,
            });

            var code = completion.Content[0].Text;
            var index = code.IndexOf(@"```csharp");
            if (index > -1)
            {
                var last = code.IndexOf(@"```", index + 9);
                if (last > -1)
                {
                    code = code.Substring(index + 9, last - index - 9);
                }
                else
                {
                    code = code.Substring(index + 9, code.Length - index - 9);
                }
            }
            return code;
        }

        public MethodInfo? CompileValidationMethod(string code, params object[] validationInput)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentNullException.ThrowIfNull(validationInput);
            MethodInfo method = null;
            
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            string assemblyName = Path.GetRandomFileName();
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>().ToList();
            references.Add(MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).Assembly.Location));

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    foreach (Diagnostic diagnostic in result.Diagnostics)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    Type type = assembly.GetType(VALIDATOR_CLASS_NAME);
                    method = type.GetMethod(VALIDATION_METHOD_NAME);
                }
            }
            return method;
        }

        public void Validate(MethodInfo method, params object[] validationInput)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(validationInput);
            try
            {
                method.Invoke(null, validationInput);
            }
            catch (TargetInvocationException ex)
            {
                throw new ValidationException("Validation error. See inner exception for details.", ex.InnerException);
            }
        }

        static class SignatureExtractor
        {
            public static string GetModelSignature<T>() where T : class
            {
                var sb = new StringBuilder();
                var attributes = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
                sb.Append(string.Join(", ", attributes.Select(p => p.PropertyType.Name + " " + p.Name)));
                return sb.ToString();
            }

            public static string GetMethodSignature<T>(string methodName) where T : class
            {
                ArgumentNullException.ThrowIfNullOrWhiteSpace(methodName);

                var addMethod = typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                return SignatureExtractor.GetMethodSignature(addMethod);
            }

            public static string GetMethodSignature(MethodInfo method)
            {
                ArgumentNullException.ThrowIfNull(method);

                var sb = new StringBuilder();

                if (method.IsPublic)
                    sb.Append("public ");
                else if (method.IsPrivate)
                    sb.Append("private ");
                else if (method.IsFamily)
                    sb.Append("protected ");
                else if (method.IsAssembly)
                    sb.Append("internal ");
                else if (method.IsFamilyOrAssembly)
                    sb.Append("protected internal ");

                if (method.IsStatic)
                    sb.Append("static ");

                sb.Append(GetTypeName(method.ReturnType));
                sb.Append(' ');

                sb.Append(method.Name);

                if (method.IsGenericMethod)
                {
                    Type[] generics = method.GetGenericArguments();
                    sb.Append('<');
                    sb.Append(string.Join(", ", generics.Select(t => t.Name)));
                    sb.Append('>');
                }

                ParameterInfo[] parameter = method.GetParameters();
                sb.Append('(');
                sb.Append(string.Join(", ", parameter.Select(p => GetParameterSignature(p))));
                sb.Append(')');

                return sb.ToString();
            }

            static string GetParameterSignature(ParameterInfo parameter)
            {
                StringBuilder sb = new StringBuilder();

                if (parameter.IsIn)
                    sb.Append("in ");
                else if (parameter.IsOut)
                    sb.Append("out ");
                else if (parameter.ParameterType.IsByRef)
                    sb.Append("ref ");

                Type paramType = parameter.ParameterType;
                if (paramType.IsByRef)
                    paramType = paramType.GetElementType();

                sb.Append(GetTypeName(paramType));

                sb.Append(' ');
                sb.Append(parameter.Name);

                return sb.ToString();
            }

            static string GetTypeName(Type type)
            {
                if (type == null)
                    return "void";

                if (type.IsGenericType)
                {
                    string typeName = type.Name;
                    int backtickIndex = typeName.IndexOf('`');
                    if (backtickIndex > 0)
                    {
                        typeName = typeName.Remove(backtickIndex);
                    }
                    Type[] genericArgs = type.GetGenericArguments();
                    string genericArgsString = string.Join(", ", genericArgs.Select(t => GetTypeName(t)));
                    return $"{typeName}<{genericArgsString}>";
                }
                else
                {
                    switch (type.FullName)
                    {
                        case "System.Int32":
                            return "int";
                        case "System.String":
                            return "string";
                        case "System.Boolean":
                            return "bool";
                        case "System.Double":
                            return "double";
                        case "System.Void":
                            return "void";
                        default:
                            return type.Name;
                    }
                }
            }
        }
    }
}