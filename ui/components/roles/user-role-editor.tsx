"use client"
import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { roleApi, roleError, type Role, type RolePage } from "@/lib/roles"

export function UserRoleEditor({ token, userId, currentUserId }: { token: string; userId: string; currentUserId: string }) {
  const router = useRouter()
  const [assigned, setAssigned] = useState<Role[]>()
  const [available, setAvailable] = useState<RolePage>()
  const [search, setSearch] = useState("")
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState("")
  const [revision, setRevision] = useState(0)
  const [error, setError] = useState("")
  const [message, setMessage] = useState("")
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(true)
  useEffect(() => {
    let active = true
    const timer = setTimeout(async () => {
      setLoading(true)
      try { const [roles, catalog] = await Promise.all([roleApi.assigned(token, userId), roleApi.list(token, page, search)]); if (active) { setAssigned(roles); setAvailable(catalog) } }
      catch (e) { if (active) setError(roleError(e)) }
      finally { if (active) setLoading(false) }
    }, 200)
    return () => { active = false; clearTimeout(timer) }
  }, [token, userId, page, search, revision])
  async function change(id: string, remove: boolean) {
    setBusy(true); setError(""); setMessage("")
    try { if (remove) await roleApi.unassign(token, userId, id); else await roleApi.assign(token, userId, id); setSelected(""); setMessage(remove ? "Rol kaldırıldı." : "Rol atandı."); setRevision(n => n + 1); router.refresh() }
    catch (e) { setError(roleError(e)); setRevision(n => n + 1) }
    finally { setBusy(false) }
  }
  return <section className="space-y-4 rounded-xl border p-5"><div className="flex items-center justify-between"><h2 className="text-lg font-semibold">Kullanıcı rolleri</h2><Link href="/roles" className="text-sm underline">Rol kataloğu</Link></div>
    <p className="text-sm text-muted-foreground">Rol değişiklikleri kullanıcının yeni girişinde veya token yenilendiğinde yetkilerine yansır.</p>
    {error && <p role="alert" className="text-sm text-destructive">{error}</p>}{message && <p role="status" className="text-sm">{message}</p>}
    {loading && <p className="text-sm">Yükleniyor…</p>}
    <ul className="space-y-2">{assigned?.map(role => <li key={role.id} className="flex items-center justify-between gap-3 rounded border p-3"><span>{role.name} <span className="font-mono text-sm text-muted-foreground">{role.code}</span></span><Button size="sm" variant="destructive" disabled={busy || loading || (role.code === "ADMIN" && userId === currentUserId)} onClick={() => { if (confirm(`${role.code} rolü bu kullanıcıdan kaldırılsın mı?`)) void change(role.id, true) }}>Kaldır</Button></li>)}</ul>
    {!loading && assigned?.length === 0 && <p className="text-sm text-muted-foreground">Kullanıcıya atanmış rol yok.</p>}
    {userId === currentUserId && <p className="text-xs text-muted-foreground">Kendi ADMIN rolünüzü kaldıramazsınız.</p>}
    <div className="space-y-2 border-t pt-4"><Input aria-label="Atanacak rolü ara" placeholder="Atanacak rolü ara…" value={search} onChange={e => { setSearch(e.target.value); setPage(1); setSelected("") }} /><div className="flex gap-2"><select aria-label="Atanacak rol" className="min-w-0 flex-1 rounded-md border bg-background px-3 text-sm" value={selected} disabled={loading || busy} onChange={e => setSelected(e.target.value)}><option value="">Rol seçin</option>{available?.items.filter(r => !assigned?.some(a => a.id === r.id)).map(r => <option key={r.id} value={r.id}>{r.name} ({r.code})</option>)}</select><Button disabled={!selected || busy || loading} onClick={() => void change(selected, false)}>Rol ata</Button></div>{available && <div className="flex justify-end gap-2"><Button size="sm" variant="outline" disabled={page <= 1 || loading} onClick={() => { setPage(p => p - 1); setSelected("") }}>Önceki roller</Button><Button size="sm" variant="outline" disabled={!available.hasNextPage || loading} onClick={() => { setPage(p => p + 1); setSelected("") }}>Sonraki roller</Button></div>}</div>
  </section>
}
