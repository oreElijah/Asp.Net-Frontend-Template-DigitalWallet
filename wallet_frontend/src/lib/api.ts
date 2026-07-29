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

  const response = await fetch(`${BASE}${path.startsWith('/') ? path : `/${path}`}`, {
    ...options,
    credentials: 'omit',
    headers,
  })

  const type = response.headers.get('content-type') || ''
  let data: unknown = null

  if (response.status !== 204) {
    data = type.includes('application/json') ? await response.json() : await response.text()
  }

  if (!response.ok) {
    if (response.status === 401) {
      sessionStorage.removeItem('cp-token')
    }

    const message = typeof data === 'string'
      ? data
      : (data as { message?: string; title?: string; error?: string } | null)?.message
        || (data as { message?: string; title?: string; error?: string } | null)?.title
        || (data as { message?: string; title?: string; error?: string } | null)?.error
        || 'Something went wrong'

    throw new ApiError(message, response.status, data)
  }

  return data as T
}

export function toFormData(values: Record<string, FormDataEntryValue | undefined>) {
  const data = new FormData()
  Object.entries(values).forEach(([key, value]) => value !== undefined && value !== '' && data.append(key, value))
  return data
}
