using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Repositories;

namespace API.Services;

public class SignatureService : ISignatureService
{
    private readonly ISignatureRepository _signatureRepository;
    private readonly IDocumentRepository _documentRepository;

    public SignatureService(ISignatureRepository signatureRepository, IDocumentRepository documentRepository)
    {
        _signatureRepository = signatureRepository;
        _documentRepository = documentRepository;
    }

    public async Task<Signature> AddSignatureAsync(string documentId, string signerId, string signerRole, string? signerName, string? signerEmail, SignatureData signatureData, string? ipAddress, string? userAgent)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            throw new InvalidOperationException("Document not found");

        // Check if signer already signed
        var existingSignatures = await _signatureRepository.GetByDocumentIdAsync(documentId);
        
        // 🔒 SECURITY: Allow multiple signatures from same signerId ONLY if:
        // 1. Document has no signatures yet, OR
        // 2. Document has exactly 1 signature and it's from the same signerId
        // This allows customer to sign both sides when creator hasn't signed yet
        if (existingSignatures.Count > 0)
        {
            var sameSignerCount = existingSignatures.Count(s => s.SignerId == signerId);
            
            // If signer already signed, check if we should allow another signature
            if (sameSignerCount > 0)
            {
                // Only allow if there's exactly 1 signature total (same signer can sign both sides)
                // Once there are 2+ signatures, no more duplicates allowed
                if (existingSignatures.Count > 1)
                {
                    throw new InvalidOperationException("Signer has already signed this document");
                }
                // If count == 1 and sameSignerCount == 1, allow (customer can sign second side)
            }
        }

        var signature = new Signature
        {
            DocumentId = documentId,
            SignerId = signerId,
            SignerRole = signerRole,
            SignerName = signerName,
            SignerEmail = signerEmail,
            SignatureData = signatureData,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SignedAt = DateTime.UtcNow
        };

        return await _signatureRepository.CreateAsync(signature);
    }
}

