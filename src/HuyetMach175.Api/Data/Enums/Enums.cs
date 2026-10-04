namespace HuyetMach175.Api.Data.Enums;

public enum RoleCode
{
    DON,
    REC,
    BBNK,
    CLIN,
    MGT,
    SYS
}

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

public enum BloodType
{
    A,
    B,
    AB,
    O
}

public enum RhFactor
{
    POSITIVE,
    NEGATIVE
}

public enum AppointmentStatus
{
    BOOKED,
    CHECKED_IN,
    CANCELLED,
    NO_SHOW
}

public enum ScreeningStatus
{
    QUALIFIED,
    DEFERRED,
    UNQUALIFIED
}

public enum TargetVolume
{
    VOL_250,
    VOL_350,
    VOL_450
}

public enum SessionStatus
{
    PENDING,
    QUALIFIED,
    ABORTED,
    COMPLETED,
    REFERRED
}

public enum ComponentCode
{
    WB,
    PRBC,
    FFP,
    PLT
}

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
    DISCARDED
}

public enum TestResult
{
    NEGATIVE,
    POSITIVE
}

public enum OverallConclusion
{
    PASSED,
    FAILED
}

public enum UrgencyLevel
{
    ROUTINE,
    URGENT
}

public enum RequestStatus
{
    PENDING,
    REJECTED,
    READY,
    COMPLETED
}

public enum AllocationStatus
{
    RESERVED,
    ISSUED,
    COMPLETED,
    RETURNED
}

public enum ReturnFinalAction
{
    RESTOCK,
    DISCARD
}

public enum ActionType
{
    INSERT,
    UPDATE,
    DELETE,
    STATUS_CHANGE
}

public enum NotificationType
{
    INFO,
    WARNING,
    URGENT_REQUEST,
    RECALL
}

public enum NotificationReferenceType
{
    BLOOD_REQUEST,
    BLOOD_BAG,
    DONATION_SESSION
}
