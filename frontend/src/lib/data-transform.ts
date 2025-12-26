/**
 * Data transformation utilities
 * Convert between frontend and backend data formats
 */

import type { 
  SignatureDataDto, 
  DocumentResponseDto, 
  ReceiptInfoDto,
  ContractMetadataDto 
} from './api-client';
import type { SignatureData, Signer, ReceiptData } from './kv';

/**
 * Transform backend SignatureDataDto to frontend SignatureData
 * NOTE: Format is now unified - no transformation needed, just mapping
 */
export function transformSignatureDataFromBackend(dto: SignatureDataDto): SignatureData {
  // Format is now unified - backend uses data: string, frontend uses data: string
  return {
    type: dto.type.toLowerCase() === 'draw' ? 'draw' : 'type',
    data: dto.data, // Already in correct format (JSON string for draw, plain text for type)
    fontFamily: dto.fontFamily,
    color: dto.color,
  };
}

/**
 * Transform frontend SignatureData to backend SignatureDataDto
 * NOTE: Format is now unified - no transformation needed, just mapping
 */
export function transformSignatureDataToBackend(data: SignatureData): SignatureDataDto {
  // Format is now unified - frontend uses data: string, backend uses data: string
  return {
    type: data.type === 'draw' ? 'draw' : 'type',
    data: data.data, // Already in correct format
    fontFamily: data.fontFamily,
    color: data.color,
  };
}

/**
 * Transform backend DocumentResponseDto to frontend Receipt format
 */
export function transformDocumentToReceipt(doc: DocumentResponseDto): any {
  // Handle enum as integer or string
  // DocumentType: 0 = Receipt, 1 = Contract
  const documentType = typeof doc.type === 'number' 
    ? (doc.type === 1 ? 'contract' : 'receipt')
    : (doc.type?.toLowerCase() === 'contract' ? 'contract' : 'receipt');
  
  // DocumentStatus: 0 = Pending, 1 = PartiallySigned, 2 = Signed
  const status = typeof doc.status === 'number'
    ? (doc.status === 0 ? 'pending' : doc.status === 1 ? 'partially_signed' : 'signed')
    : (doc.status?.toLowerCase() === 'signed' ? 'signed' : 
       doc.status?.toLowerCase() === 'partiallysigned' ? 'partially_signed' : 'pending');

  const baseReceipt = {
    id: doc.id,
    type: documentType as 'receipt' | 'contract',
    title: doc.title,
    content: doc.content,
    status,
    createdAt: new Date(doc.createdAt).getTime(),
    signedAt: doc.signedAt ? new Date(doc.signedAt).getTime() : undefined,
    viewedAt: doc.viewedAt ? new Date(doc.viewedAt).getTime() : undefined,
    receiptInfo: doc.receiptInfo,
    userId: doc.creator?.id,
    signatures: doc.signatures,
  };

  // For contracts, add document structure
  if (documentType === 'contract') {
    // Handle signingMode enum (can be string or integer)
    const signingModeValue = typeof doc.signingMode === 'number'
      ? (doc.signingMode === 1 ? 'RequiredLogin' : 'Public')
      : (doc.signingMode === 'RequiredLogin' ? 'RequiredLogin' : 'Public');
    
    // Create a map of signatures by signerId for quick lookup
    const signaturesMap = new Map(
      doc.signatures.map(sig => [
        sig.signerId,
        {
          signed: true,
          signedAt: new Date(sig.signedAt).getTime(),
          signatureData: {
            type: typeof sig.signatureData.type === 'string' 
              ? (sig.signatureData.type.toLowerCase() === 'draw' ? 'draw' : 'type')
              : 'type',
            data: sig.signatureData.data, // Already in correct format
            fontFamily: sig.signatureData.fontFamily,
            color: sig.signatureData.color,
          },
        }
      ])
    );
    
    // Build signers list: always show 2 signers (Bên A, Bên B)
    // Match signatures to signers by index
    const defaultRoles = ['Bên A', 'Bên B'];
    const signers: any[] = [];
    
    // Always create 2 signers
    for (let i = 0; i < 2; i++) {
      const signature = doc.signatures[i]; // Match by index
      
      if (signature) {
        // This signer has signed
        signers.push({
          id: signature.signerId,
          role: signature.signerRole || defaultRoles[i],
          name: signature.signerName || '',
          email: signature.signerEmail || '',
          signed: true,
          signedAt: new Date(signature.signedAt).getTime(),
          signatureData: {
            type: typeof signature.signatureData.type === 'string'
              ? (signature.signatureData.type.toLowerCase() === 'draw' ? 'draw' : 'type')
              : 'type',
            data: signature.signatureData.data,
            fontFamily: signature.signatureData.fontFamily,
            color: signature.signatureData.color,
          },
        });
      } else {
        // This signer hasn't signed yet
        signers.push({
          id: `signer-${i}`,
          role: defaultRoles[i],
          name: '',
          email: '',
          signed: false,
        });
      }
    }
    
    return {
      ...baseReceipt,
      document: {
        type: 'contract',
        title: doc.title || '',
        content: doc.content || '',
        signers,
        signingMode: signingModeValue,
        metadata: doc.metadata ? {
          contractNumber: doc.metadata.contractNumber,
          createdDate: doc.metadata.contractDate 
            ? new Date(doc.metadata.contractDate).toLocaleDateString('vi-VN')
            : new Date().toLocaleDateString('vi-VN'),
          location: doc.metadata.location || '',
        } : {},
      },
    };
  }

  // For receipts - transform receiptInfo to ReceiptData format
  if (doc.receiptInfo) {
    return {
      ...baseReceipt,
      receiptInfo: doc.receiptInfo, // Keep receiptInfo for compatibility
      data: transformReceiptInfoToReceiptData(doc.receiptInfo), // Add data field for ReceiptEditorKV
      info: undefined, // Not using legacy format
    };
  }

  // For receipts without receiptInfo
  return baseReceipt;
}

