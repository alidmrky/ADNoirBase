"use client"
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs"
import { TemplatesPanel } from "./templates-panel"
import { SendPanel } from "./send-panel"
import { CampaignsPanel } from "./campaigns-panel"
import { MessagesPanel } from "./messages-panel"

export function NotificationAdmin({ token }: { token: string }) {
  return <div className="mx-auto max-w-6xl space-y-6">
    <header><h1 className="text-2xl font-semibold">Bildirim yönetimi</h1><p className="mt-1 text-muted-foreground">Şablonları hazırlayın, gönderimleri planlayın ve sonuçları izleyin.</p></header>
    <Tabs defaultValue="templates">
      <TabsList className="mb-5"><TabsTrigger value="templates">Şablonlar</TabsTrigger><TabsTrigger value="send">Manuel gönderim</TabsTrigger><TabsTrigger value="campaigns">Kampanyalar</TabsTrigger><TabsTrigger value="messages">Gönderim kayıtları</TabsTrigger></TabsList>
      <TabsContent value="templates"><TemplatesPanel token={token} /></TabsContent>
      <TabsContent value="send"><SendPanel token={token} /></TabsContent>
      <TabsContent value="campaigns"><CampaignsPanel token={token} /></TabsContent>
      <TabsContent value="messages"><MessagesPanel token={token} /></TabsContent>
    </Tabs>
  </div>
}

