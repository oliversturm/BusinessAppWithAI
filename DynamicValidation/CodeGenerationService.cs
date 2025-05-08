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

        const string METHOD_TEXT = "Web API Function";
        const string MODEL_TEXT = "'Data Transfer Object'-Class";
        const string VALIDATOR_CLASS_NAME = "Validator";
        const string VALIDATION_METHOD_NAME = "ValidateParameters";
        const string TYPE_MARKER = "{{TYPE}}";
        const string CODE_MARKER = "{{CODE}}";
        const string RULES_MARKER = "{{RULES}}";
        const string SYSTEM_PROMPT = """
            You are a developer. You write C# code based on the specifications and rules provided by the user.
            """;
        const string BASE_PROMPT = $"""
            Create a method to validate all input parameters of a {TYPE_MARKER} in C# code.
            Declare the method inside a class named '{VALIDATOR_CLASS_NAME}'.
            The method should be declared static and be named '{VALIDATION_METHOD_NAME}'.
            If a parameter is not valid, an appropriate exception should be thrown.
            The structure of the {TYPE_MARKER} is as follows:
            {CODE_MARKER}
            Please consider the following rules for validation:
            {RULES_MARKER}
            Output only code, and no descriptive text.
            The error messages must be in English.
            Use only classes from the namespaces System and System.Text.RegularExpressions in the code.
            Do NOT use the System.Net.Mail.MailAddress class for email address verification.
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