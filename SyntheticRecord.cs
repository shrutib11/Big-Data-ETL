namespace DataGenerator;

internal sealed record SyntheticRecord(
    int CustomerId,
    string FirstName,
    string LastName,
    string Email,
    DateTime SignupDate,
    decimal Amount,
    string DeviceIp,
    string DeviceOs,
    string[] Tags
);
