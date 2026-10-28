"use client"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"
import { notificationApi, errorText, type Preferences } from "@/lib/notifications"
import { Feedback } from "./fields"
export function PreferencesPanel({ token }: { token: string }) {
  const [value, setValue] = useState<Preferences>()
  const [error, setError] = useState("")
  const [message, setMessage] = useState("")
  const [busy, setBusy] = useState(false)
  useEffect(() => { notificationApi<Preferences>("notifications/preferences", token).then(setValue).catch(e => setError(errorText(e))) }, [token])
  async function save() {
    setBusy(true); setError(""); setMessage("")
    try { await notificationApi("notifications/preferences", token, "PUT", value); setMessage("Tercihleriniz kaydedildi.") } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }
  return <section className="mx-auto max-w-xl space-y-5"><h1 className="text-2xl font-semibold">Bildirim tercihleri</h1><p className="text-muted-foreground">Pazarlama bildirimlerini kanal bazında yönetin. Parola değişikliği gibi hesap bildirimleri gönderilmeye devam eder.</p><Feedback error={error} message={message} />{value ? <form className="space-y-5 rounded-xl border p-5" onSubmit={e => { e.preventDefault(); void save() }}>{([["emailMarketing", "E-posta"], ["smsMarketing", "SMS"], ["inAppMarketing", "Uygulama içi"]] as const).map(([key, label]) => <label key={key} className="flex items-center justify-between">{label}<Switch checked={value[key]} onCheckedChange={checked => setValue(v => v ? { ...v, [key]: checked } : v)} /></label>)}<Button type="submit" disabled={busy}>Kaydet</Button></form> : <p>Yükleniyor…</p>}</section>
}
