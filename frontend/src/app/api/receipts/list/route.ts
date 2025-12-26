import { NextRequest, NextResponse } from 'next/server';
import { getDocuments } from '@/lib/api-client';
import { transformDocumentToReceipt } from '@/lib/data-transform';

/**
 * Get list of receipts/documents
 */
export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const page = parseInt(searchParams.get('page') || '1');
    const pageSize = parseInt(searchParams.get('pageSize') || '4');
    const status = searchParams.get('status') || undefined;
    const type = searchParams.get('type') || undefined;

    // Get documents from backend
    const response = await getDocuments({
      page,
      pageSize,
      status: status as any,
      type: type as any,
    });

    if (!response.data) {
      return NextResponse.json(
        { success: false, error: response.message || 'Failed to get documents' },
        { status: response.statusCode || 500 }
      );
    }

    // Transform documents to receipts
    const receipts = response.data.items.map((doc: any) => transformDocumentToReceipt(doc));

    return NextResponse.json({
      success: true,
      receipts,
      pagination: {
        total: response.data.totalCount,
        page: response.data.page,
        pageSize: response.data.pageSize,
        totalPages: response.data.totalPages,
      },
    });
  } catch (error: any) {
    console.error('List receipts error:', error);
    return NextResponse.json(
      { success: false, error: error.message || 'Internal server error' },
      { status: 500 }
    );
  }
}

