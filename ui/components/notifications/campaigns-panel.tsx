"use client"
import { useCallback, useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { notificationApi, errorText, type Template, type Page, type Campaign } from "@/lib/notifications"
import { Field, Choice, Feedback, Pagination } from "./fields"

const endpoint = "admin/notifications/campaigns"
export function CampaignsPanel({ token }: { token: string }) {
  const [result, setResult] = useState<Page<Campaign>>()
  const [page, setPage] = useState(1)
  const [templates, setTemplates] = useState<Template[]>([])
  const [name, setName] = useState("")
  const [templateCode, setTemplateCode] = useState("")
  const [type, setType] = useState("AllUsers")
  const [values, setValues] = useState("")
  const [mode, setMode] = useState("Individual")
  const [scheduled, setScheduled] = useState("")
  const [preview, setPreview] = useState<{ recipientCount: number; exceedsLimit: boolean; limit: number }>()
  const [error, setError] = useState("")
  const [message, setMessage] = useState("")
  const [busy, setBusy] = useState(false)
  const [detail, setDetail] = useState<Campaign>()
  const load = useCallback(async () => {
    try { setResult(await notificationApi<Page<Campaign>>(`${endpoint}?page=${page}`, token)) } catch (e) { setError(errorText(e)) }
  }, [token, page])
  useEffect(() => { const timer = setTimeout(() => void load(), 0); const interval = setInterval(() => void load(), 10000); return () => { clearTimeout(timer); clearInterval(interval) } }, [load])
  useEffect(() => { notificationApi<Page<Template>>("admin/notifications/templates?pageSize=100&isActive=true", token).then(p => setTemplates(p.items.filter(t => !t.isLayout))).catch(e => setError(errorText(e))) }, [token])
  const selected = templates.find(t => t.code === templateCode)
  function audience() {
    const items = values.split(/[,;\n]/).map(s => s.trim()).filter(Boolean)
    return { type, roleCodes: type === "ByRole" ? items : [], statuses: type === "ByStatus" ? items : [], userIds: type === "SelectedUsers" ? items : [], manualAddresses: type === "ManualAddresses" ? items : [] }
  }
  async function run(action: () => Promise<void>) {
    setBusy(true); setError(""); setMessage("")
    try { await action() } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }
  return <div className="space-y-6">
    <form className="space-y-4 rounded-xl border p-5" onSubmit={e => { e.preventDefault(); void run(async () => { await notificationApi(endpoint, token, "POST", { name, templateCode, channel: selected?.channel, audience: audience(), sendMode: mode, scheduledAt: scheduled ? new Date(scheduled).toISOString() : null }); setMessage("Taslak oluşturuldu. Göndermek için listedeki Başlat düğmesini kullanın."); await load() }) }}>
      <h2 className="font-semibold">Yeni kampanya</h2><Feedback error={error} message={message} />
      <div className="grid gap-3 sm:grid-cols-2"><Field label="Kampanya adı"><Input required value={name} onChange={e => setName(e.target.value)} /></Field><Field label="Şablon"><Choice required value={templateCode} onChange={e => setTemplateCode(e.target.value)}><option value="">Şablon seçin</option>{templates.map(t => <option key={t.id} value={t.code}>{t.name} · {t.channel}</option>)}</Choice></Field></div>
      <Field label="Hedef kitle"><Choice value={type} onChange={e => { setType(e.target.value); setPreview(undefined) }}><option value="AllUsers">Tüm kullanıcılar</option><option value="ByRole">Rol bazlı</option><option value="ByStatus">Durum bazlı</option><option value="SelectedUsers">Seçili kullanıcı kimlikleri</option><option value="ManualAddresses">Elle girilen adresler</option></Choice></Field>
      {type !== "AllUsers" && <Field label={type === "ByStatus" ? "Durumlar: Active, Inactive, Locked (virgülle ayırın)" : type === "ByRole" ? "Rol kodları: ADMIN, USER" : "Değerler (virgülle veya yeni satırla ayırın)"}><Textarea required value={values} onChange={e => { setValues(e.target.value); setPreview(undefined) }} /></Field>}
      <div className="grid gap-3 sm:grid-cols-2"><Field label="Gönderim biçimi"><Choice value={mode} onChange={e => setMode(e.target.value)}><option value="Individual">Kişiye özel ayrı gönderim</option><option value="SingleBcc" disabled={selected?.channel !== "Email"}>Tek Bcc e-postası</option></Choice></Field><Field label="Planlanan zaman (boşsa hemen)"><Input type="datetime-local" value={scheduled} onChange={e => setScheduled(e.target.value)} /></Field></div>
      {mode === "SingleBcc" && <p className="text-sm text-muted-foreground">Tek Bcc gönderiminde kişiselleştirme ve abonelikten çıkma bağlantısı kullanılamaz.</p>}
      <div className="flex flex-wrap items-center gap-3"><Button type="button" variant="outline" disabled={busy} onClick={() => void run(async () => setPreview(await notificationApi(`${endpoint}/preview`, token, "POST", audience())))}>Hedef kitleyi hesapla</Button>{preview && <p className="text-sm">{preview.recipientCount} alıcı · üst sınır {preview.limit}{preview.exceedsLimit ? " — sınır aşıldı" : ""}</p>}<Button type="submit" disabled={busy || !selected || preview?.exceedsLimit}>Taslak oluştur</Button></div>
    </form>
    <div className="space-y-3"><h2 className="font-semibold">Kampanyalar</h2>{!result && <p>Yükleniyor…</p>}{result?.items.length === 0 && <p>Henüz kampanya yok.</p>}{result?.items.map(c => <article key={c.id} className="flex flex-wrap items-center justify-between gap-4 rounded-lg border p-4"><div><h3 className="font-medium">{c.name}</h3><p className="text-sm text-muted-foreground">{c.status} · {c.channel} · Kuyruğa alınan {c.queuedCount} / {c.totalRecipients}{c.scheduledAt ? ` · ${new Date(c.scheduledAt).toLocaleString("tr-TR")}` : ""}</p><progress aria-label={`${c.name} ilerleme`} className="mt-2 w-56" max={Math.max(1, c.totalRecipients)} value={c.queuedCount} /></div><div className="flex gap-2"><Button variant="outline" disabled={busy} onClick={() => void run(async () => setDetail(await notificationApi<Campaign>(`${endpoint}/${c.id}`, token)))}>Detay</Button>{c.status === "Draft" && <Button disabled={busy} onClick={() => { if (confirm(`${c.name} kampanyası başlatılsın mı?`)) void run(async () => { await notificationApi(`${endpoint}/${c.id}/start`, token, "POST"); await load() }) }}>Başlat</Button>}{!["Completed", "Cancelled"].includes(c.status) && <Button variant="outline" disabled={busy} onClick={() => { if (confirm("Kalan gönderimler iptal edilsin mi?")) void run(async () => { await notificationApi(`${endpoint}/${c.id}/cancel`, token, "POST"); await load() }) }}>İptal</Button>}{!["Queued", "Expanding", "Sending"].includes(c.status) && <Button variant="destructive" disabled={busy} onClick={() => { if (confirm("Kampanya silinsin mi?")) void run(async () => { await notificationApi(`${endpoint}/${c.id}`, token, "DELETE"); await load() }) }}>Sil</Button>}</div></article>)}{result && <Pagination page={page} totalPages={result.totalPages} onPage={setPage} />}</div>
    {detail && <aside className="space-y-2 rounded-lg border p-4"><h3 className="font-semibold">{detail.name}</h3><p className="text-sm">Kampanya ID: {detail.id}</p><p>Durum: {detail.status}</p><p>Şablon: {detail.templateCode}</p>{detail.lastError && <p className="text-destructive">{detail.lastError}</p>}<Button variant="outline" onClick={() => setDetail(undefined)}>Kapat</Button></aside>}
  </div>
}

