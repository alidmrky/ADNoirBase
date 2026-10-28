import { apiClient, ApiError } from "@/lib/api-client"
import type { PagedList } from "@/lib/api-client"

export interface Role { id: string; name: string; code: string; description: string | null; isSystem: boolean; userCount: number }
export type RolePage = PagedList<Role>
export const roleApi = {
  list: (token: string, page = 1, search = "") => apiClient.get<RolePage>(`/api/admin/roles?page=${page}&searchTerm=${encodeURIComponent(search)}`, { token, cache: "no-store" }).then(r => r.data),
  create: (token: string, body: { name: string; code: string; description: string }) => apiClient.post<Role>("/api/admin/roles", body, { token }),
  update: (token: string, id: string, body: { name: string; description: string }) => apiClient.put(`/api/admin/roles/${id}`, body, { token }),
  remove: (token: string, id: string) => apiClient.delete(`/api/admin/roles/${id}`, { token }),
  assigned: (token: string, userId: string) => apiClient.get<Role[]>(`/api/admin/users/${userId}/roles`, { token, cache: "no-store" }).then(r => r.data),
  assign: (token: string, userId: string, roleId: string) => apiClient.post(`/api/admin/users/${userId}/roles/${roleId}`, undefined, { token }),
  unassign: (token: string, userId: string, roleId: string) => apiClient.delete(`/api/admin/users/${userId}/roles/${roleId}`, { token }),
}
export function roleError(error: unknown): string {
  const messages: Record<string, string> = {
    "identity.role.code_conflict": "Bu rol kodu zaten kullanılıyor.",
    "identity.role.invalid": "Ad 1–100, kod 1–50, açıklama en fazla 500 karakter olmalı. Kod harfle başlamalı; yalnızca harf, rakam ve alt çizgi içermeli.",
    "identity.role.system_immutable": "Sistem rolleri değiştirilemez veya silinemez.",
    "identity.role.in_use": "Bu rol kullanıcılara atanmış. Silmeden önce atamaları kaldırın.",
    "identity.role.cannot_remove_own_admin": "Kendi ADMIN rolünüzü kaldıramazsınız.",
    "identity.role.already_assigned": "Bu rol kullanıcıya zaten atanmış.",
    "identity.role.not_assigned": "Bu rol artık kullanıcıya atanmış değil.",
    "identity.role.not_found": "Rol bulunamadı. Listeyi yenileyin.",
  }
  if (error instanceof ApiError) return messages[error.response.messageCode] ?? "İşlem tamamlanamadı. Lütfen tekrar deneyin."
  return error instanceof Error ? error.message : "İşlem tamamlanamadı."
}
