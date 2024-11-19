using FluentValidation;
using BusinessAppWithAI.Server;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using Spectre.Console;
using Rule = BusinessAppWithAI.Server.Rule;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IValidator<BusinessObject>, BusinessObjectValidator>();
builder.Services.AddSingleton<ILanguageValidator, LanguageValidator>();

builder.Services.AddCors(options => {
  options.AddDefaultPolicy(builder => {
    builder.WithOrigins("http://localhost:51655")
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});

var app = builder.Build();
app.UseCors();

app.MapPost("/receiver", async (IValidator<BusinessObject> validator, BusinessObject businessObject) => {
  var result = await validator.ValidateAsync(businessObject);
  if (!result.IsValid) {
    return Results.ValidationProblem(result.ToDictionary());
  }

  return Results.Ok(businessObject);
});

app.MapPost("/api/configureRules", (Rule[] rules, [FromServices] ILanguageValidator validator) => {
  foreach (Rule rule in rules) {
    Console.WriteLine($"Setting rule for '{rule.Field}': '{rule.RuleText}'");
  }

  validator.SetRules(rules);
  return Results.Ok();
});

app.MapPost("/api/validate", (ValidationInput input, [FromServices] ILanguageValidator validator) => {
  var result = validator.ValidateField(input);
  var output =
    $"Validating [blue]'{input.Field}'[/] = [blue]'{input.Value}'[/] -> [{(result.Valid ? "green" : "red")}]{(result.Valid ? "OK" : $"Error ({result.Message})")}[/]";
  AnsiConsole.MarkupLine(output);
  return Results.Ok(result);
});

app.Run();