/**
 * Transform backend ReceiptInfoDto to frontend ReceiptData format
 */
export function transformReceiptInfoToReceiptData(receiptInfo: ReceiptInfoDto): ReceiptData {
  return {
    title: 'GIẤY BIÊN NHẬN TIỀN',
    fields: [
      { 
        id: 'hoTenNguoiNhan', 
        label: 'Họ và tên người nhận', 
        value: receiptInfo.receiverName || '', 
        type: 'text' 
      },
      { 
        id: 'donViNguoiNhan', 
        label: 'Đơn vị người nhận', 
        value: receiptInfo.receiverAddress || '', 
        type: 'text' 
      },
      { 
        id: 'hoTenNguoiGui', 
        label: 'Họ và tên người gửi', 
        value: receiptInfo.senderName || '', 
        type: 'text' 
      },
      { 
        id: 'donViNguoiGui', 
        label: 'Đơn vị người gửi', 
        value: receiptInfo.senderAddress || '', 
        type: 'text' 
      },
      { 
        id: 'lyDoNop', 
        label: 'Lý do nộp', 
        value: receiptInfo.reason || '', 
        type: 'text' 
      },
      { 
        id: 'soTien', 
        label: 'Số tiền', 
        value: receiptInfo.amount?.toString() || '0', 
        type: 'money' 
      },
    ],
    ngayThang: receiptInfo.date ? new Date(receiptInfo.date).toLocaleDateString('vi-VN') : '',
    diaDiem: receiptInfo.location || '',
  };
}

/**
 * Transform frontend ReceiptData to backend ReceiptInfoDto
 */
export function transformReceiptDataToReceiptInfo(data: ReceiptData): ReceiptInfoDto {
  const soTienField = data.fields.find(f => f.type === 'money');
  const amount = soTienField ? parseFloat(soTienField.value.replace(/\D/g, '')) || 0 : 0;

  return {
    senderName: data.fields.find(f => f.id === 'hoTenNguoiGui')?.value || '',
    senderAddress: data.fields.find(f => f.id === 'donViNguoiGui')?.value || '',
    receiverName: data.fields.find(f => f.id === 'hoTenNguoiNhan')?.value || '',
    receiverAddress: data.fields.find(f => f.id === 'donViNguoiNhan')?.value || '',
    amount: amount,
    reason: data.fields.find(f => f.id === 'lyDoNop')?.value || '',
    location: data.diaDiem || '',
    date: data.ngayThang ? new Date(data.ngayThang).toISOString() : undefined,
  };
}

/**
 * Transform frontend ContractMetadata to backend ContractMetadataDto
 */
/**
 * Helper function to safely parse date string to ISO string
 * Handles multiple formats:
 * - ISO format: "2024-12-25T00:00:00Z"
 * - Vietnamese format: "ngày 25 tháng 12 năm 2024"
 * - DD/MM/YYYY format: "25/12/2024"
 * - Standard Date parsing
 */
export function parseDateToISO(dateStr: string | undefined): string | undefined {
  if (!dateStr) return undefined;
  
  try {
    // If it's already an ISO string, validate and return
    if (dateStr.includes('T') || dateStr.includes('Z')) {
      const date = new Date(dateStr);
      if (!isNaN(date.getTime())) {
        return date.toISOString();
      }
    }
    
    // Try parsing Vietnamese format: "ngày X tháng Y năm Z"
    const vietnameseMatch = dateStr.match(/ngày\s+(\d+)\s+tháng\s+(\d+)\s+năm\s+(\d+)/i);
    if (vietnameseMatch) {
      const day = parseInt(vietnameseMatch[1], 10);
      const month = parseInt(vietnameseMatch[2], 10) - 1; // Month is 0-indexed
      const year = parseInt(vietnameseMatch[3], 10);
      const date = new Date(year, month, day);
      if (!isNaN(date.getTime())) {
        return date.toISOString();
      }
    }
    
    // Try parsing DD/MM/YYYY format
    const parts = dateStr.split('/');
    if (parts.length === 3) {
      const day = parseInt(parts[0], 10);
      const month = parseInt(parts[1], 10) - 1; // Month is 0-indexed
      const year = parseInt(parts[2], 10);
      const date = new Date(year, month, day);
      if (!isNaN(date.getTime())) {
        return date.toISOString();
      }
    }
    
    // Try standard Date parsing
    const date = new Date(dateStr);
    if (!isNaN(date.getTime())) {
      return date.toISOString();
    }
    
    return undefined;
  } catch (error) {
    console.error('Error parsing date:', dateStr, error);
    return undefined;
  }
}

export function transformMetadataToBackend(metadata: {
  contractNumber?: string;
  createdDate: string;
  effectiveDate?: string;
  expiryDate?: string;
  location: string;
}): ContractMetadataDto {
  return {
    contractNumber: metadata.contractNumber,
    location: metadata.location,
    contractDate: parseDateToISO(metadata.createdDate),
  };
}

/**
 * Transform backend ContractMetadataDto to frontend metadata
 */
export function transformMetadataFromBackend(metadata: ContractMetadataDto): {
  contractNumber?: string;
  createdDate: string;
  effectiveDate?: string;
  expiryDate?: string;
  location: string;
} {
  return {
    contractNumber: metadata.contractNumber,
    location: metadata.location || '',
    createdDate: metadata.contractDate ? new Date(metadata.contractDate).toLocaleDateString('vi-VN') : new Date().toLocaleDateString('vi-VN'),
  };
}

