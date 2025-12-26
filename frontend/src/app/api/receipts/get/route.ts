import { NextRequest, NextResponse } from 'next/server';
import { getDocument } from '@/lib/api-client';
import { transformDocumentToReceipt } from '@/lib/data-transform';

/**
 * Get single receipt/document by ID
 */
export async function GET(request: NextRequest) {
  try {
    const searchParams = request.nextUrl.searchParams;
    const id = searchParams.get('id');

    if (!id) {
      return NextResponse.json(
        { success: false, error: 'Receipt ID is required' },
        { status: 400 }
      );
    }

    // Get document from backend
    const response = await getDocument(id);

    if (!response.data) {
      return NextResponse.json(
        { success: false, error: response.message || 'Receipt not found' },
        { status: 404 }
      );
    }

    // Transform to frontend format
    const receipt = transformDocumentToReceipt(response.data);

    return NextResponse.json({
      success: true,
      receipt,
    });
  } catch (error: any) {
    console.error('Error getting receipt:', error);
    return NextResponse.json(
      { 
        success: false, 
        error: error instanceof Error ? error.message : 'Failed to get receipt' 
      },
      { status: 500 }
    );
  }
}

