import { test, expect } from '@playwright/test'

// auth E2E — যা শুধু আসল ব্রাউজারেই প্রমাণ হয়: CORS, httpOnly refresh cookie,
// page-reload-এ silent refresh, react-router redirect। integration test এগুলো ছুঁতে পারে না।

const password = 'Str0ng!Passw0rd'

test('register → login → silent refresh survives reload → logout', async ({ page }) => {
  // প্রতি run-এ ইউনিক email — dev DB শেয়ার্ড, আর register rate-limit (5/min) বাঁচাতে এক টেস্টে পুরো journey
  const email = `e2e-${Date.now()}@test.local`

  // --- register (UI দিয়ে — form validation + API wiring দুটোই টেস্ট হয়) ---
  await page.goto('/register')
  await page.locator('#fullName').fill('E2E User')
  await page.locator('#reg-email').fill(email)
  await page.locator('#reg-password').fill(password)
  await page.getByRole('button', { name: 'অ্যাকাউন্ট তৈরি করুন' }).click()

  // সফল হলে toast + login form-এ switch
  await expect(page.getByText('অ্যাকাউন্ট তৈরি হয়েছে')).toBeVisible()
  await expect(page.locator('#email')).toBeVisible()

  // --- login ---
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.getByRole('button', { name: 'লগইন', exact: true }).click()

  // home-এ পৌঁছে shell sidebar-এ user-এর নাম — মানে access token কাজ করছে (/me সফল)।
  // sidebar (<aside> = complementary role)-scoped: "E2E User" heading-এও আছে ("স্বাগতম, ..."),
  // plain getByText দুটোতে মিলে strict mode violation দেয়।
  await page.waitForURL('/')
  await expect(page.getByRole('complementary').getByText('E2E User')).toBeVisible()

  // --- silent refresh: পুরো E2E-র সবচেয়ে দামি assert ---
  // access token শুধু মেমোরিতে (Redux) — reload-এ হারায়। টিকে থাকার একমাত্র পথ:
  // httpOnly refresh cookie → AppBootstrap-এর /auth/refresh → নতুন token। এই chain-এর
  // যেকোনো অংশ ভাঙলে (cookie attribute, CORS credentials, bootstrap লজিক) ইউজার ছিটকে যাবে।
  await page.reload()
  await expect(page.getByRole('complementary').getByText('E2E User')).toBeVisible()
  await expect(page).toHaveURL('/')

  // --- logout ---
  await page.getByRole('button', { name: /লগআউট|Log out/i }).click()
  await page.waitForURL('/login')
  await expect(page.locator('#email')).toBeVisible()
})

test('protected route redirects anonymous user to /login', async ({ page }) => {
  await page.goto('/chat')
  await page.waitForURL('/login')
  await expect(page.locator('#email')).toBeVisible()
})

test('wrong credentials shows error and stays on login', async ({ page }) => {
  await page.goto('/login')
  await page.locator('#email').fill('nobody@test.local')
  await page.locator('#password').fill('Wr0ng!Passw0rd')
  await page.getByRole('button', { name: 'লগইন', exact: true }).click()

  await expect(page.getByText('ইমেইল বা পাসওয়ার্ড ভুল')).toBeVisible()
  await expect(page).toHaveURL(/\/login/)
})
