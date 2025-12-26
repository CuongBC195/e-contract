import { NextRequest, NextResponse } from 'next/server';
import { trackDocumentView } from '@/lib/api-client';

/**
 * Track document view
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { id } = body;

    if (!id) {
      return NextResponse.json(
        { success: false, error: 'Document ID is required' },
        { status: 400 }
      );
    }

    await trackDocumentView(id);

    return NextResponse.json({
      success: true,
    });
  } catch (error: any) {
    console.error('Error tracking view:', error);
    // Don't fail if tracking fails
    return NextResponse.json({
      success: true, // Always return success for tracking
    });
  }
}

