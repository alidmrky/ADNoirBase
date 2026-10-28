"use client"
import { useCallback, useEffect, useState } from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { notificationApi, errorText, type Page, type InAppItem } from "@/lib/notifications"
import { Feedback, Pagination } from "./fields"

export function Inbox({ token }: { token: string }) {
  const [result, setResult] = useState<Page<InAppItem>>()
  const [page, setPage] = useState(1)
  const [unread, setUnread] = useState(false)
  const [error, setError] = useState("")
  const load = useCallback(async () => {
    try { setResult(await notificationApi<Page<InAppItem>>(`notifications?page=${page}&unreadOnly=${unread}`, token)); setError("") } catch (e) { setError(errorText(e)) }
  }, [token, page, unread])
  useEffect(() => { const timer = setTimeout(() => void load(), 0); const interval = setInterval(() => void load(), 30000); const update = () => void load(); window.addEventListener("notifications-received", update); return () => { clearTimeout(timer); clearInterval(interval); window.removeEventListener("notifications-received", update) } }, [load])
  async function read(id?: string) {
    try { await notificationApi(id ? `notifications/${id}/read` : "notifications/read-all", token, "POST"); window.dispatchEvent(new Event("notifications-read")); await load() } catch (e) { setError(errorText(e)) }
  }
  return <section className="mx-auto max-w-3xl space-y-5"><h1 className="text-2xl font-semibold">Bildirimlerim</h1><div className="flex items-center justify-between"><label className="text-sm"><input type="checkbox" checked={unread} onChange={e => { setUnread(e.target.checked); setPage(1) }} /> Yalnızca okunmamışlar</label><Button variant="outline" onClick={() => void read()}>Tümünü okundu işaretle</Button></div><Feedback error={error} />{!result && <p>Yükleniyor…</p>}{result?.items.length === 0 && <p className="rounded-lg border p-8 text-center text-muted-foreground">Henüz bildirim yok.</p>}{result?.items.map(item => <article key={item.id} className={`space-y-2 rounded-lg border p-4 ${item.isRead ? "" : "border-primary/50 bg-muted/40"}`}><h2 className="font-medium">{item.title}</h2><p className="whitespace-pre-wrap text-sm">{item.body}</p><p className="text-xs text-muted-foreground">{new Date(item.createdAt).toLocaleString("tr-TR")}</p><div className="flex gap-3">{!item.isRead && <Button size="sm" variant="outline" onClick={() => void read(item.id)}>Okundu işaretle</Button>}{item.link?.startsWith("/") && !item.link.startsWith("//") && !item.link.includes("\\") && <Link className="text-sm underline" href={item.link} onClick={() => void read(item.id)}>Aç</Link>}</div></article>)}{result && <Pagination page={page} totalPages={result.totalPages} onPage={setPage} />}</section>
}
