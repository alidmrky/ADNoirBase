import type { Metadata } from "next"
import { auth } from "@/lib/auth"
import { apiClient } from "@/lib/api-client"
import type { PagedList } from "@/lib/api-client"

export const metadata: Metadata = {
  title: "Users",
}

interface UserListDto {
  id: string
  email: string
  fullName: string
  status: string
  createdAt: string
  lastLoginAt: string | null
}

async function getUsers(token: string, page = 1, pageSize = 20) {
  try {
    const result = await apiClient.get<PagedList<UserListDto>>(
      `/api/admin/users?page=${page}&pageSize=${pageSize}`,
      { token, cache: "no-store" }
    )
    return result.data
  } catch {
    return null
  }
}

export default async function UsersPage() {
  const session = await auth()
  const users = await getUsers(session?.accessToken ?? "", 1, 20)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Users</h1>
        <p className="text-muted-foreground">Manage user accounts</p>
      </div>

      <div className="rounded-md border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="h-12 px-4 text-left font-medium">Name</th>
              <th className="h-12 px-4 text-left font-medium">Email</th>
              <th className="h-12 px-4 text-left font-medium">Status</th>
              <th className="h-12 px-4 text-left font-medium">Created</th>
            </tr>
          </thead>
          <tbody>
            {users?.items.map((user) => (
              <tr key={user.id} className="border-b hover:bg-muted/30 transition-colors">
                <td className="px-4 py-3 font-medium">{user.fullName}</td>
                <td className="px-4 py-3 text-muted-foreground">{user.email}</td>
                <td className="px-4 py-3">
                  <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                    user.status === "Active"
                      ? "bg-green-100 text-green-700 dark:bg-green-900/20 dark:text-green-400"
                      : "bg-red-100 text-red-700 dark:bg-red-900/20 dark:text-red-400"
                  }`}>
                    {user.status}
                  </span>
                </td>
                <td className="px-4 py-3 text-muted-foreground">
                  {new Date(user.createdAt).toLocaleDateString()}
                </td>
              </tr>
            ))}
            {!users?.items.length && (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">
                  No users found
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {users && (
        <p className="text-sm text-muted-foreground">
          Showing {users.items.length} of {users.totalCount} users
        </p>
      )}
    </div>
  )
}
