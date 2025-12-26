import { NextRequest, NextResponse } from 'next/server';
import { signDocument } from '@/lib/api-client';
import { transformSignatureDataToBackend } from '@/lib/data-transform';
import type { SignatureData } from '@/lib/kv';

/**
 * Sign receipt/document
 * Format: Frontend sends SignatureData with data: string (unified format)
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { 
      id, 
      receiptImage, 
      signatureNguoiNhan, 
      signatureNguoiGui,
      signatureDataNguoiGui,
      signatureDataNguoiNhan,
      signerId,
    } = body;

    if (!id) {
      return NextResponse.json(
        { success: false, error: 'Receipt ID is required' },
        { status: 400 }
      );
    }

    // Determine signature data - use new format if available
    let signatureData: SignatureData | null = null;
    let signerIdToUse = signerId || 'signer-1';

    if (signatureDataNguoiGui || signatureDataNguoiNhan) {
      // Use new unified format (data: string)
      const sigData = signatureDataNguoiGui || signatureDataNguoiNhan;
      signatureData = sigData as SignatureData;
    } else if (receiptImage) {
      // Legacy: use base64 image - convert to new format
      signatureData = {
        type: 'draw',
        data: receiptImage, // Base64 image as fallback
      };
    }

    if (!signatureData || !signatureData.data || signatureData.data.trim() === '') {
      return NextResponse.json(
        { 
          success: false, 
          error: 'Signature data is required. Please draw or type your signature.',
          code: 'EMPTY_SIGNATURE'
        },
        { status: 400 }
      );
    }

    // Transform to backend DTO (just mapping, no stringify needed - format already unified)
    const backendSignatureData = transformSignatureDataToBackend(signatureData);

    // Sign document via backend API
    // Get signer info from request body if available
    const signerInfo = body.signerInfo || {}; // { role, name, email }
    
    let response;
    try {
      response = await signDocument(id, {
        signerId: signerIdToUse,
        signerRole: signerInfo.role || undefined,
        signerName: signerInfo.name || undefined,
        signerEmail: signerInfo.email || undefined,
        signatureData: backendSignatureData,
      });
    } catch (error: any) {
      // Handle "already signed" error from api-client
      const errorMessage = error instanceof Error ? error.message : String(error);
      if (errorMessage.includes('already signed') || errorMessage.includes('ALREADY_SIGNED')) {
        // If already signed, try to fetch the document to return it
        console.log('[Sign Route] Document already signed, fetching current state...');
        // This will be handled by the catch block below which will try to get the document
        throw new Error('ALREADY_SIGNED');
      }
      throw error;
    }

    if (!response.data) {
      throw new Error(response.message || 'Failed to sign document');
    }

    // Transform response to frontend format
    // Handle enum type (can be integer or string)
    const documentType = typeof response.data.type === 'number' 
      ? (response.data.type === 1 ? 'contract' : 'receipt')
      : (response.data.type?.toLowerCase() === 'contract' ? 'contract' : 'receipt');
    
    const status = typeof response.data.status === 'number'
      ? (response.data.status === 0 ? 'pending' : response.data.status === 1 ? 'partially_signed' : 'signed')
      : (response.data.status?.toLowerCase() === 'signed' ? 'signed' : 
         response.data.status?.toLowerCase() === 'partiallysigned' ? 'partially_signed' : 'pending');
    
    const receipt = {
      id: response.data.id,
      type: documentType as 'receipt' | 'contract',
      title: response.data.title,
      content: response.data.content,
      status,
      createdAt: new Date(response.data.createdAt).getTime(),
      signedAt: response.data.signedAt ? new Date(response.data.signedAt).getTime() : undefined,
      receiptInfo: response.data.receiptInfo,
      document: documentType === 'contract' ? {
        type: 'contract',
        title: response.data.title || '',
        content: response.data.content || '',
        signers: response.data.signatures.map(sig => {
          // Handle signature type (can be string or enum)
          const sigType = typeof sig.signatureData.type === 'string'
            ? sig.signatureData.type.toLowerCase()
            : 'draw'; // Default to 'draw' if not a string
          
          return {
            id: sig.signerId,
            role: sig.signerRole,
            name: sig.signerName || '',
            email: sig.signerEmail || '',
            signed: true,
            signedAt: new Date(sig.signedAt).getTime(),
            signatureData: {
              type: sigType === 'draw' ? 'draw' : 'type',
              data: sig.signatureData.data, // Already in correct format (JSON string or plain text)
              fontFamily: sig.signatureData.fontFamily,
              color: sig.signatureData.color,
            },
          };
        }),
        metadata: response.data.metadata || {},
      } : undefined,
      signatures: response.data.signatures,
    };

    // Check if status is 'Signed' (handle both string and enum)
    const isSigned = typeof response.data.status === 'string'
      ? response.data.status.toLowerCase() === 'signed'
      : response.data.status === 2; // Enum: Signed = 2
    
    return NextResponse.json({
      success: true,
      receipt,
      hasPDF: isSigned,
    });
  } catch (error: any) {
    console.error('[Sign Route] Error signing receipt:', error);
    console.error('[Sign Route] Error details:', {
      message: error instanceof Error ? error.message : String(error),
      stack: error instanceof Error ? error.stack : undefined,
    });
    
    // Handle "already signed" error gracefully
    const errorMessage = error instanceof Error ? error.message : String(error);
    if (errorMessage.includes('already signed') || errorMessage.includes('ALREADY_SIGNED')) {
      console.log('[Sign Route] Handling ALREADY_SIGNED - fetching document state...');
      // If already signed, try to fetch the document and return it
      try {
        const { getDocument } = await import('@/lib/api-client');
        const docResponse = await getDocument(id);
        if (docResponse.data) {
          console.log('[Sign Route] Successfully fetched document for ALREADY_SIGNED case');
          // Transform and return the document (same logic as success case)
          const documentType = typeof docResponse.data.type === 'number' 
            ? (docResponse.data.type === 1 ? 'contract' : 'receipt')
            : (docResponse.data.type?.toLowerCase() === 'contract' ? 'contract' : 'receipt');
          
          const status = typeof docResponse.data.status === 'number'
            ? (docResponse.data.status === 0 ? 'pending' : docResponse.data.status === 1 ? 'partially_signed' : 'signed')
            : (docResponse.data.status?.toLowerCase() === 'signed' ? 'signed' : 
               docResponse.data.status?.toLowerCase() === 'partiallysigned' ? 'partially_signed' : 'pending');
          
          const receipt = {
            id: docResponse.data.id,
            type: documentType as 'receipt' | 'contract',
            title: docResponse.data.title,
            content: docResponse.data.content,
            status,
            createdAt: new Date(docResponse.data.createdAt).getTime(),
            signedAt: docResponse.data.signedAt ? new Date(docResponse.data.signedAt).getTime() : undefined,
            receiptInfo: docResponse.data.receiptInfo,
            document: documentType === 'contract' ? {
              type: 'contract',
              title: docResponse.data.title || '',
              content: docResponse.data.content || '',
              signers: docResponse.data.signatures.map((sig: any) => {
                const sigType = typeof sig.signatureData.type === 'string'
                  ? sig.signatureData.type.toLowerCase()
                  : 'draw';
                return {
                  id: sig.signerId,
                  role: sig.signerRole,
                  name: sig.signerName || '',
                  email: sig.signerEmail || '',
                  signed: true,
                  signedAt: new Date(sig.signedAt).getTime(),
                  signatureData: {
                    type: sigType === 'draw' ? 'draw' : 'type',
                    data: sig.signatureData.data,
                    fontFamily: sig.signatureData.fontFamily,
                    color: sig.signatureData.color,
                  },
                };
              }),
              metadata: docResponse.data.metadata || {},
            } : undefined,
            signatures: docResponse.data.signatures,
          };
          
          const isSigned = typeof docResponse.data.status === 'string'
            ? docResponse.data.status.toLowerCase() === 'signed'
            : docResponse.data.status === 2;
          
          return NextResponse.json({
            success: true,
            code: 'ALREADY_SIGNED',
            receipt,
            hasPDF: isSigned,
            message: 'Document already signed by this signer',
          });
        }
      } catch (fetchError) {
        console.error('[Sign Route] Error fetching document for ALREADY_SIGNED case:', fetchError);
      }
      
      // Fallback: return success with code
      return NextResponse.json(
        { 
          success: true, 
          code: 'ALREADY_SIGNED',
          message: 'Document already signed by this signer'
        },
        { status: 200 }
      );
    }
    
    return NextResponse.json(
      { 
        success: false, 
        error: errorMessage,
        code: 'SIGNING_FAILED'
      },
      { status: 500 }
    );
  }
}

