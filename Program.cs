using System.Globalization;
using Bogus;
using DataGenerator;

// ---- configuration (all overridable via `docker run -e VAR=value`) ----
int recordCount = GetEnvInt("RECORD_COUNT", 100_000);
double duplicateRate = GetEnvDouble("DUPLICATE_RATE", 0.15);
double badCharRate = GetEnvDouble("BAD_CHAR_RATE", 0.10);
double corruptNumericRate = GetEnvDouble("CORRUPT_NUMERIC_RATE", 0.05);
string outputDir = Environment.GetEnvironmentVariable("OUTPUT_DIR") ?? "/data/output";
string? seedRaw = Environment.GetEnvironmentVariable("SEED");
string batchName = Environment.GetEnvironmentVariable("BATCH_NAME") ?? "batch";

int? seed = int.TryParse(seedRaw, out var parsedSeed) ? parsedSeed : null;
var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
if (seed.HasValue) Randomizer.Seed = new Random(seed.Value);

Directory.CreateDirectory(outputDir);

var faker = new Faker();
var chaos = new ChaosInjector(rnd);
var recentLines = new List<string>(capacity: 5_000);

string fileName = $"dirty_data_{batchName}_{recordCount}.csv";
string outputPath = Path.Combine(outputDir, fileName);

Console.WriteLine($"Generating {recordCount:N0} records -> {outputPath}");
Console.WriteLine($"  duplicateRate={duplicateRate}, badCharRate={badCharRate}, corruptNumericRate={corruptNumericRate}, seed={(seed.HasValue ? seed.Value.ToString() : "random")}");

using var writer = new StreamWriter(outputPath, append: false, System.Text.Encoding.UTF8, bufferSize: 1 << 16);
await writer.WriteLineAsync("CustomerID,FirstName,LastName,Email,SignupDate,Amount,DeviceInfo,Tags");

for (int i = 0; i < recordCount; i++)
{
    string line;

    if (recentLines.Count > 0 && rnd.NextDouble() < duplicateRate)
    {
        line = recentLines[rnd.Next(recentLines.Count)];
    }
    else
    {
        line = BuildLine(i, faker, chaos, rnd, badCharRate, corruptNumericRate);
        recentLines.Add(line);
        if (recentLines.Count > 5_000) recentLines.RemoveAt(0);
    }

    await writer.WriteLineAsync(line);

    if ((i + 1) % 100_000 == 0)
        Console.WriteLine($"  ...{i + 1:N0} / {recordCount:N0} written");
}

await writer.FlushAsync();
var sizeBytes = new FileInfo(outputPath).Length;
Console.WriteLine($"Done. Wrote {recordCount:N0} rows, {sizeBytes / 1024.0 / 1024.0:F1} MB -> {outputPath}");

// ---- helpers ----

static string BuildLine(int index, Faker faker, ChaosInjector chaos, Random rnd, double badCharRate, double corruptNumericRate)
{
    int customerId = index + 1;
    string firstName = chaos.Contaminate(faker.Name.FirstName(), badCharRate);
    string lastName = chaos.Contaminate(faker.Name.LastName(), badCharRate);
    string email = chaos.Contaminate(faker.Internet.Email(firstName, lastName), badCharRate);

    var signupDate = faker.Date.Between(DateTime.UtcNow.AddYears(-3), DateTime.UtcNow);
    string signupDateText = chaos.FormatDate(signupDate);

    decimal amount = Math.Round((decimal)faker.Random.Double(5, 5000), 2);
    string amountText = chaos.FormatAmount(amount, corruptNumericRate);

    string deviceInfoCell = ChaosInjector.BuildDeviceInfoCell(faker.Internet.Ip(), faker.PickRandom("Windows", "Linux", "MacOS", "iOS", "Android"));

    string[] tags = faker.PickRandom(
        new[] { "vip", "trial", "churned" },
        new[] { "newsletter", "referral" },
        new[] { "beta" }
    ).ToArray();
    string tagsCell = chaos.BuildTagsCell(tags);

    return string.Join(",", customerId.ToString(CultureInfo.InvariantCulture), firstName, lastName, email, signupDateText, $"\"{amountText}\"", deviceInfoCell, tagsCell);
}

static int GetEnvInt(string name, int fallback) =>
    int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;

static double GetEnvDouble(string name, double fallback) =>
    double.TryParse(Environment.GetEnvironmentVariable(name), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : fallback;
