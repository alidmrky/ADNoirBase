"use client"
import { useCallback, useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { notificationApi, errorText, type Template, type Page } from "@/lib/notifications"
import { Field, Choice, Feedback, Pagination } from "./fields"

const endpoint = "admin/notifications/templates"
const empty: Template = { id: "", code: "", name: "", description: "", channel: "Email", category: "Transactional", isSystem: false, isLayout: false, isActive: true, layoutCode: "", subject: "", htmlBody: "", textBody: "", smsBody: "", variables: [], logPolicy: "MetadataOnly", retentionDays: 90 }

export function TemplatesPanel({ token }: { token: string }) {
  const [list, setList] = useState<Page<Template>>()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState("")
  const [selected, setSelected] = useState<Template>(empty)
  const [revision, setRevision] = useState(0)
  const [error, setError] = useState("")
  const load = useCallback(async () => {
    try { setList(await notificationApi<Page<Template>>(`${endpoint}?page=${page}&search=${encodeURIComponent(search)}`, token)); setError("") }
    catch (e) { setError(errorText(e)) }
  }, [token, page, search])
  useEffect(() => { const timer = setTimeout(() => void load(), 250); return () => clearTimeout(timer) }, [load])
  async function select(id: string) {
    try { setSelected(await notificationApi<Template>(`${endpoint}/${id}`, token)); setRevision(x => x + 1) }
    catch (e) { setError(errorText(e)) }
  }
  return <div className="grid gap-6 lg:grid-cols-[minmax(240px,1fr)_2fr]">
    <section className="space-y-3">
      <div className="flex gap-2"><Input aria-label="Şablon ara" placeholder="Şablon ara…" value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /><Button onClick={() => { setSelected(empty); setRevision(x => x + 1) }}>Yeni</Button></div>
      <Feedback error={error} />
      {!list ? <p>Yükleniyor…</p> : list.items.length === 0 ? <p className="text-muted-foreground">Şablon bulunamadı.</p> : list.items.map(t => <button key={t.id} onClick={() => void select(t.id)} className={`block w-full rounded-lg border p-3 text-left hover:bg-muted ${selected.id === t.id ? "border-primary bg-muted" : ""}`}><span className="block font-medium">{t.name}</span><span className="text-xs text-muted-foreground">{t.code} · {t.channel}{t.isLayout ? " · Layout" : ""}{!t.isActive ? " · Pasif" : ""}</span></button>)}
      {list && <Pagination page={page} totalPages={list.totalPages} onPage={setPage} />}
    </section>
    <TemplateEditor key={`${selected.id}-${revision}`} initial={selected} token={token} onSaved={() => void load()} />
  </div>
}

function TemplateEditor({ initial, token, onSaved }: { initial: Template; token: string; onSaved: () => void }) {
  const [draft, setDraft] = useState(initial)
  const [samples, setSamples] = useState(JSON.stringify(Object.fromEntries(initial.variables.map(v => [v.name, v.sampleValue ?? ""])), null, 2))
  const [preview, setPreview] = useState<{ subject?: string; htmlBody?: string; textBody?: string; smsBody?: string }>()
  const [error, setError] = useState("")
  const [message, setMessage] = useState("")
  const [busy, setBusy] = useState(false)
  function update<K extends keyof Template>(key: K, value: Template[K]) { setDraft(d => ({ ...d, [key]: value })); setPreview(undefined) }
  function sampleValues(): Record<string, string> {
    const value = JSON.parse(samples)
    if (!value || Array.isArray(value) || typeof value !== "object" || Object.values(value).some(v => typeof v !== "string")) throw new Error('Örnek veriler {"full_name":"Ayşe Yılmaz"} biçiminde olmalı.')
    return value
  }
  async function run(action: () => Promise<void>) {
    setBusy(true); setError(""); setMessage("")
    try { await action() } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }
  async function save() {
    const variables = Object.entries(sampleValues()).map(([name, sampleValue]) => ({ name, sampleValue, description: draft.variables.find(v => v.name === name)?.description ?? null }))
    const payload = { ...draft, variables, layoutCode: draft.layoutCode || null }
    if (draft.id) await notificationApi(`${endpoint}/${draft.id}`, token, "PUT", payload)
    else setDraft(await notificationApi<Template>(endpoint, token, "POST", payload))
    setMessage("Şablon kaydedildi."); onSaved()
  }
  return <section className="space-y-4 rounded-xl border p-5">
    <h2 className="text-lg font-semibold">{draft.id ? "Şablonu düzenle" : "Yeni şablon"}</h2>
    <Feedback error={error} message={message} />
    <form className="space-y-4" onSubmit={e => { e.preventDefault(); void run(save) }}>
      <div className="grid gap-3 sm:grid-cols-2"><Field label="Kod"><Input required value={draft.code} disabled={!!draft.id} onChange={e => update("code", e.target.value)} /></Field><Field label="Ad"><Input required value={draft.name} onChange={e => update("name", e.target.value)} /></Field></div>
      <Field label="Açıklama"><Input value={draft.description ?? ""} onChange={e => update("description", e.target.value)} /></Field>
      <div className="grid gap-3 sm:grid-cols-2"><Field label="Kanal"><Choice value={draft.channel} disabled={!!draft.id} onChange={e => update("channel", e.target.value as Template["channel"])}><option>Email</option><option>Sms</option><option>InApp</option></Choice></Field><Field label="Kategori"><Choice value={draft.category} onChange={e => update("category", e.target.value as Template["category"])}><option value="Transactional">İşlemsel</option><option value="Marketing">Pazarlama</option></Choice></Field></div>
      <div className="flex gap-5 text-sm"><label><input type="checkbox" checked={draft.isActive} onChange={e => update("isActive", e.target.checked)} /> Aktif</label><label><input type="checkbox" disabled={!!draft.id} checked={draft.isLayout} onChange={e => update("isLayout", e.target.checked)} /> Layout şablonu</label></div>
      {draft.channel === "Email" && !draft.isLayout && <Field label="Layout kodu (isteğe bağlı)"><Input value={draft.layoutCode ?? ""} onChange={e => update("layoutCode", e.target.value)} placeholder="layout.default" /></Field>}
      {draft.channel !== "Sms" && <Field label="Konu / başlık"><Input value={draft.subject ?? ""} onChange={e => update("subject", e.target.value)} /></Field>}
      {draft.channel === "Email" && <Field label="HTML içerik"><Textarea className="min-h-48 font-mono text-sm" value={draft.htmlBody ?? ""} onChange={e => update("htmlBody", e.target.value)} /></Field>}
      <Field label={draft.channel === "Sms" ? "SMS metni" : "Düz metin"}><Textarea value={(draft.channel === "Sms" ? draft.smsBody : draft.textBody) ?? ""} onChange={e => update(draft.channel === "Sms" ? "smsBody" : "textBody", e.target.value)} /></Field>
      <Field label="Değişkenler ve önizleme değerleri (JSON)"><Textarea className="font-mono" value={samples} onChange={e => setSamples(e.target.value)} /></Field>
      <div className="grid gap-3 sm:grid-cols-2"><Field label="Kayıt politikası"><Choice value={draft.logPolicy} onChange={e => update("logPolicy", e.target.value)}><option value="MetadataOnly">Yalnızca üst bilgiler</option><option value="Full">Tam içerik</option><option value="None">Kayıt tutma</option></Choice></Field><Field label="Saklama süresi (gün)"><Input type="number" min={1} max={3650} value={draft.retentionDays} onChange={e => update("retentionDays", Number(e.target.value))} /></Field></div>
      <div className="flex flex-wrap gap-2"><Button type="submit" disabled={busy}>Kaydet</Button><Button type="button" variant="outline" disabled={busy} onClick={() => void run(async () => setPreview(await notificationApi(`${endpoint}/preview`, token, "POST", { ...draft, layoutCode: draft.layoutCode || null, sampleValues: sampleValues() })))}>Önizle</Button>
        <Button type="button" variant="outline" disabled={busy || !draft.id || draft.isLayout} onClick={() => void run(async () => { await notificationApi(`${endpoint}/${draft.id}/test`, token, "POST", { sampleValues: sampleValues() }); setMessage("Kayıtlı şablon kendi adresinize gönderilmek üzere kuyruğa alındı.") })}>Kendime test gönder</Button>
        <Button type="button" variant="destructive" disabled={busy || !draft.id || draft.isSystem} onClick={() => { if (confirm("Bu şablon silinsin mi?")) void run(async () => { await notificationApi(`${endpoint}/${draft.id}`, token, "DELETE"); setDraft(empty); onSaved(); setMessage("Şablon silindi.") }) }}>Sil</Button>
      </div>
    </form>
    {preview && <div className="space-y-2 border-t pt-4"><h3 className="font-medium">{preview.subject || "Önizleme"}</h3>{preview.htmlBody && <iframe title="Şablon önizlemesi" sandbox="" referrerPolicy="no-referrer" className="h-80 w-full rounded border bg-white" srcDoc={'<meta http-equiv="Content-Security-Policy" content="default-src \'none\'; style-src \'unsafe-inline\'; img-src data:">' + preview.htmlBody} />}<pre className="whitespace-pre-wrap text-sm">{preview.textBody || preview.smsBody}</pre></div>}
  </section>
}
