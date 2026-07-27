# Backend-aligned mobile frontend prompt

```text
Build a production-quality cross-platform mobile app (React Native + Expo + TypeScript preferred) called **CampusPay**, a school-scoped digital wallet. Build only two user experiences: **Student** (the ordinary wallet user) and **Merchant**. Do not build admin or school-admin screens.

The app must be a real API-integrated client, not a static prototype. Use React Navigation, TanStack Query, React Hook Form + Zod, a central typed API client, secure token storage (Expo SecureStore), a camera/barcode-scanning flow, and accessible responsive UI. Use a configurable `EXPO_PUBLIC_API_BASE_URL`; API paths below are relative to that base URL and use API version `v1.0`.

## Visual and UX direction

Create a polished, trustworthy Nigerian campus-payment product: deep navy/indigo primary, electric violet/blue accent, white/soft-gray surfaces, green for success, amber for pending, red for failure. Use Naira formatting (`₦`) and strong numeric typography. Keep the app mobile-first, clean, and fast; include skeleton loading, inline field errors, empty states, retry states, success receipts, confirmation sheets, and a privacy option to hide wallet balances. Never show or store a wallet PIN after it is entered.

Start with welcome/onboarding that explains “Pay, transfer, and receive on campus”, then present role selection: “Student” or “Merchant”. Persist the chosen role only for registration UX; determine the actual signed-in role from the protected profile endpoint / login outcome and route appropriately.

## Authentication and API rules (must follow exactly)

- API root pattern: `/api/v1.0/{Controller}/...` (trailing slash is optional in client URLs).
- Protected endpoints require `Authorization: Bearer <token>`. The backend returns `token` from login and also sets cookies, but mobile must use the returned JWT bearer token. Store it in SecureStore and attach it with an interceptor.
- Backend JSON uses ASP.NET Core's default camelCase serialization, e.g. `walletNumber`, `firstname`, `merchantId`, `isApproved`.
- Many command/query endpoints return this envelope: `{ succeeded: boolean, message: string | null, errors: string[] | null, data: T }`. Always handle `succeeded === false` even if HTTP status is 200, showing `message`.
- Direct controller responses such as registration, login, and profiles are not wrapped in that envelope.
- Display server validation and plain-text error responses safely. Do not expose technical stack traces.
- Password validation: minimum 8 characters and must include uppercase, lowercase, number, and non-alphanumeric character. Require a confirmed password client-side.
- Wallet PIN is exactly 4 digits. Use a masked numeric keypad/input; client validation must enforce four digits.
- Currency input must send numeric JSON decimals, never a formatted Naira string. Require amount > 0.

## Data types to implement

`Wallet`: `{ id: string, walletNumber: string, balance: number, isLocked: boolean, userId: string, createdAt: string, lastUpdatedAt: string }`

`WalletSearch`: `{ walletNumber: string, firstName: string, lastName: string, schoolCode: string }`

`Transaction`: `{ id: string, reference: string, amount: number, description?: string, type: 0|1|2, status: 0|1|2|3, senderWalletNumber?: string, senderWalletName?: string, receiverWalletNumber?: string, receiverWalletName?: string, createdAt: string }` where types are `0 Deposit`, `1 Transfer`, `2 Withdrawal`; statuses are `0 Pending`, `1 Successful`, `2 Failed`, `3 Reversed`.

## Public auth flows

### Login for both roles

`POST /api/v1.0/Account/login`

JSON request: `{ "walletNumber": "string", "password": "string" }`

Response: `{ userId, profilePicture, firstname, lastname, schoolCode, email, walletNumber, token }`.

Create a wallet-number + password sign-in screen, forgot-password link, password visibility control, and clear errors for unverified email and merchant approval pending. After login, call role-specific profile endpoints; if `/Student/profile` succeeds, enter student navigation; if `/Merchant/merchant/profile` succeeds, enter merchant navigation. Never infer a role merely from a UI selection.

### Email verification

Registration asks the user to check email. Support a deep-link / in-app verification outcome screen for a URL hitting:

`GET /api/v1.0/Account/verify_email?email={email}&token={token}`

Show a “Verify email, then sign in with the wallet number sent to you” success state. The app does not receive the true wallet number reliably from student registration, so it must not auto-login after student sign-up.

### Password recovery

- `POST /api/v1.0/Account/forgot_password`, JSON `{ "email": "string" }`.
- `POST /api/v1.0/Account/reset_password?email={email}&token={token}`, JSON `{ "password": "new password" }`.

Build request-reset, email-link/deep-link, reset-password, and completion screens. URL-encode email and token.

### Logout, password, account actions

- `POST /api/v1.0/Account/logout` (bearer). Clear local token/session regardless of response.
- `POST /api/v1.0/Account/change_password` (bearer), JSON `{ "currentPassword": "", "newPassword": "" }`.
- `DELETE /api/v1.0/Account/delete_account` (bearer). Present this as a warning: current backend only locks the wallet; it does not truly delete an account.

## Student registration and experience

### Student registration API

`POST /api/v1.0/Student/register`

The DTO fields are: `firstname`, `lastname`, `matricNumber`, `schoolCode`, `email`, `pin`, `password`, and optional `profilePicture`.

Important current-backend limitation: this action is marked `[FromBody]` while its DTO includes `IFormFile ProfilePicture`; therefore it cannot reliably accept a multipart image upload. Submit the currently supported registration fields as JSON **without `profilePicture`**. In the UI, make profile photo an optional “Add later” item and clearly keep the request JSON-only until the backend action is changed to `[FromForm]` (or a dedicated upload endpoint is added).

Create a progressive form:
1. Identity: first name, last name, matric number, school code.
2. Credentials: email, password, confirm password, 4-digit wallet PIN, PIN confirmation.
3. Review and consent.

The response contains `{ userId, profilePicture, firstname, lastname, matricNumber, schoolCode, email, walletNumber, message }`, but its `walletNumber` is currently mapped from `matricNumber` rather than the generated wallet. Treat it as informational only; tell the user their actual wallet number arrives by email after verification.

### Student navigation

Use a bottom tab bar: Home, Activity, Profile. Make Send and Add Money prominent actions on Home.

Home:
- fetch `GET /api/v1.0/Wallet/Wallet` (enveloped `Wallet`)
- show wallet number (copy action), total balance, locked-wallet banner where applicable, balance visibility toggle, quick actions: Add Money, Send Money, Activity.
- load recent transactions using `GET /api/v1.0/Transaction/history` (enveloped `Transaction[]`).

Add money:
- `POST /api/v1.0/Transaction/Deposit`, JSON `{ "amount": 0 }`, Student only.
- Response is an envelope whose data includes `transaction`, `amount`, `paymentUrl`, and `paymentReference`.
- Open `paymentUrl` in an in-app browser. On return, verify with `GET /api/v1.0/Transaction/verify/Deposit/{paymentReference}` and refresh wallet/history. Show Pending/Successful/Failed receipt states; do not mark funds available before successful verification.

Send money:
- First look up recipient: `POST /api/v1.0/Wallet/Search/WalletNumber`, request body is the raw JSON string, e.g. `"WALLET123"`, not `{ walletNumber: "..." }`. The envelope data is `WalletSearch`.
- Present recipient confirmation (name, wallet number, school code), then transfer.
- `POST /api/v1.0/Transaction/Transfer`, JSON `{ "receiverWalletNumber": "string", "amount": 0, "description": "string", "pin": "1234" }`, Student only.
- Block self-transfers in the UI. Confirm amount/recipient before requesting PIN, then show a receipt from enveloped `Transaction` data and refresh caches.

Activity:
- `GET /api/v1.0/Transaction/history`; use cards with direction inferred from the current wallet number, status chip, type label, counterpart name/number, date/time, amount, reference, and detail/receipt screen.
- `GET /api/v1.0/Wallet/Transactions` is an alternate wallet transaction endpoint; prefer `/Transaction/history` and keep the other endpoint available only if data requirements differ.

Student profile:
- `GET /api/v1.0/Student/profile` returns `{ userId, profilePicture, firstname, lastname, matricNumber, schoolCode, schoolName, email, walletNumber }`.
- Show account, school, and security settings; include change password, logout, and delete-account warning.
- `PUT /api/v1.0/Student/update_profile` is currently `[FromBody]` with an `IFormFile` property. Until backend is corrected, send JSON only `{ firstname, lastname }`; do not try multipart profile-picture upload.

## Merchant registration and experience

### Merchant registration API

`POST /api/v1.0/Merchant/register` using `multipart/form-data` (not JSON).

Required form fields: `email`, `password`, `businessName`, `shopLocation`, `schoolCode`, `pin` (exactly four digits), `bankCode`, `accountNumber`. Optional file field: `profilePicture`.

Before the banking step, retrieve available banks with public `GET /api/v1.0/Admin/banks`. Treat its server data as the source of truth and render a searchable bank selector using each bank’s name and code. Do not hardcode bank names/codes. After the user selects a bank and types account number, submitting registration performs server-side account resolution; surface `Invalid bank account details` beside this step if returned.

Merchant sign-up is a stepper:
1. Business: business name, shop location, school code.
2. Settlement account: bank selector, account number, explanatory note that the server validates it.
3. Account security: email, password and confirmation, 4-digit wallet PIN and confirmation, optional profile photo.
4. Review and submit.

Response is `{ merchantId, profilePicture, firstname, lastname, schoolCode, email, walletNumber, businessName, shopLocation, isApproved, accountNumber, accountName, bankName, message }`. After successful signup show both: “verify your email” and “your merchant account needs school approval before you can sign in.” Do not show merchant operational UI until login succeeds.

### Merchant navigation

Use tabs: Home, Charge, Activity, Profile.

Merchant Home:
- fetch `GET /api/v1.0/Wallet/Wallet` for balance and `GET /api/v1.0/Transaction/history` for recent activity.
- include balance visibility, wallet number copy, Pending/locked status, a prominent Charge customer action, a Withdraw action, and a “My QR” card.
- `GET /api/v1.0/Merchant/View/qrcode` returns raw PNG bytes serialized by the API; request it as binary/arraybuffer, convert to a mobile `data:image/png;base64,...` source, and display it.
- `GET /api/v1.0/Merchant/qrcode` downloads a PNG. Offer share/save only after platform permissions and handle the binary response.

Charge customer:
- use Expo Camera barcode scanner. The endpoint expects multipart form data: `POST /api/v1.0/Transaction/scan_to_charge` with `BarCode` as the captured barcode image file, `amount` as form text, and `pin` as form text.
- Build amount entry, camera scanning, customer/payment confirmation, then a PIN confirmation sheet. Do not place the PIN in logs, navigation params, or persisted state.
- Render success/failure receipt from enveloped transaction response and refresh balance/activity. Explain that the scanned QR represents the customer wallet and the customer's wallet is charged.

Withdraw:
- `POST /api/v1.0/Transaction/Withdrawal`, JSON `{ "amount": 0, "pin": "1234" }`, Merchant only.
- This uses the merchant’s registered settlement bank account; no bank fields are sent here. Show a confirmation identifying the masked linked account and account name from the profile.
- Initial status may be Pending. After a response, show status and use `GET /api/v1.0/Transaction/verify/Withdrawal/{reference}` when a reference is available, refreshing wallet/history. Never optimistically subtract money merely from the initial request.

Merchant profile:
- `GET /api/v1.0/Merchant/merchant/profile` returns `{ merchantId, profilePicture, firstname, lastname, schoolCode, schoolName, email, walletNumber, businessName, shopLocation, isApproved, accountNumber, accountName, bankName }`.
- Show business and settlement account information with account number masked by default, QR access, password/security/logout/delete-account controls.
- Do not currently expose editable merchant banking/photo forms as fully functional. `PUT /api/v1.0/Merchant/update_merchant_profile` uses `[FromBody]` but includes `IFormFile`, and changing bank/account does not currently regenerate the transfer recipient/bank name. If included, provide an edit-names/location-only JSON form `{ firstname, lastname, shopLocation, bankCode, accountNumber }` with a clear integration warning; hide image upload until the server is repaired.

## Implementation requirements

- Build a typed API module for every endpoint above, with form-data and raw JSON-string body support where required.
- Centralize token injection, 401 handling (clear session and return to login), HTTP/error-envelope normalization, Naira/date formatters, and transaction status/type mapping.
- Query keys should include `wallet`, `transactionHistory`, `studentProfile`, `merchantProfile`, and `banks`; invalidate wallet/history/profile after successful mutations.
- Add an API configuration screen for development only, not hardcoded localhost. Support emulator/device host URLs through environment config.
- Do not invent APIs for school listing, token refresh, bank-account verification, notifications, cash-out bank changes, or generic image upload. The backend currently has no public Student/Merchant school-list endpoint, no refresh endpoint, and no dedicated upload endpoint.
- Include mocked API fixtures for every defined response so the UI can run before an API URL is configured, but make the real API adapter the default implementation.
- Generate complete source code, setup instructions, `.env.example`, TypeScript interfaces, and a short `BACKEND_INTEGRATION.md` documenting the three backend upload/binding limitations and the student registration wallet-number mapping issue.

## Product architecture and navigation detail

Use a clear app state machine. While booting, restore the token from SecureStore, then obtain the wallet and attempt the appropriate profile request. The boot screen must not briefly reveal an authenticated dashboard before token validation has completed. If there is no token, use the public stack: Welcome, role choice, Sign in, Student registration, Merchant registration, Forgot password, Reset password, and Email verification outcome. If a valid student session is found, use the Student stack and tabs. If a valid merchant session is found, use the Merchant stack and tabs. If a token is expired, invalid, revoked, or results in 401/403, clear it, clear cached private data, and route to Sign in with a calm message such as “Your session has ended. Please sign in again.”

Keep every destructive or money-moving action outside the tab navigator in modal routes. This includes transfer confirmation, enter-PIN, payment receipt, withdrawal confirmation, camera scanner, QR preview, logout confirmation, and account lock/delete warning. Use native-feeling bottom sheets for short confirmations and full-screen routes for long forms or scanner experiences. Every screen needs a safe back action that preserves form values where appropriate, except for PIN fields; clear PIN values on navigation away, app backgrounding, request completion, and errors.

Use route guards at two levels. First, avoid rendering role-inappropriate screens based on the stored session role. Second, handle an unexpected 403 from the API by returning to the correct role entry or presenting a permission message. Merchant login can fail before a token is usable because merchant approval is checked by the backend. Its sign-in error state should distinguish “pending school approval” from invalid credentials and email not verified. Do not offer any client-side workaround for those backend rules.

## Screen specification and reusable UI

Create a consistent design system. Include `Screen`, `SafeArea`, `AppHeader`, `PrimaryButton`, `SecondaryButton`, `DestructiveButton`, `TextField`, `PasswordField`, `PinField`, `MoneyField`, `Avatar`, `BalanceCard`, `WalletNumberCard`, `TransactionRow`, `StatusPill`, `EmptyState`, `ErrorState`, `LoadingSkeleton`, `BottomSheet`, `ConfirmationSheet`, `ReceiptSheet`, `CopyButton`, and `MaskedAccountNumber` components. Components should have semantic labels, 44-pixel minimum touch targets, readable contrast, support dynamic type, and avoid color as the only way to communicate status.

The launch/welcome screen should use a small wallet/campus illustration, a concise benefit statement, and actions for Create account and Sign in. The role selector should explain the distinction in ordinary language: a Student adds money and sends funds; a Merchant accepts campus payments and withdraws to a settlement bank account. The role selector must not imply that an ordinary user can later operate merchant payment acceptance without merchant registration and approval.

Sign in should identify the input precisely as “Wallet number”, rather than email or phone number. Use a keyboard that suits the wallet-number format but do not make an unverified assumption that it is numeric only. Include a secure password field, “Forgot password?”, a loading state that disables repeated submissions, and links to role-specific registration. Do not attempt Google authentication: routes exist for browser Google auth but that flow does not return the documented mobile login payload and has no role-aware mobile contract.

The dashboard balance card should show current balance, wallet number, whether the wallet is locked, and last-updated context when available. Long press or tap copy should copy wallet number and show a toast. For security, balance hiding should be device-local only. A locked wallet banner must prevent money actions and explain that the user should contact platform support; do not claim that the mobile app can unlock it. Show recent activity below the balance with a “See all” action, no more than five rows, and an excellent empty state for a new account.

The activity list should use local filtering and sorting only after fetching the existing history endpoint. Provide filters for All, Deposits, Transfers, Withdrawals, and status; use the enum mapping specified above. A transfer must visually be “Sent” when the current wallet number is `senderWalletNumber`, “Received” when it is `receiverWalletNumber`, and neutral where it cannot be inferred. Deposits should show “Added money”; withdrawals should show “Withdrawal to bank”. Use a transaction detail screen to show reference, date/time, amount, status, description, sender, receiver, and a copy-reference action. Never promise downloadable statements or transaction disputes because the backend has no endpoint for them.

## Form behaviour and validation rules

Use React Hook Form forms backed by Zod schemas that mirror the API DTOs. Validate locally first but always show backend validation errors as authoritative. Trim ordinary text carefully, preserve passwords and PINs as typed, and send exact camelCase field names. Avoid automatic uppercasing or mutation of wallet numbers, matric numbers, school codes, account numbers, and bank codes unless the user deliberately enters those formats. Do not send undefined file fields in form data.

Student sign-up should validate: names required, matric number required, school code required, valid email, a password meeting all backend Identity rules, matching password confirmation, numeric four-digit PIN, and matching PIN confirmation. Add contextual help that a school code must be supplied by the school because the API offers no public list of schools. Let the user edit every previous step before submission. On a failed submission, retain non-sensitive identity values but reset password and PIN confirmation fields after an error that may be observable by another person on a shared device. On success, display the email verification message, email address, and next steps; do not pretend a profile image was uploaded or that the displayed response `walletNumber` is verified.

Merchant sign-up should show the bank name in the selector and retain its backend `code` value as `bankCode`. Handle bank fetching states: a skeleton selector while loading, a retry button when it fails, and a disabled Continue button until a valid bank selection and account number exist. The backend resolves the bank account only on registration, so label it “Account details will be verified when you submit” rather than presenting an immediate local confirmation. If the service returns its account name in the registration response, show it on the success page. Treat the profile picture as an optional file selected from photo library or camera, resize/compress responsibly before upload, and send it under the exact `profilePicture` key in `FormData`.

For all money entry screens, use an amount input that accepts plain digits and decimal separators but renders formatted Naira values in a non-submitted display. Do not permit zero, negative values, blank values, invalid decimals, or amounts beyond JavaScript-safe currency representation. Store monetary values in a decimal-safe string at the UI boundary, convert only to a JSON number immediately before API submission, and visually restate the exact amount on confirmation. The backend does not expose transaction limits or fees, so do not invent fees, daily limits, or exchange rates.

The transfer flow has four deliberate stages: recipient wallet number, recipient verification, amount and optional description, then confirmation/PIN. Recipient verification must call the search endpoint before the user can proceed, should debounce only the user’s typing—not the final verification request—and must invalidate a previous recipient confirmation when the wallet number changes. Send the request body exactly as a JSON string. The confirmation should include recipient first/last name, wallet number, school code, amount, and optional description. After a failed transfer, do not assume no state changed; refresh wallet and history if the request reached the server, then show the returned result.

## Network layer, errors, and reliability

Implement one API client that accepts an API base URL without a trailing slash and creates every specified endpoint URL predictably. Set `Content-Type: application/json` only for JSON requests. Do not set it manually for `FormData`; the platform must supply its multipart boundary. Configure a reasonable timeout with an explicit retry policy: GET requests may retry transient network failures once or twice; money mutations, registration, password changes, and logout should not automatically retry because a duplicate request can have user-visible consequences. If a mutation times out after dispatch, display “We could not confirm the result. Refresh activity before trying again” and refresh the wallet/history rather than blindly resubmitting.

Normalize API errors into a typed `ApiError` containing HTTP status, user-facing message, envelope errors when available, request identifier when exposed, and whether the request may have reached the server. Plain strings returned by the controllers should render as text. ASP.NET validation can return an object with an `errors` field keyed by property; map those errors onto the corresponding form controls using case-insensitive matching. Network-unavailable errors should present an offline message and a retry action. Do not cache passwords, PINs, raw FormData, bearer headers, or error bodies that may contain sensitive server information.

Use TanStack Query carefully. Wallet and profile data can have short stale times, while transaction history should be refetched when its screen becomes focused and after every successful or indeterminate financial mutation. Disable automatic refetches that would disrupt an active PIN confirmation or form. On authentication changes, remove all private query cache data. On logout or account deletion request, clear the token and private cache immediately after the action completes; if logout itself fails due to connectivity, still offer a local sign-out because the user explicitly requested it, while explaining that the server session may remain active.

Because the backend CORS policy is configured from `FrontendUrl`, include a prominent integration note for developers: ensure the deployed mobile-web/API origin is permitted where applicable. Native mobile requests are generally not browser-CORS limited, but Expo web is. Do not weaken API security in the mobile app. Store only the JWT in SecureStore; user display details may be cached only as non-authoritative convenience data and must be refreshed from protected endpoints.

## QR, camera, and binary content

The merchant charging workflow is camera-sensitive. Before opening the camera, explain why camera access is required: “Scan a customer CampusPay QR to charge their wallet.” Request permission only in response to the user initiating scan, provide a clear denial state with device settings guidance, and do not use camera access for any other purpose. The documented backend does not accept a decoded barcode string; it accepts the `BarCode` image file. Capture a suitable image of the visible QR and append it to multipart data with a filename and image MIME type. Also append `amount` and `pin` as strings. Do not call the endpoint until scanner capture, amount, and confirmation are all complete.

Show the scanned customer details only when they are actually available from the API response; the scan endpoint does not expose a preview/resolve endpoint. Therefore, the confirmation before submitting can say “Charge the scanned CampusPay QR” and show amount, but it cannot reliably show the customer name. Explain this constraint elegantly. After submission, use the returned transaction’s sender/receiver fields for the receipt. Add a manual retry/scan-again option after unreadable codes, but do not offer manual wallet-number charging as an API equivalent because only student transfer and merchant image scan routes are supplied.

For merchant QR display, keep the bytes in memory or short-lived cache, not long-term application storage. Ensure base64 conversion handles API payload formats that may be an array of byte values or binary depending on the fetch library. Create an image source adapter and test it on Android and iOS. The Download QR route returns a filename and `image/png`; use platform share/save methods only after showing an explanation and requesting relevant photo-library permissions. Do not generate a substitute QR on the client: the server-issued QR must be the source of truth.

## Receipts, status, and financial integrity

Every successful financial action should land on a receipt that can be revisited from Activity. Receipt content should contain a clear success/pending/failure heading, transaction type, Naira amount, date/time, transaction reference, source and destination where relevant, and a Done action. Pending withdrawal is not the same as a completed payout. Use amber and factual wording: “Your withdrawal request is pending.” Verification endpoints may be used where a valid reference is available, but the ultimate activity history and wallet balance are the canonical display after refresh.

Deposit is a hosted payment handoff. Create the transaction through the API, then use the response `paymentUrl` in an in-app browser with a visible return-to-app route. Avoid embedding Paystack or collecting card details in your own UI. On the user’s return, call the verification endpoint with the returned `paymentReference`; provide a “Check payment status” button if browser return behavior varies across devices. Do not show the balance as increased before server verification. If the user cancels the browser, show a neutral pending/cancelled experience and let them retry status verification from the receipt/history.

For withdrawal, the API’s initial response data can contain a transaction with an empty or unavailable reference, so write defensive UI that only renders “Check status” when a non-empty reference exists. Never construct a verification URL with an empty reference. Mask account numbers by default, allowing a momentary reveal after deliberate user interaction. Account name and bank name come from merchant profile; if absent, show placeholders without claiming data was verified.

## Testing and delivery acceptance criteria

Provide unit tests for endpoint construction, JSON payload serialization, multipart key names, `AppResponse` parsing, auth header injection, 401 cleanup, amount/PIN/password validators, transaction enum labels, and wallet-recipient request body formatting. Add component tests for sign-in, registration step validation, transfer recipient confirmation, error states, and balance hiding. Add integration-style mocked tests for student deposit handoff, transfer success/failure, merchant registration with FormData, withdrawal pending, camera permission denied, and session expiry. Include a manual QA checklist for Android and iOS that tests keyboard avoidance, safe-area layout, offline states, copy wallet number, deep link parsing, image/QR handling, and API environment configuration.

The generated repository should be organized clearly: `src/api`, `src/auth`, `src/components`, `src/features/student`, `src/features/merchant`, `src/navigation`, `src/screens/public`, `src/storage`, `src/theme`, `src/types`, `src/utils`, and `src/test`. Use strict TypeScript and no implicit `any`. Keep fake data isolated in a mock adapter selected only by development configuration. Include README setup commands, a sample `.env.example` with `EXPO_PUBLIC_API_BASE_URL`, and exact instructions for replacing it with a reachable HTTPS backend address on a physical device.

Final acceptance: a new student can register using the backend-supported JSON contract, verify email externally, sign in by wallet number, see a wallet, deposit through a hosted URL, find a recipient by wallet number, transfer with a four-digit PIN, see activity, and manage basic profile/security. A merchant can register through multipart form data with a bank list and optional photo, wait for external approval, sign in, see wallet/activity/profile and QR, scan a customer QR image to charge with merchant PIN, and request a withdrawal to the pre-registered settlement account. All other unavailable capabilities must be visibly absent or marked unavailable—not faked.

## Copywriting and privacy requirements

Write all customer-facing copy in clear, calm English appropriate for a Nigerian campus audience. Say “wallet number”, “school code”, “add money”, and “withdraw to your bank account”; avoid unexplained financial jargon. Never use placeholder claims such as “instant”, “guaranteed”, “free”, “insured”, “zero fees”, or “bank-grade” because those claims are not supported by this backend. Format dates in a locally comprehensible format and use `₦` with comma grouping and two decimal places where a fractional value is returned. Screen readers must announce monetary values, transaction state, and form errors clearly. Avoid printing full account numbers, passwords, PINs, tokens, email verification tokens, or raw QR values in screenshots, analytics, console logs, crash reports, or error toasts. Add optional analytics interfaces only; do not send events until a privacy-approved provider and consent policy are supplied. Preserve a clean separation between user-facing messages and developer diagnostics so a production build can safely omit debug information.
```

## Backend limitations accounted for in the prompt

- Student registration and both profile update DTOs include `IFormFile` but are bound with `[FromBody]`; images are not safely uploadable with the routes as implemented.
- The student registration response maps `WalletNumber` to `MatricNumber`, though the actual wallet is generated separately and emailed.
- Merchant profile updates can change bank fields but do not update the transfer recipient/bank name; the app should avoid presenting that as a completed settlement-account-change workflow.
- No public endpoint lists schools, so school code needs manual entry in registration.
