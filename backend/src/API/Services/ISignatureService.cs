using Domain.Entities;
using Domain.ValueObjects;

namespace API.Services;

public interface ISignatureService
{
    Task<Signature> AddSignatureAsync(string documentId, string signerId, string signerRole, string? signerName, string? signerEmail, SignatureData signatureData, string? ipAddress, string? userAgent);
}

