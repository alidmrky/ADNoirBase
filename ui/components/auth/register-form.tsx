"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { apiClient } from "@/lib/api-client"

const registerSchema = z
  .object({
    firstName: z.string().min(1, "First name is required").max(100),
    lastName: z.string().min(1, "Last name is required").max(100),
    email: z.string().email("Invalid email address").max(256),
    password: z.string().min(8, "Password must be at least 8 characters").max(128),
    confirmPassword: z.string(),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  })

type RegisterInput = z.infer<typeof registerSchema>
type FieldErrors = Partial<Record<keyof RegisterInput, string>>

export function RegisterForm() {
  const router = useRouter()
  const [errors, setErrors] = useState<FieldErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setServerError(null)

    const formData = new FormData(e.currentTarget)
    const raw = Object.fromEntries(formData)

    const parsed = registerSchema.safeParse(raw)
    if (!parsed.success) {
      const fieldErrors: FieldErrors = {}
      parsed.error.issues.forEach((err) => {
        const key = err.path[0] as keyof RegisterInput
        fieldErrors[key] = err.message
      })
      setErrors(fieldErrors)
      return
    }

    setErrors({})
    setLoading(true)

    try {
      await apiClient.post("/api/auth/register", parsed.data)
      router.push("/login?registered=true")
    } catch {
      setServerError("Registration failed. Please try again.")
    } finally {
      setLoading(false)
    }
  }

  const Field = ({
    id,
    label,
    type = "text",
    placeholder,
    autoComplete,
  }: {
    id: keyof RegisterInput
    label: string
    type?: string
    placeholder?: string
    autoComplete?: string
  }) => (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        name={id}
        type={type}
        placeholder={placeholder}
        autoComplete={autoComplete}
        disabled={loading}
      />
      {errors[id] && <p className="text-sm text-destructive">{errors[id]}</p>}
    </div>
  )

  return (
    <Card>
      <CardContent className="pt-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Field id="firstName" label="First Name" placeholder="John" />
            <Field id="lastName" label="Last Name" placeholder="Doe" />
          </div>
          <Field id="email" label="Email" type="email" placeholder="you@example.com" autoComplete="email" />
          <Field id="password" label="Password" type="password" placeholder="••••••••" autoComplete="new-password" />
          <Field id="confirmPassword" label="Confirm Password" type="password" placeholder="••••••••" />

          {serverError && <p className="text-sm text-destructive">{serverError}</p>}

          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? "Creating account..." : "Create account"}
          </Button>

          <p className="text-center text-sm text-muted-foreground">
            Already have an account?{" "}
            <a href="/login" className="underline hover:text-foreground">
              Sign in
            </a>
          </p>
        </form>
      </CardContent>
    </Card>
  )
}
