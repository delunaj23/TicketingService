using System.ComponentModel.DataAnnotations;

namespace ZemplerTicketing.Contracts;

public class PurchaseRequest
{
    [Required(ErrorMessage = "holderName is required.")]
    [MinLength(1, ErrorMessage = "holderName must not be empty.")]
    public string HolderName { get; set; } = string.Empty;
}
