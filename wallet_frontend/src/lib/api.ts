const configuredBase = (import.meta.env.VITE_API_BASE_URL || import.meta.env.EXPO_PUBLIC_API_BASE_URL || 'https://campus-pay-na3y.onrender.com').replace(/\/$/, '')
const BASE = configuredBase ? `${configuredBase}/api/v1.0` : '/api/v1.0'

export function getApiBaseUrl() {
  return configuredBase || window.location.origin
}

export class ApiError extends Error {
  status: number
  details?: unknown
  constructor(message: string, status: number, details?: unknown) {
    super(message)
    this.status = status
    this.details = details
  }
}

function collectErrorMessages(value: unknown): string[] {
  if (typeof value === 'string') return value.trim() ? [value.trim()] : []
  if (Array.isArray(value)) return value.flatMap(collectErrorMessages)
  if (!value || typeof value !== 'object') return []

  const record = value as Record<string, unknown>
  const direct = [record.message, record.title, record.error, record.detail]
    .flatMap(collectErrorMessages)
  const validation = record.errors
  const validationMessages = validation && typeof validation === 'object'
    ? Object.values(validation as Record<string, unknown>).flatMap(collectErrorMessages)
    : []

  return [...direct, ...validationMessages]
}

function errorMessage(data: unknown, status?: number): string {
  const messages = [...new Set(collectErrorMessages(data))]
  if (messages.length) return messages.join(' • ')
  if (status === 401) return 'Your session has expired. Please sign in again.'
  if (status === 403) return 'You do not have permission to perform this action.'
  if (status === 404) return 'The requested item could not be found.'
  if (status && status >= 500) return 'The server could not complete your request. Please try again.'
  return 'Something went wrong. Please try again.'
}

export async function api<T = unknown>(path: string, options: RequestInit = {}): Promise<T> {
  const isForm = options.body instanceof FormData
  const headers = new Headers(options.headers)
  const token = typeof window !== 'undefined' ? sessionStorage.getItem('cp-token') : null

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  if (!isForm && options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  let response: Response
  try {
    response = await fetch(`${BASE}${path.startsWith('/') ? path : `/${path}`}`, {
      ...options,
      credentials: 'omit',
      headers,
    })
  } catch (error) {
    const message = error instanceof Error && error.message
      ? `Unable to reach the server: ${error.message}`
      : 'Unable to reach the server. Please check your connection and try again.'
    throw new ApiError(message, 0, error)
  }

  const type = response.headers.get('content-type') || ''
  let data: unknown = null

  if (response.status !== 204) {
    try {
      data = type.includes('application/json') ? await response.json() : await response.text()
    } catch {
      data = null
    }
  }

  if (!response.ok) {
    if (response.status === 401) {
      sessionStorage.removeItem('cp-token')
    }

    throw new ApiError(errorMessage(data, response.status), response.status, data)
  }

  return data as T
}

export function toFormData(values: Record<string, FormDataEntryValue | undefined>) {
  const data = new FormData()
  Object.entries(values).forEach(([key, value]) => value !== undefined && value !== '' && data.append(key, value))
  return data
}
