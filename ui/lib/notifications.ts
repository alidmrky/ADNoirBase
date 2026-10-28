import type { PagedList } from "@/lib/api-client"

export type Page<T> = PagedList<T>
export type Channel = "Email" | "Sms" | "InApp"
export type Category = "Transactional" | "Marketing"
export interface Template {
  id: string; code: string; name: string; description: string | null
  channel: Channel; category: Category; isSystem: boolean; isLayout: boolean; isActive: boolean
  layoutCode: string | null; subject: string | null; htmlBody: string | null
  textBody: string | null; smsBody: string | null
  variables: { name: string; description: string | null; sampleValue: string | null }[]
  logPolicy: string; retentionDays: number
}
export interface Campaign {
  id: string; name: string; channel: Channel; templateCode: string; status: string
  totalRecipients: number; queuedCount: number; scheduledAt: string | null; lastError?: string
}
export interface Message {
  id: string; channel: Channel; status: string; subject: string | null; createdAt: string
  to: { address: string }[]; cc: { address: string }[]; bcc: { address: string }[]
  attempts: number; lastError: string | null; templateCode: string | null; campaignId: string | null
}
export interface InAppItem {
  id: string; title: string; body: string; link: string | null; isRead: boolean; createdAt: string
}
export interface Preferences { emailMarketing: boolean; smsMarketing: boolean; inAppMarketing: boolean }
export interface Attachment { fileId: string; fileName: string; length: number }
export const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5197"

export async function notificationApi<T>(path: string, token?: string, method = "GET", body?: unknown): Promise<T> {
  const multipart = body instanceof FormData
  const response = await fetch(apiUrl + "/api/" + path, {
    method, cache: "no-store",
    headers: { ...(token ? { Authorization: "Bearer " + token } : {}), ...(!multipart ? { "Content-Type": "application/json" } : {}) },
    body: body === undefined ? undefined : multipart ? body : JSON.stringify(body),
  })
  if (response.status === 401) throw new Error("Oturum süresi doldu. Lütfen tekrar giriş yapın.")
  if (response.status === 403) throw new Error("Bu işlem için yetkiniz yok.")
  if (response.status === 429) throw new Error("Çok fazla istek gönderildi. Bir dakika sonra tekrar deneyin.")
  const result = await response.json()
  if (!response.ok || !result.isSuccess) {
    const messages: Record<string, string> = {
      "notification.attachment.too_large": "Dosya boyutu izin verilen sınırı aşıyor.",
      "notification.attachment.type_not_allowed": "Bu dosya türüne izin verilmiyor.",
      "notification.attachment.too_many": "En fazla 10 ek yükleyebilirsiniz.",
      "notification.template.render_failed": "Şablon veya içerik geçersiz. Alanları ve değişkenleri kontrol edin.",
      "notification.template.system_immutable": "Sistem şablonu silinemez.",
      "notification.preference.token_invalid": "Abonelikten çıkma bağlantısı geçersiz.",
    }
    throw new Error(messages[result.messageCode] ?? result.errors?.map((e: { text?: string; code: string }) => e.text ?? e.code).join(", ") ?? result.messageCode ?? "İşlem tamamlanamadı.")
  }
  return result.data as T
}
export function errorText(error: unknown) { return error instanceof Error ? error.message : "İşlem tamamlanamadı." }

