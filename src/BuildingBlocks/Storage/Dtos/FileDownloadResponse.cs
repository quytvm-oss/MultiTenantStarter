namespace Storage.Dtos;

public class FileDownloadResponse
{
    public required Stream Stream { get; set; }
    
    public required string ContentType { get; set; }
    
    public required string FileName { get; set; }
    
    public long? ContentLength { get; set; }
}