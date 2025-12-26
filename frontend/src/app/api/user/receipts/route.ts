import { NextRequest, NextResponse } from 'next/server';
import { cookies } from 'next/headers';
import { getDocuments } from '@/lib/api-client';
import { transformDocumentToReceipt } from '@/lib/data-transform';

/**
 * Get user's receipts/documents
 * Proxy to backend /api/documents
 */
export async function GET(request: NextRequest) {
  try {
    // Get auth token from cookies
    const cookieStore = await cookies();
    const token = cookieStore.get('jwt_token')?.value;

    if (!token) {
      return NextResponse.json(
        { success: false, error: 'Unauthorized' },
        { status: 401 }
      );
    }

    const { searchParams } = new URL(request.url);
    const page = parseInt(searchParams.get('page') || '1');
    const pageSize = parseInt(searchParams.get('pageSize') || '4');

    // Get documents from backend (for current user)
    // Use apiRequest directly with token
    const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:5100';
    const queryParams = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    
    const backendResponse = await fetch(`${BACKEND_URL}/api/documents?${queryParams}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      credentials: 'include',
    });

    if (!backendResponse.ok) {
      return NextResponse.json(
        { success: false, error: 'Failed to get documents from backend' },
        { status: backendResponse.status }
      );
    }

    const backendData = await backendResponse.json().catch(() => ({ success: false }));
    
    // Check if response has ApiResponse wrapper
    const response = backendData.statusCode === 200 && backendData.data
      ? { data: backendData.data, message: backendData.message }
      : { data: backendData, message: '' };

    if (!response.data) {
      return NextResponse.json(
        { success: false, error: response.message || 'Failed to get documents' },
        { status: response.statusCode || 500 }
      );
    }

    // Transform documents to receipts
    const receipts = response.data.items?.map((doc: any) => transformDocumentToReceipt(doc)) || [];

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
    console.error('List user receipts error:', error);
    return NextResponse.json(
      { success: false, error: error.message || 'Internal server error' },
      { status: 500 }
    );
  }
}

