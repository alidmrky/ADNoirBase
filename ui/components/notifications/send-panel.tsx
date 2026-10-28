"use client"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { notificationApi, errorText, type Template, type Page, type Channel, type Attachment } from "@/lib/notifications"
import { Field, Choice, Feedback } from "./fields"

export function SendPanel({ token }: { token: string }) {
  const [templates, setTemplates] = useState<Template[]>([])
  const [channel, setChannel] = useState<Channel>("Email")
  const [templateCode, setTemplateCode] = useState("")
  const [category, setCategory] = useState("Transactional")
  const [to, setTo] = useState("")
  const [cc, setCc] = useState("")
  const [bcc, setBcc] = useState("")
  const [subject, setSubject] = useState("")
  const [body, setBody] = useState("")
  const [model, setModel] = useState("{}")
  const [attachments, setAttachments] = useState<Attachment[]>([])
  const [error, setError] = useState("")
  const [message, setMessage] = useState("")
  const [busy, setBusy] = useState(false)
  useEffect(() => { notificationApi<Page<Template>>("admin/notifications/templates?pageSize=100&isActive=true", token).then(p => setTemplates(p.items.filter(t => !t.isLayout))).catch(e => setError(errorText(e))) }, [token])
  function recipients(value: string) { return value.split(/[,;\n]/).map(s => s.trim()).filter(Boolean).map(address => channel === "InApp" ? { address, userId: address } : { address }) }
  async function upload(files: FileList | null) {
    if (!files?.length) return
    setError(""); setBusy(true)
    try {
      if (attachments.length + files.length > 10) throw new Error("En fazla 10 ek yükleyebilirsiniz.")
      for (const file of Array.from(files)) {
        const data = new FormData(); data.set("file", file)
        const attachment = await notificationApi<Attachment>("admin/notifications/attachments", token, "POST", data)
        setAttachments(previous => [...previous, attachment])
      }
    } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }
  async function send() {
    setBusy(true); setError(""); setMessage("")
    try {
      const values = JSON.parse(model)
      if (!values || Array.isArray(values) || typeof values !== "object" || Object.values(values).some(v => typeof v !== "string")) throw new Error("Değişken değerleri metin olmalı.")
      const result = await notificationApi<{ queuedMessageIds: string[]; skippedRecipients: string[] }>("admin/notifications/send", token, "POST", {
        channel, category, to: recipients(to), cc: channel === "Email" ? recipients(cc) : [], bcc: channel === "Email" ? recipients(bcc) : [],
        templateCode: templateCode || null, subject, textBody: body, smsBody: body, model: values,
        attachmentIds: channel === "Email" ? attachments.map(a => a.fileId) : [],
      })
      setMessage(`${result.queuedMessageIds.length} gönderim kuyruğa alındı. Tercih nedeniyle atlanan: ${result.skippedRecipients.length}.`)
    } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }
  return <form className="max-w-3xl space-y-4 rounded-xl border p-5" onSubmit={e => { e.preventDefault(); if (confirm("Bildirim belirtilen alıcılara gönderilsin mi?")) void send() }}>
    <Feedback error={error} message={message} />
    <div className="grid gap-3 sm:grid-cols-2"><Field label="Kanal"><Choice value={channel} onChange={e => { setChannel(e.target.value as Channel); setTemplateCode("") }}><option>Email</option><option>Sms</option><option>InApp</option></Choice></Field><Field label="Kategori"><Choice value={category} onChange={e => setCategory(e.target.value)}><option value="Transactional">İşlemsel</option><option value="Marketing">Pazarlama</option></Choice></Field></div>
    <Field label={channel === "InApp" ? "Alıcı kullanıcı ID (tek kullanıcı)" : channel === "Sms" ? "Telefon (+90…, tek alıcı)" : "Alıcılar (virgülle ayırın)"}><Textarea required value={to} onChange={e => setTo(e.target.value)} /></Field>
    {channel === "Email" && <div className="grid gap-3 sm:grid-cols-2"><Field label="Cc"><Input value={cc} onChange={e => setCc(e.target.value)} /></Field><Field label="Bcc"><Input value={bcc} onChange={e => setBcc(e.target.value)} /></Field></div>}
    <Field label="Şablon"><Choice value={templateCode} onChange={e => { setTemplateCode(e.target.value); const t = templates.find(t => t.code === e.target.value); if (t) setCategory(t.category) }}><option value="">Şablonsuz gönderim</option>{templates.filter(t => t.channel === channel).map(t => <option key={t.id} value={t.code}>{t.name}</option>)}</Choice></Field>
    {!templateCode && <><Field label="Konu / başlık"><Input required={channel !== "Sms"} value={subject} onChange={e => setSubject(e.target.value)} /></Field><Field label="Mesaj"><Textarea required className="min-h-32" value={body} onChange={e => setBody(e.target.value)} /></Field></>}
    {templateCode && <Field label="Şablon değişkenleri (JSON)"><Textarea value={model} onChange={e => setModel(e.target.value)} /></Field>}
    {channel === "Email" && <Field label="Ekler (en fazla 10 dosya, dosya başına 10 MB)"><Input type="file" multiple disabled={busy} accept=".pdf,.docx,.xlsx,.csv,.txt,.png,.jpg,.jpeg,.gif,.webp" onChange={e => void upload(e.target.files)} />{attachments.map(a => <div key={a.fileId} className="flex items-center justify-between text-sm"><span>{a.fileName} ({Math.ceil(a.length / 1024)} KB)</span><Button type="button" variant="ghost" onClick={() => setAttachments(items => items.filter(x => x.fileId !== a.fileId))}>Çıkar</Button></div>)}</Field>}
    <Button type="submit" disabled={busy}>{busy ? "İşleniyor…" : "Gönder"}</Button>
  </form>
}
