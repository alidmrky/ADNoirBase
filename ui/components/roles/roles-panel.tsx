"use client"
import { useEffect, useState } from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { roleApi, roleError, type Role, type RolePage } from "@/lib/roles"

export function RolesPanel({ token }: { token: string }) {
  const [data, setData] = useState<RolePage>()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState("")
  const [revision, setRevision] = useState(0)
  const [error, setError] = useState("")
  const [message, setMessage] = useState("")
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [editor, setEditor] = useState<{ role?: Role } | null>(null)
  const [deleting, setDeleting] = useState<Role | null>(null)
  useEffect(() => {
    let active = true
    const timer = setTimeout(async () => {
      setLoading(true)
      try { const result = await roleApi.list(token, page, search); if (active) { setData(result); setError(""); if (!result.items.length && page > 1) setPage(page - 1) } }
      catch (e) { if (active) setError(roleError(e)) }
      finally { if (active) setLoading(false) }
    }, 200)
    return () => { active = false; clearTimeout(timer) }
  }, [token, page, search, revision])
  async function remove() {
    if (!deleting) return
    setBusy(true); setError(""); setMessage("")
    try { await roleApi.remove(token, deleting.id); setMessage("Rol silindi."); setDeleting(null); setRevision(n => n + 1) }
    catch (e) { setError(roleError(e)); setDeleting(null) }
    finally { setBusy(false) }
  }
  return <section className="mx-auto max-w-5xl space-y-5">
    <header className="flex flex-wrap items-start justify-between gap-4"><div><h1 className="text-2xl font-semibold">Roller</h1><p className="mt-1 text-muted-foreground">Rol kataloğunu yönetin. Kullanıcılara atamak için <Link href="/users" className="underline">kullanıcı detayını</Link> açın.</p></div><Button onClick={() => setEditor({})}>Yeni rol</Button></header>
    <p className="text-sm text-muted-foreground">Yeni roller otomatik olarak yönetici yetkisi vermez; uygulamadaki yetki kuralları rol kodlarına göre tanımlanır.</p>
    {error && <p role="alert" className="rounded border border-destructive p-3 text-destructive">{error}</p>}{message && <p role="status" className="rounded bg-muted p-3">{message}</p>}
    <div className="flex gap-2"><Input aria-label="Rol ara" placeholder="Rol adı veya kodu ara…" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /><Button variant="outline" onClick={() => setRevision(n => n + 1)}>Yenile</Button></div>
    <div className="overflow-x-auto rounded-lg border" aria-busy={loading}><table className="w-full text-left text-sm"><thead className="bg-muted"><tr>{["Rol", "Kod", "Açıklama", "Atama", "İşlemler"].map(h => <th key={h} className="p-3">{h}</th>)}</tr></thead><tbody>{data?.items.map(role => <tr key={role.id} className="border-t"><td className="p-3 font-medium">{role.name} {role.isSystem && <Badge variant="secondary">Sistem</Badge>}</td><td className="p-3 font-mono">{role.code}</td><td className="max-w-sm break-words p-3">{role.description || "—"}</td><td className="p-3">{role.userCount}</td><td className="p-3"><div className="flex gap-2"><Button variant="outline" size="sm" disabled={role.isSystem || busy} onClick={() => setEditor({ role })}>Düzenle</Button><Button variant="destructive" size="sm" disabled={role.isSystem || role.userCount > 0 || busy} onClick={() => setDeleting(role)}>Sil</Button></div>{(role.isSystem || role.userCount > 0) && <p className="mt-1 text-xs text-muted-foreground">{role.isSystem ? "Sistem rolü korunur." : "Atanmış rol silinemez."}</p>}</td></tr>)}</tbody></table>{loading ? <p className="p-4">Yükleniyor…</p> : !error && !data?.items.length ? <p className="p-6 text-center text-muted-foreground">Rol bulunamadı.</p> : null}</div>
    {data && <div className="flex items-center justify-end gap-3 text-sm"><span>{data.totalCount} rol · Sayfa {page} / {Math.max(1, data.totalPages)}</span><Button variant="outline" disabled={loading || page <= 1} onClick={() => setPage(p => p - 1)}>Önceki</Button><Button variant="outline" disabled={loading || !data.hasNextPage} onClick={() => setPage(p => p + 1)}>Sonraki</Button></div>}
    <Dialog open={editor !== null} onOpenChange={open => { if (!open) setEditor(null) }}><DialogContent><DialogHeader><DialogTitle>{editor?.role ? "Rolü düzenle" : "Yeni rol"}</DialogTitle><DialogDescription>Rol kodu oluşturulduktan sonra değiştirilemez.</DialogDescription></DialogHeader>{editor && <RoleForm key={editor.role?.id ?? "new"} role={editor.role} token={token} onSave={() => { setEditor(null); setMessage("Rol kaydedildi."); setRevision(n => n + 1) }} />}</DialogContent></Dialog>
    <Dialog open={!!deleting} onOpenChange={open => { if (!busy && !open) setDeleting(null) }}><DialogContent><DialogHeader><DialogTitle>Rol silinsin mi?</DialogTitle><DialogDescription>{deleting?.name} ({deleting?.code}) rolü silinecek.</DialogDescription></DialogHeader><div className="flex justify-end gap-2"><Button variant="outline" disabled={busy} onClick={() => setDeleting(null)}>Vazgeç</Button><Button variant="destructive" disabled={busy} onClick={() => void remove()}>Sil</Button></div></DialogContent></Dialog>
  </section>
}

function RoleForm({ role, token, onSave }: { role?: Role; token: string; onSave: () => void }) {
  const [name, setName] = useState(role?.name ?? "")
  const [code, setCode] = useState(role?.code ?? "")
  const [description, setDescription] = useState(role?.description ?? "")
  const [error, setError] = useState("")
  const [busy, setBusy] = useState(false)
  async function save() {
    setBusy(true); setError("")
    try { if (role) await roleApi.update(token, role.id, { name, description }); else await roleApi.create(token, { name, code, description }); onSave() }
    catch (e) { setError(roleError(e)) } finally { setBusy(false) }
  }
  return <form className="space-y-4" onSubmit={e => { e.preventDefault(); void save() }}>{error && <p role="alert" className="text-sm text-destructive">{error}</p>}<label className="grid gap-1 text-sm">Ad<Input required maxLength={100} value={name} onChange={e => setName(e.target.value)} /></label><label className="grid gap-1 text-sm">Kod<Input required maxLength={50} pattern="[A-Za-z][A-Za-z0-9_]*" disabled={!!role} value={code} onChange={e => setCode(e.target.value.toUpperCase())} /></label><label className="grid gap-1 text-sm">Açıklama<Textarea maxLength={500} value={description} onChange={e => setDescription(e.target.value)} /></label><Button type="submit" disabled={busy}>{busy ? "Kaydediliyor…" : "Kaydet"}</Button></form>
}
