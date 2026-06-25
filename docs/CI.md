# CI / Validation

There is no active CI pipeline and no test project. The validation gate is a clean build:

```bash
dotnet build CoinBank.Manage.slnx
dotnet build CoinBank.Manage/CoinBank.Manage.csproj -c Release
```

Deployment publishes the manage host and builds `coinbank.manage`:

```bash
dotnet publish CoinBank.Manage/CoinBank.Manage.csproj -c Release -o publish
docker build -t coinbank.manage .
docker-compose up -d
```

Build outputs (`bin/`, `obj/`, `publish/`) stay out of source control.
