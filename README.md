# Instructions

## Background

The samples in this repository accompany a conference presentation by Jörg Neumann and Oliver Sturm. They illustrate several points made during the presentation -- they are not complete applications, and they include code blocks that seem to serve the same purpose multiple times.

One of the central ideas we presented is that business rules (by the simple example of validation) can be coordinated between customers and developers by way of a shared language. In the past, this might have been a reason to create a domain-specific language (DSL) or use one that exists already, perhaps implemented by a rules engine library. However, since large language models (LLMs) allow computers to understand human language to a degree that was unimaginable not too long ago, we can now use natural language to define such rules.

Jörg created the original implementation of a code generator for .NET, which is used in the `DynamicValidation` sample. This sample pretends (when communicating with the LLM) to implement a web service function, which helps define the required schema of the data the sample deals with. It prompts the LLM to generate C# code from a set of human language validation rules, and compiles this before running some sample validation operations.

Oliver created the `BusinessAppWithAI` sample, using a modified approach for the schema definition, but reusing much of the original code generation logic for a server-side validation implementation. He then added a React web frontend that allows users to enter both data values and dynamic validation rules. These rules are interpreted by the server, and the client calls the server for validation as required. The rules can be changed by the user at any time, and the server will re-interpret them and validate the data accordingly. Oliver also added an extra mechanism that builds a JavaScript validator on the basis of the C# one and optionally uses this for client-side validation, which saves many roundtrips to the server during editing.

Of course the implementation is not totally practical. As we point out in the presentation, for many reasons we would perform dynamic code generation in a more coordinated fashion in a real application, and store generated code for performance reasons instead of recreating it all the time. It would also make sense to make the generation mechanism more granular, so that e.g. per-property validation code can be regenerated as needed. The ease with which the rules can be changed in this demo is purely for demonstration and development purposes, and no optimization is applied for performance or cost.



## Preparation

- Create an OpenAI account as needed, then create an API key [here](https://platform.openai.com/settings/organization/api-keys)

- Create the file `.env` in the root of this repository and add your API key there so it looks like this:

```
OPENAI_API_KEY=... long gibberish API key here ...
```

Both sample projects are configured to look for a `.env` file in parent directory hierarchy, so you only need one such file to share the key between both projects.


## DynamicValidation Demo

In the folder `DynamicValidation`, run `dotnet run`

## BusinessAppWithAI Demo

- In the folder `BusinessAppWithAI/businessappwithai.client`, run `npm install` and then (optionally) `npm run dev`

- In the folder `BusinessAppWithAI/BusinessAppWithAI.Server`, run `dotnet run` -- if the client app is not running yet, it will be started in addition to the server.

(Btw, it is possible that steps like the `npm install` will already be run automatically if you use VS Code or Visual Studio.)

## Feedback

Feel free to get in touch if you have any questions or comments. 


[Jörg Neumann Email](mailto:Joerg.Neumann@neogeeks.de)

[Jörg Neumann LinkedIn](https://www.linkedin.com/in/jörgneumann/)

[Oliver Sturm Email](mailto:oliver@oliversturm.com)

[Oliver Sturm LinkedIn](https://www.linkedin.com/in/oliversturm/)

[Oliver Sturm Bluesky](https://bsky.app/profile/bsky.oliversturm.com)


