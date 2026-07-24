import { useEffect, useRef, useState } from 'react'
import { Loader2, Plus, SendHorizontal } from 'lucide-react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { baseApi } from '@/features/api/baseApi'
import { selectDefaultProvider } from '@/features/settings/uiSlice'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import { Timing } from '@/components/Timing'
import { useChatStream } from './useChatStream'
import { useLocalModelsQuery } from './modelsApi'
import { useAgentMutation } from './agentApi'
import { useLazyGetConversationQuery, useListConversationsQuery } from './conversationsApi'

interface Message {
  role: 'user' | 'assistant'
  content: string
  totalMs?: number       // frontend-মাপা (network সহ)
  llmMs?: number         // backend-মাপা (agent mode-এ; stream-এ পাওয়া যায় না)
  toolsUsed?: string[]
}

import { PROVIDERS } from '@/lib/providers'

export function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const [provider, setProvider] = useState(useAppSelector(selectDefaultProvider))
  const [model, setModel] = useState<string | undefined>(undefined)
  const [conversationId, setConversationId] = useState<string | null>(null)
  const [agentMode, setAgentMode] = useState(false)
  const { stream, isStreaming } = useChatStream()
  const [agent, { isLoading: isAgentRunning }] = useAgentMutation()
  const busy = isStreaming || isAgentRunning
  const dispatch = useAppDispatch()
  const bottomRef = useRef<HTMLDivElement>(null)

  // ollama select করলেই installed model তালিকা আনি; অন্য provider-এ skip (config-এর default model)
  const isLocal = provider === 'ollama'
  const { data: localModels } = useLocalModelsQuery(undefined, { skip: !isLocal })

  // ollama-তে model বাছা না থাকলে প্রথমটা default; provider বদলালে model reset
  useEffect(() => {
    if (isLocal && localModels?.length) setModel((m) => (m && localModels.includes(m) ? m : localModels[0]))
    else setModel(undefined)
  }, [isLocal, localModels])

  const { data: conversations } = useListConversationsQuery()
  const [fetchConversation] = useLazyGetConversationQuery()

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const newChat = () => {
    if (busy) return
    setConversationId(null)
    setMessages([])
  }

  const openConversation = async (id: string) => {
    if (busy) return
    setConversationId(id)
    try {
      const detail = await fetchConversation(id).unwrap()
      setMessages(
        detail.messages
          .filter((m) => m.role !== 'system')
          .map((m) => ({ role: m.role as 'user' | 'assistant', content: m.content })),
      )
    } catch {
      toast.error('কথোপকথন লোড করা যায়নি')
    }
  }

  const send = async () => {
    const text = input.trim()
    if (!text || busy) return

    // নতুন চ্যাট হলে এখন id বানাই — এই id backend-এ memory persist করে + sidebar-এ আসে
    let convId = conversationId
    if (!convId) {
      convId = crypto.randomUUID()
      setConversationId(convId)
    }

    setInput('')
    setMessages((m) => [...m, { role: 'user', content: text }, { role: 'assistant', content: '' }])

    const started = performance.now()
    const finish = (patch: Partial<Message>) =>
      setMessages((m) => {
        const copy = [...m]
        copy[copy.length - 1] = { ...copy[copy.length - 1], totalMs: performance.now() - started, ...patch }
        return copy
      })

    try {
      if (agentMode) {
        // agent stream করে না — tool চালানোর পর আরেক turn লাগে, তাই এক-শটে পুরো উত্তর।
        // ⚠️ conversationId পাঠাই না: /agent এখনো memory সমর্থন করে না।
        const result = await agent({ message: text, provider }).unwrap()
        finish({ content: result.answer, llmMs: result.latencyMs, toolsUsed: result.toolsUsed })
        return
      }

      await stream(
        text,
        provider,
        convId,
        (token) => {
          setMessages((m) => {
            const copy = [...m]
            const last = copy[copy.length - 1]
            copy[copy.length - 1] = { ...last, content: last.content + token }
            return copy
          })
        },
        model,
      )
      finish({})
      // reply শেষ → sidebar list refresh (title/updatedAt)
      dispatch(baseApi.util.invalidateTags(['Conversation']))
    } catch {
      toast.error('উত্তর আনা যায়নি')
      setMessages((m) => {
        const copy = [...m]
        copy[copy.length - 1] = { role: 'assistant', content: 'দুঃখিত, উত্তর আনা যায়নি।' }
        return copy
      })
    }
  }

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      void send()
    }
  }

  return (
    <div className="mx-auto flex h-[calc(100vh-8rem)] max-w-6xl gap-4">
      {/* sidebar — conversation history */}
      <aside className="hidden w-64 shrink-0 flex-col gap-2 sm:flex">
        <Button onClick={newChat} variant="outline" className="justify-start gap-2">
          <Plus className="size-4" /> নতুন চ্যাট
        </Button>
        <div className="flex-1 space-y-1 overflow-y-auto rounded-2xl border bg-card p-2">
          {conversations?.length === 0 && (
            <p className="px-3 py-2 text-xs text-muted-foreground">এখনো কোনো চ্যাট নেই</p>
          )}
          {conversations?.map((c) => (
            <button
              key={c.conversationId}
              onClick={() => openConversation(c.conversationId)}
              className={cn(
                'w-full truncate rounded-lg px-3 py-2 text-left text-sm transition-colors',
                c.conversationId === conversationId ? 'bg-muted font-medium' : 'hover:bg-muted/60',
              )}
              title={c.title}
            >
              {c.title}
            </button>
          ))}
        </div>
      </aside>

      {/* chat column */}
      <div className="flex flex-1 flex-col">
        <div className="mb-3 flex items-center justify-between">
          <h1 className="text-xl font-bold">Chat</h1>
          <div className="flex items-center gap-2">
            {/* Agent mode — LLM tool call করতে পারে (ERP query, knowledge base)।
                stream হয় না, তাই উত্তর একবারে আসে। */}
            <div className="flex items-center gap-2">
              <Switch id="agent-mode" checked={agentMode} onCheckedChange={setAgentMode} disabled={busy} />
              <Label htmlFor="agent-mode" className="cursor-pointer text-sm">Agent</Label>
            </div>

            <Select value={provider} onValueChange={setProvider}>
              <SelectTrigger className="w-36">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {PROVIDERS.map((p) => (
                  <SelectItem key={p} value={p}>
                    {p}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            {/* ollama select করলে installed model-এর ২nd dropdown */}
            {isLocal && localModels && localModels.length > 0 && (
              <Select value={model} onValueChange={setModel}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="model" />
                </SelectTrigger>
                <SelectContent>
                  {localModels.map((mo) => (
                    <SelectItem key={mo} value={mo}>
                      {mo}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>
        </div>

        <div className="flex-1 space-y-4 overflow-y-auto rounded-2xl border bg-card p-4">
          {messages.length === 0 && (
            <p className="mt-10 text-center text-sm text-muted-foreground">
              {agentMode
                ? 'Agent mode — ERP ও knowledge base থেকে tool দিয়ে উত্তর। যেমন: "জুলাই ২০২৬-এ মোট বিক্রি কত?"'
                : 'কিছু জিজ্ঞাসা করুন — উত্তর token-by-token আসবে।'}
            </p>
          )}
          {messages.map((msg, i) => (
            <div key={i} className={cn('flex', msg.role === 'user' ? 'justify-end' : 'justify-start')}>
              <div className="max-w-[80%]">
                <div
                  className={cn(
                    'whitespace-pre-wrap rounded-2xl px-4 py-2 text-sm',
                    msg.role === 'user'
                      ? 'bg-primary text-primary-foreground'
                      : 'bg-muted text-foreground',
                  )}
                >
                  {msg.content || (busy && i === messages.length - 1 ? '▍' : '')}
                </div>

                {/* কোন tool চলেছে — agent সত্যিই DB দেখেছে নাকি নিজে বানিয়েছে, তা যাচাইয়ের একমাত্র উপায় */}
                {msg.toolsUsed && msg.toolsUsed.length > 0 && (
                  <div className="mt-1 flex flex-wrap gap-1">
                    {msg.toolsUsed.map((tool, t) => (
                      <span key={t} className="rounded bg-primary/10 px-1.5 py-0.5 text-xs font-medium text-primary">
                        {tool}
                      </span>
                    ))}
                  </div>
                )}

                {msg.totalMs != null && <Timing totalMs={msg.totalMs} parts={{ LLM: msg.llmMs ?? 0 }} />}
              </div>
            </div>
          ))}
          <div ref={bottomRef} />
        </div>

        <div className="mt-3 flex items-end gap-2">
          <Textarea
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={onKeyDown}
            placeholder="আপনার বার্তা লিখুন... (Enter পাঠাতে, Shift+Enter নতুন লাইন)"
            className="min-h-11 resize-none"
            rows={1}
          />
          <Button onClick={send} disabled={busy || !input.trim()} size="icon" className="size-11 shrink-0">
            {busy ? <Loader2 className="size-4 animate-spin" /> : <SendHorizontal className="size-4" />}
          </Button>
        </div>
      </div>
    </div>
  )
}
