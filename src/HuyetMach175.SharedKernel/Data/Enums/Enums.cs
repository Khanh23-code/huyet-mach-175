using System.Text.Json.Serialization;

namespace HuyetMach175.Api.Data.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CampaignStatus
{
    DRAFT,
    PENDING_APPROVAL,
    APPROVED,
    REJECTED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CampaignType
{
    EVENT,
    RECURRING
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BloodType
{
    A,
    B,
    AB,
    O
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RhFactor
{
    POSITIVE,
    NEGATIVE
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppointmentStatus
{
    BOOKED,
    CHECKED_IN,
    CANCELLED,
    NO_SHOW
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScreeningStatus
{
    PENDING,
    QUALIFIED,
    DEFERRED,
    UNQUALIFIED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetVolume
{
    VOL_250,
    VOL_350,
    VOL_450
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SessionStatus
{
    WAITING_COLLECTION,
    COMPLETED,
    ABORTED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ComponentCode
{
    WB,
    PRBC,
    FFP,
    PLT
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BagStatus
{
    NEWLY_COLLECTED,
    READY_TESTING,
    TESTED_PASSED,
    AVAILABLE,
    LOCKED,
    RESERVED,
    ISSUED,
    TRANSFUSED,
    DISCARDED,
    PROCESSED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TestResult
{
    NEGATIVE,
    POSITIVE
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OverallConclusion
{
    PASSED,
    FAILED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UrgencyLevel
{
    ROUTINE,
    URGENT,
    EMERGENCY
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
{
    DRAFT,
    PENDING,
    REJECTED,
    READY,
    COMPLETED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AllocationStatus
{
    RESERVED,
    ISSUED,
    COMPLETED,
    RETURNED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReturnFinalAction
{
    PENDING_INSPECTION,
    RESTOCK,
    DISCARD,
    CANCELLED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionType
{
    INSERT,
    UPDATE,
    DELETE,
    STATUS_CHANGE
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationPriority
{
    Normal,
    Critical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationChannel
{
    Email
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationDeliveryStatus
{
    Pending,
    Processing,
    Accepted,
    RetryScheduled,
    FailedPermanent,
    Skipped
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PermissionAction
{
    VIEW,
    EDIT,
    APPROVE,
    CANCEL
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisposalReason
{
    EXPIRED,
    INFECTIOUS_POSITIVE,
    HEMOLYSIS,
    DAMAGED_BAG,
    COLD_CHAIN_BREACH,
    OTHER
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisposalStatus
{
    PENDING_APPROVAL,
    APPROVED,
    DISPOSED,
    REJECTED,
    CANCELLED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HandoverSessionStatus
{
    PENDING,
    CONFIRMED,
    CANCELLED,
    EXPIRED
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompatibilityRuleType
{
    RBC,
    PLASMA,
    PLATELET
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StorageZone
{
    QUARANTINE,
    RELEASED,
    DISPOSAL_HOLD
}
