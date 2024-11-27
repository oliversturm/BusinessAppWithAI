# Instructions

- Make sure you have an OpenAI account set up and create an API key [here](https://platform.openai.com/settings/organization/api-keys)

- Create a file `BusinessAppWithAI.Server/.env` and add your API key there so it looks like this:

```
OPENAI_API_KEY=... long gibberish API key here ...
```

- In the folder `businessappwithai.client`, run `npm install` and then (optionally) `npm run dev`

- In the folder `BusinessAppWithAI.Server`, run `dotnet run` -- if the client app is not running yet, it will be started in addition to the server.

(Btw, it is possible that steps like the `npm install` will already be run automatically if you use VS Code or Visual Studio.)


Feel free to get in touch if you have any questions or comments. 

[Oliver Sturm Email](mailto:oliver@oliversturm.com)

[Bluesky](https://bsky.app/profile/bsky.oliversturm.com)
