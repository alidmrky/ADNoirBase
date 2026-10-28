"use client"
import { useEffect, useState } from "react"
import Link from "next/link"
import { Bell } from "lucide-react"
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr"
import { apiUrl, notificationApi } from "@/lib/notifications"

export function NotificationBell({ token }: { token: string }) {
  const [count, setCount] = useState(0)
  const [connected, setConnected] = useState(false)
  useEffect(() => {
    let disposed = false
    const refresh = () => notificationApi<number>("notifications/unread-count", token).then(n => { if (!disposed) setCount(n) }).catch(() => {})
    const connection = new HubConnectionBuilder().withUrl(apiUrl + "/hubs/notifications", { accessTokenFactory: () => token }).withAutomaticReconnect().configureLogging(LogLevel.None).build()
    connection.on("NotificationReceived", () => { void refresh(); window.dispatchEvent(new Event("notifications-received")) })
    connection.onreconnecting(() => setConnected(false))
    connection.onreconnected(() => { setConnected(true); void refresh() })
    connection.onclose(() => { if (!disposed) setConnected(false) })
    void connection.start().then(() => { if (!disposed) { setConnected(true); void refresh() } }).catch(() => {})
    void refresh()
    const interval = setInterval(() => void refresh(), 30000)
    window.addEventListener("notifications-read", refresh)
    return () => { disposed = true; clearInterval(interval); window.removeEventListener("notifications-read", refresh); void connection.stop() }
  }, [token])
  return <Link href="/notifications" className="flex items-center gap-2 rounded-md border px-3 py-2 text-sm" title={connected ? "Canlı bağlantı açık" : "Bildirimler düzenli olarak yenileniyor"}><Bell className="h-4 w-4" /><span>Bildirimler</span><span aria-label={`${count} okunmamış bildirim`} className="ml-auto rounded-full bg-primary px-2 text-primary-foreground">{count}</span></Link>
}
