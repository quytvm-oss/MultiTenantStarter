using Core.Domain;

using Modules.Files.Contracts.Enums;

namespace Modules.Files.Domain.Events;

public sealed record FileFinalizedDomainEvent(
    Guid FileAssetId,
    string OwnerType,
    Guid? OwnerId,
    FileAssetStatus FinalStatus,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);
