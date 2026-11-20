# SnapBlast API local service setup

The API project is the only place where provider credentials belong. Do not put these values in the .NET MAUI client, XAML, Git, screenshots, or API response models.

## Values to replace

Copy `appsettings.example.json` to `appsettings.Development.json` only if you understand that local settings must remain untracked. Replace these placeholders:

| Setting | Replace with |
|---|---|
| `Mongo:ConnectionString` | MongoDB Atlas or local MongoDB connection string. Use a database user with only the permissions the API needs. |
| `Mongo:Database` | Your MongoDB database name. |
| `Mongo:QuizCollection` | Collection for saved quizzes. |
| `Mongo:UserCollection` | Collection for user records. Never store plaintext passwords. |
| `Gemini:ApiKey` | API key created in Google AI Studio. Restrict/rotate it where supported. |
| `Gemini:Model` | A model enabled for your Google AI Studio project. |
| `Ocr:Provider` | `GoogleGemini` for multimodal extraction, or `Tesseract` for local OCR. |
| `Ocr:TesseractExecutablePath` | Full path to the installed Tesseract executable. |
| `Ocr:TessdataPath` | Folder containing the language data files, such as `eng.traineddata`. |
| `Ocr:Language` | Tesseract language code, normally `eng`. |
| `AzureVision:Endpoint` and `AzureVision:Key` | Optional Azure Vision values only if Azure is enabled. |
| `Authentication:JwtSecret` | A random secret of at least 32 bytes. Never reuse the Gemini or MongoDB credential. |
| `Authentication:Issuer` and `Authentication:Audience` | Values used consistently by the API and MAUI client during JWT validation. |

## Recommended user-secrets setup

From the directory containing `SnapBlast.Api.csproj`, run:

```powershell
dotnet user-secrets set "Mongo:ConnectionString" "mongodb+srv://USER:PASSWORD@CLUSTER/"
dotnet user-secrets set "Gemini:ApiKey" "PASTE_GOOGLE_AI_STUDIO_KEY_HERE"
dotnet user-secrets set "Authentication:JwtSecret" "GENERATE_A_LONG_RANDOM_SECRET_HERE"
dotnet user-secrets set "Ocr:TesseractExecutablePath" "C:\Program Files\Tesseract-OCR\tesseract.exe"
dotnet user-secrets set "Ocr:TessdataPath" "C:\Program Files\Tesseract-OCR\tessdata"
```

The project already has a `UserSecretsId`, so these values are stored outside the repository. Do not commit the output of `dotnet user-secrets list`.

## Google AI Studio and OCR

Google AI Studio can be used for question/answer generation and, if the selected Gemini model supports image input, as the primary OCR/vision extraction provider. Treat its output as untrusted input: validate pair counts, remove unsafe text, enforce upload limits, and require the API to return structured JSON.

Tesseract is a local fallback. It must be installed on the API host and supplied with the required `.traineddata` files. Tesseract is not currently wired into the API; this file only prepares the configuration names.

## Authentication warning

MongoDB stores user records, but passwords must be hashed with a slow password-hashing algorithm such as ASP.NET Core `IPasswordHasher<TUser>` or Argon2id. Never store passwords, refresh tokens, or provider API keys in plaintext. JWT signing and refresh-token storage should be implemented server-side before enabling login in the client.

## Current implementation status

The repository currently contains API option contracts and package references, not a complete controller/startup/authentication pipeline. These settings are a safe configuration starting point; they do not by themselves enable MongoDB, login, Google AI, or Tesseract calls.
