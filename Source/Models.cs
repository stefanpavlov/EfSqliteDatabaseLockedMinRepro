using System.ComponentModel.DataAnnotations.Schema;

public enum FileStatus
{
    Staged,
    Replicated,
    Requested,
    TapeGone,
    SoftDeleted,
}

public class File(
    Guid id,
    Guid sourceId,
    int bucketId,
    string relativePath,
    long attributes,
    long size,
    DateTime createdOn,
    DateTime lastModified,
    DateTime lastAccessed)
{
    public Guid Id { get; set; } = id;
    public Guid SourceId { get; set; } = sourceId;
    public int BucketId { get; set; } = bucketId;
    public string RelativePath { get; set; } = relativePath;
    public long Attributes { get; set; } = attributes;
    public long Size { get; set; } = size;
    public List<Tape> Tapes { get; } = [];
    public DateTime CreatedOn { get; set; } = createdOn;
    public DateTime LastModified { get; set; } = lastModified;
    public DateTime LastAccessed { get; set; } = lastAccessed;
    public DateTime Added { get; set; } = DateTime.UtcNow;
    public DateTime? Requested { get; set; } = null;
    public DateTime? Replicated { get; set; } = null;
    public FileStatus Status { get; set; } = FileStatus.Staged;
    public Guid? JobMarker
    {
        get; set;
    }
    public bool IsFile => (Attributes & 0b1) == 0b1;
    public bool IsDirectory => (Attributes & 0b10) == 0b10;
    public bool IsSymlink => (Attributes & 0b100) == 0b100;
    public bool IsInJob => JobMarker.HasValue && JobMarker.Value != default;
}

public enum TapeHealth
{
    Unknown,
    Healthy,
    Suspicious,
    Damaged,
    Error,
}

public enum TapeStatus
{
    Unknown,
    Idle,
    Staging,
    Writing,
    Cloning,
    ScanRequired,
    FormatRequired,
    Error,
    Reading,
    Formatting,
    Active,
    Finalized,
    ManualInitializationMaybeRequired,
    InitializationMaybeRequired,
    InitializationRequired,
}

public enum TapeLocation
{
    Offline,
    Online,
}

public enum TapeGeneration
{
    Unknown,
    LTO5,
    LTO6,
    LTO7,
    M8,
    LTO8,
    LTO9,
}

[Flags]
public enum TapeFlags
{
    None = 0,
    WriteProtected = 1,
}

[Flags]
public enum TapeCharacteristics
{
    None = 0,
    Inaccessible = 1,
    CleaningTape = 2,
}

[ComplexType]
public sealed record AggregateFilesInfo(long Count, long Size);

public class Tape
{
    public int Id
    {
        get; set;
    }
    public string Label { get; set; } = string.Empty;
    public Guid? PhysicalGuid
    {
        get; set;
    }
    public string? Name
    {
        get; set;
    }
    public TapeHealth Health
    {
        get; set;
    }
    public TapeStatus Status { get; set; } = TapeStatus.ScanRequired;
    public TapeFlags Flags { get; set; } = TapeFlags.None;
    [NotMapped]
    public int? ReservedByBucketId
    {
        get; set;
    }
    [NotMapped]
    public TapeLocation Location
    {
        get; set;
    }
    [NotMapped]
    public string Library { get; set; } = string.Empty;
    [NotMapped]
    public string Drive { get; set; } = string.Empty;
    public AggregateFilesInfo ActiveFiles { get; set; } = new AggregateFilesInfo(0, 0);
    public AggregateFilesInfo WastedFiles { get; set; } = new AggregateFilesInfo(0, 0);
    public long? FreeSpace
    {
        get; set;
    }
    public long? Capacity
    {
        get; set;
    }
    public DateTime? FirstReplicationDate
    {
        get; set;
    }
    public TapeGeneration Generation
    {
        get; set;
    }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public List<File> Files { get; } = [];
    [NotMapped]
    public TapeCharacteristics Characteristics { get; set; } = TapeCharacteristics.None;
    [NotMapped]
    public Guid? RunningJobId { get; set; } = null;
    [NotMapped]
    public string? RunningJobType { get; set; } = null;
}
