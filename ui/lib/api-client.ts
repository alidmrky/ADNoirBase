/**
 * .NET API için type-safe fetch wrapper.
 * Token yönetimi, hata handling ve BaseResponse kontratı burada merkezi olarak yönetilir.
 */

export interface BaseResponse {
  isSuccess: boolean
  status: string
  messageCode: string
  message: string | null
  messageArgs: Record<string, unknown> | null
  errors: ResponseMessage[]
  warnings: ResponseMessage[]
  correlationId: string | null
  traceId: string | null
  timestampUtc: string
}

export interface BaseResponseWithData<T> extends BaseResponse {
  data: T
}

export interface ResponseMessage {
  code: string
  severity: "Error" | "Warning" | "Info"
  text: string | null
  args: Record<string, unknown> | null
}

export interface PagedList<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ─── HTTP Status → Error mapping ──────────────────────────────────────────────

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly response: BaseResponse
  ) {
    super(response.messageCode)
    this.name = "ApiError"
  }
}

// ─── Client ───────────────────────────────────────────────────────────────────

type RequestOptions = Omit<RequestInit, "body"> & {
  body?: unknown
  token?: string
}

const API_URL = process.env.API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? ""

async function request<T>(
  path: string,
  options: RequestOptions = {}
): Promise<BaseResponseWithData<T>> {
  const { body, token, headers: extraHeaders, ...rest } = options

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(extraHeaders ?? {}),
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...rest,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (response.status === 401) throw new Error("Oturum süresi doldu. Lütfen tekrar giriş yapın.")
  if (response.status === 403) throw new Error("Bu işlem için yetkiniz yok.")
  const data: BaseResponseWithData<T> = await response.json()

  if (!data.isSuccess) {
    throw new ApiError(response.status, data)
  }

  return data
}

// ─── Typed helpers ────────────────────────────────────────────────────────────

export const apiClient = {
  get: <T>(path: string, options?: RequestOptions) =>
    request<T>(path, { method: "GET", ...options }),

  post: <T>(path: string, body: unknown, options?: RequestOptions) =>
    request<T>(path, { method: "POST", body, ...options }),

  put: <T>(path: string, body: unknown, options?: RequestOptions) =>
    request<T>(path, { method: "PUT", body, ...options }),

  delete: <T>(path: string, options?: RequestOptions) =>
    request<T>(path, { method: "DELETE", ...options }),
}
