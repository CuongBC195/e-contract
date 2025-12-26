'use client';

import React, { useState, useRef, useEffect } from 'react';
import {
  FileDown,
  PenLine,
  Loader2,
  CheckCircle2,
  AlertCircle,
  Save,
  X,
  Users,
  Calendar,
  MapPin,
  FileText,
} from 'lucide-react';
import { toPng } from 'html-to-image';
import jsPDF from 'jspdf';
import SignatureModal, { SignatureResult } from './SignatureModal';
import { ToastContainer, useToast } from './Toast';
import { signDocument } from '@/lib/api-client';
import { formatVietnameseDate } from '@/lib/utils';
import type { Receipt, Signer, SignatureData, SignaturePoint } from '@/lib/kv';

interface PDFViewKVProps {
  receiptId: string;
  mode?: 'view' | 'edit';
  onSave?: (data: { signers: Signer[]; metadata: { location: string; createdDate: string } }) => void;
  onCancel?: () => void;
}

// Helper: Render signature SVG from data string
function renderSignatureSVGFromData(data: string, color?: string) {
  try {
    const points: SignaturePoint[][] = JSON.parse(data);
    if (!points || points.length === 0) return null;

    // Find bounding box
    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    points.forEach(stroke => {
      stroke.forEach(point => {
        if (point.x < minX) minX = point.x;
        if (point.x > maxX) maxX = point.x;
        if (point.y < minY) minY = point.y;
        if (point.y > maxY) maxY = point.y;
      });
    });

    const width = maxX - minX;
    const height = maxY - minY;
    if (width <= 0 || height <= 0) return null;

    // Build SVG paths
    const paths = points.map(stroke => {
      if (stroke.length === 0) return '';
      const pathData = stroke.map((point, idx) => 
        `${idx === 0 ? 'M' : 'L'} ${point.x} ${point.y}`
      ).join(' ');
      return `<path d="${pathData}" stroke="${color || '#000'}" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"/>`;
    });

    return (
      <svg viewBox={`${minX - 5} ${minY - 5} ${width + 10} ${height + 10}`} className="w-full h-full">
        {paths.map((path, idx) => (
          <g key={idx} dangerouslySetInnerHTML={{ __html: path }} />
        ))}
      </svg>
    );
  } catch (error) {
    console.error('Failed to render signature from data:', error);
    return null;
  }
}

