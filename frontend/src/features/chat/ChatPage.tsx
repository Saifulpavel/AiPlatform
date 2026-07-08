import { useEffect, useRef, useState } from 'react'
import { Loader2, SendHorizontal } from 'lucide-react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useChatStream } from './useChatStream'

interface Message {
  role: 'user' | 'assistant'
  content: string
}

const PROVIDERS = ['groq', 'gemini', 'mistral']

export function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const [provider, setProvider] = useState('groq')
  const { stream, isStreaming } = useChatStream()
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const send = async () => {
    const text = input.trim()
    if (!text || isStreaming) return
    setInput('')
    setMessages((m) => [...m, { role: 'user', content: text }, { role: 'assistant', content: '' }])

    try {
      await stream(text, provider, (token) => {
        setMessages((m) => {
          const copy = [...m]
          const last = copy[copy.length - 1]
          copy[copy.length - 1] = { ...last, content: last.content + token }
          return copy
        })
      })
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
    <div className="mx-auto flex h-[calc(100vh-8rem)] max-w-3xl flex-col">
      <div className="mb-3 flex items-center justify-between">
        <h1 className="text-xl font-bold">Chat</h1>
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
      </div>

      <div className="flex-1 space-y-4 overflow-y-auto rounded-2xl border bg-card p-4">
        {messages.length === 0 && (
          <p className="mt-10 text-center text-sm text-muted-foreground">
            কিছু জিজ্ঞাসা করুন — উত্তর token-by-token আসবে।
          </p>
        )}
        {messages.map((msg, i) => (
          <div key={i} className={cn('flex', msg.role === 'user' ? 'justify-end' : 'justify-start')}>
            <div
              className={cn(
                'max-w-[80%] whitespace-pre-wrap rounded-2xl px-4 py-2 text-sm',
                msg.role === 'user'
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-foreground',
              )}
            >
              {msg.content || (isStreaming && i === messages.length - 1 ? '▍' : '')}
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
        <Button onClick={send} disabled={isStreaming || !input.trim()} size="icon" className="size-11 shrink-0">
          {isStreaming ? <Loader2 className="size-4 animate-spin" /> : <SendHorizontal className="size-4" />}
        </Button>
      </div>
    </div>
  )
}
