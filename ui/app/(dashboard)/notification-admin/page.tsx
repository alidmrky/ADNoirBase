import { auth } from "@/lib/auth"
import { redirect } from "next/navigation"
import { NotificationAdmin } from "@/components/notifications/admin"
export const instant = false
export default async function NotificationAdminPage() {
  const session = await auth()
  if (!session) redirect("/login")
  if (!session.user.roles.includes("ADMIN")) redirect("/dashboard")
  return <NotificationAdmin token={session.accessToken} />
}

