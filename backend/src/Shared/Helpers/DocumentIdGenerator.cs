using Domain.Enums;
using Shared.Constants;

namespace Shared.Helpers;

public static class DocumentIdGenerator
{
    private static readonly Random Random = new Random();
    
    public static string GenerateId(DocumentType type)
    {
        var prefix = type == DocumentType.Receipt 
            ? AppConstants.ReceiptPrefix 
            : AppConstants.ContractPrefix;
        
        var randomPart = Random.Next(100000, 999999).ToString();
        return $"{prefix}{randomPart}";
    }
}

