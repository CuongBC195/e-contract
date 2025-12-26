/**
 * Backend API Client
 * Utility functions to call the .NET backend API
 */

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:5100';

export interface ApiResponse<T> {
  statusCode: number;
  message: string;
  data: T | null;
  errors?: string[] | null;
  timestamp: string;
}

export interface UserDto {
  id: string;
  email: string;
  name: string;
  role: string;
  emailVerified: boolean;
}

export interface AuthResponseDto {
  token: string;
  user: UserDto;
}

export interface ReceiptInfoDto {
  senderName?: string;
  senderAddress?: string;
  receiverName?: string;
  receiverAddress?: string;
  amount: number;
  amountInWords?: string;
  reason?: string;
  location?: string;
  date?: string;
  customFields?: Record<string, string>;
}

export interface ContractMetadataDto {
  contractNumber?: string;
  location?: string;
  contractDate?: string;
}

export interface SignerDto {
  role: string;
  name: string;
  email: string;
}

export interface SignatureDataDto {
  type: string; // "draw" or "type"
  data: string;
  fontFamily?: string;
  color?: string;
}

export interface SignatureResponseDto {
  id: string;
  signerId: string;
  signerRole: string;
  signerName?: string;
  signerEmail?: string;
  signatureData: SignatureDataDto;
  signedAt: string;
}

export type DocumentType = 'Receipt' | 'Contract';
export type DocumentStatus = 'Pending' | 'PartiallySigned' | 'Signed';
export type SigningMode = 'Public' | 'RequiredLogin';

export interface DocumentResponseDto {
  id: string;
  type: DocumentType;
  title?: string;
  content?: string;
  status: DocumentStatus;
  signingMode: SigningMode;
  receiptInfo?: ReceiptInfoDto;
  metadata?: ContractMetadataDto;
  creator?: UserDto;
  signatures: SignatureResponseDto[];
  createdAt: string;
  signedAt?: string;
  viewedAt?: string;
}

export interface PaginatedResponseDto<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateDocumentRequestDto {
  type: DocumentType;
  title?: string;
  content?: string;
  receiptInfo?: ReceiptInfoDto;
  metadata?: ContractMetadataDto;
  signers: SignerDto[];
  signingMode?: SigningMode;
}

export interface UpdateDocumentRequestDto {
  title?: string;
  content?: string;
  receiptInfo?: ReceiptInfoDto;
  metadata?: ContractMetadataDto;
  signingMode?: SigningMode;
}

export interface SignDocumentRequestDto {
  signerId: string;
  signerRole?: string;
  signerName?: string;
  signerEmail?: string;
  signatureData: SignatureDataDto;
}

export interface SendInvitationRequestDto {
  documentId: string;
  customerEmail: string;
  customerName: string;
  signingUrl: string;
}

export interface UserResponseDto {
  id: string;
  email: string;
  name: string;
  role: string;
  emailVerified: boolean;
  createdAt: string;
  lastLoginAt?: string;
  documentCount: number;
}

/**
 * Get auth token from cookies (for server-side)
 */
async function getAuthToken(): Promise<string | null> {
  if (typeof window !== 'undefined') {
    // Client-side: cookies are automatically sent
    return null;
  }
  
  // Server-side: need to read from cookies
  const { cookies } = await import('next/headers');
  const cookieStore = await cookies();
  return cookieStore.get('jwt_token')?.value || null;
}

/**
 * Make API request to backend
 */
