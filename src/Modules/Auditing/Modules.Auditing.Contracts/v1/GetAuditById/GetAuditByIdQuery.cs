using Mediator;

using Modules.Auditing.Contracts.DTOs;

namespace Modules.Auditing.Contracts.v1.GetAuditById;

public record GetAuditByIdQuery(Guid Id) : IQuery<AuditDetailDto>;