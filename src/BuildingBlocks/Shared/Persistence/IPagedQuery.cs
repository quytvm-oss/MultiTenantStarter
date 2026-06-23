namespace Shared.Persistence;

public interface IPagedQuery
{
    int? PageNumber { get; set; }
    
    int? PageSize { get; set; }
    
    //bool Descending { get; init; }
    
    string? Sort { get; set; }
}