async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const token = await getAuthToken();
  
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> || {}),
  };

  // Add auth token if available (for server-side)
  if (token && typeof window === 'undefined') {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${BACKEND_URL}${endpoint}`, {
    ...options,
    headers,
    credentials: 'include', // Include cookies for client-side
  });

  // Get response text first (can only be read once)
  let responseText: string;
  try {
    responseText = await response.text();
  } catch (error) {
    throw new Error(`Failed to read response: ${response.statusText}`);
  }

  // Try to parse as JSON
  let data: any;
  const contentType = response.headers.get('content-type');
  const hasJsonContent = contentType?.includes('application/json');
  
  if (hasJsonContent && responseText) {
    try {
      data = JSON.parse(responseText);
    } catch (error) {
      // If JSON parse fails, use text as error message
      data = { message: responseText || response.statusText };
    }
  } else if (responseText) {
    // Non-JSON response
    data = { message: responseText || response.statusText };
  } else {
    // Empty response
    data = { message: response.statusText || 'Empty response' };
  }

  // If response is not ok, throw error with API message
  if (!response.ok) {
    const errorMessage = data.message || data.errors?.[0] || `API Error: ${response.statusText}`;
    throw new Error(errorMessage);
  }

  return data;
}

// ==================== Auth APIs ====================

export async function login(email: string, password: string): Promise<ApiResponse<AuthResponseDto>> {
  return apiRequest<AuthResponseDto>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

export async function register(
  email: string,
  password: string,
  name: string
): Promise<ApiResponse<AuthResponseDto>> {
  return apiRequest<AuthResponseDto>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ email, password, name }),
  });
}

export interface VerifyEmailRequestDto {
  email: string;
  otpCode: string;
}

export async function verifyEmail(request: VerifyEmailRequestDto): Promise<ApiResponse<any>> {
  return apiRequest<any>('/api/auth/verify-email', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export interface ForgotPasswordRequestDto {
  email: string;
}

export async function requestPasswordReset(request: ForgotPasswordRequestDto): Promise<ApiResponse<object>> {
  return apiRequest<object>('/api/auth/forgot-password', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export interface ResetPasswordRequestDto {
  email: string;
  token: string;
  newPassword: string;
}

export async function resetPassword(request: ResetPasswordRequestDto): Promise<ApiResponse<object>> {
  return apiRequest<object>('/api/auth/reset-password', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function getMe(): Promise<ApiResponse<UserDto>> {
  return apiRequest<UserDto>('/api/auth/me', {
    method: 'GET',
  });
}

export async function logout(): Promise<ApiResponse<object>> {
  return apiRequest<object>('/api/auth/logout', {
    method: 'POST',
  });
}

// ==================== Document APIs ====================

export async function createDocument(
  request: CreateDocumentRequestDto
): Promise<ApiResponse<DocumentResponseDto>> {
  return apiRequest<DocumentResponseDto>('/api/documents', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function getDocuments(params?: {
  status?: DocumentStatus;
  type?: DocumentType;
  page?: number;
  pageSize?: number;
}): Promise<ApiResponse<PaginatedResponseDto<DocumentResponseDto>>> {
  const queryParams = new URLSearchParams();
  if (params?.status) queryParams.append('status', params.status);
  if (params?.type) queryParams.append('type', params.type);
  if (params?.page) queryParams.append('page', params.page.toString());
  if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());

  const query = queryParams.toString();
  return apiRequest<PaginatedResponseDto<DocumentResponseDto>>(
    `/api/documents${query ? `?${query}` : ''}`,
    { method: 'GET' }
  );
}

export async function getDocument(id: string): Promise<ApiResponse<DocumentResponseDto>> {
  return apiRequest<DocumentResponseDto>(`/api/documents/${id}`, {
    method: 'GET',
  });
}

export async function updateDocument(
  id: string,
  request: UpdateDocumentRequestDto
): Promise<ApiResponse<DocumentResponseDto>> {
  return apiRequest<DocumentResponseDto>(`/api/documents/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function deleteDocument(id: string): Promise<ApiResponse<object>> {
  return apiRequest<object>(`/api/documents/${id}`, {
    method: 'DELETE',
  });
}

export async function signDocument(
  id: string,
  request: SignDocumentRequestDto
): Promise<ApiResponse<DocumentResponseDto>> {
  return apiRequest<DocumentResponseDto>(`/api/documents/${id}/sign`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function trackDocumentView(id: string): Promise<ApiResponse<object>> {
  return apiRequest<object>(`/api/documents/${id}/track-view`, {
    method: 'POST',
  });
}

// ==================== Email APIs ====================

export async function sendInvitation(
  request: SendInvitationRequestDto
): Promise<ApiResponse<object>> {
  return apiRequest<object>('/api/email/send-invitation', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

// ==================== Admin APIs ====================

export async function getUsers(params?: {
  page?: number;
  pageSize?: number;
  search?: string;
}): Promise<ApiResponse<PaginatedResponseDto<UserResponseDto>>> {
  const queryParams = new URLSearchParams();
  if (params?.page) queryParams.append('page', params.page.toString());
  if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());
  if (params?.search) queryParams.append('search', params.search);

  const query = queryParams.toString();
  return apiRequest<PaginatedResponseDto<UserResponseDto>>(
    `/api/admin/users${query ? `?${query}` : ''}`,
    { method: 'GET' }
  );
}

export async function getUser(id: string): Promise<ApiResponse<UserResponseDto>> {
  return apiRequest<UserResponseDto>(`/api/admin/users/${id}`, {
    method: 'GET',
  });
}

export async function deleteUser(id: string): Promise<ApiResponse<object>> {
  return apiRequest<object>(`/api/admin/users/${id}`, {
    method: 'DELETE',
  });
}

// ==================== Template APIs ====================

export interface Template {
  id: string;
  name: string;
  category: string;
  content: string;
  isActive: boolean;
}

export async function getTemplates(category?: string): Promise<ApiResponse<Template[]>> {
  const query = category ? `?category=${encodeURIComponent(category)}` : '';
  return apiRequest<Template[]>(`/api/templates${query}`, {
    method: 'GET',
  });
}

export async function getTemplate(id: string): Promise<ApiResponse<Template>> {
  return apiRequest<Template>(`/api/templates/${id}`, {
    method: 'GET',
  });
}

