import type { Metadata } from "next"
import { notFound } from "next/navigation"
import { auth } from "@/lib/auth"
import { apiClient } from "@/lib/api-client"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

interface UserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  status: string
  createdAt: string
  lastLoginAt: string | null
  roles: string[]
}

// Reads the session token and calls the API on every request.
export const instant = false

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>
}): Promise<Metadata> {
  const { id } = await params
  return { title: `User ${id}` }
}

async function getUser(token: string, id: string) {
  try {
    const result = await apiClient.get<UserDto>(`/api/admin/users/${id}`, {
      token,
      cache: "no-store",
    })
    return result.data
  } catch {
    return null
  }
}

export default async function UserDetailPage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = await params
  const session = await auth()
  const user = await getUser(session?.accessToken ?? "", id)

  if (!user) notFound()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{user.fullName}</h1>
        <p className="text-muted-foreground">{user.email}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Profile</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex justify-between">
              <span className="text-muted-foreground">First Name</span>
              <span>{user.firstName}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Last Name</span>
              <span>{user.lastName}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Email</span>
              <span>{user.email}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Status</span>
              <Badge variant={user.status === "Active" ? "default" : "destructive"}>
                {user.status}
              </Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Activity</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Created</span>
              <span>{new Date(user.createdAt).toLocaleDateString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Last Login</span>
              <span>
                {user.lastLoginAt
                  ? new Date(user.lastLoginAt).toLocaleDateString()
                  : "Never"}
              </span>
            </div>
            <div className="flex justify-between items-start">
              <span className="text-muted-foreground">Roles</span>
              <div className="flex gap-1 flex-wrap justify-end">
                {user.roles.map((role) => (
                  <Badge key={role} variant="secondary">{role}</Badge>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
