namespace Domain.Enums;

public enum PushProcessingStatus
{
    Received = 0,
    Processed = 1,
    Duplicate = 2,
    UnmappedRepository = 3,
    Failed = 4
}
