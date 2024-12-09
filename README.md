# Instructions

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


