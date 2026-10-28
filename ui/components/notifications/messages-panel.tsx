"use client"
import { useCallback, useEffect, useState } from "react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { notificationApi, errorText, type Page, type Message } from "@/lib/notifications"
import { Choice, Field, Feedback, Pagination } from "./fields"

export function MessagesPanel({ token }: { token: string }) {
  const [result, setResult] = useState<Page<Message>>()
  const [page, setPage] = useState(1)
  const [channel, setChannel] = useState("")
  const [status, setStatus] = useState("")
  const [recipient, setRecipient] = useState("")
  const [campaignId, setCampaignId] = useState("")
  const [error, setError] = useState("")
  const load = useCallback(async () => {
    const query = new URLSearchParams({ page: String(page), ...(channel ? { channel } : {}), ...(status ? { status } : {}), ...(recipient ? { recipient } : {}), ...(campaignId ? { campaignId } : {}) })
    try { setResult(await notificationApi<Page<Message>>(`admin/notifications/messages?${query}`, token)); setError("") } catch (e) { setError(errorText(e)) }
  }, [token, page, channel, status, recipient, campaignId])
  useEffect(() => { const timer = setTimeout(() => void load(), 300); const interval = setInterval(() => void load(), 15000); return () => { clearTimeout(timer); clearInterval(interval) } }, [load])
  return <section className="space-y-4">
    <div className="grid gap-3 sm:grid-cols-4"><Field label="Kanal"><Choice value={channel} onChange={e => { setChannel(e.target.value); setPage(1) }}><option value="">Tümü</option><option>Email</option><option>Sms</option><option>InApp</option></Choice></Field><Field label="Durum"><Choice value={status} onChange={e => { setStatus(e.target.value); setPage(1) }}><option value="">Tümü</option>{["Pending", "Processing", "Sent", "Failed", "Cancelled", "Skipped"].map(s => <option key={s}>{s}</option>)}</Choice></Field><Field label="Alıcı"><Input value={recipient} onChange={e => { setRecipient(e.target.value); setPage(1) }} /></Field><Field label="Kampanya ID"><Input value={campaignId} onChange={e => { setCampaignId(e.target.value); setPage(1) }} /></Field></div>
    <Feedback error={error} /><Button variant="outline" onClick={() => void load()}>Yenile</Button>
    <div className="overflow-x-auto rounded-lg border"><table className="w-full text-left text-sm"><thead className="bg-muted"><tr>{["Tarih", "Kanal", "Alıcı / konu", "Durum", "Deneme", "Hata"].map(h => <th key={h} className="p-3">{h}</th>)}</tr></thead><tbody>{result?.items.map(m => <tr key={m.id} className="border-t"><td className="p-3 whitespace-nowrap">{new Date(m.createdAt).toLocaleString("tr-TR")}</td><td className="p-3">{m.channel}</td><td className="p-3"><p>{m.to.map(r => r.address).join(", ")}</p><p className="text-muted-foreground">{m.subject}</p>{m.cc.length > 0 && <p>Cc: {m.cc.map(r => r.address).join(", ")}</p>}{m.bcc.length > 0 && <p>Bcc: {m.bcc.map(r => r.address).join(", ")}</p>}</td><td className="p-3">{m.status}</td><td className="p-3">{m.attempts}</td><td className="max-w-xs break-words p-3 text-destructive">{m.lastError || "—"}</td></tr>)}</tbody></table><p className="p-3 text-sm text-muted-foreground">{result ? `${result.totalCount} kayıt` : "Yükleniyor…"}</p></div>
    {result && <Pagination page={page} totalPages={result.totalPages} onPage={setPage} />}
  </section>
}
