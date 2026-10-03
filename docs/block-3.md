# Block 3 — Subscription & Payments, Student Dashboard and Instructor Dashboard

Block 3 combines Kognia phases 7, 8 and 9.

## Phase 7 — Subscription & Payments

Implemented:

- Public subscription plan catalog
- Seeded `FREE`, `PREMIUM_MONTHLY` and `PREMIUM_ANNUAL` plans
- Subscription lifecycle with pending, active, cancelled and expired states
- Current billing-period tracking
- Cancel-at-period-end support
- Persistent payment records
- Persistent invoices with unique numbers
- Billing history endpoint
- Provider-neutral provider/reference fields
- Development/Testing sandbox checkout completion for end-to-end validation
- React plan selection, subscription status, payment history and invoice history screen

The sandbox completion endpoint is intentionally unavailable outside `Development` and `Testing`. A production Stripe/PayPal adapter, webhook signature verification, idempotency keys and PCI/commercial hardening remain release work.

## Phase 8 — Student Dashboard

Implemented:

- Active and completed course totals
- Completed lesson count
- Average progress percentage
- Certificate count
- Quiz-attempt and passed-attempt metrics
- Active subscription summary
- Per-course progress cards
- Recent learning activity
- React student dashboard route

## Phase 9 — Instructor Dashboard

Implemented:

- Total, published and draft course counts
- Enrollment totals
- Completed-enrollment totals and completion rate
- Certificates issued for owned courses
- Quiz attempt/pass metrics and pass rate
- Per-course performance table
- Administrator-wide dashboard support
- Instructor/Administrator authorization
- React instructor dashboard route

## Persistence

Block 3 adds:

- `SubscriptionPlan`
- `Subscription`
- `Payment`
- `Invoice`

Migration: `20261003234500_Block3BillingDashboards`.

## API surface

- `GET /api/billing/plans`
- `GET /api/billing/me`
- `POST /api/billing/checkout`
- `POST /api/billing/subscription/cancel`
- `POST /api/billing/sandbox/checkout/{checkoutToken}/complete` — Development/Testing only
- `GET /api/dashboard/student`
- `GET /api/dashboard/instructor`

## Frontend routes

- `/billing`
- `/dashboard/student`
- `/dashboard/instructor`

## Validation

Integration coverage verifies:

- Public seeded plan catalog
- Paid checkout creation
- Sandbox payment completion
- Subscription activation
- Paid payment/invoice persistence
- Student dashboard authentication
- Instructor dashboard role authorization

Block 3 is ready for promotion only when backend restore/build/test and frontend install/lint/build are green in GitHub Actions.
