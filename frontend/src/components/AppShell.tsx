import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { LogOut, Settings, Sparkles } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useAppDispatch } from '@/app/hooks'
import { logOut } from '@/features/auth/authSlice'
import { useLogoutMutation, useMeQuery } from '@/features/auth/authApi'
import { useT } from '@/i18n/useT'
import { Button } from '@/components/ui/button'
import { ThemeToggle } from './ThemeToggle'

// header nav — HomePage card-এর সাথে title মিল রাখা।
const navItems = [
  { to: '/chat', title: 'Chat' },
  { to: '/rag', title: 'RAG' },
  { to: '/documents', title: 'Documents' },
  { to: '/dashboard', title: 'Dashboard' },
]

// protected app layout — header (brand→home, nav, user, theme, logout) + page outlet।
export function AppShell() {
  const { data: user } = useMeQuery()
  const [logout] = useLogoutMutation()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const t = useT()

  const handleLogout = async () => {
    try {
      await logout().unwrap()
    } catch {
      /* logout best-effort */
    }
    dispatch(logOut())
    toast.success('লগআউট হয়েছে')
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="flex items-center justify-between border-b px-6 py-3">
        <div className="flex items-center gap-6">
          <Link to="/" className="flex items-center gap-2 font-semibold">
            <Sparkles className="size-5 text-primary" /> AiPlatform
          </Link>
          <nav className="hidden items-center gap-1 md:flex">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    'rounded-md px-3 py-1.5 text-sm transition-colors',
                    isActive
                      ? 'bg-accent font-medium text-accent-foreground'
                      : 'text-muted-foreground hover:bg-accent/50 hover:text-foreground',
                  )
                }
              >
                {item.title}
              </NavLink>
            ))}
          </nav>
        </div>
        <div className="flex items-center gap-3">
          {user && <span className="hidden text-sm text-muted-foreground sm:inline">{user.fullName}</span>}
          <ThemeToggle />
          <Button variant="ghost" size="icon" asChild aria-label={t('settings')}>
            <Link to="/settings">
              <Settings className="size-4" />
            </Link>
          </Button>
          <Button variant="ghost" size="sm" onClick={handleLogout} className="gap-2">
            <LogOut className="size-4" /> {t('logout')}
          </Button>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  )
}
