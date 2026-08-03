# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DataGenerator.csproj .
RUN dotnet restore DataGenerator.csproj

COPY . .
RUN dotnet publish DataGenerator.csproj -c Release -o /app --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .

ENV RECORD_COUNT=100000 \
    DUPLICATE_RATE=0.15 \
    BAD_CHAR_RATE=0.10 \
    CORRUPT_NUMERIC_RATE=0.05 \
    OUTPUT_DIR=/data/output \
    SEED= \
    BATCH_NAME=batch

VOLUME ["/data/output"]

ENTRYPOINT ["dotnet", "DataGenerator.dll"]
