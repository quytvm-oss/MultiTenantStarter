using System.ComponentModel.DataAnnotations;

namespace Jobs;

public class HangfireOptions
{
    [Required] 
    [MinLength(3)] 
    public string UserName { get; set; } = default!;


    [Required]
    [MinLength(12)]
    public string Password { get; set; } = default!;

    public int IntervalDelay { get; set; } = 10;

    public int IntervalCleanup { get; set; } = 1;

    public int IntervalThreshold { get; set; } = 5;
    
    public string Route { get; set; } = "/jobs";
}