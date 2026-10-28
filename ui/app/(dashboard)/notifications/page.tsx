import { auth } from "@/lib/auth"
import { redirect } from "next/navigation"
import { Inbox } from "@/components/notifications/inbox"
export const instant = false
export default async function NotificationsPage() {
  const session = await auth()
  if (!session) redirect("/login")
  return <Inbox token={session.accessToken} />
}
