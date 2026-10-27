import { redirect } from "next/navigation"
import { auth } from "@/lib/auth"
import { DashboardNav } from "@/components/layout/dashboard-nav"

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const session = await auth()

  if (!session) {
    redirect("/login")
  }

  return (
    <div className="flex min-h-screen">
      <DashboardNav user={session.user} />
      <main className="flex-1 p-6 overflow-auto">
        {children}
      </main>
    </div>
  )
}
