"use client"
import { useSearchParams } from "next/navigation"
import { useState } from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { notificationApi, errorText } from "@/lib/notifications"
import { Feedback } from "./fields"
export function UnsubscribeForm() {
  const token = useSearchParams().get("token")
  const [done, setDone] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState("")
  async function unsubscribe() {
    setBusy(true); setError("")
    try { await notificationApi("notifications/unsubscribe", undefined, "POST", { token }); setDone(true) } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }
  return <main className="mx-auto mt-24 max-w-lg space-y-5 rounded-xl border p-8"><h1 className="text-2xl font-semibold">Abonelikten çık</h1><Feedback error={error} />{done ? <p role="status">Pazarlama bildirimleri kapatıldı.</p> : token ? <><p>Pazarlama bildirimlerini almak istemiyorsanız aşağıdaki düğmeyle tercihinizi onaylayın.</p><Button disabled={busy} onClick={() => void unsubscribe()}>Abonelikten çık</Button></> : <p>Bağlantı geçersiz: token bulunamadı.</p>}<Link className="block text-sm underline" href="/login">Giriş sayfasına dön</Link></main>
}
