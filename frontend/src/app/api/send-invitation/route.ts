import { NextRequest, NextResponse } from 'next/server';
import { sendInvitation } from '@/lib/api-client';

/**
 * Send invitation email to customer for signing document
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { 
      customerEmail,
      customerName,
      receiptId,
      documentId,
      signingUrl,
      documentData,
    } = body;

    // Use documentId if provided, otherwise use receiptId
    const docId = documentId || receiptId;
    
    if (!docId) {
      return NextResponse.json(
        { success: false, error: 'Document ID is required' },
        { status: 400 }
      );
    }

    if (!customerEmail) {
      return NextResponse.json(
        { success: false, error: 'Customer email is required' },
        { status: 400 }
      );
    }

    // Use customerName if provided, otherwise extract from email
    const name = customerName || customerEmail.split('@')[0];

    // Call backend API
    try {
      const response = await sendInvitation({
        documentId: docId,
        customerEmail: customerEmail.trim(),
        customerName: name,
        signingUrl: signingUrl || `${process.env.NEXT_PUBLIC_BASE_URL || 'http://localhost:3000'}/?id=${docId}`,
      });

      // Check if response is successful (statusCode 200 or 201)
      if (response && (response.statusCode === 200 || response.statusCode === 201)) {
        return NextResponse.json({
          success: true,
          message: response.message || 'Email invitation sent successfully',
        });
      }

      // If response exists but statusCode is not 200/201
      return NextResponse.json(
        { 
          success: false, 
          error: response?.message || 'Failed to send invitation email' 
        },
        { status: response?.statusCode || 500 }
      );
    } catch (error: any) {
      // apiRequest throws error if !response.ok
      const errorMessage = error instanceof Error ? error.message : 'Failed to send invitation email';
      return NextResponse.json(
        { 
          success: false, 
          error: errorMessage 
        },
        { status: 500 }
      );
    }
  } catch (error: any) {
    console.error('[Send Invitation] Error:', error);
    return NextResponse.json(
      { 
        success: false, 
        error: error instanceof Error ? error.message : 'Failed to send invitation email' 
      },
      { status: 500 }
    );
  }
}