export default function PDFViewKV({ receiptId, mode = 'view', onSave, onCancel }: PDFViewKVProps) {
  const [receipt, setReceipt] = useState<Receipt | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [completed, setCompleted] = useState(false);
  
  // Edit mode state
  const [location, setLocation] = useState('TP. Cần Thơ');
  const [createdDate, setCreatedDate] = useState(formatVietnameseDate(new Date()));
  const [signers, setSigners] = useState<Signer[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  
  // Signature state
  const [isSignatureModalOpen, setIsSignatureModalOpen] = useState(false);
  const [currentSignerIndex, setCurrentSignerIndex] = useState<number | null>(null);
  const [localSignatures, setLocalSignatures] = useState<Record<string, string>>({});
  const [signatureDataMap, setSignatureDataMap] = useState<Record<string, SignatureData>>({});

  // Action states
  const [signing, setSigning] = useState(false);
  const [exporting, setExporting] = useState(false);

  const pdfContainerRef = useRef<HTMLDivElement>(null);
  const { toasts, showToast, removeToast } = useToast();

  useEffect(() => {
    const fetchPDF = async () => {
      try {
        const res = await fetch(`/api/receipts/get?id=${receiptId}`);
        const data = await res.json();
        
        if (data.success && data.receipt) {
          setReceipt(data.receipt);
          
          // Initialize edit state
          if (mode === 'edit') {
            const metadata = data.receipt.document?.metadata;
            if (metadata) {
              setLocation(metadata.location || 'TP. Cần Thơ');
              setCreatedDate(metadata.createdDate || formatVietnameseDate(new Date()));
            }
            if (data.receipt.document?.signers) {
              setSigners(data.receipt.document.signers);
            } else {
              // Default signers
              setSigners([
                { id: 'signer-0', role: 'Bên A', name: '', position: '', organization: '', signed: false },
                { id: 'signer-1', role: 'Bên B', name: '', position: '', organization: '', signed: false },
              ]);
            }
          }
          
          // Check if already fully signed
          if (data.receipt.status === 'signed') {
            setCompleted(true);
          }
        } else {
          setError(data.error || 'Không tìm thấy tài liệu');
        }
      } catch (err) {
        console.error('Error fetching PDF:', err);
        setError('Có lỗi xảy ra khi tải tài liệu');
      } finally {
        setLoading(false);
      }
    };

    fetchPDF();
  }, [receiptId, mode]);

  // Check if fully signed
  const isFullySigned = (receipt: Receipt): boolean => {
    if (receipt.document?.signers) {
      const signedCount = receipt.document.signers.filter(s => s.signed).length;
      return signedCount >= 2;
    }
    return receipt.status === 'signed';
  };

  const handleSignerChange = (index: number, field: keyof Signer, value: any) => {
    const updated = [...signers];
    updated[index] = { ...updated[index], [field]: value };
    setSigners(updated);
  };

  const handleOpenSignature = (signerIndex: number) => {
    setCurrentSignerIndex(signerIndex);
    setIsSignatureModalOpen(true);
  };

  const handleSignatureComplete = async (result: SignatureResult) => {
    if (currentSignerIndex === null) return;

    const signatureData: SignatureData = {
      type: result.type,
      data: result.data,
      color: result.color,
      fontFamily: result.fontFamily,
    };

    const signer = signers[currentSignerIndex];
    
    // Update local state immediately for UI feedback
    const updated = [...signers];
    updated[currentSignerIndex] = {
      ...updated[currentSignerIndex],
      signed: true,
      signedAt: Date.now(),
      signatureData: signatureData,
    };
    setSigners(updated);
    
    // Store for preview
    setLocalSignatures(prev => ({
      ...prev,
      [signer.id]: result.previewDataUrl
    }));
    setSignatureDataMap(prev => ({
      ...prev,
      [signer.id]: signatureData
    }));
    
    setIsSignatureModalOpen(false);
    setCurrentSignerIndex(null);

    // Submit signature to backend immediately
    try {
      const response = await signDocument(receiptId, {
        signerId: signer.id,
        signerRole: signer.role,
        signerName: signer.name || '',
        signerEmail: signer.email || '',
        signatureData: {
          type: signatureData.type,
          data: signatureData.data,
          fontFamily: signatureData.fontFamily,
          color: signatureData.color,
        },
      });

      if (response.success || response.statusCode === 409) { // 409 is ALREADY_SIGNED
        showToast(`✓ Đã ký cho ${signer.role}`, 'success');
        // Reload receipt to get updated status
        const res = await fetch(`/api/receipts/get?id=${receiptId}`);
        const data = await res.json();
        if (data.success && data.receipt) {
          setReceipt(data.receipt);
          setCompleted(isFullySigned(data.receipt));
        }
      } else {
        showToast(response.message || 'Ký thất bại', 'error');
        // Revert local state on error
        const reverted = [...signers];
        reverted[currentSignerIndex] = {
          ...reverted[currentSignerIndex],
          signed: false,
          signedAt: undefined,
          signatureData: undefined,
        };
        setSigners(reverted);
      }
    } catch (error: any) {
      console.error('Error signing:', error);
      showToast(error.message || 'Có lỗi xảy ra khi ký', 'error');
      // Revert local state on error
      const reverted = [...signers];
      reverted[currentSignerIndex] = {
        ...reverted[currentSignerIndex],
        signed: false,
        signedAt: undefined,
        signatureData: undefined,
      };
      setSigners(reverted);
    }
  };

  const handleSave = async () => {
    if (!location.trim()) {
      showToast('Vui lòng nhập địa điểm', 'error');
      return;
    }

    if (!onSave) return;

    setIsSaving(true);
    try {
      await onSave({
        signers: signers,
        metadata: {
          location: location.trim(),
          createdDate,
        },
      });
    } catch (error) {
      console.error('Save error:', error);
      showToast('Lưu thất bại. Vui lòng thử lại.', 'error');
    } finally {
      setIsSaving(false);
    }
  };

  const handleExportPDF = async () => {
    if (!pdfContainerRef.current || !receipt) return;

    setExporting(true);
    try {
      const dataUrl = await toPng(pdfContainerRef.current, {
        quality: 1.0,
        backgroundColor: '#ffffff',
        pixelRatio: 2,
      });

      const pdf = new jsPDF({
        orientation: 'portrait',
        unit: 'mm',
        format: 'a4',
      });

      const pageWidth = pdf.internal.pageSize.getWidth();
      const pageHeight = pdf.internal.pageSize.getHeight();

      const img = new Image();
      img.src = dataUrl;
      
      await new Promise((resolve) => {
        img.onload = () => {
          let finalWidth = pageWidth;
          let finalHeight = (img.height / img.width) * pageWidth;

          if (finalHeight > pageHeight) {
            finalHeight = pageHeight;
            finalWidth = (img.width / img.height) * pageHeight;
          }

          pdf.addImage(dataUrl, 'PNG', 0, 0, finalWidth, finalHeight);
          pdf.save(`PDF_${receiptId}.pdf`);
          resolve(null);
        };
      });

      showToast('Đã tải xuống PDF', 'success');
    } catch (error) {
      console.error('Error exporting PDF:', error);
      showToast('Không thể xuất PDF', 'error');
    } finally {
      setExporting(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-glass">
        <div className="flex flex-col items-center gap-4">
          <Loader2 className="w-8 h-8 animate-spin text-gray-600" />
          <span className="text-gray-500">Đang tải tài liệu...</span>
        </div>
      </div>
    );
  }

  if (error || !receipt || !receipt.pdfUrl) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-glass">
        <div className="glass-card rounded-2xl p-8 max-w-md text-center">
          <AlertCircle className="w-16 h-16 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-bold mb-2">Không tìm thấy tài liệu</h2>
          <p className="text-gray-500">{error || 'Tài liệu không tồn tại'}</p>
        </div>
      </div>
    );
  }

  const pdfUrl = receipt.pdfUrl.startsWith('http') 
    ? receipt.pdfUrl 
    : `${process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:5100'}${receipt.pdfUrl}`;
  
  // Use edit state if in edit mode, otherwise use receipt data
  const displaySigners = mode === 'edit' ? signers : (receipt.document?.signers || [
    { id: 'signer-1', role: 'Bên A', name: '', signed: false, signatureData: null },
    { id: 'signer-2', role: 'Bên B', name: '', signed: false, signatureData: null }
  ]);
  const displayLocation = mode === 'edit' ? location : (receipt.document?.metadata?.location || 'TP. Cần Thơ');
  const displayCreatedDate = mode === 'edit' ? createdDate : (receipt.document?.metadata?.createdDate || new Date().toLocaleDateString('vi-VN'));

  return (
    <div className="min-h-screen bg-gradient-glass">
      <ToastContainer toasts={toasts} onRemove={removeToast} />

      {/* Header - Only in edit mode */}
      {mode === 'edit' && (
        <div className="border-b border-gray-200 bg-white/80 backdrop-blur-sm sticky top-0 z-10">
          <div className="max-w-7xl mx-auto px-6 py-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <button
                  onClick={onCancel}
                  className="p-2 hover:bg-gray-100 rounded-xl transition-colors"
                >
                  <X className="w-5 h-5 text-gray-600" />
                </button>
                <div>
                  <h1 className="text-xl font-bold text-gray-900">Chỉnh sửa PDF</h1>
                  <p className="text-sm text-gray-500">Chỉnh sửa thông tin và chữ ký</p>
                </div>
              </div>
              <button
                onClick={handleSave}
                disabled={isSaving}
                className="flex items-center gap-2 px-4 py-2 bg-black text-white rounded-xl hover:bg-gray-800 disabled:opacity-50"
              >
                {isSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                Lưu
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="max-w-7xl mx-auto py-8 px-4">
        <div className={`grid grid-cols-1 ${mode === 'edit' ? 'lg:grid-cols-3' : ''} gap-6`}>
          {/* Main Content: PDF + Footer */}
          <div className={mode === 'edit' ? 'lg:col-span-2' : ''}>
            {/* Success Banner */}
            {completed && (
              <div className="glass-card rounded-2xl p-6 mb-6 text-center border-2 border-green-400">
                <CheckCircle2 className="w-12 h-12 text-green-600 mx-auto mb-3" />
                <h3 className="text-xl font-bold text-green-900 mb-2">
                  ✅ Tài liệu đã hoàn tất!
                </h3>
                <p className="text-green-700">
                  Tất cả các bên đã ký xác nhận. Bạn có thể tải xuống file PDF.
                </p>
              </div>
            )}

            {/* PDF Content */}
            <div className="glass-card rounded-2xl p-8 mb-6" ref={pdfContainerRef}>
              {/* PDF Display */}
              <div className="mb-8 border-2 border-gray-200 rounded-xl overflow-hidden bg-gray-50">
                <object
                  data={pdfUrl}
                  type="application/pdf"
                  className="w-full"
                  style={{ minHeight: '800px', height: '800px' }}
                  aria-label="PDF Document"
                >
                  <p className="p-8 text-center text-gray-500">
                    Trình duyệt của bạn không hỗ trợ hiển thị PDF. 
                    <a href={pdfUrl} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline ml-2">
                      Tải xuống PDF
                    </a>
                  </p>
                </object>
              </div>

              {/* Signatures Footer */}
              <div className="mt-12 border-t-2 border-gray-300 pt-8">
                <p className="text-right mb-8 text-gray-600">
                  {displayLocation}, {displayCreatedDate}
                </p>

                <div className={`grid gap-8 ${displaySigners.length > 2 ? 'grid-cols-2' : `grid-cols-${displaySigners.length}`}`}>
                  {displaySigners.map((signer, index) => {
                    const hasLocalSignature = localSignatures[signer.id];
                    const hasBackendSignature = signer.signed && signer.signatureData;
                    const signatureData = mode === 'edit' ? signer.signatureData : (signer.signatureData || null);

                    return (
                      <div key={signer.id} className="text-center">
                        <p className="font-bold mb-2">{signer.role}</p>
                        <p className="text-sm italic text-gray-500 mb-4">(Ký và ghi rõ họ tên)</p>

                        <div className="min-h-[100px] flex items-center justify-center mb-4">
                          {hasLocalSignature ? (
                            <img
                              src={localSignatures[signer.id]}
                              alt={`Chữ ký ${signer.role}`}
                              className="h-20 w-auto object-contain"
                            />
                          ) : hasBackendSignature && signatureData ? (
                            <>
                              {signatureData.type === 'type' ? (
                                <span 
                                  className="text-2xl italic" 
                                  style={{ 
                                    fontFamily: signatureData.fontFamily || 'cursive',
                                    color: signatureData.color || '#000'
                                  }}
                                >
                                  {signatureData.data}
                                </span>
                              ) : signatureData.type === 'draw' ? (
                                <svg 
                                  viewBox="0 0 250 80" 
                                  className="w-full h-full"
                                  style={{ maxWidth: '200px', maxHeight: '80px' }}
                                >
                                  {renderSignatureSVGFromData(signatureData.data, signatureData.color)}
                                </svg>
                              ) : (
                                <span className="text-green-600 text-sm font-medium">✓ Đã ký</span>
                              )}
                            </>
                          ) : !completed && !signer.signed && mode === 'edit' ? (
                            <button
                              onClick={() => handleOpenSignature(index)}
                              className="flex items-center gap-2 px-4 py-2 border-2 border-dashed border-blue-400 rounded-xl text-blue-600 hover:border-blue-600 hover:bg-blue-50 transition-colors"
                            >
                              <PenLine className="w-4 h-4" />
                              Ký ngay
                            </button>
                          ) : !completed && !signer.signed && mode === 'view' ? (
                            <button
                              onClick={() => handleOpenSignature(index)}
                              className="flex items-center gap-2 px-4 py-2 border-2 border-dashed border-blue-400 rounded-xl text-blue-600 hover:border-blue-600 hover:bg-blue-50 transition-colors"
                            >
                              <PenLine className="w-4 h-4" />
                              Ký xác nhận
                            </button>
                          ) : (
                            <span className="text-gray-400 italic">
                              {signer.signed ? '✓ Đã ký' : 'Chưa ký'}
                            </span>
                          )}
                        </div>

                        {mode === 'edit' ? (
                          <input
                            type="text"
                            value={signer.name || ''}
                            onChange={(e) => handleSignerChange(index, 'name', e.target.value)}
                            placeholder="Họ và tên"
                            className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:border-blue-300 focus:outline-none text-center"
                          />
                        ) : (
                          <p className="border-t border-dotted border-gray-400 pt-2 inline-block px-8">
                            {signer.name || '...........................'}
                          </p>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>

            {/* Actions - Only in view mode */}
            {mode === 'view' && (
              <div className="glass-card rounded-2xl p-6">
                <div className="flex flex-col gap-3">
                  <button
                    onClick={handleExportPDF}
                    disabled={exporting || !completed}
                    className="w-full px-6 py-3 border-2 border-black text-black rounded-xl hover:bg-gray-100 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                  >
                    {exporting ? (
                      <>
                        <Loader2 className="w-5 h-5 animate-spin" />
                        Đang xuất...
                      </>
                    ) : (
                      <>
                        <FileDown className="w-5 h-5" />
                        Tải xuống PDF
                      </>
                    )}
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Sidebar - Only in edit mode */}
          {mode === 'edit' && (
            <div className="space-y-6">
              {/* Metadata */}
              <div className="glass-card rounded-2xl p-6">
                <h3 className="font-bold text-gray-900 mb-4 flex items-center gap-2">
                  <Calendar className="w-5 h-5" />
                  Thông tin tài liệu
                </h3>

                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2 flex items-center gap-1">
                      <Calendar className="w-4 h-4" />
                      Ngày lập
                    </label>
                    <input
                      type="text"
                      value={createdDate}
                      onChange={(e) => setCreatedDate(e.target.value)}
                      className="w-full px-4 py-2.5 glass-input rounded-xl"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2 flex items-center gap-1">
                      <MapPin className="w-4 h-4" />
                      Địa điểm <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="text"
                      value={location}
                      onChange={(e) => setLocation(e.target.value)}
                      placeholder="TP. Cần Thơ"
                      className="w-full px-4 py-2.5 glass-input rounded-xl"
                    />
                  </div>
                </div>
              </div>

              {/* Signers */}
              <div className="glass-card rounded-2xl p-6">
                <h3 className="font-bold text-gray-900 mb-4 flex items-center gap-2">
                  <Users className="w-5 h-5" />
                  Các bên ký ({signers.length})
                </h3>

                <div className="space-y-4">
                  {signers.map((signer, index) => (
                    <div
                      key={signer.id}
                      className="p-4 bg-white border border-gray-200 rounded-xl space-y-3"
                    >
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-bold text-gray-700">
                          {signer.role}
                        </span>
                        {signer.signed ? (
                          <span className="text-xs px-2 py-1 bg-green-100 text-green-700 rounded-lg">
                            ✓ Đã ký
                          </span>
                        ) : (
                          <button
                            onClick={() => handleOpenSignature(index)}
                            className="text-xs px-2 py-1 bg-blue-50 text-blue-700 rounded-lg hover:bg-blue-100 transition-colors flex items-center gap-1"
                          >
                            <PenLine className="w-3 h-3" />
                            Ký ngay
                          </button>
                        )}
                      </div>

                      <input
                        type="text"
                        value={signer.name}
                        onChange={(e) => handleSignerChange(index, 'name', e.target.value)}
                        placeholder="Họ và tên"
                        className="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:border-blue-300 focus:outline-none"
                      />
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Signature Modal */}
      <SignatureModal
        isOpen={isSignatureModalOpen}
        onApply={handleSignatureComplete}
        onClose={() => {
          setIsSignatureModalOpen(false);
          setCurrentSignerIndex(null);
        }}
      />
    </div>
  );
}
